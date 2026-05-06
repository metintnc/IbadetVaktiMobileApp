using hadis.Models;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace hadis.Services
{
    /// <summary>
    /// Namaz vakitlerini Azure API'den çekip önbelekleyen servis.
    /// Sadece Azure API kullanır - Aladhan fallback kaldırıldı.
    /// </summary>
    public class PrayerTimesService
    {
        private readonly NamazVaktiApiService _namazVaktiApiService;
        private readonly string _cacheDir;
        private static readonly HttpClient _calendarHttpClient = new();
        private const int CACHE_TTL_DAYS = 45; // 45 gün sonra cache expire olur

        public PrayerTimesService(NamazVaktiApiService namazVaktiApiService)
        {
            _namazVaktiApiService = namazVaktiApiService;
            _cacheDir = Path.Combine(FileSystem.AppDataDirectory, "prayer_cache");
            EnsureCacheDirectory();
        }

        private void EnsureCacheDirectory()
        {
            try
            {
                if (!Directory.Exists(_cacheDir))
                    Directory.CreateDirectory(_cacheDir);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cache dizini oluşturma hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Konum değiştiğinde tüm cache'i siler
        /// </summary>
        public void ClearCache()
        {
            try
            {
                if (Directory.Exists(_cacheDir))
                {
                    foreach (var file in Directory.GetFiles(_cacheDir, "*.json"))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cache silme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Belirli bir tarih için namaz vakitlerini döndürür.
        /// Stratejisi:
        /// 1. Local Cache
        /// 2. Azure API
        /// 3. Offline Cache (fallback)
        /// </summary>
        public async Task<Dictionary<string, DateTime>?> GetPrayerTimesForDateAsync(
            DateTime date, string ilce, string sehir, string ulke = "Türkiye", double? lat = null, double? lon = null)
        {
            // 1. Bu ayı cache'den kontrol et
            var cachedData = await LoadMonthCacheAsync(sehir, ilce, date);
            if (cachedData != null)
            {
                System.Diagnostics.Debug.WriteLine($"✅ Cache hit: {date:yyyy-MM-dd}");
                return cachedData;
            }

            // 2. Koordinatlar mevcutsa aylık takvim kaynağından çek
            if (HasValidCoordinates(lat, lon))
            {
                var calendarMonth = await GetPrayerTimesMonthFromCalendarAsync(date, lat!.Value, lon!.Value);
                if (calendarMonth != null && calendarMonth.Count > 0)
                {
                    DailyNamazVakitleri? requestedDay = null;

                    foreach (var item in calendarMonth)
                    {
                        await SaveDateCacheAsync(sehir, ilce, item.Date, item.Vakitler);

                        if (item.Date.Date == date.Date)
                            requestedDay = item.Vakitler;
                    }

                    if (requestedDay != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Takvim kaynağından çekildi: {date:yyyy-MM-dd}");
                        return ConvertToDateTimeDictionary(requestedDay, date);
                    }
                }
            }

            // 3. Azure API'den ilçe ID'sini bul ve veri çek
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 İlçe ID araniyor: {sehir}/{ilce} ({ulke})");

                var ilceId = await _namazVaktiApiService.GetIlceIdBySehir(sehir, ilce, ulke);
                
                if (ilceId.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ İlçe ID bulundu: {ilceId} ({sehir}/{ilce})");
                    
                    DailyNamazVakitleri vakitler;
                    
                    if (date.Date == DateTime.Now.Date)
                    {
                        System.Diagnostics.Debug.WriteLine($"📞 GetBugunVakitleri çağrılıyor (ID: {ilceId})");
                        vakitler = await _namazVaktiApiService.GetBugunVakitleri(ilceId.Value);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"📞 GetTarihVakitleri çağrılıyor (ID: {ilceId}, Tarih: {date:yyyy-MM-dd})");
                        vakitler = await _namazVaktiApiService.GetTarihVakitleri(ilceId.Value, date);
                    }

                    if (vakitler != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Vakitler alındı: İmsak={vakitler.Imsak}, Yatsı={vakitler.Yatsi}");
                        
                        var result = ConvertToDateTimeDictionary(vakitler, date);
                        
                        // Cache'e kaydet
                        await SaveDateCacheAsync(sehir, ilce, date, vakitler);
                        
                        System.Diagnostics.Debug.WriteLine($"✅ Azure API'den çekildi: {date:yyyy-MM-dd}");
                        return result;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Azure API veri döndürmedi (null): {date:yyyy-MM-dd}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"❌ İlçe ID bulunamadı: {sehir}/{ilce}");
                    System.Diagnostics.Debug.WriteLine($"   Tüm ilçeleri listelemek için debug'a bakın");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Azure API hatası: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"   Inner: {ex.InnerException.Message}");
            }

            // 4. Offline fallback - Tüm cache dosyalarından ara
            var offlineResult = await TryOfflineFallbackAsync(sehir, ilce, date);
            if (offlineResult != null)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Offline fallback: {date:yyyy-MM-dd}");
                return offlineResult;
            }

            System.Diagnostics.Debug.WriteLine($"❌ Veri bulunamadı: {date:yyyy-MM-dd}");
            return null;
        }

        /// <summary>
        /// Sonraki günlerin vakitlerini arka planda önceden yükler.
        /// Varsayılan olarak 15 gün cache'lenir.
        /// </summary>
        public async Task PrefetchNextDaysAsync(string ilce, string sehir, double? lat = null, double? lon = null, int dayCount = 15)
        {
            try
            {
                if (dayCount <= 0)
                    return;

                var startDate = DateTime.Now.Date.AddDays(1);
                var manuelUlke = Preferences.Default.Get("ManuelUlke", "Türkiye");

                for (int offset = 0; offset < dayCount; offset++)
                {
                    var targetDate = startDate.AddDays(offset);

                    var vakitler = await GetPrayerTimesForDateAsync(targetDate, ilce, sehir, manuelUlke, lat, lon);
                    if (vakitler != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Prefetch tamamlandı: {targetDate:yyyy-MM-dd}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Prefetch veri döndürmedi: {targetDate:yyyy-MM-dd}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Prefetch hatası: {ex.Message}");
            }
        }

        // ================================================================
        // Cache I/O
        // ================================================================

        private string GetCacheFilePath(string sehir, string ilce, DateTime date)
        {
            var s = sehir?.Replace(" ", "").ToLowerInvariant() ?? "";
            var i = ilce?.Replace(" ", "").ToLowerInvariant() ?? "";
            return Path.Combine(_cacheDir, $"prayer_{s}_{i}_{date:yyyy_MM_dd}.json");
        }

        /// <summary>
        /// Ayın tamamı için cache'ten veri yüklemeye çalışır
        /// İçinde aradığımız tarihe ait veri varsa onu döndürür
        /// </summary>
        private async Task<Dictionary<string, DateTime>?> LoadMonthCacheAsync(string sehir, string ilce, DateTime date)
        {
            try
            {
                string filePath = GetCacheFilePath(sehir, ilce, date);
                if (!File.Exists(filePath)) 
                    return null;

                // TTL kontrolü
                var fileInfo = new FileInfo(filePath);
                if ((DateTime.Now - fileInfo.LastWriteTime).TotalDays > CACHE_TTL_DAYS)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Cache expired: {filePath}");
                    File.Delete(filePath);
                    return null;
                }

                string json = await File.ReadAllTextAsync(filePath);
                var cachedVakitler = JsonSerializer.Deserialize<DailyNamazVakitleri>(json);
                
                if (cachedVakitler != null && cachedVakitler.Tarih == date.ToString("yyyy-MM-dd"))
                {
                    return ConvertToDateTimeDictionary(cachedVakitler, date);
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cache yükleme hatası: {ex.Message}");
                return null;
            }
        }

        private async Task SaveDateCacheAsync(string sehir, string ilce, DateTime date, DailyNamazVakitleri vakitler)
        {
            try
            {
                string filePath = GetCacheFilePath(sehir, ilce, date);
                string json = JsonSerializer.Serialize(vakitler);
                await File.WriteAllTextAsync(filePath, json);

                // Eski cache dosyalarını temizle
                CleanExpiredCacheFiles();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cache yazma hatası: {ex.Message}");
            }
        }

        private void CleanExpiredCacheFiles()
        {
            try
            {
                // Günde 1 kez temizle
                var lastClean = Preferences.Default.Get("LastCacheCleanDate", DateTime.MinValue);
                if ((DateTime.Now - lastClean).TotalHours < 24)
                    return;

                if (!Directory.Exists(_cacheDir)) 
                    return;

                foreach (var file in Directory.GetFiles(_cacheDir, "*.json"))
                {
                    var fileInfo = new FileInfo(file);
                    if ((DateTime.Now - fileInfo.LastWriteTime).TotalDays > CACHE_TTL_DAYS)
                    {
                        File.Delete(file);
                        System.Diagnostics.Debug.WriteLine($"🧹 Eski cache silindi: {Path.GetFileName(file)}");
                    }
                }

                Preferences.Default.Set("LastCacheCleanDate", DateTime.Now);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cache temizleme hatası: {ex.Message}");
            }
        }

        private static bool HasValidCoordinates(double? lat, double? lon)
        {
            return lat.HasValue && lon.HasValue &&
                   Math.Abs(lat.Value) > 0.0001 && Math.Abs(lon.Value) > 0.0001;
        }

        private async Task<List<(DateTime Date, DailyNamazVakitleri Vakitler)>?> GetPrayerTimesMonthFromCalendarAsync(DateTime date, double latitude, double longitude)
        {
            try
            {
                var url = string.Format(
                    CultureInfo.InvariantCulture,
                    "https://api.aladhan.com/v1/calendar/{0}/{1}?latitude={2}&longitude={3}&method=13",
                    date.Year,
                    date.Month,
                    latitude,
                    longitude);

                var response = await _calendarHttpClient.GetFromJsonAsync<CalendarResponse>(url);
                var monthData = response?.Data;
                if (monthData == null || monthData.Count == 0)
                    return null;

                var result = new List<(DateTime Date, DailyNamazVakitleri Vakitler)>();

                for (int index = 0; index < monthData.Count; index++)
                {
                    var day = monthData[index];
                    if (day?.Timings == null)
                        continue;

                    var dayDate = ResolveCalendarDate(day, date.Year, date.Month, index);
                    if (dayDate == null)
                        continue;

                    result.Add((dayDate.Value, new DailyNamazVakitleri
                    {
                        Imsak = NormalizeCalendarTime(day.Timings.Fajr),
                        Gunes = NormalizeCalendarTime(day.Timings.Sunrise),
                        Ogle = NormalizeCalendarTime(day.Timings.Dhuhr),
                        Ikindi = NormalizeCalendarTime(day.Timings.Asr),
                        Aksam = NormalizeCalendarTime(day.Timings.Maghrib),
                        Yatsi = NormalizeCalendarTime(day.Timings.Isha),
                        GregorianDateShort = ParseGregorianShortDate(day.Date?.Gregorian?.Date) ?? dayDate.Value.ToString("dd.MM.yyyy"),
                        GregorianDateLong = day.Date?.Readable ?? dayDate.Value.ToString("dd.MM.yyyy"),
                        GregorianDateIso = ParseGregorianIsoDate(day.Date?.Gregorian?.Date) ?? dayDate.Value.ToString("yyyy-MM-dd"),
                        HijriDateShort = string.Empty,
                        HijriDateLong = string.Empty,
                        QiblaTime = string.Empty,
                        ShapeMoonUrl = string.Empty,
                        AstronomicalSunset = string.Empty,
                        AstronomicalSunrise = string.Empty
                    }));
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Calendar API hatası: {ex.Message}");
                return null;
            }
        }

        private static DateTime? ResolveCalendarDate(CalendarData day, int year, int month, int index)
        {
            var rawDate = day?.Date?.Gregorian?.Date;
            if (!string.IsNullOrWhiteSpace(rawDate) && DateTime.TryParse(rawDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                return parsedDate.Date;

            try
            {
                return new DateTime(year, month, index + 1);
            }
            catch
            {
                return null;
            }
        }

        private static string NormalizeCalendarTime(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var match = Regex.Match(value, @"\d{1,2}:\d{2}");
            return match.Success ? match.Value : value.Trim();
        }

        private static string? ParseGregorianIsoDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return parsed.ToString("yyyy-MM-dd");

            return null;
        }

        private static string? ParseGregorianShortDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return parsed.ToString("dd.MM.yyyy");

            return null;
        }

        // ================================================================
        // Offline Fallback
        // ================================================================

        private async Task<Dictionary<string, DateTime>?> TryOfflineFallbackAsync(string sehir, string ilce, DateTime date)
        {
            try
            {
                if (!Directory.Exists(_cacheDir)) 
                    return null;

                var s = sehir?.Replace(" ", "").ToLowerInvariant() ?? "";
                var i = ilce?.Replace(" ", "").ToLowerInvariant() ?? "";
                string pattern = $"prayer_{s}_{i}_*.json";

                // Tüm cache dosyalarında aradığımız tarihi bul
                foreach (var file in Directory.GetFiles(_cacheDir, pattern))
                {
                    try
                    {
                        string json = await File.ReadAllTextAsync(file);
                        var vakitler = JsonSerializer.Deserialize<DailyNamazVakitleri>(json);
                        
                        if (vakitler != null && vakitler.Tarih == date.ToString("yyyy-MM-dd"))
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ Offline cache'te tarih bulundu: {file}");
                            return ConvertToDateTimeDictionary(vakitler, date);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Offline fallback hatası: {ex.Message}");
            }

            return null;
        }

        // ================================================================
        // Helpers
        // ================================================================

        private static Dictionary<string, DateTime> ConvertToDateTimeDictionary(DailyNamazVakitleri vakitler, DateTime date)
        {
            DateTime ParseTime(string timeStr)
            {
                if (string.IsNullOrEmpty(timeStr)) 
                    return DateTime.MinValue;
                
                try
                {
                    return DateTime.Parse($"{date:yyyy-MM-dd} {timeStr}");
                }
                catch
                {
                    return DateTime.MinValue;
                }
            }

            return new Dictionary<string, DateTime>
            {
                { "Imsak", ParseTime(vakitler.Imsak) },
                { "gunes", ParseTime(vakitler.Gunes) },
                { "Ogle", ParseTime(vakitler.Ogle) },
                { "Ikindi", ParseTime(vakitler.Ikindi) },
                { "Aksam", ParseTime(vakitler.Aksam) },
                { "Yatsi", ParseTime(vakitler.Yatsi) }
            };
        }
    }
}
using hadis.Models;
using System.Text.Json;
using System.Net.Http;


namespace hadis.Services
{
    /// <summary>
    /// Namaz vakitlerini API'den çekip önbellekleyen servis.
    /// Offline-first: Her zaman önce cache'e bakar, cache miss olursa API'ye gider.
    /// Multi-month cache, TTL, ve konum bazlı invalidation destekler.
    /// </summary>
    public class PrayerTimesService
    {
        private readonly HttpClient _httpClient;
        private readonly string _cacheDir;
        private const int CACHE_TTL_DAYS = 45; // 45 gün sonra cache expire olur

        public PrayerTimesService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
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

                // Eski v2 cache dosyasını da temizle (migration)
                var legacyFile = Path.Combine(FileSystem.AppDataDirectory, "prayer_times_cache_v2.json");
                if (File.Exists(legacyFile))
                    File.Delete(legacyFile);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cache silme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Belirli bir tarih için namaz vakitlerini döndürür.
        /// Önce cache'e bakar, yoksa API'ye gider, API başarısız olursa en yakın cache'i dener.
        /// </summary>
        public async Task<Dictionary<string, DateTime>?> GetPrayerTimesForDateAsync(
            DateTime date, string ilce, string sehir, double? lat = null, double? lon = null)
        {
            // 1. Bu aya ait cache'e bak
            var cachedData = await LoadMonthCacheAsync(date);
            if (cachedData != null)
            {
                var todayData = FindDataForDate(cachedData, date);
                if (todayData != null)
                {
                    System.Diagnostics.Debug.WriteLine($"📦 Cache hit: {date:yyyy-MM-dd}");
                    return ConvertToDictionary(todayData, date);
                }
            }

            // 2. Cache'te yok → API'den çek
            try
            {
                var freshData = await FetchMonthlyDataAsync(date, ilce, sehir, lat, lon);
                if (freshData != null && freshData.Count > 0)
                {
                    await SaveMonthCacheAsync(date, freshData);

                    var todayData = FindDataForDate(freshData, date);
                    if (todayData != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"🌐 API'den çekildi: {date:yyyy-MM-dd}");
                        return ConvertToDictionary(todayData, date);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"API Hatası: {ex.Message}");
            }

            // 3. API başarısız → Tüm cache dosyalarında ara (offline fallback)
            var fallbackResult = await TryOfflineFallbackAsync(date);
            if (fallbackResult != null)
            {
                System.Diagnostics.Debug.WriteLine($"📴 Offline fallback: {date:yyyy-MM-dd}");
                return fallbackResult;
            }

            // 4. Eski v2 cache'ten migration denemesi
            var legacyResult = await TryLegacyCacheAsync(date);
            if (legacyResult != null)
            {
                System.Diagnostics.Debug.WriteLine($"📜 Legacy cache fallback: {date:yyyy-MM-dd}");
                return legacyResult;
            }

            return null;
        }

        /// <summary>
        /// Yarınki vakitleri arka planda prefetch eder (widget, bildirim için)
        /// </summary>
        public async Task PrefetchNextDaysAsync(string ilce, string sehir, double? lat = null, double? lon = null)
        {
            try
            {
                var tomorrow = DateTime.Now.AddDays(1);
                var cachedData = await LoadMonthCacheAsync(tomorrow);
                if (cachedData == null || FindDataForDate(cachedData, tomorrow) == null)
                {
                    // Yarının ayı cache'te yok, çek
                    var freshData = await FetchMonthlyDataAsync(tomorrow, ilce, sehir, lat, lon);
                    if (freshData != null && freshData.Count > 0)
                    {
                        await SaveMonthCacheAsync(tomorrow, freshData);
                        System.Diagnostics.Debug.WriteLine($"⏩ Prefetch tamamlandı: {tomorrow:yyyy-MM}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Prefetch hatası: {ex.Message}");
            }
        }

        // ================================================================
        // Cache I/O — Ay bazlı dosyalama
        // ================================================================

        private string GetCacheFilePath(DateTime date)
        {
            return Path.Combine(_cacheDir, $"prayer_{date:yyyy_MM}.json");
        }

        private async Task<List<CalendarData>?> LoadMonthCacheAsync(DateTime date)
        {
            try
            {
                string filePath = GetCacheFilePath(date);
                if (!File.Exists(filePath)) return null;

                // TTL kontrolü
                var fileInfo = new FileInfo(filePath);
                if ((DateTime.Now - fileInfo.LastWriteTime).TotalDays > CACHE_TTL_DAYS)
                {
                    System.Diagnostics.Debug.WriteLine($"⏰ Cache expired: {filePath}");
                    File.Delete(filePath);
                    return null;
                }

                string json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<List<CalendarData>>(json);
            }
            catch
            {
                return null;
            }
        }

        private async Task SaveMonthCacheAsync(DateTime date, List<CalendarData> data)
        {
            try
            {
                string filePath = GetCacheFilePath(date);
                string json = JsonSerializer.Serialize(data);
                await File.WriteAllTextAsync(filePath, json);

                // Eski cache dosyalarını temizle (3 aydan eski)
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
                // Check if we cleaned recently (limit to once per 24 hours)
                var lastClean = Preferences.Default.Get("LastCacheCleanDate", DateTime.MinValue);
                if ((DateTime.Now - lastClean).TotalHours < 24)
                    return;

                if (!Directory.Exists(_cacheDir)) return;

                foreach (var file in Directory.GetFiles(_cacheDir, "*.json"))
                {
                    var fileInfo = new FileInfo(file);
                    if ((DateTime.Now - fileInfo.LastWriteTime).TotalDays > CACHE_TTL_DAYS)
                    {
                        File.Delete(file);
                        System.Diagnostics.Debug.WriteLine($"🗑️ Eski cache silindi: {Path.GetFileName(file)}");
                    }
                }

                Preferences.Default.Set("LastCacheCleanDate", DateTime.Now);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cache temizleme hatası: {ex.Message}");
            }
        }

        // ================================================================
        // Offline Fallback — Tüm cache dosyalarından en yakın veriyi bul
        // ================================================================

        private async Task<Dictionary<string, DateTime>?> TryOfflineFallbackAsync(DateTime date)
        {
            try
            {
                if (!Directory.Exists(_cacheDir)) return null;

                foreach (var file in Directory.GetFiles(_cacheDir, "*.json"))
                {
                    try
                    {
                        string json = await File.ReadAllTextAsync(file);
                        var data = JsonSerializer.Deserialize<List<CalendarData>>(json);
                        if (data != null)
                        {
                            var match = FindDataForDate(data, date);
                            if (match != null)
                                return ConvertToDictionary(match, date);
                        }
                    }
                    catch { /* Dosya bozuksa atla */ }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Offline fallback hatası: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Eski v2 cache dosyasından migration
        /// </summary>
        private async Task<Dictionary<string, DateTime>?> TryLegacyCacheAsync(DateTime date)
        {
            try
            {
                var legacyFile = Path.Combine(FileSystem.AppDataDirectory, "prayer_times_cache_v2.json");
                if (!File.Exists(legacyFile)) return null;

                string json = await File.ReadAllTextAsync(legacyFile);
                var data = JsonSerializer.Deserialize<List<CalendarData>>(json);
                if (data != null)
                {
                    // Veriyi yeni formata migrate et
                    await SaveMonthCacheAsync(date, data);

                    var match = FindDataForDate(data, date);
                    if (match != null)
                        return ConvertToDictionary(match, date);
                }
            }
            catch { }

            return null;
        }

        // ================================================================
        // API
        // ================================================================

        private async Task<List<CalendarData>?> FetchMonthlyDataAsync(DateTime date, string ilce, string sehir, double? lat, double? lon)
        {
            try
            {
                string url;

                if (lat.HasValue && lon.HasValue && Math.Abs(lat.Value) > 0.0001 && Math.Abs(lon.Value) > 0.0001)
                {
                    url = $"https://api.aladhan.com/v1/calendar?latitude={lat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}&longitude={lon.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}&method=13&month={date.Month}&year={date.Year}";
                }
                else
                {
                    url = $"https://api.aladhan.com/v1/calendarByAddress?address={ilce},{sehir},Turkey&method=13&month={date.Month}&year={date.Year}";
                }
                
                HttpResponseMessage response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                string json = await response.Content.ReadAsStringAsync();
                var calendarResponse = JsonSerializer.Deserialize<CalendarResponse>(json);

                return calendarResponse?.Data;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fetch hatası: {ex.Message}");
                return null;
            }
        }

        // ================================================================
        // Helpers
        // ================================================================

        private static CalendarData? FindDataForDate(List<CalendarData> data, DateTime date)
        {
            string searchDate = date.ToString("dd-MM-yyyy");
            return data.FirstOrDefault(d => d.Date.Gregorian.Date == searchDate);
        }

        private static Dictionary<string, DateTime> ConvertToDictionary(CalendarData data, DateTime date)
        {
           Timings t = data.Timings;

           DateTime ParseTime(string timeStr)
           {
               string cleanTime = timeStr.Split(' ')[0]; 
               return date.Date + TimeSpan.Parse(cleanTime);
           }

           return new Dictionary<string, DateTime>
           {
               { "İmsak", ParseTime(t.Fajr) },
               { "gunes", ParseTime(t.Sunrise) },
               { "Ogle", ParseTime(t.Dhuhr) },
               { "İkindi", ParseTime(t.Asr) },
               { "Aksam", ParseTime(t.Maghrib) },
               { "Yatsi", ParseTime(t.Isha) }
           };
        }
    }
}

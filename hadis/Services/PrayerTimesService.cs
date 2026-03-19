using hadis.Models;
using System.Text.Json;

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
            DateTime date, string ilce, string sehir, double? lat = null, double? lon = null)
        {
            // 1. Bu ayı cache'den kontrol et
            var cachedData = await LoadMonthCacheAsync(date);
            if (cachedData != null)
            {
                System.Diagnostics.Debug.WriteLine($"✅ Cache hit: {date:yyyy-MM-dd}");
                return cachedData;
            }

            // 2. Azure API'den ilçe ID'sini bul ve veri çek
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 İlçe ID araniyor: {sehir}/{ilce}");
                
                var ilceId = await _namazVaktiApiService.GetIlceIdBySehir(sehir, ilce);
                
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
                        await SaveDateCacheAsync(date, vakitler);
                        
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

            // 3. Offline fallback - Tüm cache dosyalarından ara
            var offlineResult = await TryOfflineFallbackAsync(date);
            if (offlineResult != null)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Offline fallback: {date:yyyy-MM-dd}");
                return offlineResult;
            }

            System.Diagnostics.Debug.WriteLine($"❌ Veri bulunamadı: {date:yyyy-MM-dd}");
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
                
                // Cache'te var mı kontrol et
                var cachedData = await LoadMonthCacheAsync(tomorrow);
                if (cachedData != null)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Yarın verisi cache'te mevcut: {tomorrow:yyyy-MM-dd}");
                    return;
                }

                // Azure API'den çek
                var ilceId = await _namazVaktiApiService.GetIlceIdBySehir(sehir, ilce);
                if (ilceId.HasValue)
                {
                    var vakitler = await _namazVaktiApiService.GetTarihVakitleri(ilceId.Value, tomorrow);
                    if (vakitler != null)
                    {
                        await SaveDateCacheAsync(tomorrow, vakitler);
                        System.Diagnostics.Debug.WriteLine($"✅ Prefetch tamamlandı: {tomorrow:yyyy-MM-dd}");
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

        private string GetCacheFilePath(DateTime date)
        {
            return Path.Combine(_cacheDir, $"prayer_{date:yyyy_MM}.json");
        }

        /// <summary>
        /// Ayın tamamı için cache'ten veri yüklemeye çalışır
        /// İçinde aradığımız tarihe ait veri varsa onu döndürür
        /// </summary>
        private async Task<Dictionary<string, DateTime>?> LoadMonthCacheAsync(DateTime date)
        {
            try
            {
                string filePath = GetCacheFilePath(date);
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

        private async Task SaveDateCacheAsync(DateTime date, DailyNamazVakitleri vakitler)
        {
            try
            {
                string filePath = GetCacheFilePath(date);
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

        // ================================================================
        // Offline Fallback
        // ================================================================

        private async Task<Dictionary<string, DateTime>?> TryOfflineFallbackAsync(DateTime date)
        {
            try
            {
                if (!Directory.Exists(_cacheDir)) 
                    return null;

                // Tüm cache dosyalarında aradığımız tarihi bul
                foreach (var file in Directory.GetFiles(_cacheDir, "*.json"))
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
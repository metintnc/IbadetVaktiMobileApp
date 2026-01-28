using hadis.Models;
using System.Text.Json;

namespace hadis.Services
{
    public static class PrayerTimesService
    {
        private static readonly string CacheFile = Path.Combine(FileSystem.AppDataDirectory, "prayer_times_cache_v1.json");

        public static void ClearCache()
        {
            try
            {
                if (File.Exists(CacheFile))
                {
                    File.Delete(CacheFile);
                }
            }
            catch { }
        }

        public static async Task<Dictionary<string, DateTime>> GetPrayerTimesForDateAsync(DateTime date, string ilce, string sehir)
        {
            // 1. Önbelleğe bak
            var cachedData = await LoadFromCacheAsync();
            if (cachedData != null)
            {
                // Bugünün verisi var mı?
                var todayData = FindDataForDate(cachedData, date);
                if (todayData != null)
                {
                    return ConvertToDictionary(todayData, date);
                }
            }

            // 2. Yoksa API'den çek (Tüm ayı çekiyoruz)
            try
            {
                var freshData = await FetchMonthlyDataAsync(date, ilce, sehir);
                if (freshData != null && freshData.Count > 0)
                {
                    // Cache'i güncelle
                    await SaveToCacheAsync(freshData);

                    // Bugünü tekrar ara
                    var todayData = FindDataForDate(freshData, date);
                    if (todayData != null)
                    {
                        return ConvertToDictionary(todayData, date);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Hatası: {ex.Message}");
                // API hata verirse ve elimizde eski cache varsa belki bir gün sonrasını vs bulabiliriz?
                // Şimdilik null dönelim, UI hata göstersin.
            }

            return null;
        }

        private static async Task<List<CalendarData>> LoadFromCacheAsync()
        {
            try
            {
                if (!File.Exists(CacheFile)) return null;
                string json = await File.ReadAllTextAsync(CacheFile);
                return JsonSerializer.Deserialize<List<CalendarData>>(json);
            }
            catch
            {
                return null;
            }
        }

        private static async Task SaveToCacheAsync(List<CalendarData> data)
        {
            try
            {
                string json = JsonSerializer.Serialize(data);
                await File.WriteAllTextAsync(CacheFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cache Yazma Hatası: {ex.Message}");
            }
        }

        private static async Task<List<CalendarData>> FetchMonthlyDataAsync(DateTime date, string ilce, string sehir)
        {
            try
            {
                using HttpClient http = new HttpClient();
                // Aladhan Calendar API: month ve year parametreleri ile tüm ayı çeker
                string url = $"https://api.aladhan.com/v1/calendarByAddress?address={ilce},{sehir},Turkey&method=13&month={date.Month}&year={date.Year}";
                
                HttpResponseMessage response = await http.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                string json = await response.Content.ReadAsStringAsync();
                var calendarResponse = JsonSerializer.Deserialize<CalendarResponse>(json);

                return calendarResponse?.Data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fetch Hatası: {ex.Message}");
                return null;
            }
        }

        private static CalendarData FindDataForDate(List<CalendarData> data, DateTime date)
        {
            // API formatı DD-MM-YYYY
            string searchDate = date.ToString("dd-MM-yyyy");
            return data.FirstOrDefault(d => d.Date.Gregorian.Date == searchDate);
        }

        private static Dictionary<string, DateTime> ConvertToDictionary(CalendarData data, DateTime date)
        {
           Timings t = data.Timings;

           // Saat stringlerini parse et (Örn: "05:12 (EEST)")
           // Genelde "HH:mm" gelir ama bazen timezone ekli olabilir, TimeSpan.Parse ilk kısmı alır genellikle.
           // Çözücü metodu: "05:12" kısmını almak.
           DateTime ParseTime(string timeStr)
           {
               // (EST) gibi ekleri temizle
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

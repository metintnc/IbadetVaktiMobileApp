using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using hadis.Models;
using System.IO;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Networking;

namespace hadis.Services
{
    public class QuranApiService
    {
        private readonly HttpClient _client = new();
        private readonly string _cacheDir;

        public QuranApiService()
        {
            _cacheDir = Path.Combine(FileSystem.AppDataDirectory, "quran_cache");
            if (!Directory.Exists(_cacheDir))
            {
                Directory.CreateDirectory(_cacheDir);
            }
        }

        public bool CheckCacheStatus()
        {
            // Basit kontrol: Fatiha (1) ve Nas (114) hem Türkçe hem Arapça var mı?
            // İsterseniz daha detaylı kontrol yapılabilir (114 dosya x 2)
            bool fatihaAr = File.Exists(Path.Combine(_cacheDir, "surah_1_ar.json"));
            bool nasAr = File.Exists(Path.Combine(_cacheDir, "surah_114_ar.json"));
            bool fatihaTr = File.Exists(Path.Combine(_cacheDir, "surah_1_tr.json"));
            
            return fatihaAr && nasAr && fatihaTr;
        }

        public async Task<List<AlQuranAyah>> GetSurahAsync(int surahNo, string lang = "tr.diyanet")
        {
            // Cache dosya ismini belirle
            string cacheKey = lang == "ar" ? "ar" : "tr"; // Basitleştirme: tr.diyanet -> tr, ar -> ar
            string fileName = $"surah_{surahNo}_{cacheKey}.json";
            string filePath = Path.Combine(_cacheDir, fileName);

            // 1. Cache Kontrol
            if (File.Exists(filePath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    var cachedSurah = JsonSerializer.Deserialize<AlQuranData>(json);
                    if (cachedSurah?.Ayahs != null)
                    {
                        return cachedSurah.Ayahs;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Cache okuma hatası ({fileName}): {ex.Message}");
                }
            }

            // 2. İnternet Kontrol ve API İsteği
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                // İnternet yok ve cache de yok -> Boş dön
                return new List<AlQuranAyah>();
            }

            try
            {
                var url = $"https://api.alquran.cloud/v1/surah/{surahNo}/{lang}";
                var response = await _client.GetStringAsync(url);
                var result = JsonSerializer.Deserialize<AlQuranResponse>(response);

                // İsteğe bağlı: Tekil de olsa cache'e atılabilir ama "Tam İndir" mantığı ile çakışmasın diye şimdilik ellemiyoruz.
                // Veya: Kullanıcı okudukça da kaydetsin? 
                // Şimdilik sadece "Tam İndir" ile full offline yapalım. 
                
                return result?.Data?.Ayahs ?? new List<AlQuranAyah>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API İsteği hatası: {ex.Message}");
                return new List<AlQuranAyah>();
            }
        }

        public async Task DownloadAndCacheFullQuranAsync(IProgress<string> progress)
        {
            try
            {
                // 1. Arapça
                progress?.Report("Arapça Kur'an indiriliyor...");
                string arUrl = "https://api.alquran.cloud/v1/quran/quran-uthmani";
                var arResponse = await _client.GetStringAsync(arUrl);
                var arResult = JsonSerializer.Deserialize<AlQuranFullResponse>(arResponse);
                
                if (arResult?.Data?.Surahs != null)
                {
                    progress?.Report("Arapça sureler kaydediliyor...");
                    foreach (var surah in arResult.Data.Surahs)
                    {
                        string fileName = $"surah_{surah.Number}_ar.json";
                        string filePath = Path.Combine(_cacheDir, fileName);
                        string json = JsonSerializer.Serialize(surah);
                        await File.WriteAllTextAsync(filePath, json);
                    }
                }

                // 2. Türkçe (Diyanet)
                progress?.Report("Türkçe Meali indiriliyor...");
                string trUrl = "https://api.alquran.cloud/v1/quran/tr.diyanet";
                var trResponse = await _client.GetStringAsync(trUrl);
                var trResult = JsonSerializer.Deserialize<AlQuranFullResponse>(trResponse);

                if (trResult?.Data?.Surahs != null)
                {
                    progress?.Report("Türkçe sureler kaydediliyor...");
                    foreach (var surah in trResult.Data.Surahs)
                    {
                        string fileName = $"surah_{surah.Number}_tr.json"; // tr.diyanet -> tr
                        string filePath = Path.Combine(_cacheDir, fileName);
                        string json = JsonSerializer.Serialize(surah);
                        await File.WriteAllTextAsync(filePath, json);
                    }
                }

                progress?.Report("Tamamlandı");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"İndirme Hatası: {ex.Message}");
                throw; // UI tarafında yakalamak için fırlat
            }
        }
    }
}

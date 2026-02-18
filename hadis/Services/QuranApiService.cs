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
            _cacheDir = Path.Combine(FileSystem.AppDataDirectory, "quran_cache_v2"); // New cache dir for new API
            if (!Directory.Exists(_cacheDir))
            {
                Directory.CreateDirectory(_cacheDir);
            }
        }

        public bool CheckCacheStatus()
        {
            // Check if Fatiha (1) and Nas (114) exist in new format
            bool fatiha = File.Exists(Path.Combine(_cacheDir, "surah_1.json"));
            bool nas = File.Exists(Path.Combine(_cacheDir, "surah_114.json"));
            
            return fatiha && nas;
        }

        public async Task<List<Ayah>> GetSurahAsync(int surahNo)
        {
            string fileName = $"surah_{surahNo}.json";
            string filePath = Path.Combine(_cacheDir, fileName);

            AcikKuranData surahData = null;

            // 1. Check Cache
            if (File.Exists(filePath))
            {
                try
                {
                    string json = await File.ReadAllTextAsync(filePath);
                    var response = JsonSerializer.Deserialize<AcikKuranData>(json);
                    if (response != null)
                    {
                        surahData = response;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Cache read error ({fileName}): {ex.Message}");
                }
            }

            // 2. Fetch from API if not in cache
            if (surahData == null)
            {
                if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    return new List<Ayah>();
                }

                try
                {
                    // author=11 (Diyanet Ä°ÅŸleri)
                    var url = $"https://api.acikkuran.com/surah/{surahNo}?author=11";
                    var responseString = await _client.GetStringAsync(url);
                    var apiResponse = JsonSerializer.Deserialize<AcikKuranResponse>(responseString);
                    
                    if (apiResponse?.Data != null)
                    {
                        surahData = apiResponse.Data;
                        
                        // Cache it immediately (saving per-surah usage is fine)
                        string jsonToSave = JsonSerializer.Serialize(surahData);
                        await File.WriteAllTextAsync(filePath, jsonToSave);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"API Request error: {ex.Message}");
                    return new List<Ayah>();
                }
            }

            // 3. Map to Ayah Model
            var ayahs = new List<Ayah>();
            if (surahData?.Verses != null)
            {
                foreach (var v in surahData.Verses)
                {
                    ayahs.Add(new Ayah
                    {
                        Number = v.VerseNumber,
                        ArabicText = v.Verse,
                        Translation = v.Translation?.Text ?? "",
                        Transliteration = v.Transcription ?? "",
                        IsSaved = false // Will be set by ViewModel
                    });
                }
            }

            return ayahs;
        }

        public async Task DownloadAndCacheFullQuranAsync(IProgress<string> progress)
        {
            try
            {
                progress?.Report("Kur'an verileri indiriliyor...");
                
                // AcikKuran API is per-surah. We need to fetch 114 surahs.
                // To be nice to the API and efficient, we can do it in batches or sequentially.
                // 114 is small enough for sequential with progress updates, ensuring order and less timeout risk.
                
                int totalSurahs = 114;
                for (int i = 1; i <= totalSurahs; i++)
                {
                    progress?.Report($"Sureler indiriliyor... ({i}/{totalSurahs})");
                    
                    // Reuse GetSurahAsync which allows logic for fetching and caching
                    // BUT GetSurahAsync returns mapped list. We want to ensure it fetches from NET if not cached.
                    // Actually GetSurahAsync logic "Check Cache -> If Null -> Fetch & Cache" is exactly what we want.
                    // If the user already visited some surahs, they are cached. We just fill the gaps.
                    // However, we want to force download if we are "Downloading Full Quran"? 
                    // Usually "Download" implies "Ensure Offline Availability". Relying on CheckCacheStatus logic is fine.
                    
                    // Optimization: We could check File.Exists here to skip 'await' overhead if we want.
                    string fileName = $"surah_{i}.json";
                    string filePath = Path.Combine(_cacheDir, fileName);
                    
                    if (!File.Exists(filePath))
                    {
                        await GetSurahAsync(i);
                        // Add a small delay to be polite to the API
                        await Task.Delay(50);
                    }
                }

                progress?.Report("TamamlandÄ±");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Download Error: {ex.Message}");
                throw;
            }
        }
    }
}


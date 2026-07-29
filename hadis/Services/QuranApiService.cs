using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using hadis.Models;
using System.IO;
using System.Threading;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Networking;

namespace hadis.Services
{
    public class QuranApiService
    {
        private readonly HttpClient _client;
        private readonly string _cacheDir;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

        public QuranApiService(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient("QuranApi");
            _cacheDir = Path.Combine(FileSystem.AppDataDirectory, "quran_cache_v2");
            EnsureCacheDirectory();
        }

        private static SemaphoreSlim GetFileLock(string filePath)
        {
            return _fileLocks.GetOrAdd(filePath, _ => new SemaphoreSlim(1, 1));
        }

        private void EnsureCacheDirectory()
        {
            try
            {
                if (!Directory.Exists(_cacheDir))
                {
                    Directory.CreateDirectory(_cacheDir);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cache dizini oluşturma hatası: {ex.Message}");
            }
        }

        public bool CheckCacheStatus()
        {
            var authorId = Preferences.Default.Get("MealAuthorId", "11");
            bool fatiha = File.Exists(Path.Combine(_cacheDir, $"surah_1_author_{authorId}.json"));
            bool nas = File.Exists(Path.Combine(_cacheDir, $"surah_114_author_{authorId}.json"));
            
            return fatiha && nas;
        }

        public bool IsPageTranslationCached(int pageNumber)
        {
            var authorId = Preferences.Default.Get("MealAuthorId", "11");
            string fileName = $"page_{pageNumber}_author_{authorId}.json";
            string filePath = Path.Combine(_cacheDir, fileName);
            return File.Exists(filePath);
        }

        public async Task<List<Ayah>> GetSurahAsync(int surahNo, CancellationToken cancellationToken = default)
        {
            var authorId = Preferences.Default.Get("MealAuthorId", "11");
            string fileName = $"surah_{surahNo}_author_{authorId}.json";
            string filePath = Path.Combine(_cacheDir, fileName);

            var fileLock = GetFileLock(filePath);
            await fileLock.WaitAsync(cancellationToken);

            AcikKuranData surahData = null;

            try
            {
                // 1. Check Cache
                if (File.Exists(filePath))
                {
                    try
                    {
                        string json = await File.ReadAllTextAsync(filePath, cancellationToken);
                        var response = JsonSerializer.Deserialize<AcikKuranData>(json);
                        if (response != null)
                        {
                            surahData = response;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
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
                        var url = $"https://api.acikkuran.com/surah/{surahNo}?author={authorId}";
                        var response = await _client.GetAsync(url, cancellationToken);
                        response.EnsureSuccessStatusCode();
                        var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                        var apiResponse = JsonSerializer.Deserialize<AcikKuranResponse>(responseString);
                        
                        if (apiResponse?.Data != null)
                        {
                            surahData = apiResponse.Data;
                            
                            // Cache it immediately with authorId suffix
                            string jsonToSave = JsonSerializer.Serialize(surahData);
                            await File.WriteAllTextAsync(filePath, jsonToSave, cancellationToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"API Request error: {ex.Message}");
                        return new List<Ayah>();
                    }
                }
            }
            finally
            {
                fileLock.Release();
            }

            // 3. Map to Ayah Model
            var ayahs = new List<Ayah>();
            if (surahData?.Verses != null)
            {
                var surahName = KuranDataService.GetSureByNo(surahNo)?.Ad ?? "";
                foreach (var v in surahData.Verses)
                {
                    ayahs.Add(new Ayah
                    {
                        Number = v.VerseNumber,
                        ArabicText = v.Verse,
                        Translation = v.Translation?.Text ?? "",
                        Transliteration = v.Transcription ?? "",
                        Page = v.Page,
                        SurahId = surahNo,
                        SurahName = surahName,
                        IsSaved = false // Will be set by ViewModel
                    });
                }
            }

            return ayahs;
        }

        public async Task DownloadAndCacheFullQuranAsync(IProgress<string> progress, CancellationToken cancellationToken = default)
        {
            try
            {
                progress?.Report("Kur'an verileri indiriliyor...");
                
                int totalSurahs = 114;
                int completedCount = 0;
                var authorId = Preferences.Default.Get("MealAuthorId", "11");

                // Aynı anda en fazla 4 paralel indirme isteği (API dostu ve 4-5 kat daha hızlı)
                using var semaphore = new SemaphoreSlim(4);

                var tasks = Enumerable.Range(1, totalSurahs).Select(async surahNo =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        string fileName = $"surah_{surahNo}_author_{authorId}.json";
                        string filePath = Path.Combine(_cacheDir, fileName);
                        
                        if (!File.Exists(filePath))
                        {
                            await GetSurahAsync(surahNo, cancellationToken);
                        }

                        int current = Interlocked.Increment(ref completedCount);
                        progress?.Report($"Sureler indiriliyor... ({current}/{totalSurahs})");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);
                progress?.Report("Tamamlandı");
            }
            catch (OperationCanceledException)
            {
                // İptal edildi
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Download Error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Ayah>> GetPageTranslationAsync(int pageNumber, CancellationToken cancellationToken = default)
        {
            var authorId = Preferences.Default.Get("MealAuthorId", "11");
            string fileName = $"page_{pageNumber}_author_{authorId}.json";
            string filePath = Path.Combine(_cacheDir, fileName);

            var fileLock = GetFileLock(filePath);
            await fileLock.WaitAsync(cancellationToken);

            try
            {
                // 1. Try cache first
                if (File.Exists(filePath))
                {
                    try
                    {
                        string json = await File.ReadAllTextAsync(filePath, cancellationToken);
                        var apiResponse = JsonSerializer.Deserialize<AcikKuranPageResponse>(json);
                        if (apiResponse?.Data != null)
                        {
                            return MapToAyahs(apiResponse.Data);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Page cache read error: {ex.Message}");
                    }
                }
                
                // 2. Fetch from API if online
                if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    return null;
                }
                
                try
                {
                    var url = $"https://api.acikkuran.com/page/{pageNumber}?author={authorId}";
                    var response = await _client.GetAsync(url, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                    var apiResponse = JsonSerializer.Deserialize<AcikKuranPageResponse>(responseString);
                    
                    if (apiResponse?.Data != null)
                    {
                        // Cache it immediately with authorId suffix
                        await File.WriteAllTextAsync(filePath, responseString, cancellationToken);
                        return MapToAyahs(apiResponse.Data);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error fetching page translation from API: {ex.Message}");
                }
            }
            finally
            {
                fileLock.Release();
            }
            
            return null;
        }

        private List<Ayah> MapToAyahs(List<AcikKuranVerse> verses)
        {
            var ayahs = new List<Ayah>();
            foreach (var v in verses)
            {
                ayahs.Add(new Ayah
                {
                    Number = v.VerseNumber,
                    ArabicText = v.Verse,
                    Translation = v.Translation?.Text ?? "",
                    Transliteration = v.Transcription ?? "",
                    Page = v.Page,
                    SurahId = v.Surah?.Id ?? 0,
                    SurahName = v.Surah?.Name ?? ""
                });
            }
            return ayahs;
        }
    }
}

using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using hadis.Models;

namespace hadis.Services;

public class QuranApiService
{
    private readonly HttpClient _client;
    private readonly string _cacheDir;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();

    public QuranApiService(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient("QuranApi");
        _cacheDir = Path.Combine(FileSystem.AppDataDirectory, "quran_cache_v2");
        Directory.CreateDirectory(_cacheDir);
    }

    private static string Author => Preferences.Default.Get("MealAuthorId", "11");
    private string SurahPath(int id, string author) => Path.Combine(_cacheDir, $"surah_{id}_author_{author}.json");
    private string LegacyPagePath(int page, string author) => Path.Combine(_cacheDir, $"page_{page}_author_{author}.json");

    public bool CheckCacheStatus()
    {
        var author = Author;
        return KuranDataService.GetSureler().All(s => File.Exists(SurahPath(s.SureNo, author)));
    }

    public bool IsPageTranslationCached(int pageNumber)
    {
        var author = Author;
        return KuranDataService.GetPageRanges(pageNumber).All(r => File.Exists(SurahPath(r.Surah.SureNo, author)))
            || File.Exists(LegacyPagePath(pageNumber, author));
    }

    public Task<List<Ayah>> GetSurahAsync(int surahNo, CancellationToken cancellationToken = default) =>
        GetSurahAsync(surahNo, Author, cancellationToken);

    private async Task<List<Ayah>> GetSurahAsync(int id, string author, CancellationToken token)
    {
        var surah = KuranDataService.GetSureByNo(id);
        if (surah == null) return new();

        var path = SurahPath(id, author);
        var gate = _fileLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(token);
        try
        {
            var data = await ReadCacheAsync<AcikKuranData>(path, token);
            if (data?.Verses?.Count != surah.AyetSayisi)
            {
                if (Connectivity.NetworkAccess != NetworkAccess.Internet) return new();
                data = await DownloadSurahAsync(surah, author, token);
                var temporary = path + ".tmp";
                try
                {
                    await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(data), token);
                    File.Move(temporary, path, true);
                }
                finally
                {
                    File.Delete(temporary);
                }
            }

            return data.Verses.Select(v => MapVerse(v, surah)).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !token.IsCancellationRequested)
        {
            System.Diagnostics.Debug.WriteLine($"Sûre {id} yüklenemedi: {ex.Message}");
            return new();
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<AcikKuranData> DownloadSurahAsync(Sure surah, string author, CancellationToken token)
    {
        string edition = author switch
        {
            "11" => "diyanetisleriyeni",
            "14" => "elmaliliorj",
            "15" => "elmalilisade",
            "27" => "sates",
            "8" => "bayraktar",
            _ => throw new InvalidDataException("Bilinmeyen meal yazarı.")
        };

        using var payload = await _client.GetFromJsonAsync<JsonDocument>(
            $"surahs/{surah.SureNo}?translations={edition}&all=1", token);
        var data = payload!.RootElement.GetProperty("data");
        var verses = data.GetProperty("verses");
        if (data.GetProperty("surah").GetProperty("number").GetInt32() != surah.SureNo ||
            verses.GetArrayLength() != surah.AyetSayisi)
            throw new InvalidDataException("Sûrenin ayetleri eksik.");

        // This edition supplies the Turkish reading when the translation source omits it.
        var readings = await _client.GetFromJsonAsync<AlQuranResponse>(
            $"https://api.alquran.cloud/v1/surah/{surah.SureNo}/tr.transliteration", token);
        if (readings?.Data?.Number != surah.SureNo || readings.Data.Ayahs.Count != surah.AyetSayisi)
            throw new InvalidDataException("Sûrenin okunuşu eksik.");

        var result = new AcikKuranData { Id = surah.SureNo, Name = surah.Ad, Verses = new() };
        foreach (var verse in verses.EnumerateArray())
        {
            int number = verse.GetProperty("verse_number").GetInt32();
            int global = verse.GetProperty("global_number").GetInt32();
            var reading = readings.Data.Ayahs[result.Verses.Count];
            if (number != result.Verses.Count + 1 || reading.Number != global)
                throw new InvalidDataException("Ayet numaraları uyuşmuyor.");

            var translation = verse.GetProperty("translations").EnumerateArray()
                .Single(t => t.GetProperty("slug").GetString() == edition).GetProperty("text").GetString();
            if (string.IsNullOrWhiteSpace(translation)) throw new InvalidDataException("Ayet meali eksik.");
            result.Verses.Add(new AcikKuranVerse
            {
                Id = global, VerseNumber = number, Page = KuranDataService.GetReaderPage(global) - 1,
                Verse = verse.GetProperty("arabic").GetProperty("uthmani").GetString()!,
                Transcription = reading.Text, Translation = new AcikKuranTranslation { Text = translation }
            });
        }
        return result;
    }

    private static async Task<T?> ReadCacheAsync<T>(string path, CancellationToken token) where T : class
    {
        if (!File.Exists(path)) return null;
        try
        {
            using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: token);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return null;
        }
    }

    private static Ayah MapVerse(AcikKuranVerse verse, Sure surah) => new()
    {
        Number = verse.VerseNumber, ArabicText = verse.Verse, Translation = verse.Translation?.Text ?? "",
        Transliteration = verse.Transcription ?? "", Page = verse.Page, SurahId = surah.SureNo, SurahName = surah.Ad
    };

    public async Task DownloadAndCacheFullQuranAsync(IProgress<string> progress, CancellationToken cancellationToken = default)
    {
        var author = Author;
        int completed = 0;
        var options = new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cancellationToken };
        await Parallel.ForEachAsync(KuranDataService.GetSureler(), options, async (surah, token) =>
        {
            var verses = await GetSurahAsync(surah.SureNo, author, token);
            if (verses.Count != surah.AyetSayisi)
                throw new IOException($"{surah.Ad} sûresi indirilemedi.");
            progress.Report($"Sûreler indiriliyor... ({Interlocked.Increment(ref completed)}/114)");
        });
    }

    public async Task<List<Ayah>> GetPageTranslationAsync(int pageNumber, CancellationToken cancellationToken = default)
    {
        var author = Author;
        var ranges = KuranDataService.GetPageRanges(pageNumber).ToArray();
        var legacy = await ReadCacheAsync<AcikKuranPageResponse>(LegacyPagePath(pageNumber, author), cancellationToken);
        var page = new List<Ayah>();
        foreach (var (surah, first, last) in ranges)
        {
            var verses = legacy?.Data?.Where(v => v.Surah?.Id == surah.SureNo &&
                    v.VerseNumber >= first && v.VerseNumber <= last)
                .Select(v => MapVerse(v, surah)).OrderBy(v => v.Number).ToList();
            if (verses == null || !verses.Select(v => v.Number).SequenceEqual(Enumerable.Range(first, last - first + 1)))
            {
                var chapter = await GetSurahAsync(surah.SureNo, author, cancellationToken);
                verses = chapter.Where(v => v.Number >= first && v.Number <= last).ToList();
                if (verses.Count != last - first + 1) return new();
            }
            page.AddRange(verses);
        }
        return page;
    }
}

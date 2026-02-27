using hadis.Models;
using System.Text.Json;

namespace hadis.Services
{
    /// <summary>
    /// Kaydedilen ayetleri yöneten thread-safe servis
    /// </summary>
    public static class SavedAyahsService
    {
        private static readonly string FileName = Path.Combine(FileSystem.AppDataDirectory, "saved_ayahs.json");
        private static readonly SemaphoreSlim _lock = new(1, 1);
        private static List<SavedAyah>? _savedAyahs;

        public static async Task<List<SavedAyah>> GetSavedAyahsAsync()
        {
            await _lock.WaitAsync();
            try
            {
                return await GetSavedAyahsInternalAsync();
            }
            finally
            {
                _lock.Release();
            }
        }

        private static async Task<List<SavedAyah>> GetSavedAyahsInternalAsync()
        {
            if (_savedAyahs != null)
                return _savedAyahs;

            if (!File.Exists(FileName))
            {
                _savedAyahs = new List<SavedAyah>();
                return _savedAyahs;
            }

            try
            {
                var json = await File.ReadAllTextAsync(FileName);
                _savedAyahs = JsonSerializer.Deserialize<List<SavedAyah>>(json) ?? new List<SavedAyah>();
            }
            catch
            {
                _savedAyahs = new List<SavedAyah>();
            }

            return _savedAyahs;
        }

        public static async Task SaveAyahAsync(SavedAyah ayah)
        {
            await _lock.WaitAsync();
            try
            {
                await GetSavedAyahsInternalAsync();

                // Check if already saved (same sure and ayah number)
                if (_savedAyahs!.Any(a => a.SureNo == ayah.SureNo && a.Number == ayah.Number))
                    return;

                _savedAyahs.Add(ayah);
                await SaveToFileAsync();
            }
            finally
            {
                _lock.Release();
            }
        }

        public static async Task RemoveAyahAsync(SavedAyah ayah)
        {
            await _lock.WaitAsync();
            try
            {
                await GetSavedAyahsInternalAsync();
                
                var item = _savedAyahs!.FirstOrDefault(a => a.SureNo == ayah.SureNo && a.Number == ayah.Number);
                if (item != null)
                {
                    _savedAyahs.Remove(item);
                    await SaveToFileAsync();
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        private static async Task SaveToFileAsync()
        {
            var json = JsonSerializer.Serialize(_savedAyahs);
            await File.WriteAllTextAsync(FileName, json);
        }
    }
}

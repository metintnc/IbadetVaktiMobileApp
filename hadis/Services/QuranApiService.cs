using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using hadis.Models;

namespace hadis.Services
{
    public class QuranApiService
    {
        private readonly HttpClient _client = new();

        public async Task<List<AlQuranAyah>> GetSurahAsync(int surahNo, string lang = "tr.diyanet")
        {
            var url = $"https://api.alquran.cloud/v1/surah/{surahNo}/{lang}";
            var response = await _client.GetStringAsync(url);
            var result = JsonSerializer.Deserialize<AlQuranResponse>(response);
            return result?.Data?.Ayahs ?? new List<AlQuranAyah>();
        }
    }
}

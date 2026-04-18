using System.Net.Http.Json;
using System.Text.Json.Serialization;
using hadis.Models;

namespace hadis.Services
{
    /// <summary>
    /// Azure uzerinde deploy edilmis Diyanet API'sinden namaz vakitlerini alir
    /// </summary>
    public class NamazVaktiApiService
    {
        private readonly HttpClient _httpClient;
        // API URL'sini ApiSecrets üzerinden alır, böylece github'da gözükmez
        private static readonly string BaseUrl = ApiSecrets.ApiUrl;
        
        // Ülke ID'leri (API'de sabit)
        private const int TurkiyeCountryId = 2;
        private const int AzerbaijanCountryId = 5;
        private const int GermanyCountryId = 13;
        private const int SaudiArabiaCountryId = 64;
        
        // Önbelleklenmiş il ve ilçe listeleri
        private List<PlaceInfo> _cachedStates;
        private readonly Dictionary<int, List<PlaceInfo>> _cachedStatesByCountry = new();
        private Dictionary<int, List<PlaceInfo>> _cachedCities = new();

        public NamazVaktiApiService()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            SetApiHeaders();
            // Azure App Service Cold Start (uyku modundan uyanma) genellikle 30-50 saniye sürer.
            // Bu sebeple timeout süresi 90 saniyeye çıkarıldı.
            _httpClient.Timeout = TimeSpan.FromSeconds(90); 
        }

        private void SetApiHeaders()
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Add("UserName", ApiSecrets.UserName);
                _httpClient.DefaultRequestHeaders.Add("SecretCode", ApiSecrets.SecretCode);
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Header ayarlama hatasi: {ex.Message}");
            }
        }

        // ================================================================
        // Namaz Vakitleri
        // ================================================================

        /// <summary>
        /// Belirli bir ilce icin gunluk namaz vakitlerini alir
        /// </summary>
        public async Task<DailyNamazVakitleri> GetBugunVakitleri(int cityId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetBugunVakitleri basliyor (ID: {cityId})");
                
                var response = await _httpClient.GetAsync($"api/AwqatSalah/Daily/{cityId}");
                
                System.Diagnostics.Debug.WriteLine($"   HTTP Status: {response.StatusCode}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"API Hatasi: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"   Detay: {errorContent.Substring(0, Math.Min(200, errorContent.Length))}");
                    return null;
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<DailyNamazVakitleri>>>();
                
                if (apiResponse?.Success == true && apiResponse.Data?.Count > 0)
                {
                    var result = apiResponse.Data[0];
                    System.Diagnostics.Debug.WriteLine($"Veri alindi: {result.Tarih} - Imsak: {result.Imsak}");
                    return result;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"API yaniti basarisiz veya bos");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetBugunVakitleri Hatasi: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"   Message: {ex.Message}");
                if (ex.InnerException != null)
                    System.Diagnostics.Debug.WriteLine($"   Inner: {ex.InnerException.Message}");
                return null;
            }
        }

        /// <summary>
        /// Belirli bir ilce ve tarih icin namaz vakitlerini alir
        /// </summary>
        public async Task<DailyNamazVakitleri> GetTarihVakitleri(int cityId, DateTime tarih)
        {
            try
            {
                // Daily endpoint sadece bugunu dondurur
                var response = await _httpClient.GetAsync($"api/AwqatSalah/Daily/{cityId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"API Hatasi: {response.StatusCode}");
                    return null;
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<DailyNamazVakitleri>>>();
                
                if (apiResponse?.Success == true && apiResponse.Data?.Count > 0)
                {
                    return apiResponse.Data[0];
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NamazVaktiApiService Hatasi: {ex.Message}");
                return null;
            }
        }

        // ================================================================
        // Konum API'si (Countries -> States -> Cities)
        // ================================================================

        /// <summary>
        /// Türkiye'deki tüm illeri getirir
        /// </summary>
        public async Task<List<PlaceInfo>> GetTumIller()
        {
            return await GetIllerByCountryId(TurkiyeCountryId);
        }

        private async Task<List<PlaceInfo>> GetIllerByCountryId(int countryId)
        {
            try
            {
                if (countryId == TurkiyeCountryId && _cachedStates != null)
                    return _cachedStates;

                if (_cachedStatesByCountry.TryGetValue(countryId, out var cachedStates))
                    return cachedStates;

                System.Diagnostics.Debug.WriteLine($"GetIllerByCountryId basliyor (countryId: {countryId})...");

                var response = await _httpClient.GetAsync($"api/Place/States/{countryId}");

                System.Diagnostics.Debug.WriteLine($"   HTTP Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"API Hatasi: {response.StatusCode}");
                    return null;
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<PlaceInfo>>>();

                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    _cachedStatesByCountry[countryId] = apiResponse.Data;

                    if (countryId == TurkiyeCountryId)
                        _cachedStates = apiResponse.Data;

                    System.Diagnostics.Debug.WriteLine($"{apiResponse.Data.Count} il basariyla yuklendi (countryId: {countryId})");
                    return apiResponse.Data;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetIllerByCountryId Hatasi: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Bir ilin ilcelerini getirir
        /// </summary>
        public async Task<List<PlaceInfo>> GetIlceler(int stateId)
        {
            try
            {
                if (_cachedCities.ContainsKey(stateId))
                    return _cachedCities[stateId];

                System.Diagnostics.Debug.WriteLine($"GetIlceler basliyor (stateId: {stateId})...");
                
                var response = await _httpClient.GetAsync($"api/Place/Cities/{stateId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"API Hatasi: {response.StatusCode}");
                    return null;
                }

                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<PlaceInfo>>>();
                
                if (apiResponse?.Success == true && apiResponse.Data != null)
                {
                    _cachedCities[stateId] = apiResponse.Data;
                    System.Diagnostics.Debug.WriteLine($"{apiResponse.Data.Count} ilce yuklendi (stateId: {stateId})");
                    return apiResponse.Data;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetIlceler Hatasi: {ex.Message}");
                return null;
            }
        }

        private static PlaceInfo? FindBestPlaceMatch(IEnumerable<PlaceInfo> places, string value)
        {
            if (places == null || string.IsNullOrWhiteSpace(value))
                return null;

            var normalized = NormalizeForSearch(value);

            var exact = places.FirstOrDefault(i =>
                NormalizeForSearch(i.Name) == normalized ||
                NormalizeForSearch(i.Code) == normalized);

            if (exact != null)
                return exact;

            return places.FirstOrDefault(i =>
                NormalizeForSearch(i.Name).Contains(normalized) ||
                NormalizeForSearch(i.Code).Contains(normalized));
        }

        private async Task<int?> FindCityIdInCountry(int countryId, string sehirAdi, string ilceAdi = null)
        {
            var states = await GetIllerByCountryId(countryId);
            if (states == null || states.Count == 0)
                return null;

            var stateMatch = FindBestPlaceMatch(states, sehirAdi);
            if (stateMatch != null)
            {
                var stateCities = await GetIlceler(stateMatch.Id);
                if (stateCities != null && stateCities.Count > 0)
                {
                    var ilceMatch = FindBestPlaceMatch(stateCities, ilceAdi);
                    if (ilceMatch != null)
                        return ilceMatch.Id;

                    var sehirAsCityMatch = FindBestPlaceMatch(stateCities, sehirAdi);
                    if (sehirAsCityMatch != null)
                        return sehirAsCityMatch.Id;

                    return stateCities[0].Id;
                }

                return stateMatch.Id;
            }

            foreach (var state in states)
            {
                var stateCities = await GetIlceler(state.Id);
                if (stateCities == null || stateCities.Count == 0)
                    continue;

                var cityMatch = FindBestPlaceMatch(stateCities, sehirAdi);
                if (cityMatch != null)
                    return cityMatch.Id;

                var ilceMatch = FindBestPlaceMatch(stateCities, ilceAdi);
                if (ilceMatch != null)
                    return ilceMatch.Id;
            }

            return null;
        }

        private static string NormalizeForSearch(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            return input.ToUpper(new System.Globalization.CultureInfo("tr-TR"))
                        .Replace("Ö", "O").Replace("Ü", "U").Replace("Ş", "S")
                        .Replace("Ç", "C").Replace("Ğ", "G").Replace("İ", "I").Replace("I", "I")
                        .Replace("Ə", "E").Replace("Ä", "A").Replace("ẞ", "SS");
        }

        /// <summary>
        /// Sehir adindan ilce/city ID'sini bulur
        /// Mevcut PrayerTimesService ile uyumluluk icin korunuyor
        /// </summary>
        public async Task<int?> GetIlceIdBySehir(string sehirAdi, string ilceAdi = null, string ulkeName = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetIlceIdBySehir cagrildi: {sehirAdi}/{ilceAdi}");

                var manuelUlke = !string.IsNullOrEmpty(ulkeName) ? ulkeName : Preferences.Default.Get("ManuelUlke", "Türkiye");

                if (NormalizeForSearch(manuelUlke) == NormalizeForSearch("Almanya"))
                {
                    var germanyCityId = await FindCityIdInCountry(GermanyCountryId, sehirAdi, ilceAdi);
                    if (germanyCityId.HasValue)
                    {
                        System.Diagnostics.Debug.WriteLine($"Almanya sehir/ilce bulundu (ID: {germanyCityId})");
                        return germanyCityId;
                    }
                }
                else if (NormalizeForSearch(manuelUlke) == NormalizeForSearch("Azerbaycan"))
                {
                    var azerbaijanCityId = await FindCityIdInCountry(AzerbaijanCountryId, sehirAdi, ilceAdi);
                    if (azerbaijanCityId.HasValue)
                    {
                        System.Diagnostics.Debug.WriteLine($"Azerbaycan sehir/ilce bulundu (ID: {azerbaijanCityId})");
                        return azerbaijanCityId;
                    }
                }
                else if (NormalizeForSearch(manuelUlke) == NormalizeForSearch("Suudi Arabistan") ||
                         NormalizeForSearch(manuelUlke) == NormalizeForSearch("S. Arabistan"))
                {
                    var saudiCityId = await FindCityIdInCountry(SaudiArabiaCountryId, sehirAdi, ilceAdi);
                    if (saudiCityId.HasValue)
                    {
                        System.Diagnostics.Debug.WriteLine($"Suudi Arabistan sehir/ilce bulundu (ID: {saudiCityId})");
                        return saudiCityId;
                    }
                }
                
                // 1. Turkiye illerini al
                var iller = await GetTumIller();
                if (iller == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Iller listesi alinamadi");
                    return null;
                }

                // 2. Sehir adiyla eslesen ili bul
                var il = iller.FirstOrDefault(i =>
                    NormalizeForSearch(i.Name) == NormalizeForSearch(sehirAdi) ||
                    NormalizeForSearch(i.Code) == NormalizeForSearch(sehirAdi));

                if (il == null)
                {
                    // Kismi eslestirme dene
                    il = iller.FirstOrDefault(i =>
                        NormalizeForSearch(i.Name).Contains(NormalizeForSearch(sehirAdi)) ||
                        NormalizeForSearch(i.Code).Contains(NormalizeForSearch(sehirAdi)));
                }

                if (il == null)
                {
                    // Azerbaycan sehirlerini kontrol et (legacy fallback - State ID = 658)
                    var azeIlceler = await GetIlceler(658);
                    if (azeIlceler != null && azeIlceler.Count > 0)
                    {
                        var azeMatch = azeIlceler.FirstOrDefault(i =>
                            NormalizeForSearch(i.Name) == NormalizeForSearch(sehirAdi) ||
                            NormalizeForSearch(i.Name).Contains(NormalizeForSearch(sehirAdi)));

                        if (azeMatch != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Azerbaycan sehri bulundu: {azeMatch.Name} (ID: {azeMatch.Id})");
                            return azeMatch.Id;
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"Il bulunamadi: {sehirAdi}");
                    System.Diagnostics.Debug.WriteLine($"   Mevcut iller: {string.Join(", ", iller.Select(i => i.Name).Take(10))}");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"Il bulundu: {il.Name} (ID: {il.Id})");

                // 3. Ilceleri al
                var ilceler = await GetIlceler(il.Id);
                if (ilceler == null || ilceler.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Ilceler alinamadi (stateId: {il.Id})");
                    return null;
                }

                // 4. Ilce adi verilmisse ilceyi bul
                if (!string.IsNullOrEmpty(ilceAdi))
                {
                    var ilce = ilceler.FirstOrDefault(i =>
                        NormalizeForSearch(i.Name) == NormalizeForSearch(ilceAdi) ||
                        NormalizeForSearch(i.Code) == NormalizeForSearch(ilceAdi));

                    if (ilce == null)
                    {
                        ilce = ilceler.FirstOrDefault(i =>
                            NormalizeForSearch(i.Name).Contains(NormalizeForSearch(ilceAdi)) ||
                            NormalizeForSearch(i.Code).Contains(NormalizeForSearch(ilceAdi)));
                    }

                    if (ilce != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ilce bulundu: {ilce.Name} (ID: {ilce.Id})");
                        return ilce.Id;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Ilce bulunamadi: {ilceAdi}, merkez ilce kullaniliyor");
                    }
                }

                // 5. Merkez ilceyi dondur
                var merkez = ilceler.FirstOrDefault(i =>
                    NormalizeForSearch(i.Name) == NormalizeForSearch(il.Name) ||
                    NormalizeForSearch(i.Code) == NormalizeForSearch(il.Code));

                if (merkez != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Merkez ilce: {merkez.Name} (ID: {merkez.Id})");
                    return merkez.Id;
                }

                // Ilk ilceyi dondur
                System.Diagnostics.Debug.WriteLine($"Merkez bulunamadi, ilk ilce kullaniliyor: {ilceler[0].Name} (ID: {ilceler[0].Id})");
                return ilceler[0].Id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetIlceIdBySehir Hatasi: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }
    }

    // ================================================================
    // API Yanit Modelleri
    // ================================================================

    /// <summary>
    /// Tum API yanitlari bu wrapper icinde gelir
    /// </summary>
    public class ApiResponse<T>
    {
        [JsonPropertyName("data")]
        public T Data { get; set; }

        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    /// <summary>
    /// Gunluk namaz vakitleri yanit modeli
    /// Property isimleri Turkce (mevcut kodla uyumluluk), JsonPropertyName ile API alanlarina esleniyor
    /// </summary>
    public class DailyNamazVakitleri
    {
        // API'deki "fajr" -> Imsak
        [JsonPropertyName("fajr")]
        public string Imsak { get; set; }

        // API'deki "sunrise" -> Gunes
        [JsonPropertyName("sunrise")]
        public string Gunes { get; set; }

        // API'deki "dhuhr" -> Ogle
        [JsonPropertyName("dhuhr")]
        public string Ogle { get; set; }

        // API'deki "asr" -> Ikindi
        [JsonPropertyName("asr")]
        public string Ikindi { get; set; }

        // API'deki "maghrib" -> Aksam
        [JsonPropertyName("maghrib")]
        public string Aksam { get; set; }

        // API'deki "isha" -> Yatsi
        [JsonPropertyName("isha")]
        public string Yatsi { get; set; }

        // Tarih bilgileri
        [JsonPropertyName("gregorianDateShort")]
        public string GregorianDateShort { get; set; }

        [JsonPropertyName("gregorianDateLong")]
        public string GregorianDateLong { get; set; }

        [JsonPropertyName("gregorianDateLongIso8601")]
        public string GregorianDateIso { get; set; }

        [JsonPropertyName("hijriDateShort")]
        public string HijriDateShort { get; set; }

        [JsonPropertyName("hijriDateLong")]
        public string HijriDateLong { get; set; }

        // Kible vakti
        [JsonPropertyName("qiblaTime")]
        public string QiblaTime { get; set; }

        // Ay sekli URL
        [JsonPropertyName("shapeMoonUrl")]
        public string ShapeMoonUrl { get; set; }

        // Astronomik degerler
        [JsonPropertyName("astronomicalSunset")]
        public string AstronomicalSunset { get; set; }

        [JsonPropertyName("astronomicalSunrise")]
        public string AstronomicalSunrise { get; set; }

        /// <summary>
        /// PrayerTimesService ile uyumluluk icin Tarih property'si
        /// API'den gelen gregorianDateShort formatini yyyy-MM-dd'ye cevirir
        /// </summary>
        [JsonIgnore]
        public string Tarih
        {
            get
            {
                try
                {
                    if (!string.IsNullOrEmpty(GregorianDateIso))
                    {
                        var dt = DateTime.Parse(GregorianDateIso);
                        return dt.ToString("yyyy-MM-dd");
                    }
                    if (!string.IsNullOrEmpty(GregorianDateShort))
                    {
                        // Format: "19.03.2026" -> "2026-03-19"
                        var parts = GregorianDateShort.Split('.');
                        if (parts.Length == 3)
                            return $"{parts[2]}-{parts[1]}-{parts[0]}";
                    }
                }
                catch { }
                return null;
            }
        }
    }

    /// <summary>
    /// Konum bilgisi (Ulke, Il, Ilce icin ortak model)
    /// </summary>
    public class PlaceInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    /// <summary>
    /// Geriye uyumluluk icin IlceInfo
    /// </summary>
    public class IlceInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonIgnore]
        public string Ilce => Name;

        [JsonIgnore]
        public string Sehir { get; set; }

        [JsonIgnore]
        public double Latitude { get; set; }

        [JsonIgnore]
        public double Longitude { get; set; }
    }
}
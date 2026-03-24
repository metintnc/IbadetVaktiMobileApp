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
        // API URL'sini environment variable'dan oku, yoksa varsayılan URL kullan
        // Kendi API'nizi deploy edip NAMAZVAKTI_API_URL ortam değişkenini ayarlayın
        private static readonly string BaseUrl = 
            Environment.GetEnvironmentVariable("NAMAZVAKTI_API_URL") 
            ?? "https://your-api-url-here.azurewebsites.net/";
        
        // Turkiye ulke ID'si (API'de sabit)
        private const int TurkiyeCountryId = 2;
        
        // Onbelleklenmis il ve ilce listeleri
        private List<PlaceInfo> _cachedStates;
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
                _httpClient.DefaultRequestHeaders.Add("UserName", "***");
                _httpClient.DefaultRequestHeaders.Add("SecretCode", "***");
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
        /// Turkiye'deki tum illeri getirir
        /// </summary>
        public async Task<List<PlaceInfo>> GetTumIller()
        {
            try
            {
                if (_cachedStates != null)
                    return _cachedStates;

                System.Diagnostics.Debug.WriteLine($"GetTumIller basliyor...");
                
                var response = await _httpClient.GetAsync($"api/Place/States/{TurkiyeCountryId}");
                
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
                    _cachedStates = apiResponse.Data;
                    System.Diagnostics.Debug.WriteLine($"{_cachedStates.Count} il basariyla yuklendi");
                    return _cachedStates;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetTumIller Hatasi: {ex.GetType().Name}: {ex.Message}");
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

        private static string NormalizeForSearch(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            return input.ToUpper(new System.Globalization.CultureInfo("tr-TR"))
                        .Replace("Ö", "O").Replace("Ü", "U").Replace("Ş", "S")
                        .Replace("Ç", "C").Replace("Ğ", "G").Replace("İ", "I").Replace("I", "I");
        }

        /// <summary>
        /// Sehir adindan ilce/city ID'sini bulur
        /// Mevcut PrayerTimesService ile uyumluluk icin korunuyor
        /// </summary>
        public async Task<int?> GetIlceIdBySehir(string sehirAdi, string ilceAdi = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetIlceIdBySehir cagrildi: {sehirAdi}/{ilceAdi}");
                
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
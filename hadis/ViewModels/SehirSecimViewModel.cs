using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hadis.Helpers;
using hadis.Models;
using hadis.Services;
using hadis.Data;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace hadis.ViewModels
{
    public partial class SehirSecimViewModel : ObservableObject
    {
        private readonly PrayerTimesService _prayerTimesService;
        private readonly NamazVaktiApiService _namazVaktiApiService;
        private List<City> _allCities = new();
        private List<City> _turkeyCities;
        private List<City> _azerbaijanCities;
        private List<City> _germanyCities;
        private List<City> _saudiArabiaCities;
        private List<City> _countriesList;
        private string _selectedCountryName;
        
        [ObservableProperty]
        private ObservableCollection<City> _filteredCities;

        [ObservableProperty]
        private string _searchText;

        [ObservableProperty]
        private string _title = "Konum Seç";

        [ObservableProperty]
        private string _searchPlaceholder = "Şehir ara...";

        [ObservableProperty]
        private bool _isBackVisible = true;

        [ObservableProperty]
        private bool _isLoading = false;

        private City? _selectedCityForDistrict;
        private bool _isSelectingDistrict;
        private bool _isSelectingCountry = true;

        [ObservableProperty]
        private City? _selectedCity;

        partial void OnSelectedCityChanged(City? value)
        {
            if (value is not null)
            {
                // Fire and forget, but handle carefully
                _ = HandleSelection(value);
            }
        }

        private async Task HandleSelection(City city)
        {
            await SelectCityAsync(city);
            // Reset selection to allow re-selecting the same item if needed (though we usually navigate away)
            // But since we might just switch list (SwitchToDistricts), we should clear selection.
            SelectedCity = null;
        }

        public SehirSecimViewModel(PrayerTimesService prayerTimesService, NamazVaktiApiService namazVaktiApiService = null)
        {
            _prayerTimesService = prayerTimesService;
            _namazVaktiApiService = namazVaktiApiService;
            InitializeCities();
        }

        private void InitializeCities()
        {
            _saudiArabiaCities = new List<City>
            {
                new City("Riyad", 24.7136, 46.6753),
                new City("Medine", 24.5247, 39.5692),
                new City("Mekke", 21.3891, 39.8579),
                new City("Cidde", 21.4858, 39.1925),
                new City("Ad Dammam", 26.4207, 50.0888),
                new City("Hail", 27.5114, 41.7208),
                new City("Shaqra", 25.2486, 45.2461),
                new City("Al Hufuf", 25.3647, 49.5876),
                new City("Buraydah", 26.3592, 43.9818),
                new City("Tabuk", 28.3838, 36.5662)
            };

            _germanyCities = new List<City>
            {
                new City("Berlin", 52.5200, 13.4050),
                new City("Hamburg", 53.5511, 9.9937),
                new City("München", 48.1351, 11.5820),
                new City("Köln", 50.9375, 6.9603),
                new City("Frankfurt am Main", 50.1109, 8.6821),
                new City("Stuttgart", 48.7758, 9.1829),
                new City("Düsseldorf", 51.2277, 6.7735),
                new City("Dortmund", 51.5136, 7.4653),
                new City("Essen", 51.4556, 7.0116),
                new City("Leipzig", 51.3397, 12.3731),
                new City("Bremen", 53.0793, 8.8017),
                new City("Dresden", 51.0504, 13.7373),
                new City("Hannover", 52.3759, 9.7320),
                new City("Nürnberg", 49.4521, 11.0767),
                new City("Duisburg", 51.4344, 6.7623)
            };

            _turkeyCities = new List<City>
            {
                new City("Adana", 37.0000, 35.3213),
                new City("Adıyaman", 37.7648, 38.2786),
                new City("Afyonkarahisar", 38.7507, 30.5567),
                new City("Ağrı", 39.7191, 43.0503),
                new City("Amasya", 40.6499, 35.8353),
                new City("Ankara", 39.9334, 32.8597),
                new City("Antalya", 36.8969, 30.7133),
                new City("Artvin", 41.1828, 41.8183),
                new City("Aydın", 37.8560, 27.8416),
                new City("Balıkesir", 39.6484, 27.8826),
                new City("Bilecik", 40.1451, 29.9799),
                new City("Bingöl", 38.8851, 40.4981),
                new City("Bitlis", 38.4006, 42.1095),
                new City("Bolu", 40.7350, 31.6061),
                new City("Burdur", 37.7204, 30.2908),
                new City("Bursa", 40.1885, 29.0610),
                new City("Çanakkale", 40.1553, 26.4142),
                new City("Çankırı", 40.6013, 33.6134),
                new City("Çorum", 40.5506, 34.9556),
                new City("Denizli", 37.7765, 29.0864),
                new City("Diyarbakır", 37.9144, 40.2306),
                new City("Edirne", 41.6771, 26.5557),
                new City("Elazığ", 38.6810, 39.2264),
                new City("Erzincan", 39.7500, 39.5000),
                new City("Erzurum", 39.9000, 41.2700),
                new City("Eskişehir", 39.7767, 30.5206),
                new City("Gaziantep", 37.0662, 37.3833),
                new City("Giresun", 40.9128, 38.3895),
                new City("Gümüşhane", 40.4600, 39.4700),
                new City("Hakkari", 37.5833, 43.7333),
                new City("Hatay", 36.4018, 36.3498),
                new City("Isparta", 37.7648, 30.5566),
                new City("Mersin", 36.8000, 34.6333),
                new City("İstanbul", 41.0082, 28.9784),
                new City("İzmir", 38.4189, 27.1287),
                new City("Kars", 40.6167, 43.1000),
                new City("Kastamonu", 41.3887, 33.7827),
                new City("Kayseri", 38.7312, 35.4787),
                new City("Kırklareli", 41.7333, 27.2167),
                new City("Kırşehir", 39.1425, 34.1709),
                new City("Kocaeli", 40.8533, 29.8815),
                new City("Konya", 37.8667, 32.4833),
                new City("Kütahya", 39.4167, 29.9833),
                new City("Malatya", 38.3552, 38.3095),
                new City("Manisa", 38.6191, 27.4289),
                new City("Kahramanmaraş", 37.5858, 36.9371),
                new City("Mardin", 37.3212, 40.7245),
                new City("Muğla", 37.2153, 28.3636),
                new City("Muş", 38.7432, 41.5064),
                new City("Nevşehir", 38.6244, 34.7144),
                new City("Niğde", 37.9667, 34.6833),
                new City("Ordu", 40.9839, 37.8764),
                new City("Rize", 41.0201, 40.5234),
                new City("Sakarya", 40.7569, 30.3783),
                new City("Samsun", 41.2867, 36.3300),
                new City("Siirt", 37.9333, 41.9500),
                new City("Sinop", 42.0231, 35.1531),
                new City("Sivas", 39.7477, 37.0179),
                new City("Tekirdağ", 40.9833, 27.5167),
                new City("Tokat", 40.3167, 36.5500),
                new City("Trabzon", 41.0028, 39.7167),
                new City("Tunceli", 39.1079, 39.5401),
                new City("Şanlıurfa", 37.1591, 38.7969),
                new City("Uşak", 38.6823, 29.4082),
                new City("Van", 38.4891, 43.4089),
                new City("Yozgat", 39.8181, 34.8147),
                new City("Zonguldak", 41.4564, 31.7987),
                new City("Aksaray", 38.3687, 34.0370),
                new City("Bayburt", 40.2552, 40.2249),
                new City("Karaman", 37.1759, 33.2287),
                new City("Kırıkkale", 39.8468, 33.5153),
                new City("Batman", 37.8812, 41.1351),
                new City("Şırnak", 37.5164, 42.4611),
                new City("Bartın", 41.6344, 32.3375),
                new City("Ardahan", 41.1105, 42.7022),
                new City("Iğdır", 39.9196, 44.0459),
                new City("Yalova", 40.6500, 29.2667),
                new City("Karabük", 41.2061, 32.6204),
                new City("Kilis", 36.7184, 37.1212),
                new City("Osmaniye", 37.0742, 36.2467),
                new City("D\u00fczce", 40.8438, 31.1565)
            };

            _azerbaijanCities = new List<City>
            {
                new City("Askeran", 0, 0),
                new City("Siyazan", 0, 0),
                new City("Qarasu", 0, 0),
                new City("Alı Bayranlı", 0, 0),
                new City("Salyan", 0, 0),
                new City("Mingacevir", 0, 0),
                new City("Tovuz", 0, 0),
                new City("Goycay", 0, 0),
                new City("Mastaga", 0, 0),
                new City("Sarur", 0, 0),
                new City("Xudat", 0, 0),
                new City("Baku", 0, 0),
                new City("Kuba", 0, 0),
                new City("Kusary", 0, 0),
                new City("Gence", 0, 0),
                new City("Seki", 0, 0),
                new City("Lachin", 0, 0),
                new City("Nahcivan", 0, 0),
                new City("Yevlax", 0, 0),
                new City("Agdam", 0, 0),
                new City("Kacmaz", 0, 0),
                new City("Sumqayit", 0, 0),
                new City("Astara", 0, 0),
                new City("Samaxi", 0, 0),
                new City("Ordubad", 0, 0),
                new City("Lenkeran", 0, 0),
                new City("Kazakh", 0, 0),
                new City("Sabirabad", 0, 0),
                new City("Susa", 0, 0),
                new City("Zakataly", 0, 0),
                new City("Kelbecer Rayonu", 0, 0),
                new City("Zangelan", 0, 0),
                new City("Horadız", 0, 0),
                new City("Agbend", 0, 0),
                new City("Cebrayil", 0, 0),
                new City("Fuzuli", 0, 0),
                new City("Zengilan", 0, 0),
            };

            _countriesList = new List<City>
            {
                new City("Türkiye", 0, 0),
                new City("Azerbaycan", 0, 0),
                new City("Almanya", 0, 0),
                new City("Suudi Arabistan", 0, 0)
            };

            SwitchToCountries();
        }

        partial void OnSearchTextChanged(string value)
        {
            var searchTerm = value?.ToLower();

            if (_isSelectingCountry)
            {
                var countries = _countriesList;
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    countries = countries.Where(c => c.Name.ToLower().Contains(searchTerm)).ToList();
                }
                FilteredCities = new ObservableCollection<City>(countries);
                return;
            }

            if (_isSelectingDistrict && _selectedCityForDistrict != null)
            {
                if (TurkeyDistricts.All.TryGetValue(_selectedCityForDistrict.Name, out var districts))
                {
                    if (string.IsNullOrWhiteSpace(searchTerm))
                    {
                        FilteredCities = new ObservableCollection<City>(districts.Select(d => new City(d, 0, 0)));
                    }
                    else
                    {
                        var filtered = districts.Where(d => d.ToLower().Contains(searchTerm)).Select(d => new City(d, 0, 0));
                        FilteredCities = new ObservableCollection<City>(filtered);
                    }
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                FilteredCities = new ObservableCollection<City>(_allCities.OrderBy(c => c.Name));
            }
            else
            {
                var filtered = _allCities.Where(c => c.Name.ToLower().Contains(searchTerm)).OrderBy(c => c.Name);
                FilteredCities = new ObservableCollection<City>(filtered);
            }
        }

        [RelayCommand]
        private async Task SelectCityAsync(City city)
        {
            if (city == null) return;

            if (_isSelectingCountry)
            {
                _selectedCountryName = city.Name;
                SwitchToCities();
            }
            else if (_isSelectingDistrict)
            {
                // Kullanıcı ilçe seçiyor → doğrudan kaydet
                await FinalizeSelection(city.Name);
            }
            else
            {
                // Kullanıcı şehir seçiyor → ilçe listesine geç (Türkiye için)
                SwitchToDistricts(city);
            }
        }

        private void SwitchToCountries()
        {
            _isSelectingCountry = true;
            _isSelectingDistrict = false;
            _selectedCountryName = null;
            
            Title = "\u00dclke Se\u00e7";
            SearchText = "";
            SearchPlaceholder = "\u00dclke ara...";
            
            FilteredCities = new ObservableCollection<City>(_countriesList);
        }

        private async void SwitchToDistricts(City city)
        {
            if (TurkeyDistricts.All.TryGetValue(city.Name, out var districts))
            {
                _selectedCityForDistrict = city;
                _isSelectingDistrict = true;

                Title = $"{city.Name} - İlçe Seç";
                SearchText = "";
                SearchPlaceholder = "İlçe ara...";

                await Task.Delay(50); // Prevent MAUI CollectionView binding crash from rapid double-updates due to SearchText

                var districtObjs = districts.OrderBy(d => d).Select(d => new City(d, 0, 0)).ToList();
                FilteredCities = new ObservableCollection<City>(districtObjs);
            }
            else
            {
                _selectedCityForDistrict = city;
                _ = FinalizeSelection(city.Name);
            }
        }

        private async void SwitchToCities()
        {
            _isSelectingCountry = false;
            _isSelectingDistrict = false;
            _selectedCityForDistrict = null;
            
            Title = "Konum Se\u00e7";
            SearchText = "";
            SearchPlaceholder = "Şehir ara...";

            _allCities = _selectedCountryName switch
            {
                "Azerbaycan" => _azerbaijanCities,
                "Almanya" => _germanyCities,
                "Suudi Arabistan" => _saudiArabiaCities,
                _ => _turkeyCities
            };

            await Task.Delay(50); // Prevent MAUI UI freeze
            FilteredCities = new ObservableCollection<City>(_allCities.OrderBy(c => c.Name));
        }

        private async Task FinalizeSelection(string district)
        {
            if (_selectedCityForDistrict == null) return;

            var mevcutAnaSehir = Preferences.Default.Get("ManuelSehir", string.Empty);
            
            if (string.IsNullOrEmpty(mevcutAnaSehir))
            {
                Preferences.Default.Set("ManuelSehir", _selectedCityForDistrict.Name);
                Preferences.Default.Set("ManuelIlce", district);
                Preferences.Default.Set("ManuelUlke", _selectedCountryName ?? "Türkiye");
                Preferences.Default.Set("ManuelLatitude", _selectedCityForDistrict.Latitude);
                Preferences.Default.Set("ManuelLongitude", _selectedCityForDistrict.Longitude);
                Preferences.Default.Set("OtomatikKonum", false);

                var sharedName = $"{AppInfo.PackageName}.xamarinessentials";
                Preferences.Set("ManuelLatitude", _selectedCityForDistrict.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture), sharedName);
                Preferences.Set("ManuelLongitude", _selectedCityForDistrict.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture), sharedName);
                Preferences.Set("OtomatikKonum", false, sharedName);
            }
            else
            {
                SaveAddedCity(_selectedCityForDistrict.Name, district, _selectedCountryName ?? "Türkiye", _selectedCityForDistrict.Latitude, _selectedCityForDistrict.Longitude);
            }
            
            _prayerTimesService.ClearCache();

#if ANDROID
            UpdateAndroidWidget();
#endif

            await Shell.Current.GoToAsync(".."); 
        }

#if ANDROID
        private void UpdateAndroidWidget()
        {
            try
            {
                var context = Android.App.Application.Context;
                var appWidgetManager = Android.Appwidget.AppWidgetManager.GetInstance(context);
                // Note: referencing specific types might require using statements.
                // Assuming namespace 'hadis.Platforms.Android' is available or qualified.
                // Since this is in ViewModels, it might not have visibility to Platforms folder types easily unless using partials or interface.
                // Safest to send MessagingCenter message.
                MessagingCenter.Send(this, "UpdateWidget");
            }
            catch { }
        }
#endif

        [RelayCommand]
        private async Task FindLocationAsync()
        {
            IsLoading = true;
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status != PermissionStatus.Granted)
                {
                    await App.Current.MainPage.DisplayAlert("İzin Gerekli", "Konum izni verilmedi.", "Tamam");
                    return; 
                }
                
                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));

                if (location != null)
                {
                    // Reverse Geocoding...
                    string cityName = null;
                    string countryName = null;
                    try
                    {
                        var placemarks = await Geocoding.Default.GetPlacemarksAsync(location.Latitude, location.Longitude);
                        var placemark = placemarks?.FirstOrDefault();
                        if (placemark != null)
                        {
                            cityName = placemark.AdminArea;
                            countryName = placemark.CountryName;
                        }
                    }
                    catch { }

                    // Determine the correct list based on the country
                    List<City> searchList = _turkeyCities; // default

                    if (!string.IsNullOrEmpty(countryName))
                    {
                        if (countryName.IndexOf("Azerbaijan", StringComparison.OrdinalIgnoreCase) >= 0 || countryName.IndexOf("Azerbaycan", StringComparison.OrdinalIgnoreCase) >= 0)
                            searchList = _azerbaijanCities;
                        else if (countryName.IndexOf("Germany", StringComparison.OrdinalIgnoreCase) >= 0 || countryName.IndexOf("Almanya", StringComparison.OrdinalIgnoreCase) >= 0)
                            searchList = _germanyCities;
                        else if (countryName.IndexOf("Saudi", StringComparison.OrdinalIgnoreCase) >= 0 || countryName.IndexOf("Arabia", StringComparison.OrdinalIgnoreCase) >= 0)
                            searchList = _saudiArabiaCities;
                    }

                    City foundCity = null;
                    if (!string.IsNullOrEmpty(cityName))
                    {
                        foundCity = searchList.FirstOrDefault(c => c.Name.Equals(cityName, StringComparison.OrdinalIgnoreCase));
                        if (foundCity == null)
                        {
                             foundCity = searchList.FirstOrDefault(c => c.Name.IndexOf(cityName, StringComparison.OrdinalIgnoreCase) >= 0);
                        }
                    }
                    
                    if (foundCity == null)
                    {
                        foundCity = searchList
                            .OrderBy(c => Location.CalculateDistance(location.Latitude, location.Longitude, c.Latitude, c.Longitude, DistanceUnits.Kilometers))
                            .FirstOrDefault();
                    }

                    if (foundCity != null)
                    {
                        _selectedCityForDistrict = foundCity; // Set context
                        await FinalizeSelection("Otomatik Konum"); // District as "Otomatik" or handle differently?
                        // Original code passed "Otomatik Konum" as district name, and passed 'true' for isAuto.
                        // I need to support 'isAuto' param in FinalizeSelection.
                        await FinalizeSelectionAuto(foundCity, location.Latitude, location.Longitude);
                    }
                    else
                    {
                         await App.Current.MainPage.DisplayAlert("Bulunamadı", "Şehir eşleştirilemedi.", "Tamam");
                    }
                }
                else
                {
                     await App.Current.MainPage.DisplayAlert("Hata", "Konum alınamadı.", "Tamam");
                }
            }
            catch (Exception ex)
            {
                await App.Current.MainPage.DisplayAlert("Hata", ex.Message, "Tamam");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task FinalizeSelectionAuto(City city, double lat, double lon)
        {
            var mevcutAnaSehir = Preferences.Default.Get("ManuelSehir", string.Empty);
            var mevcutAnaIlce = Preferences.Default.Get("ManuelIlce", string.Empty);

            bool isCurrentlyAuto = Preferences.Default.Get("OtomatikKonum", false) && string.IsNullOrEmpty(mevcutAnaSehir);

            // Eğer hali hazırda "manuel" bir ana konum varsa ve otomatik moda yeni geçiliyorsa, manuel konumu listeye kaydedelim
            if (!isCurrentlyAuto && !string.IsNullOrEmpty(mevcutAnaSehir) && mevcutAnaIlce != "Otomatik Konum")
            {
                double oldLat = 0.0;
                double oldLon = 0.0;
                try { oldLat = Preferences.Default.Get("ManuelLatitude", lat); } catch { }
                try { oldLon = Preferences.Default.Get("ManuelLongitude", lon); } catch { }
                
                SaveAddedCity(mevcutAnaSehir, mevcutAnaIlce, Preferences.Default.Get("ManuelUlke", "Türkiye"), oldLat, oldLon);
            }

            // Otomatik konumun özel adı veya verisi listeye KAYDEDİLMİYOR.
            // Sadece Otomatik Konum modu genel olarak aktif ediliyor.
            Preferences.Default.Remove("ManuelSehir");
            Preferences.Default.Remove("ManuelIlce");
            Preferences.Default.Remove("ManuelUlke");
            
            Preferences.Default.Set("ManuelLatitude", lat);
            Preferences.Default.Set("ManuelLongitude", lon);
            Preferences.Default.Set("OtomatikKonum", true);

            var sharedName = $"{AppInfo.PackageName}.xamarinessentials";
            Preferences.Set("ManuelLatitude", lat.ToString(System.Globalization.CultureInfo.InvariantCulture), sharedName);
            Preferences.Set("ManuelLongitude", lon.ToString(System.Globalization.CultureInfo.InvariantCulture), sharedName);
            Preferences.Set("OtomatikKonum", true, sharedName);

            _prayerTimesService.ClearCache();
            
#if ANDROID
             MessagingCenter.Send(this, "UpdateWidget");
#endif
            await Shell.Current.GoToAsync("..");
        }

        private void SaveAddedCity(string sehir, string ilce, string ulke, double latitude, double longitude)
        {
            try
            {
                var json = Preferences.Default.Get(AppConstants.PREF_ADDED_CITIES, string.Empty);
                var addedCities = string.IsNullOrWhiteSpace(json)
                    ? new List<AddedCity>()
                    : JsonSerializer.Deserialize<List<AddedCity>>(json) ?? new List<AddedCity>();

                var existing = addedCities.FirstOrDefault(x =>
                    x.Sehir.Equals(sehir, StringComparison.OrdinalIgnoreCase) &&
                    x.Ilce.Equals(ilce, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    addedCities.Remove(existing);
                }

                // Listeye en başa ekle (son eklenen en üstte)
                addedCities.Insert(0, new AddedCity
                {
                    Sehir = sehir,
                    Ilce = ilce,
                    Ulke = ulke,
                    Latitude = latitude,
                    Longitude = longitude
                });

                // Toplamda en fazla 2 konum tutulmasını sağla (1 Ana Konum, 1 Eklenen Konum)
                if (addedCities.Count > 2)
                {
                    addedCities = addedCities.Take(2).ToList();
                }

                Preferences.Default.Set(AppConstants.PREF_ADDED_CITIES, JsonSerializer.Serialize(addedCities));
            }
            catch
            {
                // ignore malformed cache
            }
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            if (_isSelectingDistrict)
            {
                SwitchToCities();
            }
            else if (!_isSelectingCountry)
            {
                SwitchToCountries();
            }
            else
            {
                await Shell.Current.GoToAsync("..");
            }
        }

        public bool TryHandleBack()
        {
            if (_isSelectingDistrict)
            {
                SwitchToCities();
                return true;
            }
            else if (!_isSelectingCountry)
            {
                SwitchToCountries();
                return true;
            }
            return false;
        }
    }
}

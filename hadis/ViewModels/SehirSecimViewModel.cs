using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hadis.Models;
using hadis.Services;
using hadis.Data;
using System.Collections.ObjectModel;

namespace hadis.ViewModels
{
    public partial class SehirSecimViewModel : ObservableObject
    {
        private readonly PrayerTimesService _prayerTimesService;
        private List<City> _allCities;
        
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

        private City? _selectedCityForDistrict;
        private bool _isSelectingDistrict;

        public SehirSecimViewModel(PrayerTimesService prayerTimesService)
        {
            _prayerTimesService = prayerTimesService;
            InitializeCities();
        }

        private void InitializeCities()
        {
            _allCities = new List<City>
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
                new City("Düzce", 40.8438, 31.1565)
            };

            FilteredCities = new ObservableCollection<City>(_allCities.OrderBy(c => c.Name));
        }

        partial void OnSearchTextChanged(string value)
        {
            var searchTerm = value?.ToLower();

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

            if (_isSelectingDistrict)
            {
                await FinalizeSelection(city.Name);
            }
            else
            {
                SwitchToDistricts(city);
            }
        }

        private void SwitchToDistricts(City city)
        {
            if (TurkeyDistricts.All.TryGetValue(city.Name, out var districts))
            {
                _selectedCityForDistrict = city;
                _isSelectingDistrict = true;
                
                Title = $"{city.Name} - İlçe Seç";
                SearchText = "";
                SearchPlaceholder = "İlçe ara...";
                
                var districtObjs = districts.OrderBy(d => d).Select(d => new City(d, 0, 0)).ToList();
                FilteredCities = new ObservableCollection<City>(districtObjs);
            }
            else
            {
                _ = FinalizeSelection(city.Name);
            }
        }

        private void SwitchToCities()
        {
            _isSelectingDistrict = false;
            _selectedCityForDistrict = null;
            
            Title = "Konum Seç";
            SearchText = "";
            SearchPlaceholder = "Şehir ara...";
            
            FilteredCities = new ObservableCollection<City>(_allCities.OrderBy(c => c.Name));
        }

        private async Task FinalizeSelection(string district)
        {
            if (_selectedCityForDistrict == null) return;

            // Kayıt İşlemleri
            Preferences.Default.Set("ManuelSehir", _selectedCityForDistrict.Name);
            Preferences.Default.Set("ManuelIlce", district);
            Preferences.Default.Set("ManuelLatitude", _selectedCityForDistrict.Latitude);
            Preferences.Default.Set("ManuelLongitude", _selectedCityForDistrict.Longitude);
            Preferences.Default.Set("OtomatikKonum", false);
            
            // Widget için
            var sharedName = $"{AppInfo.PackageName}.xamarinessentials";
            Preferences.Set("ManuelLatitude", _selectedCityForDistrict.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture), sharedName);
            Preferences.Set("ManuelLongitude", _selectedCityForDistrict.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture), sharedName);
            Preferences.Set("OtomatikKonum", false, sharedName);

            // Cache Temizle
            _prayerTimesService.ClearCache();

            // Widget Güncelleme Sinyali (Android Only logic is in Platform specific code, invoked via MessagingCenter or dependency, typically handled by View or Service, but simpler: fire MainThread invoke or similar)
            // MVVM'de direkt Android API çağıramayız.
            // Ama basitlik için burada DependencyService veya MessagingCenter kullanabiliriz.
            // Veya sadece Page tarafında bu işi yapabiliriz? 
            // Better: Invoke an event or MessagingCenter.
            // For now, assume Widget Update is handled by app resuming or similar, OR invoke specific platform code via conditional compilation inside VM (ugly but works in MAUI shared project).
            
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
                    try
                    {
                        var placemarks = await Geocoding.Default.GetPlacemarksAsync(location.Latitude, location.Longitude);
                        var placemark = placemarks?.FirstOrDefault();
                        if (placemark != null) cityName = placemark.AdminArea;
                    }
                    catch { }

                    City foundCity = null;
                    if (!string.IsNullOrEmpty(cityName))
                    {
                        foundCity = _allCities.FirstOrDefault(c => c.Name.Equals(cityName, StringComparison.OrdinalIgnoreCase));
                        if (foundCity == null)
                        {
                             foundCity = _allCities.FirstOrDefault(c => c.Name.IndexOf(cityName, StringComparison.OrdinalIgnoreCase) >= 0);
                        }
                    }
                    
                    if (foundCity == null)
                    {
                        foundCity = _allCities
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
        }

        private async Task FinalizeSelectionAuto(City city, double lat, double lon)
        {
            Preferences.Default.Set("ManuelSehir", city.Name);
            Preferences.Default.Set("ManuelIlce", "Otomatik Konum");
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

        [RelayCommand]
        private async Task GoBackAsync()
        {
            if (_isSelectingDistrict)
            {
                SwitchToCities();
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
            return false;
        }
    }
}

using hadis.Models;
using System.Collections.ObjectModel;

namespace hadis
{
    public partial class SehirSecim : ContentPage
    {
        private ObservableCollection<City> _allCities;
        private ObservableCollection<City> _filteredCities;

        public SehirSecim()
        {
            InitializeComponent();
            InitializeCities();
        }

        private void InitializeCities()
        {
            _allCities = new ObservableCollection<City>
            {
                new City("Otomatik Konum (GPS)", 0, 0),
                new City("Adana", 37.0017, 35.3289),
                new City("Adýyaman", 37.7636, 38.2765),
                new City("Afyonkarahisar", 38.7507, 30.5567),
                new City("Aðrý", 39.7191, 43.0503),
                new City("Amasya", 40.6499, 35.8353),
                new City("Ankara", 39.9334, 32.8597),
                new City("Antalya", 36.8969, 30.7133),
                new City("Artvin", 41.1828, 41.8183),
                new City("Aydýn", 37.8444, 27.8458),
                new City("Balýkesir", 39.6484, 27.8826),
                new City("Bilecik", 40.1500, 29.9833),
                new City("Bingöl", 38.8854, 40.4983),
                new City("Bitlis", 38.4001, 42.1083),
                new City("Bolu", 40.7392, 31.6061),
                new City("Burdur", 37.7267, 30.2900),
                new City("Bursa", 40.1826, 29.0665),
                new City("Çanakkale", 40.1553, 26.4142),
                new City("Çankýrý", 40.6013, 33.6134),
                new City("Çorum", 40.5506, 34.9556),
                new City("Denizli", 37.7765, 29.0864),
                new City("Diyarbakýr", 37.9144, 40.2306),
                new City("Edirne", 41.6771, 26.5557),
                new City("Elazýð", 38.6748, 39.2226),
                new City("Erzincan", 39.7500, 39.5000),
                new City("Erzurum", 39.9000, 41.2700),
                new City("Eskiþehir", 39.7767, 30.5206),
                new City("Gaziantep", 37.0662, 37.3833),
                new City("Giresun", 40.9128, 38.3895),
                new City("Gümüþhane", 40.4386, 39.5086),
                new City("Hakkari", 37.5744, 43.7408),
                new City("Hatay", 36.4018, 36.3498),
                new City("Isparta", 37.7648, 30.5566),
                new City("Mersin", 36.8121, 34.6415),
                new City("Ýstanbul", 41.0082, 28.9784),
                new City("Ýzmir", 38.4237, 27.1428),
                new City("Kars", 40.6167, 43.1000),
                new City("Kastamonu", 41.3887, 33.7827),
                new City("Kayseri", 38.7205, 35.4826),
                new City("Kýrklareli", 41.7333, 27.2167),
                new City("Kýrþehir", 39.1458, 34.1709),
                new City("Kocaeli", 40.8533, 29.8815),
                new City("Konya", 37.8667, 32.4833),
                new City("Kütahya", 39.4167, 29.9833),
                new City("Malatya", 38.3552, 38.3095),
                new City("Manisa", 38.6191, 27.4289),
                new City("Kahramanmaraþ", 37.5858, 36.9371),
                new City("Mardin", 37.3212, 40.7350),
                new City("Muðla", 37.2153, 28.3636),
                new City("Muþ", 38.7432, 41.5064),
                new City("Nevþehir", 38.6939, 34.6857),
                new City("Niðde", 37.9667, 34.6833),
                new City("Ordu", 40.9839, 37.8764),
                new City("Rize", 41.0201, 40.5234),
                new City("Sakarya", 40.7569, 30.3783),
                new City("Samsun", 41.2867, 36.3300),
                new City("Siirt", 37.9333, 41.9500),
                new City("Sinop", 42.0267, 35.1550),
                new City("Sivas", 39.7477, 37.0179),
                new City("Tekirdað", 40.9833, 27.5167),
                new City("Tokat", 40.3167, 36.5500),
                new City("Trabzon", 41.0015, 39.7178),
                new City("Tunceli", 39.1079, 39.5401),
                new City("Þanlýurfa", 37.1591, 38.7969),
                new City("Uþak", 38.6823, 29.4082),
                new City("Van", 38.4891, 43.4089),
                new City("Yozgat", 39.8181, 34.8147),
                new City("Zonguldak", 41.4564, 31.7987),
                new City("Aksaray", 38.3687, 34.0370),
                new City("Bayburt", 40.2552, 40.2249),
                new City("Karaman", 37.1759, 33.2287),
                new City("Kýrýkkale", 39.8468, 33.5153),
                new City("Batman", 37.8812, 41.1351),
                new City("Þýrnak", 37.4187, 42.4918),
                new City("Bartýn", 41.5811, 32.4610),
                new City("Ardahan", 41.1105, 42.7022),
                new City("Iðdýr", 39.8880, 44.0048),
                new City("Yalova", 40.6500, 29.2667),
                new City("Karabük", 41.2061, 32.6204),
                new City("Kilis", 36.7184, 37.1212),
                new City("Osmaniye", 37.0742, 36.2478),
                new City("Düzce", 40.8438, 31.1565)
            };

            _filteredCities = new ObservableCollection<City>(_allCities);
            CitiesCollectionView.ItemsSource = _filteredCities;
        }

        private void SearchBar_TextChanged(object? sender, TextChangedEventArgs e)
        {
            string searchText = e.NewTextValue?.ToLowerInvariant() ?? string.Empty;

            _filteredCities.Clear();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                foreach (var city in _allCities)
                {
                    _filteredCities.Add(city);
                }
            }
            else
            {
                var filtered = _allCities
                    .Where(c => c.Name.ToLowerInvariant().Contains(searchText))
                    .ToList();

                foreach (var city in filtered)
                {
                    _filteredCities.Add(city);
                }
            }
        }

        private async void CitiesCollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is City selectedCity)
            {
                // Seçili þehri kaydet
                Preferences.Default.Set("ManuelLatitude", selectedCity.Latitude);
                Preferences.Default.Set("ManuelLongitude", selectedCity.Longitude);
                Preferences.Default.Set("ManuelSehir", selectedCity.Name);
                
                // Widget'ý güncelle
                UpdateWidget();

                // Seçimi temizle (animasyon için)
                CitiesCollectionView.SelectedItem = null;

                // Geri dön
                await Navigation.PopAsync();
            }
        }

        private async void OnCityTapped(object? sender, EventArgs e)
        {
            if (sender is Grid grid && grid.BindingContext is City selectedCity)
            {
                // Animasyon ekle
                await grid.ScaleTo(0.95, 50);
                await grid.ScaleTo(1.0, 50);

                // Otomatik Konum seçildi mi kontrol et
                if (selectedCity.Latitude == 0 && selectedCity.Longitude == 0)
                {
                    // Otomatik konum aktif et
                    Preferences.Default.Set("OtomatikKonum", true);
                    Preferences.Default.Remove("ManuelLatitude");
                    Preferences.Default.Remove("ManuelLongitude");
                    Preferences.Default.Remove("ManuelSehir");
                }
                else
                {
                    // Manuel konum kaydet
                    Preferences.Default.Set("OtomatikKonum", false);
                    Preferences.Default.Set("ManuelLatitude", selectedCity.Latitude);
                    Preferences.Default.Set("ManuelLongitude", selectedCity.Longitude);
                    Preferences.Default.Set("ManuelSehir", selectedCity.Name);
                }
                
                // Widget'ý güncelle
                UpdateWidget();

                // Geri dön
                await Navigation.PopAsync();
            }
        }
        
        private void UpdateWidget()
        {
#if ANDROID
            try
            {
                var context = Android.App.Application.Context;
                var appWidgetManager = Android.Appwidget.AppWidgetManager.GetInstance(context);
                var componentName = new Android.Content.ComponentName(context, 
                    Java.Lang.Class.FromType(typeof(Platforms.Android.ClockWeatherWidget)));
                var appWidgetIds = appWidgetManager?.GetAppWidgetIds(componentName);

                if (appWidgetIds != null && appWidgetIds.Length > 0)
                {
                    // Widget'larý güncelle
                    var intent = new Android.Content.Intent(context, 
                        typeof(Platforms.Android.ClockWeatherWidget));
                    intent.SetAction(Android.Appwidget.AppWidgetManager.ActionAppwidgetUpdate);
                    intent.PutExtra(Android.Appwidget.AppWidgetManager.ExtraAppwidgetIds, appWidgetIds);
                    context.SendBroadcast(intent);
                    
                    System.Diagnostics.Debug.WriteLine("Widget guncelleme broadcast gonderildi");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Widget guncelleme hatasi: {ex.Message}");
            }
#endif
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hadis.Models;
using hadis.Helpers;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace hadis.ViewModels
{
    public partial class KonumViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;

        public KonumViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            AddedLocations = new ObservableCollection<AddedCity>();
            LoadAddedLocations();
        }

        [ObservableProperty]
        private ObservableCollection<AddedCity> addedLocations;

        [ObservableProperty]
        private AddedCity? selectedLocation;

        public void LoadAddedLocations()
        {
            try
            {
                AddedLocations.Clear();

                var temp = new List<AddedCity>();

                var mainCityName = Preferences.Default.Get("ManuelSehir", string.Empty);
                var mainDistrictName = Preferences.Default.Get("ManuelIlce", string.Empty);

                double latitude = 0;
                double longitude = 0;
                try { latitude = Preferences.Default.Get("ManuelLatitude", 0.0); } catch { }
                try { longitude = Preferences.Default.Get("ManuelLongitude", 0.0); } catch { }

                if (!string.IsNullOrEmpty(mainCityName))
                {
                    temp.Add(new AddedCity
                    {
                        Sehir = mainCityName,
                        Ilce = mainDistrictName,
                        Latitude = latitude,
                        Longitude = longitude,
                        Ulke = "Ana Konum"
                    });
                }

                var json = Preferences.Default.Get(AppConstants.PREF_ADDED_CITIES, string.Empty);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    try
                    {
                        var extraCities = JsonSerializer.Deserialize<List<AddedCity>>(json);
                        if (extraCities != null)
                        {
                            foreach (var city in extraCities)
                            {
                                if (!temp.Any(x => string.Equals(x.Sehir, city.Sehir, StringComparison.OrdinalIgnoreCase) && 
                                                   string.Equals(x.Ilce, city.Ilce, StringComparison.OrdinalIgnoreCase)))
                                {
                                    temp.Add(city);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to load added cities: {ex.Message}");
                    }
                }

                foreach (var c in temp)
                {
                    AddedLocations.Add(c);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ana yükleme hatasý: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task AddLocation()
        {
            try
            {
                var nav = Application.Current?.MainPage?.Navigation;
                if (nav != null)
                {
                    var sehirSecimPage = _serviceProvider.GetRequiredService<SehirSecim>();
                    await nav.PushAsync(sehirSecimPage);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation Error: {ex.Message}");
            }
        }

        [RelayCommand]
        private void RemoveLocation(AddedCity cityToRemove)
        {
            if (cityToRemove == null) return;

            bool isMain = cityToRemove.Ulke == "Ana Konum";

            if (isMain)
            {
                Preferences.Default.Remove("ManuelSehir");
                Preferences.Default.Remove("ManuelIlce");
                Preferences.Default.Remove("ManuelLatitude");
                Preferences.Default.Remove("ManuelLongitude");
            }
            else
            {
                var json = Preferences.Default.Get(AppConstants.PREF_ADDED_CITIES, string.Empty);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var extraCities = JsonSerializer.Deserialize<List<AddedCity>>(json);
                    if (extraCities != null)
                    {
                        var item = extraCities.FirstOrDefault(c => string.Equals(c.Sehir, cityToRemove.Sehir, StringComparison.OrdinalIgnoreCase) && 
                                                                     string.Equals(c.Ilce, cityToRemove.Ilce, StringComparison.OrdinalIgnoreCase));
                        if (item != null)
                        {
                            extraCities.Remove(item);
                            Preferences.Default.Set(AppConstants.PREF_ADDED_CITIES, JsonSerializer.Serialize(extraCities));
                        }
                    }
                }
            }

            AddedLocations.Remove(cityToRemove);
        }

        [RelayCommand]
        private async Task SelectLocation(AddedCity city)
        {
            if (city == null) return;

            Preferences.Default.Set("ManuelSehir", city.Sehir);
            Preferences.Default.Set("ManuelIlce", city.Ilce);
            Preferences.Default.Set("ManuelLatitude", city.Latitude);
            Preferences.Default.Set("ManuelLongitude", city.Longitude);

            var nav = Application.Current?.MainPage?.Navigation;
            if (nav != null)
            {
                // Go back to main page (as we popped up from main)
                await nav.PopToRootAsync();
            }
        }
    }
}
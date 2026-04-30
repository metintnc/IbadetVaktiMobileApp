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
                bool isAuto = Preferences.Default.Get("OtomatikKonum", false);
                
                double latitude = 0;
                double longitude = 0;
                try { latitude = Preferences.Default.Get("ManuelLatitude", 0.0); } catch { }
                try { longitude = Preferences.Default.Get("ManuelLongitude", 0.0); } catch { }

                if (isAuto && string.IsNullOrEmpty(mainCityName))
                {
                    temp.Add(new AddedCity
                    {
                        Sehir = "GPS / Ge�erli Konum",
                        Ilce = "Otomatik Konum",
                        Latitude = latitude,
                        Longitude = longitude,
                        Ulke = "Ana Konum"
                    });
                }
                else if (!string.IsNullOrEmpty(mainCityName))
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
                System.Diagnostics.Debug.WriteLine($"Ana y�kleme hatas�: {ex.Message}");
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
                Preferences.Default.Set("OtomatikKonum", false);

                // Otomatik konum cache'lerini de temizle
                Preferences.Default.Remove("LastAutoSehir");
                Preferences.Default.Remove("LastAutoIlce");
                Preferences.Default.Remove("LastAutoLatitude");
                Preferences.Default.Remove("LastAutoLongitude");

                // Shared preferences'i da guncelle (widget icin)
                var sharedName = $"{AppInfo.PackageName}.xamarinessentials";
                Preferences.Set("OtomatikKonum", false, sharedName);
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
            
            if (city.Ulke == "Ana Konum")
            {
                var n = Application.Current?.MainPage?.Navigation;
                if (n != null)
                {
                    await n.PopToRootAsync();
                }
                return;
            }

            // E�er eski konum bir manuel konumsa, onu ekstra listeye al�yoruz
            var oldSehir = Preferences.Default.Get("ManuelSehir", string.Empty);
            var oldIlce = Preferences.Default.Get("ManuelIlce", string.Empty);
            bool oldIsAuto = Preferences.Default.Get("OtomatikKonum", false);

            var json = Preferences.Default.Get(AppConstants.PREF_ADDED_CITIES, string.Empty);
            var saved = string.IsNullOrWhiteSpace(json) ? new List<AddedCity>() : JsonSerializer.Deserialize<List<AddedCity>>(json) ?? new List<AddedCity>();

            // Yeni se�ileni listeden ��kar (��nk� Ana Konum olacak)
            saved.RemoveAll(c => string.Equals(c.Sehir, city.Sehir, StringComparison.OrdinalIgnoreCase) && string.Equals(c.Ilce, city.Ilce, StringComparison.OrdinalIgnoreCase));

            // E�er eskinin �zerine yaz�l�yorsa eskiyi listeye aktar
            if (!oldIsAuto && !string.IsNullOrEmpty(oldSehir))
            {
                double oldLat = 0; double oldLon = 0;
                try { oldLat = Preferences.Default.Get("ManuelLatitude", 0.0); } catch {}
                try { oldLon = Preferences.Default.Get("ManuelLongitude", 0.0); } catch {}
                
                saved.Insert(0, new AddedCity
                {
                    Sehir = oldSehir,
                    Ilce = oldIlce,
                    Ulke = Preferences.Default.Get("ManuelUlke", "T�rkiye"),
                    Latitude = oldLat,
                    Longitude = oldLon
                });
            }

            if (saved.Count > 2)
            {
                saved = saved.Take(2).ToList();
            }

            Preferences.Default.Set(AppConstants.PREF_ADDED_CITIES, JsonSerializer.Serialize(saved));

            Preferences.Default.Set("ManuelSehir", city.Sehir);
            Preferences.Default.Set("ManuelIlce", city.Ilce);
            Preferences.Default.Set("ManuelLatitude", city.Latitude);
            Preferences.Default.Set("ManuelLongitude", city.Longitude);
            Preferences.Default.Set("OtomatikKonum", false);

            var nav = Application.Current?.MainPage?.Navigation;
            if (nav != null)
            {
                await nav.PopToRootAsync();
            }
        }
    }
}

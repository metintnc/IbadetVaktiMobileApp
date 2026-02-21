using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using hadis.Helpers;
using hadis.Models;
using hadis.Services;
using System.Text.Json;

namespace hadis.ViewModels
{
    public partial class MainPageViewModel : ObservableObject, IDisposable
    {
        private readonly BackgroundService _backgroundService;
        private readonly ThemeService _themeService;
        private readonly StatusBarService _statusBarService;
        private readonly IAppNotificationService _notificationService;
        private readonly PrayerTimesService _prayerTimesService;
        private readonly System.Timers.Timer _timer;
        private Dictionary<string, DateTime>? _namazVakitleri;
        private bool _prefetchDone;

        // --- Observable Properties: Geri Sayım ---
        [ObservableProperty]
        private string _namazIsmi = "";

        [ObservableProperty]
        private string _kalanSure = " - - ";

        // --- Observable Properties: Konum ---
        [ObservableProperty]
        private string _konumText = "";

        // --- Observable Properties: Hicri Tarih ---
        [ObservableProperty]
        private string _hicriTarih = "";

        // --- Observable Properties: Ayet ---
        [ObservableProperty]
        private string _gununAyeti = "";

        // --- Observable Properties: Namaz Vakitleri ---
        [ObservableProperty]
        private string _imsakVakit = "";

        [ObservableProperty]
        private string _gunesVakit = "";

        [ObservableProperty]
        private string _ogleVakit = "";

        [ObservableProperty]
        private string _ikindiVakit = "";

        [ObservableProperty]
        private string _aksamVakit = "";

        [ObservableProperty]
        private string _yatsiVakit = "";

        // --- Observable Properties: Hata Durumları ---
        [ObservableProperty]
        private bool _isInternetErrorVisible;

        [ObservableProperty]
        private bool _isLocationErrorVisible;

        [ObservableProperty]
        private string _errorTitle = "İnternet Bağlantısı Yok";

        [ObservableProperty]
        private string _errorDescription = "Namaz vakitlerini güncellemek için lütfen internet bağlantınızı kontrol ediniz.";

        // --- Observable Properties: Aktif namaz vurgulama ---
        [ObservableProperty]
        private Color _imsakVakitColor = Colors.White;

        [ObservableProperty]
        private Color _gunesVakitColor = Colors.White;

        [ObservableProperty]
        private Color _ogleVakitColor = Colors.White;

        [ObservableProperty]
        private Color _ikindiVakitColor = Colors.White;

        [ObservableProperty]
        private Color _aksamVakitColor = Colors.White;

        [ObservableProperty]
        private Color _yatsiVakitColor = Colors.White;

        // Dış erişim: BackgroundService ve ThemeService (tema/arkaplan UI işlemleri için code-behind kullanacak)
        public BackgroundService BackgroundService => _backgroundService;
        public ThemeService ThemeService => _themeService;
        public StatusBarService StatusBarService => _statusBarService;

        // Event: Page'e Widget güncelleme sinyali
        public event Action? WidgetUpdateRequested;

        // Event: Konum hatası → SehirSecim sayfasına yönlendir
        public event Action? NavigateToSehirSecim;

        // Namaz vakitleri dışarıya (notification scheduling için)
        public Dictionary<string, DateTime>? NamazVakitleri => _namazVakitleri;

        public MainPageViewModel(
            BackgroundService backgroundService,
            ThemeService themeService,
            StatusBarService statusBarService,
            IAppNotificationService notificationService,
            PrayerTimesService prayerTimesService)
        {
            _backgroundService = backgroundService;
            _themeService = themeService;
            _statusBarService = statusBarService;
            _notificationService = notificationService;
            _prayerTimesService = prayerTimesService;

            // Timer başlat
            _timer = new System.Timers.Timer(AppConstants.TIMER_INTERVAL_MS);
            _timer.Elapsed += (s, e) => MainThread.BeginInvokeOnMainThread(UpdateCountdown);
            _timer.Start();
        }

        /// <summary>
        /// Tüm verileri yükle (OnAppearing'de çağrılır)
        /// </summary>
        [RelayCommand]
        private async Task LoadDataAsync()
        {
            HicriTarih = PrayerTimeHelper.GetHicriTarih();
            GununAyeti = PrayerTimeHelper.GetDailyAyet();

            // Önce konumu al, sonra namaz vakitlerini (varsa konumla) çek
            var location = await LoadKonumBilgisiAsync();
            await FetchPrayerTimesAsync(location);
        }

        /// <summary>
        /// Konum bilgisini göster ve bulunan konumu döndür
        /// </summary>
        private async Task<Location?> LoadKonumBilgisiAsync()
        {
            Location? foundLocation = null;
            try
            {
                bool otomatikKonum = Preferences.Default.Get("OtomatikKonum", true);

                if (!otomatikKonum)
                {
                    string manuelSehir = Preferences.Default.Get("ManuelSehir", "");
                    string manuelIlce = Preferences.Default.Get("ManuelIlce", "");

                    if (!string.IsNullOrEmpty(manuelSehir))
                    {
                        KonumText = (!string.IsNullOrEmpty(manuelIlce) && manuelIlce != manuelSehir)
                            ? $"{manuelIlce} / {manuelSehir}"
                            : manuelSehir;
                        
                        // Manuel modda koordinatları pref'ten alıp location objesi oluşturabiliriz
                        double lat = Preferences.Default.Get("ManuelLatitude", 0.0);
                        double lon = Preferences.Default.Get("ManuelLongitude", 0.0);
                        return new Location(lat, lon);
                    }
                }

                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status != PermissionStatus.Granted)
                {
                    KonumText = "Konum İzni Verilmedi";
                    return null;
                }

                foundLocation = await Geolocation.GetLastKnownLocationAsync();
                if (foundLocation == null)
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                    foundLocation = await Geolocation.GetLocationAsync(request);
                }

                if (foundLocation != null)
                {
                    try
                    {
                        var placemarks = await Geocoding.Default.GetPlacemarksAsync(foundLocation.Latitude, foundLocation.Longitude);
                        var placemark = placemarks?.FirstOrDefault();

                        if (placemark != null)
                        {
                            string il = placemark.AdminArea ?? "";
                            string ilce = placemark.SubAdminArea ?? placemark.Locality ?? "";

                            if (!string.IsNullOrEmpty(il) && !string.IsNullOrEmpty(ilce))
                                KonumText = $"{ilce} / {il}";
                            else if (!string.IsNullOrEmpty(il))
                                KonumText = il;
                            else
                                KonumText = $"Lat: {foundLocation.Latitude:F2}, Lon: {foundLocation.Longitude:F2}";
                        }
                        else
                        {
                            KonumText = $"Lat: {foundLocation.Latitude:F2}, Lon: {foundLocation.Longitude:F2}";
                        }
                    }
                    catch
                    {
                        KonumText = $"Lat: {foundLocation.Latitude:F2}, Lon: {foundLocation.Longitude:F2}";
                    }
                }
                else
                {
                    KonumText = "Konum Alınamadı";
                }
            }
            catch (FeatureNotSupportedException)
            {
                KonumText = "Konum Desteklenmiyor";
            }
            catch (PermissionException)
            {
                KonumText = "Konum İzni Gerekli";
            }
            catch
            {
                KonumText = "Konum Hatası";
            }

            return foundLocation;
        }

        /// <summary>
        /// Namaz vakitlerini çek ve güncelle
        /// </summary>
        /// <summary>
        /// Namaz vakitlerini çek ve güncelle
        /// </summary>
        public async Task FetchPrayerTimesAsync(Location? locationOverride = null)
        {
            try
            {
                string ilce = "";
                string sehir = "";
                double? latitude = null;
                double? longitude = null;
                bool otomatikKonum = Preferences.Default.Get("OtomatikKonum", true);
                bool manuelKonumVar = !otomatikKonum && !string.IsNullOrEmpty(Preferences.Default.Get("ManuelSehir", ""));

                if (locationOverride != null)
                {
                     // Override varsa direkt koordinatları kullan
                     latitude = locationOverride.Latitude;
                     longitude = locationOverride.Longitude;
                     
                     // Helper variable for logic flow
                     sehir = "?"; // Will be filled later or ignored if coordinates used
                     ilce = "?";
                }
                else if (!manuelKonumVar)
                {
                    var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        IsLocationErrorVisible = true;
                        IsInternetErrorVisible = false;
                        return;
                    }
                }

                if (!otomatikKonum)
                {
                    sehir = Preferences.Default.Get("ManuelSehir", "");
                    ilce = Preferences.Default.Get("ManuelIlce", "");

                    try
                    {
                        latitude = Preferences.Default.Get("ManuelLatitude", 0.0);
                        longitude = Preferences.Default.Get("ManuelLongitude", 0.0);
                    }
                    catch
                    {
                        if (double.TryParse(Preferences.Default.Get("ManuelLatitude", "0"),
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out double l1) &&
                            double.TryParse(Preferences.Default.Get("ManuelLongitude", "0"),
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out double l2))
                        {
                            latitude = l1;
                            longitude = l2;
                        }
                    }
                }
                else if (locationOverride == null)
                {
                    var konum = await Geolocation.GetLastKnownLocationAsync();
                    if (konum == null)
                    {
                        var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(3));
                        konum = await Geolocation.GetLocationAsync(request);
                    }

                    if (konum != null)
                    {
                        var sharedName = $"{AppInfo.PackageName}.xamarinessentials";
                        Preferences.Set("ManuelLatitude",
                            konum.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture), sharedName);
                        Preferences.Set("ManuelLongitude",
                            konum.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture), sharedName);

                        Preferences.Default.Set("ManuelLatitude", konum.Latitude);
                        Preferences.Default.Set("ManuelLongitude", konum.Longitude);

                        latitude = konum.Latitude;
                        longitude = konum.Longitude;

                        WidgetUpdateRequested?.Invoke();

                        try
                        {
                            var placemarks = await Geocoding.Default.GetPlacemarksAsync(konum.Latitude, konum.Longitude);
                            var placemark = placemarks?.FirstOrDefault();
                            if (placemark != null)
                            {
                                sehir = placemark.AdminArea ?? "";
                                ilce = placemark.SubAdminArea ?? placemark.Locality ?? "";
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        IsLocationErrorVisible = true;
                        IsInternetErrorVisible = false;
                        return;
                    }
                }

                if (string.IsNullOrEmpty(sehir) || string.IsNullOrEmpty(ilce))
                {
                    IsLocationErrorVisible = true;
                    IsInternetErrorVisible = false;
                    ResetPrayerTimes();
                    return;
                }

                IsLocationErrorVisible = false;

                if (Connectivity.NetworkAccess == NetworkAccess.None)
                {
                    if (_namazVakitleri == null || _namazVakitleri.Count == 0)
                    {
                        ShowInternetError(true);
                        return;
                    }
                }

                var vakitler = await _prayerTimesService.GetPrayerTimesForDateAsync(DateTime.Now, ilce, sehir, latitude, longitude);

                if (vakitler != null)
                {
                    _namazVakitleri = vakitler;
                    IsInternetErrorVisible = false;
                    IsLocationErrorVisible = false;
                    UpdateAllPrayerTimes();

                    try
                    {
                        await _notificationService.ScheduleNotificationsAsync(vakitler);

                        if (Preferences.Default.Get("PersistentNotificationEnabled", false))
                        {
                            PersistentNotificationUpdater.UpdatePrayerTimes(vakitler);
                            PersistentNotificationUpdater.StartUpdating(_notificationService, vakitler);
                        }

                        // Arka planda bir sonraki ayın verilerini önceden çek (sadece ilk seferde)
                        if (!_prefetchDone)
                        {
                            _prefetchDone = true;
                            _ = _prayerTimesService.PrefetchNextDaysAsync(ilce, sehir, latitude, longitude);
                        }
                    }
                    catch (Exception notifEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Bildirim zamanlama hatası: {notifEx.Message}");
                    }
                }
                else
                {
                    ResetPrayerTimes();
                    _namazVakitleri = null;
                    ShowInternetError(false);
                }
            }
            catch (FeatureNotEnabledException)
            {
                IsLocationErrorVisible = true;
                IsInternetErrorVisible = false;
            }
            catch (PermissionException)
            {
                IsLocationErrorVisible = true;
                IsInternetErrorVisible = false;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Namaz vakitleri çekme hatası: {e.Message}");
                ResetPrayerTimes();
                ShowInternetError(false);
            }
        }

        /// <summary>
        /// Geri sayımı güncelle (Timer tarafından çağrılır)
        /// </summary>
        private void UpdateCountdown()
        {
            try
            {
                if (_namazVakitleri == null || _namazVakitleri.Count == 0)
                    return;

                var result = PrayerTimeHelper.GetNextPrayer(_namazVakitleri);

                // Son vakit geçtiyse ertesi gün İmsak'ı ayarla
                if (result.Key == "İmsak" && result.DisplayName == "İmsak Vaktine" &&
                    _namazVakitleri["İmsak"] <= DateTime.Now)
                {
                    _namazVakitleri["İmsak"] = _namazVakitleri["İmsak"].AddDays(1);
                }

                NamazIsmi = result.DisplayName;
                KalanSure = PrayerTimeHelper.FormatCountdown(result.Remaining);

                UpdatePrayerTimeColors(result.Index);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Geri sayım güncelleme hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Aktif namaz vakti rengini güncelle
        /// </summary>
        private void UpdatePrayerTimeColors(int activeIndex)
        {
            // Tüm renkleri varsayılana döndür
            ImsakVakitColor = Colors.White;
            GunesVakitColor = Colors.White;
            OgleVakitColor = Colors.White;
            IkindiVakitColor = Colors.White;
            AksamVakitColor = Colors.White;
            YatsiVakitColor = Colors.White;

            // Bir önceki namaz Silver olsun
            int previousIndex = activeIndex > 0 ? activeIndex - 1 : 5;

            Color[] colors = { Colors.White, Colors.White, Colors.White, Colors.White, Colors.White, Colors.White };
            colors[previousIndex] = Colors.Silver;

            ImsakVakitColor = colors[0];
            GunesVakitColor = colors[1];
            OgleVakitColor = colors[2];
            IkindiVakitColor = colors[3];
            AksamVakitColor = colors[4];
            YatsiVakitColor = colors[5];
        }

        private void UpdateAllPrayerTimes()
        {
            if (_namazVakitleri == null) return;

            ImsakVakit = PrayerTimeHelper.FormatTime(_namazVakitleri["İmsak"]);
            GunesVakit = PrayerTimeHelper.FormatTime(_namazVakitleri["gunes"]);
            OgleVakit = PrayerTimeHelper.FormatTime(_namazVakitleri["Ogle"]);
            IkindiVakit = PrayerTimeHelper.FormatTime(_namazVakitleri["İkindi"]);
            AksamVakit = PrayerTimeHelper.FormatTime(_namazVakitleri["Aksam"]);
            YatsiVakit = PrayerTimeHelper.FormatTime(_namazVakitleri["Yatsi"]);
        }

        private void ResetPrayerTimes()
        {
            KalanSure = "- -";
            NamazIsmi = "";
            ImsakVakit = "- -";
            GunesVakit = "- -";
            OgleVakit = "- -";
            IkindiVakit = "- -";
            AksamVakit = "- -";
            YatsiVakit = "- -";
        }

        private void ShowInternetError(bool isNoInternet)
        {
            if (isNoInternet)
            {
                ErrorTitle = "İnternet Bağlantısı Yok";
                ErrorDescription = "Namaz vakitlerini güncellemek için lütfen internet bağlantınızı kontrol ediniz.";
            }
            else
            {
                ErrorTitle = "Veri Alınamadı";
                ErrorDescription = "Sunucu ile bağlantı kurulamadı. Lütfen daha sonra tekrar deneyiniz.";
            }
            IsInternetErrorVisible = true;
        }

        /// <summary>
        /// Connectivity değişikliğini işle
        /// </summary>
        public async Task OnConnectivityChangedAsync(NetworkAccess networkAccess)
        {
            if (networkAccess != NetworkAccess.None)
            {
                if (IsInternetErrorVisible)
                {
                    await Task.Delay(500);
                }
                await FetchPrayerTimesAsync();
            }
            else
            {
                if (_namazVakitleri == null || _namazVakitleri.Count == 0)
                {
                    ShowInternetError(true);
                }
            }
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}

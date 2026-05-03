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
        private readonly IDispatcherTimer _timer;
        private Dictionary<string, DateTime>? _namazVakitleri;
        private bool _prefetchDone;
        private bool _disposed;

        // --- Observable Properties: Geri SayÄ±m ---
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

        // --- Observable Properties: Hata DurumlarÄ± ---
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

        // DÄ±ÅŸ eriÅŸim: BackgroundService ve ThemeService
        public BackgroundService BackgroundService => _backgroundService;
        public ThemeService ThemeService => _themeService;
        public StatusBarService StatusBarService => _statusBarService;

        // Event: Page'e Widget gÃ¼ncelleme sinyali
        public event Action? WidgetUpdateRequested;

        // Event: Konum hatasÄ± ? SehirSecim sayfasÄ±na yÃ¶nlendir
        public event Action? NavigateToSehirSecim;

        // Namaz vakitleri dÄ±ŞarÄ±ya
        public Dictionary<string, DateTime>? NamazVakitleri => _namazVakitleri;

        // Geocoding sonuÃ§larÄ±nÄ± cache'lemek iÃ§in
        private string? _cachedSehir;
        private string? _cachedIlce;

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

            // Timer başlat - IDispatcherTimer kullanarak UI threading sorunlarını önleriz
            _timer = Application.Current.Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(AppConstants.TIMER_INTERVAL_MS);
            _timer.Tick += OnTimerElapsed;
            _timer.Start();
        }

        /// <summary>
        /// Timer event handler
        /// </summary>
        private void OnTimerElapsed(object? sender, EventArgs e)
        {
            if (_disposed) return;
            UpdateCountdown();
        }

        /// <summary>
        /// TÃ¼m verileri yÃ¼kle (OnAppearing'de Ã§aÄŸrÄ±lÄ±r)
        /// </summary>
        [RelayCommand]
        private async Task LoadDataAsync()
        {
            HicriTarih = PrayerTimeHelper.GetHicriTarih();
            GununAyeti = PrayerTimeHelper.GetDailyAyet();

            bool otomatikKonum = Preferences.Default.Get("OtomatikKonum", false);

            if (otomatikKonum)
            {
                // Show last cached auto location first so UI doesn't block while fetching GPS
                string lastAutoSehir = Preferences.Default.Get("LastAutoSehir", "");
                string lastAutoIlce = Preferences.Default.Get("LastAutoIlce", "");
                double lat = Preferences.Default.Get("LastAutoLatitude", 0.0);
                double lon = Preferences.Default.Get("LastAutoLongitude", 0.0);

                if (!string.IsNullOrEmpty(lastAutoSehir))
                {
                    KonumText = (!string.IsNullOrEmpty(lastAutoIlce) && lastAutoIlce != lastAutoSehir)
                        ? $"{lastAutoIlce} / {lastAutoSehir}"
                        : lastAutoSehir;
                        
                    _cachedSehir = lastAutoSehir;
                    _cachedIlce = lastAutoIlce;
                    
                    var tempLoc = new Location(lat, lon);
                    _ = FetchPrayerTimesAsync(tempLoc, lastAutoSehir, lastAutoIlce); // Fire and update UI immediately from local cache if possible
                }
            }

            // Konum bilgisini al (şehir/ilçe de cache'lenir)
            var locationInfo = await LoadKonumBilgisiAsync();
            
            // Geocoding sonuçlarını FetchPrayerTimesAsync'e geçir (tekrar Geocoding yapılmasın) ve taze veriyi internetten çek
            await FetchPrayerTimesAsync(locationInfo.Location, locationInfo.Sehir, locationInfo.Ilce);
        }

        /// <summary>
        /// Konum bilgisini gÃ¶ster ve bulunan konumu dÃ¶ndÃ¼r (ÅŸehir/ilÃ§e bilgisiyle birlikte)
        /// </summary>
        private async Task<(Location? Location, string? Sehir, string? Ilce)> LoadKonumBilgisiAsync()
        {
            Location? foundLocation = null;
            string? sehir = null;
            string? ilce = null;
            
            try
            {
                bool otomatikKonum = Preferences.Default.Get("OtomatikKonum", false);

                if (!otomatikKonum)
                {
                    string manuelSehir = Preferences.Default.Get("ManuelSehir", "");
                    string manuelIlce = Preferences.Default.Get("ManuelIlce", "");

                    if (!string.IsNullOrEmpty(manuelSehir))
                    {
                        KonumText = (!string.IsNullOrEmpty(manuelIlce) && manuelIlce != manuelSehir)
                            ? $"{manuelIlce} / {manuelSehir}"
                            : manuelSehir;
                        
                        double lat = Preferences.Default.Get("ManuelLatitude", 0.0);
                        double lon = Preferences.Default.Get("ManuelLongitude", 0.0);
                        
                        _cachedSehir = manuelSehir;
                        _cachedIlce = manuelIlce;
                        
                        return (new Location(lat, lon), manuelSehir, manuelIlce);
                    }
                    else
                    {
                        KonumText = "Konum Seçiniz";
                        return (null, null, null);
                    }
                }
                else
                {
                    var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    }

                    if (status != PermissionStatus.Granted)
                    {
                        KonumText = "Konum İzni Verilmedi";
                        return (null, null, null);
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
                            // Tek seferlik Geocoding çağrısı - sonuçlar cache'lenir
                            var placemarks = await Geocoding.Default.GetPlacemarksAsync(foundLocation.Latitude, foundLocation.Longitude);
                            var placemark = placemarks?.FirstOrDefault();

                            if (placemark != null)
                            {
                                sehir = placemark.AdminArea ?? "";
                                ilce = placemark.SubAdminArea ?? placemark.Locality ?? "";
                                
                                _cachedSehir = sehir;
                                _cachedIlce = ilce;

                                Preferences.Default.Set("LastAutoSehir", sehir);
                                Preferences.Default.Set("LastAutoIlce", ilce);
                                Preferences.Default.Set("LastAutoLatitude", foundLocation.Latitude);
                                Preferences.Default.Set("LastAutoLongitude", foundLocation.Longitude);

                                if (!string.IsNullOrEmpty(sehir) && !string.IsNullOrEmpty(ilce))
                                    KonumText = $"{ilce} / {sehir}";
                                else if (!string.IsNullOrEmpty(sehir))
                                    KonumText = sehir;
                                else
                                    KonumText = $"Lat: {foundLocation.Latitude:F2}, Lon: {foundLocation.Longitude:F2}";

                                // Bulunan konumu manuel konuma kaydet (bir sonraki sefer hızlı olsun)
                                Preferences.Default.Set("ManuelSehir", sehir);
                                Preferences.Default.Set("ManuelIlce", ilce);
                                Preferences.Default.Set("ManuelLatitude", foundLocation.Latitude);
                                Preferences.Default.Set("ManuelLongitude", foundLocation.Longitude);
                                
                                System.Diagnostics.Debug.WriteLine($"✅ Konum kaydedildi: {sehir}/{ilce} (Lat: {foundLocation.Latitude:F2}, Lon: {foundLocation.Longitude:F2})");
                            }
                            else
                            {
                                KonumText = $"Lat: {foundLocation.Latitude:F2}, Lon: {foundLocation.Longitude:F2}";
                                System.Diagnostics.Debug.WriteLine($"⚠️ Geocoding sonucu null geldi");
                            }
                        }
                        catch (Exception ex)
                        {
                            KonumText = $"Lat: {foundLocation.Latitude:F2}, Lon: {foundLocation.Longitude:F2}";
                            System.Diagnostics.Debug.WriteLine($"⚠️ Geocoding hatası: {ex.Message}");
                        }
                    }
                    else
                    {
                        KonumText = "Konum Alınamadı";
                    }
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
            catch (Exception ex)
            {
                KonumText = "Konum Hatası";
                System.Diagnostics.Debug.WriteLine($"❌ LoadKonumBilgisiAsync hatası: {ex.Message}");
            }

            return (foundLocation, sehir, ilce);
        }

        /// <summary>
        /// Namaz vakitlerini Ã§ek ve gÃ¼ncelle
        /// </summary>
        public async Task FetchPrayerTimesAsync(Location? locationOverride = null, string? cachedSehir = null, string? cachedIlce = null)
        {
            try
            {
                string ilce = cachedIlce ?? "";
                string sehir = cachedSehir ?? "";
                double? latitude = null;
                double? longitude = null;
                bool otomatikKonum = Preferences.Default.Get("OtomatikKonum", false);

                if (locationOverride != null)
                {
                    latitude = locationOverride.Latitude;
                    longitude = locationOverride.Longitude;

                    var sharedName = $"{AppInfo.PackageName}.xamarinessentials";
                    Preferences.Set("ManuelLatitude",
                        locationOverride.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture), sharedName);
                    Preferences.Set("ManuelLongitude",
                        locationOverride.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture), sharedName);

                    Preferences.Default.Set("ManuelLatitude", locationOverride.Latitude);
                    Preferences.Default.Set("ManuelLongitude", locationOverride.Longitude);

                    WidgetUpdateRequested?.Invoke();

                    // Cache'lenmiş şehir/ilçe parametreleri kontrol et
                    if (!string.IsNullOrEmpty(sehir) && !string.IsNullOrEmpty(ilce))
                    {
                        // Cache'lenmiş değerler var, doğrudan kullan
                        System.Diagnostics.Debug.WriteLine($"✅ Cache'lenmiş konum kullanıldı: {sehir}/{ilce}");
                    }
                    else
                    {
                        // Geocoding yapılması gerekli - eski koddan kopyalama yerine sadece cache kontrol
                        if (!string.IsNullOrEmpty(_cachedSehir))
                        {
                            sehir = _cachedSehir;
                            ilce = _cachedIlce ?? "";
                            System.Diagnostics.Debug.WriteLine($"✅ İnternal cache kullanıldı: {sehir}/{ilce}");
                        }
                        else
                        {
                            try
                            {
                                var placemarks = await Geocoding.Default.GetPlacemarksAsync(locationOverride.Latitude, locationOverride.Longitude);
                                var placemark = placemarks?.FirstOrDefault();
                                if (placemark != null)
                                {
                                    sehir = placemark.AdminArea ?? "";
                                    ilce = placemark.SubAdminArea ?? placemark.Locality ?? "";
                                    _cachedSehir = sehir;
                                    _cachedIlce = ilce;
                                    System.Diagnostics.Debug.WriteLine($"✅ Geocoding sonucu: {sehir}/{ilce}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"❌ Geocoding sonucu null");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"❌ Geocoding hatası: {ex.Message}");
                            }
                        }
                    }
                }
                else if (!otomatikKonum)
                {
                    sehir = Preferences.Default.Get("ManuelSehir", "");
                    ilce = Preferences.Default.Get("ManuelIlce", "");

                    if (string.IsNullOrEmpty(sehir))
                    {
                        IsLocationErrorVisible = true;
                        IsInternetErrorVisible = false;
                        return;
                    }

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
                else
                {
                    // Otomatik konum modu, locationOverride yok
                    var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        IsLocationErrorVisible = true;
                        IsInternetErrorVisible = false;
                        return;
                    }

                    var konum = await Geolocation.GetLastKnownLocationAsync();
                    if (konum == null)
                    {
                        var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(3));
                        konum = await Geolocation.GetLocationAsync(request);
                    }

                    if (konum != null)
                    {
                        latitude = konum.Latitude;
                        longitude = konum.Longitude;

                        var sharedName = $"{AppInfo.PackageName}.xamarinessentials";
                        Preferences.Set("ManuelLatitude",
                            konum.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture), sharedName);
                        Preferences.Set("ManuelLongitude",
                            konum.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture), sharedName);

                        Preferences.Default.Set("ManuelLatitude", konum.Latitude);
                        Preferences.Default.Set("ManuelLongitude", konum.Longitude);

                        WidgetUpdateRequested?.Invoke();

                        // Cache'e bak, yoksa Geocoding yap
                        if (!string.IsNullOrEmpty(_cachedSehir))
                        {
                            sehir = _cachedSehir;
                            ilce = _cachedIlce ?? "";
                            System.Diagnostics.Debug.WriteLine($"âœ… Cache'lenmiÅŸ konum kullanÄ±ldÄ±: {sehir}/{ilce}");
                        }
                        else
                        {
                            try
                            {
                                var placemarks = await Geocoding.Default.GetPlacemarksAsync(konum.Latitude, konum.Longitude);
                                var placemark = placemarks?.FirstOrDefault();
                                if (placemark != null)
                                {
                                    sehir = placemark.AdminArea ?? "";
                                    ilce = placemark.SubAdminArea ?? placemark.Locality ?? "";
                                    _cachedSehir = sehir;
                                    _cachedIlce = ilce;
                                    System.Diagnostics.Debug.WriteLine($"âœ… Geocoding sonucu: {sehir}/{ilce}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"âŒ Geocoding sonucu null");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"âŒ Geocoding hatasÄ±: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        // Konum bulunamadÄ±, Ã¶nceki kaydÄ±lmÄ±Å› konumu kontrol et
                        System.Diagnostics.Debug.WriteLine($"âš ï¸ Konum bulunamadÄ±, Ã¶nceki konumu kontrol ediliyor...");
                        var savedSehir = Preferences.Default.Get("ManuelSehir", "");
                        var savedIlce = Preferences.Default.Get("ManuelIlce", "");
                        var savedLat = Preferences.Default.Get("ManuelLatitude", 0.0);
                        var savedLon = Preferences.Default.Get("ManuelLongitude", 0.0);

                        if (!string.IsNullOrEmpty(savedSehir) && !string.IsNullOrEmpty(savedIlce) && (savedLat != 0 || savedLon != 0))
                        {
                            // Önceki konum var, devam et (uyarı gösterme)
                            sehir = savedSehir;
                            ilce = savedIlce;
                            latitude = savedLat;
                            longitude = savedLon;
                            _cachedSehir = sehir;
                            _cachedIlce = ilce;
                            System.Diagnostics.Debug.WriteLine($"✅ Önceki konum kullanıldı: {sehir}/{ilce}");
                        }
                        else
                        {
                            // Hiç konum bulunamadı ve eski konum yok - hata göster
                            IsLocationErrorVisible = true;
                            IsInternetErrorVisible = false;
                            System.Diagnostics.Debug.WriteLine($"❌ Konum bulunamadı ve önceki konum da yok");
                            return;
                        }
                    }
                }

                // Tüm gerekli bilgiler kontrol et
                if ((string.IsNullOrEmpty(sehir) || string.IsNullOrEmpty(ilce)) && 
                    (!latitude.HasValue || !longitude.HasValue || (Math.Abs(latitude.Value) < 0.0001 && Math.Abs(longitude.Value) < 0.0001)))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Eksik konum bilgisi: sehir={sehir}, ilce={ilce}, lat={latitude}, lon={longitude}");
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

                System.Diagnostics.Debug.WriteLine($"📞 GetPrayerTimesForDateAsync çağrılıyor: {sehir}/{ilce}");
                string manuelUlke = Preferences.Default.Get("ManuelUlke", "Türkiye");
                var vakitler = await _prayerTimesService.GetPrayerTimesForDateAsync(DateTime.Now, ilce, sehir, manuelUlke, latitude, longitude);

                if (vakitler != null)
                {
                    _namazVakitleri = vakitler;
                    IsInternetErrorVisible = false;
                    IsLocationErrorVisible = false;
                    UpdateAllPrayerTimes();

                    try
                    {
                        await _notificationService.ScheduleNotificationsAsync(vakitler);
                        _ = _notificationService.ScheduleMultiDayNotificationsAsync(7);

                        if (Preferences.Default.Get("PersistentNotificationEnabled", false))
                        {
                            PersistentNotificationUpdater.UpdatePrayerTimes(vakitler);
                            PersistentNotificationUpdater.StartUpdating(_notificationService, vakitler);
                        }

                        if (!_prefetchDone)
                        {
                            _prefetchDone = true;
                            _ = _prayerTimesService.PrefetchNextDaysAsync(ilce, sehir, latitude, longitude);
                        }
                    }
                    catch (Exception notifEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"🔔 Bildirim zamanlama hatası: {notifEx.Message}");
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
        /// Geri sayÄ±mÄ± gÃ¼ncelle (Timer tarafÄ±ndan Ã§aÄŸrÄ±lÄ±r)
        /// </summary>
        private void UpdateCountdown()
        {
            try
            {
                if (_namazVakitleri == null || _namazVakitleri.Count == 0)
                    return;

                var result = PrayerTimeHelper.GetNextPrayer(_namazVakitleri);

                if (result.Key == "Imsak" && result.DisplayName == "İmsak Vaktine" &&
                    _namazVakitleri["Imsak"] <= DateTime.Now)
                {
                    _namazVakitleri["Imsak"] = _namazVakitleri["Imsak"].AddDays(1);
                }

                NamazIsmi = result.DisplayName;
                KalanSure = PrayerTimeHelper.FormatCountdown(result.Remaining);

                UpdatePrayerTimeColors(result.Index);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Geri sayÄ±m gÃ¼ncelleme hatasÄ±: {ex.Message}");
            }
        }

        private void UpdatePrayerTimeColors(int activeIndex)
        {
            int previousIndex = activeIndex > 0 ? activeIndex - 1 : 5;

            ImsakVakitColor = previousIndex == 0 ? Colors.Silver : Colors.White;
            GunesVakitColor = previousIndex == 1 ? Colors.Silver : Colors.White;
            OgleVakitColor = previousIndex == 2 ? Colors.Silver : Colors.White;
            IkindiVakitColor = previousIndex == 3 ? Colors.Silver : Colors.White;
            AksamVakitColor = previousIndex == 4 ? Colors.Silver : Colors.White;
            YatsiVakitColor = previousIndex == 5 ? Colors.Silver : Colors.White;
        }

        private void UpdateAllPrayerTimes()
        {
            if (_namazVakitleri == null) return;

            ImsakVakit = PrayerTimeHelper.FormatTime(_namazVakitleri["Imsak"]);
            GunesVakit = PrayerTimeHelper.FormatTime(_namazVakitleri["gunes"]);
            OgleVakit = PrayerTimeHelper.FormatTime(_namazVakitleri["Ogle"]);
            IkindiVakit = PrayerTimeHelper.FormatTime(_namazVakitleri["Ikindi"]);
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
        /// Connectivity deÄŸiÅŸikliÄŸini iÅŸle
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (_timer != null)
                {
                    _timer.Tick -= OnTimerElapsed;
                    _timer.Stop();
                }
            }

            _disposed = true;
        }

        public void ShowAddCityPlaceholder()
        {
            NamazIsmi = "Şehir Ekle";
            KalanSure = "+";
            ImsakVakit = "+";
            GunesVakit = "+";
            OgleVakit = "+";
            IkindiVakit = "+";
            AksamVakit = "+";
            YatsiVakit = "+";

            ImsakVakitColor = Colors.White;
            GunesVakitColor = Colors.White;
            OgleVakitColor = Colors.White;
            IkindiVakitColor = Colors.White;
            AksamVakitColor = Colors.White;
            YatsiVakitColor = Colors.White;

            IsInternetErrorVisible = false;
            IsLocationErrorVisible = false;
        }

        public async Task<bool> ShowPrayerTimesForCityAsync(string sehir, string ilce, string ulke, double latitude, double longitude)
        {
            try
            {
                var vakitler = await _prayerTimesService.GetPrayerTimesForDateAsync(DateTime.Now, ilce, sehir, ulke, latitude, longitude);
                if (vakitler == null)
                {
                    return false;
                }

                _namazVakitleri = vakitler;
                KonumText = string.IsNullOrWhiteSpace(ilce) || ilce.Equals(sehir, StringComparison.OrdinalIgnoreCase)
                    ? sehir
                    : $"{ilce} / {sehir}";

                IsInternetErrorVisible = false;
                IsLocationErrorVisible = false;
                UpdateAllPrayerTimes();
                UpdateCountdown();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

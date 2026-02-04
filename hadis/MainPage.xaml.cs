using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Maui.Devices.Sensors;
using hadis.Models;
using hadis.Services;
using hadis.Helpers;

#if ANDROID
using Android.OS;
using Android.Views;
#endif

namespace hadis
{
    public partial class MainPage : ContentPage
    {
        private Dictionary<string, DateTime> _namazvakitleri;
        private readonly System.Timers.Timer _timer;
        private readonly BackgroundService _backgroundService;
        private readonly ThemeService _themeService;
        private readonly StatusBarService _statusBarService;
        private readonly IAppNotificationService _notificationService;
        private string _currentImageName;

        public MainPage(BackgroundService backgroundService, ThemeService themeService, StatusBarService statusBarService, IAppNotificationService notificationService)
        {
            InitializeComponent();
            _backgroundService = backgroundService;
            _themeService = themeService;
            _statusBarService = statusBarService;
            _notificationService = notificationService;

            // Timer'ı başlat
            _timer = new System.Timers.Timer(AppConstants.TIMER_INTERVAL_MS);
            _timer.Elapsed += async (s, e) => await MainThread.InvokeOnMainThreadAsync(GeriSayımıGüncelle);
            _timer.Start();

            // İlk yüklemeleri SYNCHRONOUS olarak yap (Flicker önlemek için)
            InitializeBackgroundSync();
        }

        private void InitializeBackgroundSync()
        {
             try
             {
                 string savedTheme = Preferences.Default.Get(AppConstants.PREF_APP_THEME, AppConstants.THEME_SYSTEM);
                 
                 // Eğer tema "System" veya "Main" ise zaman bazlı resmi hemen hesapla ve ata
                 if (savedTheme == AppConstants.THEME_SYSTEM || savedTheme.StartsWith("Main"))
                 {
                     var now = DateTime.Now;
                     var info = TimeBasedBackgroundConfig.GetBackgroundForTime(now.Hour, now.Minute);
                     
                     // String olarak ata (Hızlı)
                     BackgroundImage.Source = info.Image;
                     _currentImageName = info.Image;
                     
                     // Renkleri de hemen ayarla
                     _statusBarService.SetStatusBarColor(info.StatusBarColor);
                     // TabBar rengi AppShell tarafından yönetiliyor olabilir ama yine de servisi çağırabiliriz
                     // Ancak BackgroundService tam ayarı zaten yapacak, burada kritik olan Görseli koymak.
                 }
                 
                 // Arkaplan servisini çağır (validasyon ve diğer ayarlar için)
                 // currentImageName gönderdiğimiz için tekrar yükleme yapmayacak
                 SetTimeBasedBackground();
             }
             catch(Exception ex)
             {
                 Console.WriteLine($"Init Background Error: {ex.Message}");
                 SetTimeBasedBackground();
             }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Connectivity eventini dinle
            Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;

            // Özel tema varsa uygula
            ApplyTheme();

            SetTimeBasedBackground();
            
            // Sayfa her gösterildiğinde gerekli verileri güncelle
            await Task.WhenAll(
                KonumBilgisiniGoster(),
                NamazVakitleriniÇek(),
                ayetgoster()
            );
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            // Event dinlemeyi bırak
            Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
        }

        private async void Connectivity_ConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                // İnternet geldiyse verileri çek
                await MainThread.InvokeOnMainThreadAsync(async () => 
                {
                    // Eğer overlay açıksa kullanıcıya tepki ver
                    if (InternetErrorOverlay.IsVisible)
                    {
                        InternetErrorOverlay.IsVisible = true; // Refresh UI trigger
                        await Task.Delay(500); // UI flicker önlemek için kısa bekleme
                    }
                    await NamazVakitleriniÇek();
                });
            }
            else
            {
                // İnternet gittiyse ve veri yoksa uyarı göster
                if (_namazvakitleri == null || _namazvakitleri.Count == 0)
                {
                    MainThread.BeginInvokeOnMainThread(() => InternetErrorOverlay.IsVisible = true);
                }
            }
        }

        public async Task NamazVakitleriniÇek()
        {
            try
            {
                // 1. Önce Konum Durumunu Kontrol Et
                string ilce = "";
                string sehir = "";
                bool otomatikKonum = Preferences.Default.Get("OtomatikKonum", true);

                bool manuelKonumVar = !otomatikKonum && !string.IsNullOrEmpty(Preferences.Default.Get("ManuelSehir", ""));
                
                // Eğer manuel konum yoksa, GPS iznine ve verisine bak
                if (!manuelKonumVar)
                {
                    var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted)
                    {
                        // İzin yok -> Konum Yok Hatası Göster
                         MainThread.BeginInvokeOnMainThread(() => 
                         {
                             LocationErrorOverlay.IsVisible = true;
                             InternetErrorOverlay.IsVisible = false;
                         });
                         return;
                    }
                }
                
                // Konum izni var veya manuel seçili, şimdi verileri almayı dene
                if (!otomatikKonum)
                {
                    sehir = Preferences.Default.Get("ManuelSehir", "");
                    ilce = Preferences.Default.Get("ManuelIlce", "");
                }
                else
                {
                    var konum = await Geolocation.GetLastKnownLocationAsync();
                    if (konum == null)
                    {
                        var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(3));
                        konum = await Geolocation.GetLocationAsync(request);
                    }

                    if (konum != null)
                    {
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
                        catch
                        {
                             // Geocoding hatası, koordinat var ama isim yok, yine de devam edilebilir ama bizim API isim istiyor.
                             // Şimdilik boş geçelim, aşağıda yakalanır.
                        }
                    }
                    else
                    {
                        // Konum alınamadı (GPS kapalı veya bina içi) -> Konum Hatası Göster
                         MainThread.BeginInvokeOnMainThread(() => 
                         {
                             LocationErrorOverlay.IsVisible = true;
                             InternetErrorOverlay.IsVisible = false;
                         });
                         return;
                    }
                }

                if (string.IsNullOrEmpty(sehir) || string.IsNullOrEmpty(ilce))
                {
                    // Şehir/İlçe bulunamadı -> Konum Hatası
                     MainThread.BeginInvokeOnMainThread(() => 
                     {
                         LocationErrorOverlay.IsVisible = true;
                         InternetErrorOverlay.IsVisible = false;
                     });
                    ResetPrayerTimes();
                    return;
                }

                // Konum OK, şimdi İnternet ve Veri Kontrolü
                // Konum hatası overlay'ini gizle (çünkü konum var)
                MainThread.BeginInvokeOnMainThread(() => LocationErrorOverlay.IsVisible = false);

                if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                     // Eğer önbellekte veri yoksa engelleyici ekranı göster
                     if (_namazvakitleri == null || _namazvakitleri.Count == 0)
                     {
                         InternetErrorOverlay.IsVisible = true;
                         return;
                     }
                     // Veri varsa, internet olmasa da devam edebiliriz (cache varsa)
                }

                // Yeni Servisi Kullan
                var vakitler = await PrayerTimesService.GetPrayerTimesForDateAsync(DateTime.Now, ilce, sehir);

                if (vakitler != null)
                {
                    _namazvakitleri = vakitler;
                    
                    // Veriler geldi, overlayleri gizle
                    MainThread.BeginInvokeOnMainThread(() => 
                    {
                        InternetErrorOverlay.IsVisible = false;
                        LocationErrorOverlay.IsVisible = false;
                        UpdateAllPrayerTimes();
                    });

                    // Bildirimleri zamanla
                    try
                    {
                        await _notificationService.ScheduleNotificationsAsync(vakitler);
                        Console.WriteLine("✅ Bildirimler başarıyla zamanlandı.");
                    }
                    catch (Exception notifEx)
                    {
                        Console.WriteLine($"⚠️ Bildirim zamanlama hatası: {notifEx.Message}");
                    }
                }
                else
                {
                    // Vakitler null ise
                    Console.WriteLine("❌ Namaz vakitleri alınamadı.");
                    ResetPrayerTimes();
                    
                    _namazvakitleri = null;

                    if (_namazvakitleri == null || _namazvakitleri.Count == 0)
                    {
                         InternetErrorOverlay.IsVisible = true;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"❌ Namaz vakitleri çekme hatası: {e.Message}");
                ResetPrayerTimes();
                 if (_namazvakitleri == null || _namazvakitleri.Count == 0)
                 {
                     // Genel hata durumunda internet hatası gösterilebilir veya özel bir hata
                     InternetErrorOverlay.IsVisible = true;
                 }
            }
        }
        
        private async void OnLocationErrorRetry_Clicked(object sender, EventArgs e)
        {
            // Şehir seçim sayfasına git
            await Navigation.PushAsync(new SehirSecim());
        }

        private void SetTimeBasedBackground()
        {
            string savedTheme = Preferences.Default.Get(AppConstants.PREF_APP_THEME, AppConstants.THEME_SYSTEM);
            
            // BackgroundService artık (bool, string) dönüyor
            var result = _backgroundService.SetTimeBasedBackground(BackgroundImage, BackgroundOverlay, savedTheme, _currentImageName);
            
            bool isBright = result.IsBright;
            if (!string.IsNullOrEmpty(result.ImageName))
            {
                _currentImageName = result.ImageName;
            }

            // Custom veya Simsiyah tema değilse, adaptif cam efektini uygula
            if (savedTheme != AppConstants.THEME_CUSTOM && savedTheme != "PitchBlack")
            {
                _themeService.ApplyAdaptiveGlassTheme(isBright,
                    MainCountdownFrame, namazismi, kalan, Konum,
                    ImsakFrame, imsakyazı, imsakvakit,
                    GunesFrame, gunesyazı, gunesvakit,
                    OgleFrame, ogleyazı, oglevakit,
                    IkindiFrame, ikindiyazı, ikindivakit,
                    AksamFrame, aksamyazı, aksamvakit,
                    YatsiFrame, yatsıyazı, yatsıvakit,
                    AyetFrame, gununayeti);
            }
        }

        private void ApplyTheme()
        {
            string savedTheme = Preferences.Default.Get(AppConstants.PREF_APP_THEME, AppConstants.THEME_SYSTEM);

            if (savedTheme != AppConstants.THEME_CUSTOM)
            {
                _themeService.ResetToDefaultStyles(
                    MainCountdownFrame, namazismi, kalan, Konum,
                    ImsakFrame, imsakyazı, imsakvakit,
                    GunesFrame, gunesyazı, gunesvakit,
                    OgleFrame, ogleyazı, oglevakit,
                    IkindiFrame, ikindiyazı, ikindivakit,
                    AksamFrame, aksamyazı, aksamvakit,
                    YatsiFrame, yatsıyazı, yatsıvakit,
                    AyetFrame, gununayeti);
                return;
            }

            // Özel tema uygula
            _themeService.ApplyCustomTheme(
                MainCountdownFrame, namazismi, kalan, Konum,
                ImsakFrame, imsakyazı, imsakvakit,
                GunesFrame, gunesyazı, gunesvakit,
                OgleFrame, ogleyazı, oglevakit,
                IkindiFrame, ikindiyazı, ikindivakit,
                AksamFrame, aksamyazı, aksamvakit,
                YatsiFrame, yatsıyazı, yatsıvakit,
                AyetFrame, gununayeti);

            // Özel arkaplanı uygula
            string customThemeJson = Preferences.Default.Get(AppConstants.PREF_CUSTOM_THEME, string.Empty);
            if (!string.IsNullOrEmpty(customThemeJson))
            {
                try
                {
                    var theme = JsonSerializer.Deserialize<CustomTheme>(customThemeJson);
                    if (theme != null && !string.IsNullOrEmpty(theme.BackgroundImage))
                    {
                        _backgroundService.ApplyCustomBackground(BackgroundImage, BackgroundOverlay, theme.BackgroundImage);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Custom tema arkaplan hatası: {ex.Message}");
                }
            }
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            await AnimateFrames();
        }

        private async Task AnimateFrames()
        {
            // Ana geri sayım frame'ini başlangıçta görünmez ve küçük yap
            MainCountdownFrame.Opacity = 0;
            MainCountdownFrame.Scale = 0.7;

            // İlk satır frame'leri
            ImsakFrame.Opacity = 0;
            ImsakFrame.Scale = 0.7;
            GunesFrame.Opacity = 0;
            GunesFrame.Scale = 0.7;
            OgleFrame.Opacity = 0;
            OgleFrame.Scale = 0.7;

            // İkinci satır frame'leri
            IkindiFrame.Opacity = 0;
            IkindiFrame.Scale = 0.7;
            AksamFrame.Opacity = 0;
            AksamFrame.Scale = 0.7;
            YatsiFrame.Opacity = 0;
            YatsiFrame.Scale = 0.7;

            // Ayet frame'i
            AyetFrame.Opacity = 0;
            AyetFrame.Scale = 0.7;

            // 1. Ana geri sayım frame'i büyüsün
            await Task.WhenAll(
                MainCountdownFrame.FadeTo(1, 500, Easing.CubicOut),
                MainCountdownFrame.ScaleTo(1.0, 600, Easing.SpringOut)
            );

            await Task.Delay(100);

            // 2. İlk satır frame'leri kademeli olarak büyüsün
            var imsakTask = AnimateSingleFrame(ImsakFrame);
            await Task.Delay(80);
            var gunesTask = AnimateSingleFrame(GunesFrame);
            await Task.Delay(80);
            var ogleTask = AnimateSingleFrame(OgleFrame);
            await Task.Delay(100);

            // 3. İkinci satır frame'leri kademeli olarak büyüsün
            var ikindiTask = AnimateSingleFrame(IkindiFrame);
            await Task.Delay(80);
            var aksamTask = AnimateSingleFrame(AksamFrame);
            await Task.Delay(80);
            var yatsiTask = AnimateSingleFrame(YatsiFrame);
            await Task.Delay(150);

            // 4. Son olarak ayet frame'i büyüsün
            await Task.WhenAll(
                AyetFrame.FadeTo(1, 500, Easing.CubicOut),
                AyetFrame.ScaleTo(1.0, 600, Easing.SpringOut)
            );
        }

        private Task AnimateSingleFrame(Frame frame)
        {
            return Task.WhenAll(
                frame.FadeTo(1, 400, Easing.CubicOut),
                frame.ScaleTo(1.0, 500, Easing.SpringOut)
            );
        }

        protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);

            // Tab değişirken hızlı küçülme
            await Task.WhenAll(
                MainCountdownFrame.ScaleTo(0.7, 250, Easing.CubicIn),
                ImsakFrame.ScaleTo(0.7, 250, Easing.CubicIn),
                GunesFrame.ScaleTo(0.7, 250, Easing.CubicIn),
                OgleFrame.ScaleTo(0.7, 250, Easing.CubicIn),
                IkindiFrame.ScaleTo(0.7, 250, Easing.CubicIn),
                AksamFrame.ScaleTo(0.7, 250, Easing.CubicIn),
                YatsiFrame.ScaleTo(0.7, 250, Easing.CubicIn),
                AyetFrame.ScaleTo(0.7, 250, Easing.CubicIn)
            );
        }

        public async Task ayetgoster()
        {
            try
            {
                string[] ayetler = new string[]
                {
                    "Hiç bilenlerle bilmeyenler bir olur mu? (Zümer, 9)",
                    "Şüphesiz Allah sabredenlerle beraberdir. (Bakara, 153)",
                    "Gerçekten güçlükle beraber bir kolaylık vardır. (İnşirah, 6)",
                    "Allah, kullarına karşı çok şefkatlidir. (Şura, 19)",
                    "Ey iman edenler! Sabır ve namazla Allah'tan yardım isteyin. (Bakara, 45)",
                    "Göklerde ve yerde ne varsa hepsi Allah'ındır. (Bakara, 284)",
                    "Zorlukla beraber bir kolaylık vardır. (İnşirah, 5)",
                    "Kıyamet günü herkese amel defteri verilecektir. (İsra, 13)",
                    "İyilik ve takva üzerine yardımlaşın. (Maide, 2)",
                    "Şüphesiz dönüş ancak Allah'adır. (Bakara, 156)"
                };

                int gunIndex = DateTime.Now.DayOfYear % ayetler.Length;
                string bugununAyeti = ayetler[gunIndex];
                if (gununayeti != null)
                {
                    gununayeti.Text = bugununAyeti;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ayet gösterme hatası: {ex.Message}");
            }
        }

        public void GeriSayımıGüncelle()
        {
            try
            {
                if (_namazvakitleri == null || _namazvakitleri.Count == 0)
                {
                    return;
                }

                DateTime simdi = DateTime.Now;
                TimeSpan kalansure;
                string sonraki;

                if (_namazvakitleri["İmsak"] > simdi)
                {
                    kalansure = _namazvakitleri["İmsak"] - simdi;
                    sonraki = "İmsak Vaktine";
                    UpdatePrayerTimeColors(imsakvakit, yatsıvakit);
                }
                else if (_namazvakitleri["gunes"] > simdi)
                {
                    kalansure = _namazvakitleri["gunes"] - simdi;
                    sonraki = "Güneşin Doğmasına";
                    UpdatePrayerTimeColors(gunesvakit, imsakvakit);
                }
                else if (_namazvakitleri["Ogle"] > simdi)
                {
                    kalansure = _namazvakitleri["Ogle"] - simdi;
                    sonraki = "Öğle Namazına";
                    UpdatePrayerTimeColors(oglevakit, gunesvakit);
                }
                else if (_namazvakitleri["İkindi"] > simdi)
                {
                    kalansure = _namazvakitleri["İkindi"] - simdi;
                    sonraki = "İkindi Namazına";
                    UpdatePrayerTimeColors(ikindivakit, oglevakit);
                }
                else if (_namazvakitleri["Aksam"] > simdi)
                {
                    kalansure = _namazvakitleri["Aksam"] - simdi;
                    sonraki = "Akşam Namazına";
                    UpdatePrayerTimeColors(aksamvakit, ikindivakit);
                }
                else if (_namazvakitleri["Yatsi"] > simdi)
                {
                    kalansure = _namazvakitleri["Yatsi"] - simdi;
                    sonraki = "Yatsı Namazına";
                    UpdatePrayerTimeColors(yatsıvakit, aksamvakit);
                }
                else
                {
                    _namazvakitleri["İmsak"] = _namazvakitleri["İmsak"].AddDays(1);
                    kalansure = _namazvakitleri["İmsak"] - simdi;
                    sonraki = "İmsak Vaktine";
                    UpdatePrayerTimeColors(imsakvakit, yatsıvakit);
                }

                namazismi.Text = sonraki;
                kalan.Text = $"{kalansure.Hours:D2} : {kalansure.Minutes:D2} : {kalansure.Seconds:D2}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Geri sayım güncelleme hatası: {ex.Message}");
            }
        }

        private void UpdatePrayerTimeColors(Label current, Label previous)
        {
            current.TextColor = Colors.White;
            previous.TextColor = Colors.Silver;
        }

        private void UpdateAllPrayerTimes()
        {
            if (_namazvakitleri == null) return;

            yatsıvakit.Text = $"{_namazvakitleri["Yatsi"].Hour:D2}:{_namazvakitleri["Yatsi"].Minute:D2}";
            aksamvakit.Text = $"{_namazvakitleri["Aksam"].Hour:D2} : {_namazvakitleri["Aksam"].Minute:D2}";
            ikindivakit.Text = $"{_namazvakitleri["İkindi"].Hour:D2} : {_namazvakitleri["İkindi"].Minute:D2}";
            oglevakit.Text = $"{_namazvakitleri["Ogle"].Hour:D2} : {_namazvakitleri["Ogle"].Minute:D2}";
            gunesvakit.Text = $"{_namazvakitleri["gunes"].Hour:D2} : {_namazvakitleri["gunes"].Minute:D2}";
            imsakvakit.Text = $"{_namazvakitleri["İmsak"].Hour:D2}:{_namazvakitleri["İmsak"].Minute:D2}";
        }



        private void ResetPrayerTimes()
        {
            kalan.Text = "- -";
            namazismi.Text = "";
            yatsıvakit.Text = "- -";
            aksamvakit.Text = "- -";
            ikindivakit.Text = "- -";
            oglevakit.Text = "- -";
            gunesvakit.Text = "- -";
            imsakvakit.Text = "- -";
        }

        public async Task<(double Latitude, double Longitude)> GetKonum()
        {
            try
            {
                // Önce manuel konum ayarı var mı kontrol et
                bool otomatikKonum = Preferences.Default.Get("OtomatikKonum", true);

                if (!otomatikKonum)
                {
                    // Manuel konum kullan
                    double manuelLat = Preferences.Default.Get("ManuelLatitude", 0.0);
                    double manuelLon = Preferences.Default.Get("ManuelLongitude", 0.0);

                    if (manuelLat != 0 && manuelLon != 0)
                    {
                        return (manuelLat, manuelLon);
                    }
                }

                // Otomatik konum kullan
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    Konum.Text = "Lütfen Konum Seçiniz!";
                }

                if (status != PermissionStatus.Granted)
                {
                    Console.WriteLine("⚠️ Konum izni verilmedi");
                    return (0, 0);
                }

                var konum = await Geolocation.GetLastKnownLocationAsync();

                if (konum == null)
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                    konum = await Geolocation.GetLocationAsync(request);
                }

                if (konum != null)
                {
                    return (konum.Latitude, konum.Longitude);
                }
                else
                {
                    Console.WriteLine("⚠️ Konum null döndü");
                    return (0, 0);
                }
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                Console.WriteLine($"❌ Konum özelliği desteklenmiyor: {fnsEx.Message}");
                return (0, 0);
            }
            catch (PermissionException pEx)
            {
                Console.WriteLine($"❌ Konum izni hatası: {pEx.Message}");
                return (0, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Konum Hatası: {ex.Message}");
                return (0, 0);
            }
        }

        public async Task KonumBilgisiniGoster()
        {
            try
            {
                // Önce manuel konum ayarı var mı kontrol et
                bool otomatikKonum = Preferences.Default.Get("OtomatikKonum", true);

                if (!otomatikKonum)
                {
                    // Manuel konum göster
                    string manuelSehir = Preferences.Default.Get("ManuelSehir", "");
                    string manuelIlce = Preferences.Default.Get("ManuelIlce", "");

                    if (!string.IsNullOrEmpty(manuelSehir))
                    {
                        if (!string.IsNullOrEmpty(manuelIlce) && manuelIlce != manuelSehir)
                        {
                             Konum.Text = $"{manuelIlce} / {manuelSehir}";
                        }
                        else
                        {
                             Konum.Text = manuelSehir;
                        }
                        return;
                    }
                }

                // Otomatik konum göster
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status != PermissionStatus.Granted)
                {
                    Konum.Text = "Konum İzni Verilmedi";
                    return;
                }

                var konum = await Geolocation.GetLastKnownLocationAsync();

                if (konum == null)
                {
                    var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                    konum = await Geolocation.GetLocationAsync(request);
                }

                if (konum != null)
                {
                    try
                    {
                        var placemarks = await Geocoding.Default.GetPlacemarksAsync(konum.Latitude, konum.Longitude);
                        var placemark = placemarks?.FirstOrDefault();

                        if (placemark != null)
                        {
                            string il = placemark.AdminArea ?? "";
                            string ilce = placemark.SubAdminArea ?? placemark.Locality ?? "";

                            if (!string.IsNullOrEmpty(il) && !string.IsNullOrEmpty(ilce))
                            {
                                Konum.Text = $"{ilce} / {il}";
                            }
                            else if (!string.IsNullOrEmpty(il))
                            {
                                Konum.Text = il;
                            }
                            else
                            {
                                Konum.Text = $"Lat: {konum.Latitude:F2}, Lon: {konum.Longitude:F2}";
                            }
                        }
                        else
                        {
                            Konum.Text = $"Lat: {konum.Latitude:F2}, Lon: {konum.Longitude:F2}";
                        }
                    }
                    catch (Exception geocodingEx)
                    {
                        Console.WriteLine($"❌ Geocoding Hatası: {geocodingEx.Message}");
                        Konum.Text = $"Lat: {konum.Latitude:F2}, Lon: {konum.Longitude:F2}";
                    }
                }
                else
                {
                    Konum.Text = "Konum Alınamadı";
                }
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                Console.WriteLine($"❌ Konum özelliği desteklenmiyor: {fnsEx.Message}");
                Konum.Text = "Konum Desteklenmiyor";
            }
            catch (PermissionException pEx)
            {
                Console.WriteLine($"❌ Konum izni hatası: {pEx.Message}");
                Konum.Text = "Konum İzni Gerekli";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Konum Bilgisi Hatası: {ex.Message}");
                Konum.Text = "Konum Hatası";
            }
        }

        private async void Konum_Tapped(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new SehirSecim());
        }

    }
}

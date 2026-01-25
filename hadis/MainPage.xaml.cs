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

        public MainPage(
            BackgroundService backgroundService,
            ThemeService themeService,
            StatusBarService statusBarService)
        {
            InitializeComponent();

            // Servisleri inject et
            _backgroundService = backgroundService;
            _themeService = themeService;
            _statusBarService = statusBarService;

            // Timer'ı başlat
            _timer = new System.Timers.Timer(AppConstants.TIMER_INTERVAL_MS);
            _timer.Elapsed += async (s, e) => await MainThread.InvokeOnMainThreadAsync(GeriSayımıGüncelle);
            _timer.Start();

            // İlk yüklemeleri yap
            _ = InitializePageAsync();
        }

        private async Task InitializePageAsync()
        {
            await Task.WhenAll(
                NamazVakitleriniÇek(),
                ayetgoster(),
                KonumBilgisiniGoster()
            );
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Özel tema varsa uygula
            ApplyTheme();

            // Sayfa her gösterildiğinde konum ve vakitleri güncelle
            await KonumBilgisiniGoster();
            await NamazVakitleriniÇek();

            // Zamana göre arkaplan ayarla
            SetTimeBasedBackground();
        }

        private void SetTimeBasedBackground()
        {
            string savedTheme = Preferences.Default.Get(AppConstants.PREF_APP_THEME, AppConstants.THEME_SYSTEM);
            _backgroundService.SetTimeBasedBackground(BackgroundImage, BackgroundOverlay, savedTheme);
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
            gununayeti.Text = bugununAyeti;
        }

        public void GeriSayımıGüncelle()
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

            // Tüm vakitleri güncelle
            UpdateAllPrayerTimes();
        }

        private void UpdatePrayerTimeColors(Label current, Label previous)
        {
            current.TextColor = Colors.White;
            previous.TextColor = Colors.Silver;
        }

        private void UpdateAllPrayerTimes()
        {
            yatsıvakit.Text = $"{_namazvakitleri["Yatsi"].Hour:D2}:{_namazvakitleri["Yatsi"].Minute:D2}";
            aksamvakit.Text = $"{_namazvakitleri["Aksam"].Hour:D2} : {_namazvakitleri["Aksam"].Minute:D2}";
            ikindivakit.Text = $"{_namazvakitleri["İkindi"].Hour:D2} : {_namazvakitleri["İkindi"].Minute:D2}";
            oglevakit.Text = $"{_namazvakitleri["Ogle"].Hour:D2} : {_namazvakitleri["Ogle"].Minute:D2}";
            gunesvakit.Text = $"{_namazvakitleri["gunes"].Hour:D2} : {_namazvakitleri["gunes"].Minute:D2}";
            imsakvakit.Text = $"{_namazvakitleri["İmsak"].Hour:D2}:{_namazvakitleri["İmsak"].Minute:D2}";
        }

        public async Task NamazVakitleriniÇek()
        {
            try
            {
                string ilce = "";
                string sehir = "";
                bool otomatikKonum = Preferences.Default.Get("OtomatikKonum", true);

                if (!otomatikKonum)
                {
                    // Manuel konum
                    sehir = Preferences.Default.Get("ManuelSehir", "");
                    ilce = Preferences.Default.Get("ManuelIlce", "");
                }
                else
                {
                    // Otomatik konum
                    var konum = await Geolocation.GetLastKnownLocationAsync();
                    if (konum == null)
                    {
                        var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                        konum = await Geolocation.GetLocationAsync(request);
                    }

                    if (konum != null)
                    {
                        var placemarks = await Geocoding.Default.GetPlacemarksAsync(konum.Latitude, konum.Longitude);
                        var placemark = placemarks?.FirstOrDefault();
                        if (placemark != null)
                        {
                            sehir = placemark.AdminArea ?? "";
                            ilce = placemark.SubAdminArea ?? placemark.Locality ?? "";
                        }
                    }
                }

                if (string.IsNullOrEmpty(sehir) || string.IsNullOrEmpty(ilce))
                {
                    ResetPrayerTimes();
                    return;
                }

                HttpClient http = new HttpClient();
                string url = $"https://api.aladhan.com/v1/timingsByAddress?address={ilce},{sehir},Turkey&method=13";
                HttpResponseMessage response = await http.GetAsync(url);
                string vakitler = await response.Content.ReadAsStringAsync();

                var root = JsonDocument.Parse(vakitler).RootElement.GetProperty("data");
                root = root.GetProperty("timings");

                string imsak = root.GetProperty("Fajr").GetString();
                string gunes = root.GetProperty("Sunrise").GetString();
                string ogle = root.GetProperty("Dhuhr").GetString();
                string ikindi = root.GetProperty("Asr").GetString();
                string aksam = root.GetProperty("Maghrib").GetString();
                string yatsi = root.GetProperty("Isha").GetString();

                DateTime imsakvakti = DateTime.Today + TimeSpan.Parse(imsak);
                DateTime gunesvakti = DateTime.Today + TimeSpan.Parse(gunes);
                DateTime oglevakti = DateTime.Today + TimeSpan.Parse(ogle);
                DateTime ikindivakti = DateTime.Today + TimeSpan.Parse(ikindi);
                DateTime aksamvakti = DateTime.Today + TimeSpan.Parse(aksam);
                DateTime yatsivakti = DateTime.Today + TimeSpan.Parse(yatsi);

                _namazvakitleri = new Dictionary<string, DateTime>
                {
                    { "İmsak", imsakvakti },
                    { "gunes", gunesvakti },
                    { "Ogle", oglevakti },
                    { "İkindi", ikindivakti },
                    { "Aksam", aksamvakti },
                    { "Yatsi", yatsivakti }
                };
            }
            catch (Exception e)
            {
                Console.WriteLine($"❌ Namaz vakitleri çekme hatası: {e.Message}");
                ResetPrayerTimes();
            }
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
                    if (!string.IsNullOrEmpty(manuelSehir))
                    {
                        Konum.Text = manuelSehir;
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

        // IDisposable implementation for timer cleanup
        ~MainPage()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}

using System.Text.Json;
using Microsoft.Maui.Devices.Sensors;
using hadis.Models;

namespace hadis
{
    public partial class MainPage : ContentPage
    {
        Dictionary<string, DateTime> _namazvakitleri;
        private System.Timers.Timer _timer;
        
        public MainPage()
        {
            InitializeComponent();
            _ = NamazVakitleriniÇek();
            _timer = new System.Timers.Timer(500);
            _timer.Elapsed += async (s, e) => await MainThread.InvokeOnMainThreadAsync(GeriSayımıGüncelle);
            _timer.Start();
            _ = ayetgoster();
            _ = KonumBilgisiniGoster();
        }
        
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Status bar rengini güncelle
            UpdateStatusBarColor();
            
            // Özel tema varsa uygula
            ApplyCustomTheme();
            
            // Sayfa her gösterildiğinde konum bilgisini ve namaz vakitlerini güncelle
            await KonumBilgisiniGoster();
            await NamazVakitleriniÇek();
        }

        private void ApplyCustomTheme()
        {
            // Kayitli tema tercihini kontrol et
            string savedTheme = Preferences.Default.Get("AppTheme", "System");
            
            // Eğer Custom tema seçili değilse, varsayılan stillere dön
            if (savedTheme != "Custom")
            {
                ResetToDefaultStyles();
                return;
            }
            
            // Ozel tema yukle
            string customThemeJson = Preferences.Default.Get("CustomTheme", string.Empty);
            
            if (string.IsNullOrEmpty(customThemeJson))
            {
                ResetToDefaultStyles();
                return;
            }
            
            try
            {
                var theme = JsonSerializer.Deserialize<CustomTheme>(customThemeJson);
                if (theme != null)
                {
                    // Arkaplan uygula
                    ApplyCustomBackground(theme.BackgroundImage);
                    
                    // Ana Frame renkleri
                    MainCountdownFrame.BackgroundColor = Color.FromArgb(theme.MainFrameBackground);
                    MainCountdownFrame.BorderColor = Color.FromArgb(theme.MainFrameBorder);
                    
                    // Ana frame text renkleri
                    namazismi.TextColor = Color.FromArgb(theme.MainFrameText);
                    kalan.TextColor = Color.FromArgb(theme.MainFrameText);
                    Konum.TextColor = Color.FromArgb(theme.MainFrameText);
                    
                    // Kucuk Frame'ler renkleri
                    ImsakFrame.BackgroundColor = Color.FromArgb(theme.SmallFrameBackground);
                    ImsakFrame.BorderColor = Color.FromArgb(theme.SmallFrameBorder);
                    imsakyazı.TextColor = Color.FromArgb(theme.SmallFrameText);
                    imsakvakit.TextColor = Color.FromArgb(theme.SmallFrameText);
                    
                    GunesFrame.BackgroundColor = Color.FromArgb(theme.SmallFrameBackground);
                    GunesFrame.BorderColor = Color.FromArgb(theme.SmallFrameBorder);
                    gunesyazı.TextColor = Color.FromArgb(theme.SmallFrameText);
                    gunesvakit.TextColor = Color.FromArgb(theme.SmallFrameText);
                    
                    OgleFrame.BackgroundColor = Color.FromArgb(theme.SmallFrameBackground);
                    OgleFrame.BorderColor = Color.FromArgb(theme.SmallFrameBorder);
                    ogleyazı.TextColor = Color.FromArgb(theme.SmallFrameText);
                    oglevakit.TextColor = Color.FromArgb(theme.SmallFrameText);
                    
                    IkindiFrame.BackgroundColor = Color.FromArgb(theme.SmallFrameBackground);
                    IkindiFrame.BorderColor = Color.FromArgb(theme.SmallFrameBorder);
                    ikindiyazı.TextColor = Color.FromArgb(theme.SmallFrameText);
                    ikindivakit.TextColor = Color.FromArgb(theme.SmallFrameText);
                    
                    AksamFrame.BackgroundColor = Color.FromArgb(theme.SmallFrameBackground);
                    AksamFrame.BorderColor = Color.FromArgb(theme.SmallFrameBorder);
                    aksamyazı.TextColor = Color.FromArgb(theme.SmallFrameText);
                    aksamvakit.TextColor = Color.FromArgb(theme.SmallFrameText);
                    
                    YatsiFrame.BackgroundColor = Color.FromArgb(theme.SmallFrameBackground);
                    YatsiFrame.BorderColor = Color.FromArgb(theme.SmallFrameBorder);
                    yatsıyazı.TextColor = Color.FromArgb(theme.SmallFrameText);
                    yatsıvakit.TextColor = Color.FromArgb(theme.SmallFrameText);
                    
                    // Ayet Frame renkleri
                    AyetFrame.BackgroundColor = Color.FromArgb(theme.AyetFrameBackground);
                    AyetFrame.BorderColor = Color.FromArgb(theme.AyetFrameBorder);
                    gununayeti.TextColor = Color.FromArgb(theme.AyetFrameText);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ozel tema uygulama hatasi: {ex.Message}");
                ResetToDefaultStyles();
            }
        }

        private void ApplyCustomBackground(string backgroundValue)
        {
            if (string.IsNullOrEmpty(backgroundValue))
                return;

            try
            {
                // Kayitli opacity degerini al
                double opacity = Preferences.Default.Get("BackgroundOpacity", 0.3);
                
                // CustomTheme'den opacity degerini al (varsa)
                string customThemeJson = Preferences.Default.Get("CustomTheme", string.Empty);
                if (!string.IsNullOrEmpty(customThemeJson))
                {
                    try
                    {
                        var theme = System.Text.Json.JsonSerializer.Deserialize<Models.CustomTheme>(customThemeJson);
                        if (theme != null)
                        {
                            opacity = theme.BackgroundOpacity;
                        }
                    }
                    catch { }
                }
                
                // Eger resim dosyasiysa
                if (backgroundValue.EndsWith(".jpg") || backgroundValue.EndsWith(".png"))
                {
                    BackgroundImage.Source = ImageSource.FromFile(backgroundValue);
                    BackgroundImage.IsVisible = true;
                    BackgroundOverlay.IsVisible = true;
                    BackgroundOverlay.Opacity = opacity;
                    
                    // Overlay rengi tema bazinda ayarla
                    var currentTheme = Application.Current?.UserAppTheme == AppTheme.Unspecified 
                        ? Application.Current?.RequestedTheme ?? AppTheme.Light 
                        : Application.Current?.UserAppTheme ?? AppTheme.Light;
                    
                    BackgroundOverlay.Background = new SolidColorBrush(
                        currentTheme == AppTheme.Dark ? Colors.Black : Colors.White);
                }
                // Eger gradient ise
                else if (backgroundValue.StartsWith("gradient_"))
                {
                    BackgroundImage.IsVisible = false;
                    BackgroundOverlay.IsVisible = true;
                    
                    switch (backgroundValue)
                    {
                        case "gradient_blue":
                            BackgroundOverlay.Background = new LinearGradientBrush
                            {
                                StartPoint = new Point(0, 0),
                                EndPoint = new Point(1, 1),
                                GradientStops = new GradientStopCollection
                                {
                                    new GradientStop { Color = Color.FromArgb("#1e3c72"), Offset = 0.0f },
                                    new GradientStop { Color = Color.FromArgb("#2a5298"), Offset = 1.0f }
                                }
                            };
                            break;
                        case "gradient_green":
                            BackgroundOverlay.Background = new LinearGradientBrush
                            {
                                StartPoint = new Point(0, 0),
                                EndPoint = new Point(1, 1),
                                GradientStops = new GradientStopCollection
                                {
                                    new GradientStop { Color = Color.FromArgb("#134E5E"), Offset = 0.0f },
                                    new GradientStop { Color = Color.FromArgb("#71B280"), Offset = 1.0f }
                                }
                            };
                            break;
                        case "gradient_dark_blue":
                            BackgroundOverlay.Background = new LinearGradientBrush
                            {
                                StartPoint = new Point(0, 0),
                                EndPoint = new Point(1, 1),
                                GradientStops = new GradientStopCollection
                                {
                                    new GradientStop { Color = Color.FromArgb("#2C3E50"), Offset = 0.0f },
                                    new GradientStop { Color = Color.FromArgb("#4CA1AF"), Offset = 1.0f }
                                }
                            };
                            break;
                        case "gradient_night":
                            BackgroundOverlay.Background = new LinearGradientBrush
                            {
                                StartPoint = new Point(0, 0),
                                EndPoint = new Point(1, 1),
                                GradientStops = new GradientStopCollection
                                {
                                    new GradientStop { Color = Color.FromArgb("#141E30"), Offset = 0.0f },
                                    new GradientStop { Color = Color.FromArgb("#243B55"), Offset = 1.0f }
                                }
                            };
                            break;
                    }
                    BackgroundOverlay.Opacity = opacity;
                }
                // Eger hex renk koduysa
                else if (backgroundValue.StartsWith("#"))
                {
                    BackgroundImage.IsVisible = false;
                    BackgroundOverlay.IsVisible = true;
                    BackgroundOverlay.Background = new SolidColorBrush(Color.FromArgb(backgroundValue));
                    BackgroundOverlay.Opacity = opacity;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Arkaplan uygulama hatasi: {ex.Message}");
            }
        }
        
        private void ResetToDefaultStyles()
        {
            // Aktif temayı al
            var currentTheme = Application.Current?.UserAppTheme == AppTheme.Unspecified 
                ? Application.Current?.RequestedTheme ?? AppTheme.Light 
                : Application.Current?.UserAppTheme ?? AppTheme.Light;

            // Varsayılan renklere dön (Styles.xaml'deki değerler)
            if (currentTheme == AppTheme.Dark)
            {
                // Dark tema varsayılan renkleri
                MainCountdownFrame.BackgroundColor = Colors.Transparent;
                MainCountdownFrame.BorderColor = Color.FromArgb("#26A69A");
                
                namazismi.TextColor = Colors.White;
                kalan.TextColor = Colors.White;
                Konum.TextColor = Colors.White;
                
                ImsakFrame.BackgroundColor = Colors.Transparent;
                ImsakFrame.BorderColor = Color.FromArgb("#26A69A");
                imsakyazı.TextColor = Colors.White;
                imsakvakit.TextColor = Colors.White;
                
                GunesFrame.BackgroundColor = Colors.Transparent;
                GunesFrame.BorderColor = Color.FromArgb("#26A69A");
                gunesyazı.TextColor = Colors.White;
                gunesvakit.TextColor = Colors.White;
                
                OgleFrame.BackgroundColor = Colors.Transparent;
                OgleFrame.BorderColor = Color.FromArgb("#26A69A");
                ogleyazı.TextColor = Colors.White;
                oglevakit.TextColor = Colors.White;
                
                IkindiFrame.BackgroundColor = Colors.Transparent;
                IkindiFrame.BorderColor = Color.FromArgb("#26A69A");
                ikindiyazı.TextColor = Colors.White;
                ikindivakit.TextColor = Colors.White;
                
                AksamFrame.BackgroundColor = Colors.Transparent;
                AksamFrame.BorderColor = Color.FromArgb("#26A69A");
                aksamyazı.TextColor = Colors.White;
                aksamvakit.TextColor = Colors.White;
                
                YatsiFrame.BackgroundColor = Colors.Transparent;
                YatsiFrame.BorderColor = Color.FromArgb("#26A69A");
                yatsıyazı.TextColor = Colors.White;
                yatsıvakit.TextColor = Colors.White;
                
                AyetFrame.BackgroundColor = Color.FromArgb("#E6102825");
                AyetFrame.BorderColor = Color.FromArgb("#26A69A");
                gununayeti.TextColor = Colors.White;
            }
            else
            {
                // Light tema varsayılan renkleri - TÜM FRAME'LER ŞEFFAF
                MainCountdownFrame.BackgroundColor = Colors.Transparent;  // Değişti
                MainCountdownFrame.BorderColor = Color.FromArgb("#009688");

                namazismi.TextColor = Color.FromArgb("#00796B");
                kalan.TextColor = Color.FromArgb("#00796B");
                Konum.TextColor = Color.FromArgb("#00796B");

                ImsakFrame.BackgroundColor = Colors.Transparent;  // Değişti
                ImsakFrame.BorderColor = Color.FromArgb("#009688");
                imsakyazı.TextColor = Color.FromArgb("#00796B");
                imsakvakit.TextColor = Color.FromArgb("#00796B");

                GunesFrame.BackgroundColor = Colors.Transparent;  // Değişti
                GunesFrame.BorderColor = Color.FromArgb("#009688");
                gunesyazı.TextColor = Color.FromArgb("#00796B");
                gunesvakit.TextColor = Color.FromArgb("#00796B");

                OgleFrame.BackgroundColor = Colors.Transparent;  // Değişti
                OgleFrame.BorderColor = Color.FromArgb("#009688");
                ogleyazı.TextColor = Color.FromArgb("#00796B");
                oglevakit.TextColor = Color.FromArgb("#00796B");

                IkindiFrame.BackgroundColor = Colors.Transparent;  // Değişti
                IkindiFrame.BorderColor = Color.FromArgb("#009688");
                ikindiyazı.TextColor = Color.FromArgb("#00796B");
                ikindivakit.TextColor = Color.FromArgb("#00796B");

                AksamFrame.BackgroundColor = Colors.Transparent;  // Değişti
                AksamFrame.BorderColor = Color.FromArgb("#009688");
                aksamyazı.TextColor = Color.FromArgb("#00796B");
                aksamvakit.TextColor = Color.FromArgb("#00796B");

                YatsiFrame.BackgroundColor = Colors.Transparent;  // Değişti
                YatsiFrame.BorderColor = Color.FromArgb("#009688");
                yatsıyazı.TextColor = Color.FromArgb("#00796B");
                yatsıvakit.TextColor = Color.FromArgb("#00796B");

                AyetFrame.BackgroundColor = Colors.Transparent;  // Değişti
                AyetFrame.BorderColor = Color.FromArgb("#00796B");
                gununayeti.TextColor = Color.FromArgb("#00796B");
            }
        }
        
        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            
            // Tab ile gelince frame animasyonları
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
            var imsakTask = Task.WhenAll(
                ImsakFrame.FadeTo(1, 400, Easing.CubicOut),
                ImsakFrame.ScaleTo(1.0, 500, Easing.SpringOut)
            );
            
            await Task.Delay(80);
            
            var gunesTask = Task.WhenAll(
                GunesFrame.FadeTo(1, 400, Easing.CubicOut),
                GunesFrame.ScaleTo(1.0, 500, Easing.SpringOut)
            );
            
            await Task.Delay(80);
            
            var ogleTask = Task.WhenAll(
                OgleFrame.FadeTo(1, 400, Easing.CubicOut),
                OgleFrame.ScaleTo(1.0, 500, Easing.SpringOut)
            );
            
            await Task.Delay(100);
            
            // 3. İkinci satır frame'leri kademeli olarak büyüsün
            var ikindiTask = Task.WhenAll(
                IkindiFrame.FadeTo(1, 400, Easing.CubicOut),
                IkindiFrame.ScaleTo(1.0, 500, Easing.SpringOut)
            );
            
            await Task.Delay(80);
            
            var aksamTask = Task.WhenAll(
                AksamFrame.FadeTo(1, 400, Easing.CubicOut),
                AksamFrame.ScaleTo(1.0, 500, Easing.SpringOut)
            );
            
            await Task.Delay(80);
            
            var yatsiTask = Task.WhenAll(
                YatsiFrame.FadeTo(1, 400, Easing.CubicOut),
                YatsiFrame.ScaleTo(1.0, 500, Easing.SpringOut)
            );
            
            await Task.Delay(150);
            
            // 4. Son olarak ayet frame'i büyüsün
            await Task.WhenAll(
                AyetFrame.FadeTo(1, 500, Easing.CubicOut),
                AyetFrame.ScaleTo(1.0, 600, Easing.SpringOut)
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
        
        private void UpdateStatusBarColor()
        {
            // Aktif temayı al
            var currentTheme = Application.Current?.UserAppTheme == AppTheme.Unspecified 
                ? Application.Current?.RequestedTheme ?? AppTheme.Light 
                : Application.Current?.UserAppTheme ?? AppTheme.Light;

#if ANDROID
            Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.RunOnUiThread(() =>
            {
                var window = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window;
                if (window != null)
                {
                    if (currentTheme == AppTheme.Dark)
                    {
                        // Koyu tema - siyah status bar
                        window.SetStatusBarColor(Android.Graphics.Color.Black);
                        
                        // Android 6.0 ve üzeri için metin rengini ayarla
                        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
                        {
                            // Koyu tema için açık renkli iconlar
                            window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)
                                Android.Views.SystemUiFlags.Visible;
                        }
                    }
                    else
                    {
                        // Açık tema - beyaz status bar
                        window.SetStatusBarColor(Android.Graphics.Color.White);
                        
                        // Android 6.0 ve üzeri için metin rengini ayarla
                        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
                        {
                            // Açık tema için koyu renkli iconlar
                            window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)
                                (Android.Views.SystemUiFlags.LightStatusBar);
                        }
                    }
                }
            });
#endif
        }
        public async Task ayetgoster()
        {
            string[] ayetler = new string[]
            {
            "Hiç bilenlerle bilmeyenler bir olur mu? (Zümer, 9)",
            "Şüphesiz Allah sabredenlerle beraberdir. (Bakara, 153)",
            "Gerçekten güçlükle beraber bir kolaylık vardır. (İnşirah, 6)",
            "Allah, kullarına karşı çok şefkatlidir. (Şura, 19)",
            "Ey iman edenler! Sabır ve namazla Allah’tan yardım isteyin. (Bakara, 45)",
            "Göklerde ve yerde ne varsa hepsi Allah’ındır. (Bakara, 284)",
            "Zorlukla beraber bir kolaylık vardır. (İnşirah, 5)",
            "Kıyamet günü herkese amel defteri verilecektir. (İsra, 13)",
            "İyilik ve takva üzere yardımlaşın. (Maide, 2)",
            "Şüphesiz dönüş ancak Allah’adır. (Bakara, 156)"
            };
            int gunIndex = DateTime.Now.DayOfYear % ayetler.Length;
            string bugununAyeti = ayetler[gunIndex];
            gununayeti.Text = bugununAyeti;


            string[] hadisler = new string[]
            {
            "Ameller niyetlere göredir. (Buhârî, 1)",
            "Kolaylaştırın, zorlaştırmayın. (Buhârî, 11)",
            "Güzel söz sadakadır. (Müslim, 56)",
            "Tebessüm sadakadır. (Tirmizî, Birr 36)",
            "Faydasız şeyi terk et. (Tirmizî, Zühd 11)",
            "Temizlik imanın yarısıdır. (Müslim, Tahâret 1)",
            "Allah işini sağlam yapanı sever. (Taberânî)",
            "En hayırlınız, ahlakı en güzel olandır. (Tirmizî, Birr 61)"
            };
            string bugununhadisi = hadisler[gunIndex];

        }
        public void GeriSayımıGüncelle()
        {

            if (_namazvakitleri == null || _namazvakitleri.Count == 0)
            {
                return;
            }
            TimeSpan kalansure;
            string sonraki;
            DateTime simdi = DateTime.Now;
            if (_namazvakitleri["İmsak"] > simdi)
            {
                kalansure = _namazvakitleri["İmsak"] - simdi;
                sonraki = "İmsak Vaktine";
                aksamvakit.TextColor = Colors.Silver;
                yatsıvakit.TextColor = Colors.White;
            }
            else if (_namazvakitleri["gunes"] > simdi)
            {
                kalansure = _namazvakitleri["gunes"] - simdi;
                sonraki = "Güneşin Doğmasına";
                yatsıvakit.TextColor = Colors.Silver;
                imsakvakit.TextColor = Colors.White;
            }
            else if (_namazvakitleri["Ogle"] > simdi)
            {
                kalansure = _namazvakitleri["Ogle"] - simdi;
                sonraki = "Öğle Namazına";
                imsakvakit.TextColor = Colors.Silver;
                gunesvakit.TextColor = Colors.White;
            }
            else if (_namazvakitleri["İkindi"] > simdi)
            {
                kalansure = _namazvakitleri["İkindi"] - simdi;
                sonraki = "İkindi Namazına";
                gunesvakit.TextColor = Colors.Silver;
                oglevakit.TextColor = Colors.White;
            }
            else if (_namazvakitleri["Aksam"] > simdi)
            {
                kalansure = _namazvakitleri["Aksam"] - simdi;
                sonraki = "Akşam Namazına";
                oglevakit.TextColor = Colors.Silver;
                ikindivakit.TextColor = Colors.White;
            }
            else if (_namazvakitleri["Yatsi"] > simdi)
            {
                kalansure = _namazvakitleri["Yatsi"] - simdi;
                sonraki = "Yatsı Namazına";
                ikindivakit.TextColor = Colors.Silver;
                aksamvakit.TextColor = Colors.White;
            }
            else
            {
                _namazvakitleri["İmsak"] = _namazvakitleri["İmsak"].AddDays(1);
                kalansure = _namazvakitleri["İmsak"] - simdi;
                sonraki = "İmsak Vaktine";
                aksamvakit.TextColor = Colors.Silver;
                yatsıvakit.TextColor = Colors.White;
            }
            namazismi.Text = sonraki;
            kalan.Text = $"{kalansure.Hours:D2} : {kalansure.Minutes:D2} : {kalansure.Seconds:D2}";
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

                var (latitude, longitude) = await GetKonum();
                if (latitude == 0 && longitude == 0)
                {
                    kalan.Text = "- -";
                    namazismi.Text = "";
                    return;
                }

                if (latitude == 0 && longitude == 0)
                {
                    // Konum alınamadıysa işlemi iptal et, UI çökmemiş olur
                    kalan.Text = "- -";
                    namazismi.Text = "";
                    return;
                }
                HttpClient http = new HttpClient();
                string url = $"https://api.aladhan.com/v1/timings?latitude={latitude}&longitude={longitude}&method=13";
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

                _namazvakitleri = new Dictionary<string, DateTime>();
                _namazvakitleri.Add("İmsak", imsakvakti);
                _namazvakitleri.Add("gunes", gunesvakti);
                _namazvakitleri.Add("Ogle", oglevakti);
                _namazvakitleri.Add("İkindi", ikindivakti);
                _namazvakitleri.Add("Aksam", aksamvakti);
                _namazvakitleri.Add("Yatsi", yatsivakti);


            }
            catch (Exception e)
            {
                kalan.Text = "- -";
                yatsıvakit.Text = "- -";
                aksamvakit.Text = "- -";
                ikindivakit.Text = "- -";
                oglevakit.Text = "- -";
                gunesvakit.Text = "- -";
                imsakvakit.Text = "- -";
            }

        }

        public async Task<(double Latiude, double longitude)> GetKonum()
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
                // İlk olarak konum izinlerini kontrol et
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status != PermissionStatus.Granted)
                {
                    Console.WriteLine("Konum izni verilmedi");
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
                    Console.WriteLine("Konum null döndü");
                    return (0, 0);
                }
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                Console.WriteLine($"Konum özelliği desteklenmiyor: {fnsEx.Message}");
                return (0, 0);
            }
            catch (PermissionException pEx)
            {
                Console.WriteLine($"Konum izni hatası: {pEx.Message}");
                return (0, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Konum Hatası: {ex.Message}");
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
                // İlk olarak konum izinlerini kontrol et
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
                        Console.WriteLine($"Geocoding Hatası: {geocodingEx.Message}");
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
                Console.WriteLine($"Konum özelliği desteklenmiyor: {fnsEx.Message}");
                Konum.Text = "Konum Desteklenmiyor";
            }
            catch (PermissionException pEx)
            {
                Console.WriteLine($"Konum izni hatası: {pEx.Message}");
                Konum.Text = "Konum İzni Gerekli";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Konum Bilgisi Hatası: {ex.Message}");
                Konum.Text = "Konum Hatası";
            }
        }

        private async void Konum_Tapped(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new SehirSecim());
        }
    }
}

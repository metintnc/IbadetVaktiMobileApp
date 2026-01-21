using System.Text.Json;
using Microsoft.Maui.Devices.Sensors;
using hadis.Models;
#if ANDROID
using Android.OS;
using Android.Views;
#endif

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

            // Özel tema varsa uygula (bu status bar'ı ayarlar)
            ApplyCustomTheme();

            // Custom tema yoksa, saate göre otomatik arkaplan ayarla (bu da status bar'ı ayarlar)


            // Sayfa her gösterildiğinde konum bilgisini ve namaz vakitlerini güncelle
            await KonumBilgisiniGoster();
            await NamazVakitleriniÇek();
            SetTimeBasedBackground();
        }

        private void SetTimeBasedBackground()
        {
            // Sadece Custom tema değilse otomatik arkaplan uygula
            string savedTheme = Preferences.Default.Get("AppTheme", "System");
            if (savedTheme == "Custom")
            {
                Console.WriteLine("Custom tema aktif - otomatik arkaplan devre dışı");
                Console.WriteLine("⚠️ Status bar rengi Custom tema tarafından ayarlanmalı!");
                return;
            }

            Console.WriteLine("🎨 SetTimeBasedBackground çalışıyor - Status bar rengi ayarlanacak");

            DateTime now = DateTime.Now;
            int currentHour = now.Hour;
            int currentMinute = now.Minute;
            string backgroundImage = "";
            string statusBarColor = "#000000"; // Varsayılan siyah

            Console.WriteLine($"Şu anki saat: {currentHour}:{currentMinute:D2}");

            // Saatlere göre arkaplan ve status bar rengi belirleme
            if (currentHour >= 0 && currentHour < 5)
            {
                backgroundImage = "sun_01.png";
                statusBarColor = "#05051B";
            }
            else if (currentHour == 5 && currentMinute < 30)
            {
                backgroundImage = "sun_02.png";
                statusBarColor = "#060723";
            }
            else if ((currentHour == 5 && currentMinute >= 30) || (currentHour == 6))
            {
                backgroundImage = "sun_03.png";
                statusBarColor = "#4B427E";
            }
            else if (currentHour >= 7 && currentHour < 9)
            {
                backgroundImage = "sun_04.png";
                statusBarColor = "#4077D9";
            }
            else if (currentHour >= 9 && currentHour < 11)
            {
                backgroundImage = "sun_05.png";
                statusBarColor = "#2F71E4";
            }
            else if (currentHour >= 11 && currentHour < 13)
            {
                backgroundImage = "sun_06.png";
                statusBarColor = "#5E92F3";
            }
            else if (currentHour >= 13 && currentHour < 15)
            {
                backgroundImage = "sun_07.png";
                statusBarColor = "#5C89F2";
            }
            else if (currentHour >= 15 && currentHour < 17)
            {
                backgroundImage = "sun_08.png";
                statusBarColor = "#6376C6";
            }
            else if (currentHour >= 17 && currentHour < 19)
            {
                backgroundImage = "sun_09.png";
                statusBarColor = "#22133A";
            }
            else if (currentHour >= 19 && currentHour < 24)
            {
                backgroundImage = "sun_10.png";
                statusBarColor = "#08091D";
            }

            // Arkaplanı uygula
            try
            {
                Console.WriteLine($"Arkaplan resmi ayarlanıyor: {backgroundImage}");

                BackgroundImage.Source = ImageSource.FromFile(backgroundImage);
                BackgroundImage.IsVisible = true;

                // Overlay'i uygula (kontrast kontrolü)
                ApplyContrastOverlay(backgroundImage);

                // Status bar rengini arkaplan resmine göre ayarla
                SetStatusBarColorForBackground(statusBarColor);

                Console.WriteLine($"Arkaplan ve status bar rengi başarıyla ayarlandı! Status Bar: {statusBarColor}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Arkaplan ayarlama hatası: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void SetStatusBarColorForBackground(string hexColor)
        {
#if ANDROID
            try
            {
                var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                if (activity?.Window == null)
                {
                    Console.WriteLine("Activity veya Window null - status bar ayarlanamıyor");
                    return;
                }

                activity.RunOnUiThread(() =>
                {
                    try
                    {
                        // Hex rengini Android Color'a çevir
                        var color = Android.Graphics.Color.ParseColor(hexColor);
                        activity.Window.SetStatusBarColor(color);
                        
                        Console.WriteLine($"Status bar rengi değiştirildi: {hexColor}");
                        
                        // Rengin açık mı koyu mu olduğunu hesapla
                        bool isLightColor = IsColorLight(hexColor);
                        
                        // Android 6.0 ve üzeri için icon rengini ayarla
                        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                        {
                            var decorView = activity.Window.DecorView;
                            var systemUiVisibility = decorView.SystemUiVisibility;
                            
                            if (isLightColor)
                            {
                                // Açık renk için koyu iconlar
                                decorView.SystemUiVisibility = (StatusBarVisibility)
                                    ((int)systemUiVisibility | (int)SystemUiFlags.LightStatusBar);
                                Console.WriteLine("Status bar iconları koyu yapıldı (açık arkaplan için)");
                            }
                            else
                            {
                                // Koyu renk için açık iconlar
                                decorView.SystemUiVisibility = (StatusBarVisibility)
                                    ((int)systemUiVisibility & ~(int)SystemUiFlags.LightStatusBar);
                                Console.WriteLine("Status bar iconları açık yapıldı (koyu arkaplan için)");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Status bar renk ayarlama hatası (UI thread): {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Status bar renk ayarlama hatası: {ex.Message}");
            }
#endif
        }

        private bool IsColorLight(string hexColor)
        {
            // Hex rengi RGB'ye çevir ve parlaklığı hesapla
            try
            {
                hexColor = hexColor.Replace("#", "");
                int r = Convert.ToInt32(hexColor.Substring(0, 2), 16);
                int g = Convert.ToInt32(hexColor.Substring(2, 2), 16);
                int b = Convert.ToInt32(hexColor.Substring(4, 2), 16);

                // Parlaklık hesaplama formülü (0-255 arası)
                double brightness = (r * 0.299 + g * 0.587 + b * 0.114);

                // 128'den büyükse açık renk
                return brightness > 128;
            }
            catch
            {
                return false; // Hata durumunda koyu kabul et
            }
        }

        private void ApplyCustomTheme()
        {
            // Kayitli tema tercihini kontrol et
            string savedTheme = Preferences.Default.Get("AppTheme", "System");

            // Eğer Custom tema seçili değilse, varsayılan stillere dön
            if (savedTheme != "Custom")
            {
                ResetToDefaultStyles();
                // SetTimeBasedBackground kendi status bar'ını ayarlayacak, burada bir şey yapma
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

                    // Custom tema için status bar rengini arkaplan türüne göre ayarla
                    SetStatusBarColorForCustomTheme(theme.BackgroundImage);

                    // Ana Frame renkleri - Glassmorphism
                    MainCountdownFrame.BorderColor = Color.FromArgb(theme.MainFrameBorder);
                    var mainBaseColor = Color.FromArgb(theme.MainFrameBackground);
                    MainCountdownFrame.Background = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop { Color = mainBaseColor.WithAlpha(0.3f), Offset = 0.0f },
                            new GradientStop { Color = mainBaseColor.WithAlpha(0.2f), Offset = 1.0f }
                        }
                    };

                    // Ana frame text renkleri
                    namazismi.TextColor = Color.FromArgb(theme.MainFrameText);
                    kalan.TextColor = Color.FromArgb(theme.MainFrameText);
                    Konum.TextColor = Color.FromArgb(theme.MainFrameText);

                    // Kucuk Frame'ler renkleri - Glassmorphism
                    var smallBaseColor = Color.FromArgb(theme.SmallFrameBackground);
                    var smallBorderColor = Color.FromArgb(theme.SmallFrameBorder);
                    var smallTextColor = Color.FromArgb(theme.SmallFrameText);

                    ImsakFrame.BorderColor = smallBorderColor;
                    ImsakFrame.Background = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop { Color = smallBaseColor.WithAlpha(0.3f), Offset = 0.0f },
                            new GradientStop { Color = smallBaseColor.WithAlpha(0.2f), Offset = 1.0f }
                        }
                    };
                    imsakyazı.TextColor = smallTextColor;
                    imsakvakit.TextColor = smallTextColor;

                    GunesFrame.BorderColor = smallBorderColor;
                    GunesFrame.Background = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop { Color = smallBaseColor.WithAlpha(0.3f), Offset = 0.0f },
                            new GradientStop { Color = smallBaseColor.WithAlpha(0.2f), Offset = 1.0f }
                        }
                    };
                    gunesyazı.TextColor = smallTextColor;
                    gunesvakit.TextColor = smallTextColor;

                    OgleFrame.BorderColor = smallBorderColor;
                    OgleFrame.Background = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop { Color = smallBaseColor.WithAlpha(0.3f), Offset = 0.0f },
                            new GradientStop { Color = smallBaseColor.WithAlpha(0.2f), Offset = 1.0f }
                        }
                    };
                    ogleyazı.TextColor = smallTextColor;
                    oglevakit.TextColor = smallTextColor;

                    IkindiFrame.BorderColor = smallBorderColor;
                    IkindiFrame.Background = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop { Color = smallBaseColor.WithAlpha(0.3f), Offset = 0.0f },
                            new GradientStop { Color = smallBaseColor.WithAlpha(0.2f), Offset = 1.0f }
                        }
                    };
                    ikindiyazı.TextColor = smallTextColor;
                    ikindivakit.TextColor = smallTextColor;

                    AksamFrame.BorderColor = smallBorderColor;
                    AksamFrame.Background = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop { Color = smallBaseColor.WithAlpha(0.3f), Offset = 0.0f },
                            new GradientStop { Color = smallBaseColor.WithAlpha(0.2f), Offset = 1.0f }
                        }
                    };
                    aksamyazı.TextColor = smallTextColor;
                    aksamvakit.TextColor = smallTextColor;

                    YatsiFrame.BorderColor = smallBorderColor;
                    YatsiFrame.Background = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop { Color = smallBaseColor.WithAlpha(0.3f), Offset = 0.0f },
                            new GradientStop { Color = smallBaseColor.WithAlpha(0.2f), Offset = 1.0f }
                        }
                    };
                    yatsıyazı.TextColor = smallTextColor;
                    yatsıvakit.TextColor = smallTextColor;

                    // Ayet Frame - Glassmorphism efekti ile
                    AyetFrame.BorderColor = Color.FromArgb(theme.AyetFrameBorder);

                    // Glassmorphism background
                    var baseColor = Color.FromArgb(theme.AyetFrameBackground);
                    AyetFrame.Background = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1),
                        GradientStops = new GradientStopCollection
                        {
                            new GradientStop { Color = baseColor.WithAlpha(0.3f), Offset = 0.0f },
                            new GradientStop { Color = baseColor.WithAlpha(0.2f), Offset = 1.0f }
                        }
                    };

                    gununayeti.TextColor = Color.FromArgb(theme.AyetFrameText);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ozel tema uygula hatasi: {ex.Message}");
                ResetToDefaultStyles();
            }
        }

        private void SetStatusBarColorForCustomTheme(string backgroundValue)
        {
            if (string.IsNullOrEmpty(backgroundValue))
                return;

            string statusBarColor = "#000000"; // Varsayılan

            // Arkaplan türüne göre status bar rengini belirle
            if (backgroundValue.EndsWith(".jpg") || backgroundValue.EndsWith(".png"))
            {
                // Özel arkaplan resimleri için ortalama bir renk kullan
                // sun_ resimlerine göre ayarla
                if (backgroundValue.Contains("sun_01")) statusBarColor = "#0D1B2A";
                else if (backgroundValue.Contains("sun_02")) statusBarColor = "#1B263B";
                else if (backgroundValue.Contains("sun_03")) statusBarColor = "#415A77";
                else if (backgroundValue.Contains("sun_04")) statusBarColor = "#E07A5F";
                else if (backgroundValue.Contains("sun_05")) statusBarColor = "#81B29A";
                else if (backgroundValue.Contains("sun_06")) statusBarColor = "#3D5A80";
                else if (backgroundValue.Contains("sun_07")) statusBarColor = "#4A90A4";
                else if (backgroundValue.Contains("sun_08")) statusBarColor = "#98C1D9";
                else if (backgroundValue.Contains("sun_09")) statusBarColor = "#EE6C4D";
                else if (backgroundValue.Contains("sun_10")) statusBarColor = "#293241";
                else statusBarColor = "#1A1A1A"; // Diğer resimler için koyu gri
            }
            else if (backgroundValue.StartsWith("gradient_"))
            {
                // Gradient'ler için ilk rengi al
                switch (backgroundValue)
                {
                    case "gradient_blue":
                        statusBarColor = "#1e3c72";
                        break;
                    case "gradient_green":
                        statusBarColor = "#134E5E";
                        break;
                    case "gradient_dark_blue":
                        statusBarColor = "#2C3E50";
                        break;
                    case "gradient_night":
                        statusBarColor = "#141E30";
                        break;
                }
            }
            else if (backgroundValue.StartsWith("#"))
            {
                // Hex renk kodu ise direkt kullan
                statusBarColor = backgroundValue;
            }

            SetStatusBarColorForBackground(statusBarColor);
        }

        private void ApplyCustomBackground(string backgroundValue)
        {
            if (string.IsNullOrEmpty(backgroundValue))
                return;

            try
            {
                double opacity = Preferences.Default.Get("BackgroundOpacity", 0.3);
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

                if (backgroundValue.EndsWith(".jpg") || backgroundValue.EndsWith(".png"))
                {
                    BackgroundImage.Source = ImageSource.FromFile(backgroundValue);
                    BackgroundImage.IsVisible = true;
                    // Overlay'i uygula (kontrast kontrolü)
                    ApplyContrastOverlay(backgroundValue);
                }
                else if (backgroundValue.StartsWith("gradient_"))
                {
                    BackgroundImage.IsVisible = false;
                    // Overlay'i uygula (kontrast kontrolü)
                    ApplyContrastOverlay(backgroundValue);
                }
                else if (backgroundValue.StartsWith("#"))
                {
                    BackgroundImage.IsVisible = false;
                    // Overlay'i uygula (kontrast kontrolü)
                    ApplyContrastOverlay(backgroundValue);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Arkaplan uygulama hatasi: {ex.Message}");
            }
        }

        // --- YENİ: Kontrast overlay fonksiyonu ---
        private void ApplyContrastOverlay(string backgroundValue)
        {
            // Parlak arka planlarda overlay'i daha koyu ve opak yap
            float overlayOpacity = 0.25f;
            Color overlayColor = Colors.Black;
            bool isBright = false;

            // sun_0X.png dosyaları için gün batımı ve gündüz resimleri parlak kabul edilir
            if (backgroundValue.Contains("sun_04") || backgroundValue.Contains("sun_05") || backgroundValue.Contains("sun_06") || backgroundValue.Contains("sun_07") || backgroundValue.Contains("sun_08") || backgroundValue.Contains("sun_09"))
            {
                isBright = true;
            }
            // Gradientler için de açık renkli olanlar
            if (backgroundValue == "gradient_blue" || backgroundValue == "gradient_green")
            {
                isBright = true;
            }
            // Hex renk kodu ise, parlaklık kontrolü
            if (backgroundValue.StartsWith("#") && backgroundValue.Length == 7)
            {
                try
                {
                    int r = Convert.ToInt32(backgroundValue.Substring(1, 2), 16);
                    int g = Convert.ToInt32(backgroundValue.Substring(3, 2), 16);
                    int b = Convert.ToInt32(backgroundValue.Substring(5, 2), 16);
                    double brightness = (r * 0.299 + g * 0.587 + b * 0.114);
                    if (brightness > 180) // Daha parlak renkler için eşik
                        isBright = true;
                }
                catch { }
            }
            if (isBright)
            {
                overlayOpacity = 0.35f; // Parlak arka planlarda daha koyu overlay
            }
            else
            {
                overlayOpacity = 0.20f; // Koyu arka planlarda daha şaffaf overlay
            }
            BackgroundOverlay.IsVisible = true;
            BackgroundOverlay.Background = new SolidColorBrush(overlayColor.WithAlpha(overlayOpacity));
        }

        private void ResetToDefaultStyles()
        {
            // Aktif temayı al
            var currentTheme = Application.Current?.UserAppTheme == AppTheme.Unspecified
                ? Application.Current?.RequestedTheme ?? AppTheme.Light
                : Application.Current?.UserAppTheme ?? AppTheme.Light;

            // NOT: Status bar rengini burada AYARLAMA - SetTimeBasedBackground zaten ayarlıyor!
            // Sadece Custom tema yoksa ve otomatik arkaplan devre dışıysa tema bazlı renk kullan

            // Varsayılan renklere dön (Styles.xaml'deki değerler)
            if (currentTheme == AppTheme.Dark)
            {
                // Dark tema varsayılan renkleri - Glassmorphism
                MainCountdownFrame.BorderColor = Color.FromArgb("#80FFFFFF");
                MainCountdownFrame.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#30FFFFFF"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#20FFFFFF"), Offset = 1.0f }
                    }
                };

                namazismi.TextColor = Colors.White;
                kalan.TextColor = Colors.White;
                Konum.TextColor = Colors.White;

                // Küçük frame'ler - Glassmorphism
                ImsakFrame.BorderColor = Color.FromArgb("#80FFFFFF");
                ImsakFrame.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#30FFFFFF"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#20FFFFFF"), Offset = 1.0f }
                    }
                };
                imsakyazı.TextColor = Colors.White;
                imsakvakit.TextColor = Colors.White;

                GunesFrame.BorderColor = Color.FromArgb("#80FFFFFF");
                GunesFrame.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#30FFFFFF"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#20FFFFFF"), Offset = 1.0f }
                    }
                };
                gunesyazı.TextColor = Colors.White;
                gunesvakit.TextColor = Colors.White;

                OgleFrame.BorderColor = Color.FromArgb("#80FFFFFF");
                OgleFrame.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#30FFFFFF"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#20FFFFFF"), Offset = 1.0f }
                    }
                };
                ogleyazı.TextColor = Colors.White;
                oglevakit.TextColor = Colors.White;

                IkindiFrame.BorderColor = Color.FromArgb("#80FFFFFF");
                IkindiFrame.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#30FFFFFF"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#20FFFFFF"), Offset = 1.0f }
                    }
                };
                ikindiyazı.TextColor = Colors.White;
                ikindivakit.TextColor = Colors.White;

                AksamFrame.BorderColor = Color.FromArgb("#80FFFFFF");
                AksamFrame.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#30FFFFFF"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#20FFFFFF"), Offset = 1.0f }
                    }
                };
                aksamyazı.TextColor = Colors.White;
                aksamvakit.TextColor = Colors.White;

                YatsiFrame.BorderColor = Color.FromArgb("#80FFFFFF");
                YatsiFrame.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#30FFFFFF"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#20FFFFFF"), Offset = 1.0f }
                    }
                };
                yatsıyazı.TextColor = Colors.White;
                yatsıvakit.TextColor = Colors.White;

                // Ayet Frame - Glassmorphism korunuyor
                AyetFrame.BorderColor = Color.FromArgb("#80FFFFFF");
                AyetFrame.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#30FFFFFF"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#20FFFFFF"), Offset = 1.0f }
                    }
                };
                gununayeti.TextColor = Colors.White;
            }
            else
            {
                // Light tema varsayılan renkleri - Glassmorphism
                MainCountdownFrame.BorderColor = Color.FromArgb("#80009688");
                MainCountdownFrame.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#40FFFFFF"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#30FFFFFF"), Offset = 1.0f }
                    }
                };

                namazismi.TextColor = Color.FromArgb("#00796B");
                kalan.TextColor = Color.FromArgb("#00796B");
                Konum.TextColor = Color.FromArgb("#00796B");

                // Küçük frame'ler - Glassmorphism
                ImsakFrame.BorderColor = Color.FromArgb("#80009688");
                ImsakFrame.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#40FFFFFF"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#30FFFFFF"), Offset = 1.0f }
                    }
                };
                imsakyazı.TextColor = Color.FromArgb("#00796B");
                imsakvakit.TextColor = Color.FromArgb("#00796B");

                GunesFrame.BorderColor = Color.FromArgb("#80009688");
                GunesFrame.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#40FFFFFF"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#30FFFFFF"), Offset = 1.0f }
                    }
                };
                gunesyazı.TextColor = Color.FromArgb("#00796B");
                gunesvakit.TextColor = Color.FromArgb("#00796B");

                OgleFrame.BorderColor = Color.FromArgb("#80009688");
                OgleFrame.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#40FFFFFF"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#30FFFFFF"), Offset = 1.0f }
                    }
                };
                ogleyazı.TextColor = Color.FromArgb("#00796B");
                oglevakit.TextColor = Color.FromArgb("#00796B");

                IkindiFrame.BorderColor = Color.FromArgb("#80009688");
                IkindiFrame.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#40FFFFFF"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#30FFFFFF"), Offset = 1.0f }
                    }
                };
                ikindiyazı.TextColor = Color.FromArgb("#00796B");
                ikindivakit.TextColor = Color.FromArgb("#00796B");

                AksamFrame.BorderColor = Color.FromArgb("#80009688");
                AksamFrame.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#40FFFFFF"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#30FFFFFF"), Offset = 1.0f }
                    }
                };
                aksamyazı.TextColor = Color.FromArgb("#00796B");
                aksamvakit.TextColor = Color.FromArgb("#00796B");

                YatsiFrame.BorderColor = Color.FromArgb("#80009688");
                YatsiFrame.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#40FFFFFF"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#30FFFFFF"), Offset = 1.0f }
                    }
                };
                yatsıyazı.TextColor = Color.FromArgb("#00796B");
                yatsıvakit.TextColor = Color.FromArgb("#00796B");

                // Ayet Frame - Glassmorphism korunuyor
                AyetFrame.BorderColor = Color.FromArgb("#8000796B");
                AyetFrame.Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(1, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Color.FromArgb("#40FFFFFF"), Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#30FFFFFF"), Offset = 1.0f }
                    }
                };
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
            "Göklerde ve yerde ne varsа hepsi Allah’ındır. (Bakara, 284)",
            "Zorlukla beraber bir kolaylık vardır. (İnşirah, 5)",
            "Kıyamet günü herkese amel defteri verilecektir. (İsra, 13)",
            "İyilik ve takva üzerine yardımlaşın. (Maide, 2)",
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
            "Tebessüm sadakadir. (Tirmizî, Birr 36)",
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
                    kalan.Text = "- -";
                    namazismi.Text = "";
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
                    Konum.Text = "Lütfen Konum Seçiniz!";
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

using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using hadis.Models;
using hadis.Services;

namespace hadis
{
    public partial class zikirmatik : ContentPage
    {
        private int sayı = 0;
        private int toplam = 0;
        private int hedef = 100;
        private string seciliZikir = "Sübhanallah";
        private bool sesDurum = true;
        private const string ZikirHistoryKey = "ZikirHistory";
        private readonly StatusBarService _statusBarService;
        private readonly TabBarService _tabBarService;
        private readonly IImageService _imageService;

        public zikirmatik(StatusBarService statusBarService, TabBarService tabBarService, IImageService imageService)
        {
            InitializeComponent();
            _statusBarService = statusBarService;
            _tabBarService = tabBarService;
            _imageService = imageService;
            sayı = Preferences.Default.Get("sonSayi", 0);
            toplam = Preferences.Default.Get("Toplam", 0);
            hedef = Preferences.Default.Get("ZikirHedef", 100);
            seciliZikir = Preferences.Default.Get("SeciliZikir", "Sübhanallah");
            sesDurum = Preferences.Default.Get("SesDurum", true);
            zikirsayisi.Text = sayı.ToString();
            SeciliZikirLabel.Text = $"Seçili Zikir: {seciliZikir}";
            HedefLabel.Text = $"Hedef: {hedef}";
            SesTitresimIcon.Text = sesDurum ? "🔊" : "🔇";
            UpdateProgress();
            HeaderFrame.SizeChanged += OnHeaderFrameSizeChanged;
        }

        private void OnHeaderFrameSizeChanged(object sender, EventArgs e)
        {
            if (sender is Frame frame)
            {
                double availableWidth = frame.Width - 48;
                if (availableWidth > 0)
                {
                    double progress = Math.Min((double)sayı / (double)hedef, 1.0);
                    IlerlemeIbresi.WidthRequest = availableWidth * progress;
                }
            }
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            ApplyCustomTheme();
            await AnimateZikirEntry();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            await LoadBackground();

            // Zikirmatik sayfası için özel StatusBar ve TabBar renkleri
            _statusBarService.SetStatusBarColor("#000000"); // Siyah
            _tabBarService.SetTabBarColor("#1D1F1E"); // Özel zikirmatik rengi
            
            Task.Run(async () =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    ApplyCustomTheme();
                });
                await AnimateZikirEntry();
            });
        }

        private async Task LoadBackground()
        {
            try
            {
                string imageName = Application.Current.RequestedTheme == AppTheme.Dark ? "bg_dark.jpg" : "bg_light.jpg";
                BackgroundImage.Source = await _imageService.GetOptimizedBackgroundImageAsync(imageName);
                BackgroundImage.IsVisible = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Zikirmatik Background Load Error: {ex.Message}");
            }
        }

        private void ApplyCustomTheme()
        {
            string savedTheme = Preferences.Default.Get("AppTheme", "System");
            if (savedTheme != "Custom")
            {
                ResetToDefaultStyles();
                return;
            }
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
                    zikirsayisi.TextColor = Color.FromArgb(theme.MainFrameText);
                    ZikirBaslik.TextColor = Color.FromArgb(theme.MainFrameText);
                    zikirbutton.BorderColor = Color.FromArgb(theme.MainFrameBorder);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Özel tema uygulama hatası: {ex.Message}");
                ResetToDefaultStyles();
            }
        }

        private void ResetToDefaultStyles()
        {
            var currentTheme = Application.Current?.UserAppTheme == AppTheme.Unspecified 
                ? Application.Current?.RequestedTheme ?? AppTheme.Light 
                : Application.Current?.UserAppTheme ?? AppTheme.Light;

            if (currentTheme == AppTheme.Dark)
            {
                zikirsayisi.TextColor = Colors.White;
                ZikirBaslik.TextColor = Colors.White;
                zikirbutton.BorderColor = Color.FromArgb("#80FFFFFF");
            }
            else
            {
                zikirsayisi.TextColor = Color.FromArgb("#00796B");
                ZikirBaslik.TextColor = Color.FromArgb("#00796B");
                zikirbutton.BorderColor = Color.FromArgb("#80009688");
            }
        }
        
        private async Task AnimateZikirEntry()
        {
            zikirbutton.Opacity = 0;
            zikirbutton.Scale = 0.5;
            await Task.WhenAll(
                zikirbutton.FadeTo(1, 500, Easing.CubicOut),
                zikirbutton.ScaleTo(1.0, 600, Easing.SpringOut)
            );
        }
        
        protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);
            await Task.WhenAll(
                zikirbutton.FadeTo(0, 300, Easing.CubicIn),
                zikirbutton.ScaleTo(0.5, 400, Easing.CubicIn)
            );
        }

        private Dictionary<string, Dictionary<string, int>> LoadZikirHistory()
        {
            var json = Preferences.Default.Get(ZikirHistoryKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return new Dictionary<string, Dictionary<string, int>>();
            return JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(json) ?? new();
        }

        private void SaveZikirHistory(Dictionary<string, Dictionary<string, int>> history)
        {
            var json = JsonSerializer.Serialize(history);
            Preferences.Default.Set(ZikirHistoryKey, json);
        }

        private async void zikirbutton_Clicked(object sender, EventArgs e)
        {
            sayı++;
            toplam++;
            zikirsayisi.Text = sayı.ToString();
            Preferences.Default.Set("sonSayi", sayı);
            Preferences.Default.Set("Toplam", toplam);

            var history = LoadZikirHistory();
            string today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (!history.ContainsKey(today))
                history[today] = new Dictionary<string, int>();
            if (!history[today].ContainsKey(seciliZikir))
                history[today][seciliZikir] = 0;
            history[today][seciliZikir]++;
            SaveZikirHistory(history);

            UpdateProgress();
            
            if(sayı == hedef)
            {
                await CelebrateAchievement();
                await DisplayAlert("Tebrikler! 🎉", $"{hedef} {seciliZikir} tamamlandı!", "Tamam");
                
                if (sesDurum)
                {
                    try
                    {
                        Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
            else if(sayı % 33 == 0 && sesDurum)
            {
                try
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            else if(sesDurum)
            {
                try
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(30));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            
            await AnimateButton();
        }

        private async Task AnimateButton()
        {
            await zikirbutton.ScaleTo(0.95, 50, Easing.CubicOut);
            await zikirbutton.ScaleTo(1.0, 100, Easing.SpringOut);
        }

        private void UpdateProgress()
        {
            double progress = Math.Min((double)sayı / (double)hedef, 1.0);
            IlerlemeBar.Progress = progress;
            double availableWidth = HeaderFrame.Width > 0 ? HeaderFrame.Width - 48 : 300;
            IlerlemeIbresi.WidthRequest = availableWidth * progress;
            int yuzde = (int)(progress * 100);
            int kalan = Math.Max(0, hedef - sayı);
            IlerlemeYuzdeLabel.Text = $"İlerleme: {yuzde}%";
            KalanLabel.Text = $"(Kalan: {kalan})";
            if (progress >= 1.0)
            {
                IlerlemeYuzdeLabel.TextColor = Colors.Green;
            }
            else
            {
                var currentTheme = Application.Current?.UserAppTheme == AppTheme.Unspecified 
                    ? Application.Current?.RequestedTheme ?? AppTheme.Light 
                    : Application.Current?.UserAppTheme ?? AppTheme.Light;
                IlerlemeYuzdeLabel.TextColor = currentTheme == AppTheme.Dark 
                    ? Colors.White 
                    : Color.FromArgb("#00796B");
            }
        }

        private async Task CelebrateAchievement()
        {
            await zikirbutton.ScaleTo(1.2, 200, Easing.SpringOut);
            await zikirbutton.ScaleTo(1.0, 200, Easing.SpringIn);
        }

        private async void sifirla_Clicked(object sender, EventArgs e)
        {
            bool cevap = await DisplayAlert("Emin misiniz?", "Zikir sayacını sıfırlamak istediğimize emin misiniz?", "Evet, Sıfırla", "Hayır");
            if (cevap)
            {
                sayı = 0;
                zikirsayisi.Text = sayı.ToString();
                Preferences.Default.Set("sonSayi", sayı);
                UpdateProgress();
                
                if (sesDurum)
                {
                    try
                    {
                        Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(100));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
        }

        private async void HedefAyarla_Clicked(object sender, EventArgs e)
        {
            string result = await DisplayPromptAsync(
                "Hedef Ayarla",
                "Yeni hedef değerini girin (33, 66, 99, 100, vb.)",
                initialValue: hedef.ToString(),
                keyboard: Keyboard.Numeric);
            
            if (!string.IsNullOrEmpty(result) && int.TryParse(result, out int yeniHedef) && yeniHedef > 0)
            {
                hedef = yeniHedef;
                HedefLabel.Text = $"Hedef: {hedef}";
                Preferences.Default.Set("ZikirHedef", hedef);
                UpdateProgress();
                await AnimateProgressUpdate();
            }
        }

        private async Task AnimateProgressUpdate()
        {
            await IlerlemeIbresi.ScaleTo(1.05, 200, Easing.CubicOut);
            await IlerlemeIbresi.ScaleTo(1.0, 200, Easing.CubicIn);
        }

        private async void ZikirSec_Clicked(object sender, EventArgs e)
        {
            string[] zikirler = new string[]
            {
                "Sübhanallah",
                "Elhamdülillah",
                "Allahu Ekber",
                "La ilahe illallah",
                "Estağfirullah",
                "Sübhanallahi ve bihamdihi",
                "La havle vela kuvvete illa billah",
                "Kendi Zikrini Gir"
            };
            
            string secim = await DisplayActionSheet("Zikir Seçin", "İptal", null, zikirler);
            
            if (!string.IsNullOrEmpty(secim) && secim != "İptal")
            {
                if (secim == "Kendi Zikrini Gir")
                {
                    string customZikir = await DisplayPromptAsync("Kendi Zikrini Gir", "Lütfen istediğiniz zikri yazın:", initialValue: "", maxLength: 50);
                    if (!string.IsNullOrWhiteSpace(customZikir))
                    {
                        seciliZikir = customZikir.Trim();
                        SeciliZikirLabel.Text = $"Seçili Zikir: {seciliZikir}";
                        Preferences.Default.Set("SeciliZikir", seciliZikir);
                    }
                }
                else
                {
                    seciliZikir = secim;
                    SeciliZikirLabel.Text = $"Seçili Zikir: {seciliZikir}";
                    Preferences.Default.Set("SeciliZikir", seciliZikir);
                }
            }
        }

        private async void SesTitresim_Clicked(object sender, EventArgs e)
        {
            sesDurum = !sesDurum;
            SesTitresimIcon.Text = sesDurum ? "🔊" : "🔇";
            Preferences.Default.Set("SesDurum", sesDurum);
            
            string mesaj = sesDurum ? "Ses/Titreşim Açık" : "Ses/Titreşim Kapalı";
            await DisplayAlert("Bilgi", mesaj, "Tamam");
        }

        private async void Istatistik_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new IstatistikPage());
        }
    }
}

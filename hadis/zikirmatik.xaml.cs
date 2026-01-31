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

        protected override void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            ApplyCustomTheme();
            // Giriş animasyonunu başlat
            _ = AnimateZikirEntry();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // StatusBar rengini ayarla
            _statusBarService.SetStatusBarColor("#000000");
            _tabBarService.SetTabBarColor("#1D1F1E");
        }

        private async Task LoadBackground()
        {
            try
            {
                // UI thread'i bloklamamak için kısa bir gecikme veya yield
                await Task.Yield(); 
                
                string imageName = Application.Current.RequestedTheme == AppTheme.Dark ? "ayarlararkaplan.png" : "bg_light.jpg";
                var imageSource = await _imageService.GetOptimizedBackgroundImageAsync(imageName);
                
                // UI güncellemesi MainThread'de olmalı
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    BackgroundImage.Source = imageSource;
                    BackgroundImage.IsVisible = true;
                    // Opacity animasyonu ile yumuşak geçiş
                    BackgroundImage.FadeTo(1, 500); 
                });
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
            // 1. Görsel animasyonu anında başlat (Fire-and-forget)
            _ = AnimateButton();

            // 2. Titreşim Geri Bildirimi
            if (sesDurum)
            {
                try
                {
#if ANDROID
                    // "Dokunma Titreşimi" kapalı olsa bile titretmek için "Usage.Game" veya "Usage.Alarm" kullanmalıyız.
                    var vibrator = (Android.OS.Vibrator?)Platform.CurrentActivity?.GetSystemService(Android.Content.Context.VibratorService);
                    if (vibrator != null && vibrator.HasVibrator)
                    {
                        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q) // Android 10+
                        {
                            var effect = Android.OS.VibrationEffect.CreateOneShot(80, Android.OS.VibrationEffect.DefaultAmplitude);
                            // Usage.Alarm en güçlü yetkidir ve 'Silent' modda bile çalışabilir.
                            // Obsolete uyarısı için Enum'u (int) cast ederek kullanıyoruz.
                            var attributes = new Android.OS.VibrationAttributes.Builder()
                                .SetUsage((int)Android.OS.VibrationAttributesUsageClass.Alarm) 
                                .Build();
                            vibrator.Vibrate(effect, attributes);
                        }
                        else if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O) // Android 8+
                        {
                            var effect = Android.OS.VibrationEffect.CreateOneShot(80, Android.OS.VibrationEffect.DefaultAmplitude);
                            var audioAttrs = new Android.Media.AudioAttributes.Builder()
                                .SetContentType(Android.Media.AudioContentType.Sonification)
                                .SetUsage(Android.Media.AudioUsageKind.Alarm)
                                .Build();
                            vibrator.Vibrate(effect, audioAttrs);
                        }
                        else
                        {
                            vibrator.Vibrate(80);
                        }
                    }
                    else
                    {
                         Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(80));
                    }
#else
                    // iOS için standart yöntem
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(80));
#endif
                }
                catch
                {
                    // Hata yut
                }
            }
            else
            {
                 Console.WriteLine("DEBUGGING: sesDurum is FALSE. Vibration skipped.");
            }

            // 3. Mantıksal İşlemler
            sayı++;
            toplam++;
            zikirsayisi.Text = sayı.ToString();
            
            // Verileri kaydet
            Preferences.Default.Set("sonSayi", sayı);
            Preferences.Default.Set("Toplam", toplam);

            // Geçmişi güncelle
            var history = LoadZikirHistory();
            string today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (!history.ContainsKey(today)) history[today] = new Dictionary<string, int>();
            if (!history[today].ContainsKey(seciliZikir)) history[today][seciliZikir] = 0;
            history[today][seciliZikir]++;
            SaveZikirHistory(history);

            UpdateProgress();
            
            // 4. Hedef Kontrolü (Ekstra uzun titreşim/uyarı)
            if(sayı == hedef)
            {
                // Hedefe ulaşıldı - Konfetiler ve Uzun Titreşim
                _ = CelebrateAchievement(); // Async bekleme yapma, arayüzü kilitlemesin
                
                if (sesDurum)
                {
                    try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500)); } catch { }
                }

                await DisplayAlert("Tebrikler! 🎉", $"{hedef} {seciliZikir} tamamlandı!", "Tamam");
            }
            else if(sayı % 33 == 0 && sesDurum)
            {
                // 33'lü set ara uyarısı - Hafifçe hissettir
                try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(150)); } catch { }
            }
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

        private void sifirla_Clicked(object sender, EventArgs e)
        {
            // DisplayAlert yerine custom overlay göster
            ResetConfirmationOverlay.IsVisible = true;
        }

        private void OnCancelReset_Clicked(object sender, EventArgs e)
        {
            ResetConfirmationOverlay.IsVisible = false;
        }

        private void OnConfirmReset_Clicked(object sender, EventArgs e)
        {
            ResetConfirmationOverlay.IsVisible = false;

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

        private void HedefAyarla_Clicked(object sender, EventArgs e)
        {
            HedefEntry.Text = hedef.ToString();
            TargetSelectionOverlay.IsVisible = true;
        }

        private void OnCancelTarget_Clicked(object sender, EventArgs e)
        {
            // Klavyeyi kapat
            HedefEntry.IsEnabled = false;
            HedefEntry.IsEnabled = true;
            HedefEntry.Unfocus();
            
            TargetSelectionOverlay.IsVisible = false;
        }

        private void OnPresetTarget_Clicked(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                HedefEntry.Text = btn.Text;
            }
        }

        protected override bool OnBackButtonPressed()
        {
            if (CustomZikirOverlay.IsVisible)
            {
                CustomZikirOverlay.IsVisible = false;
                ZikirSelectionOverlay.IsVisible = true; // Geri basınca listeye dön
                return true;
            }
            if (ZikirSelectionOverlay.IsVisible)
            {
                ZikirSelectionOverlay.IsVisible = false;
                return true;
            }
            if (TargetSelectionOverlay.IsVisible)
            {
                TargetSelectionOverlay.IsVisible = false;
                return true;
            }
            if (ResetConfirmationOverlay.IsVisible)
            {
                ResetConfirmationOverlay.IsVisible = false;
                return true;
            }
            return base.OnBackButtonPressed();
        }

        private async void OnSaveTarget_Clicked(object sender, EventArgs e)
        {
            // Klavyeyi kapat
            HedefEntry.IsEnabled = false;
            HedefEntry.IsEnabled = true;
            HedefEntry.Unfocus();

            if (!string.IsNullOrEmpty(HedefEntry.Text) && int.TryParse(HedefEntry.Text, out int yeniHedef) && yeniHedef > 0)
            {
                hedef = yeniHedef;
                HedefLabel.Text = $"Hedef: {hedef}";
                Preferences.Default.Set("ZikirHedef", hedef);
                UpdateProgress();
                await AnimateProgressUpdate();
                TargetSelectionOverlay.IsVisible = false;
            }
            else
            {
                HedefEntry.Text = "";
                HedefEntry.Placeholder = "Geçerli sayı girin!";
            }
        }

        private async Task AnimateProgressUpdate()
        {
            await IlerlemeIbresi.ScaleTo(1.05, 200, Easing.CubicOut);
            await IlerlemeIbresi.ScaleTo(1.0, 200, Easing.CubicIn);
        }

        private void ZikirSec_Clicked(object sender, EventArgs e)
        {
            ZikirSelectionOverlay.IsVisible = true;
        }

        private void OnCancelZikirSelection_Clicked(object sender, EventArgs e)
        {
            ZikirSelectionOverlay.IsVisible = false;
        }

        private void OnSelectZikir_Clicked(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                seciliZikir = btn.Text;
                SeciliZikirLabel.Text = $"Seçili Zikir: {seciliZikir}";
                Preferences.Default.Set("SeciliZikir", seciliZikir);
                ZikirSelectionOverlay.IsVisible = false;
            }
        }

        private void OnOpenCustomZikir_Clicked(object sender, EventArgs e)
        {
            ZikirSelectionOverlay.IsVisible = false;
            CustomZikirEntry.Text = "";
            CustomZikirOverlay.IsVisible = true;
        }

        private void OnCancelCustomZikir_Clicked(object sender, EventArgs e)
        {
            // Klavyeyi kapat
            CustomZikirEntry.IsEnabled = false;
            CustomZikirEntry.IsEnabled = true;
            CustomZikirEntry.Unfocus();

            CustomZikirOverlay.IsVisible = false;
            ZikirSelectionOverlay.IsVisible = true; // Listeye geri dön
        }

        private void OnSaveCustomZikir_Clicked(object sender, EventArgs e)
        {
            // Klavyeyi kapat
            CustomZikirEntry.IsEnabled = false;
            CustomZikirEntry.IsEnabled = true;
            CustomZikirEntry.Unfocus();

            if (!string.IsNullOrWhiteSpace(CustomZikirEntry.Text))
            {
                seciliZikir = CustomZikirEntry.Text.Trim();
                SeciliZikirLabel.Text = $"Seçili Zikir: {seciliZikir}";
                Preferences.Default.Set("SeciliZikir", seciliZikir);
                CustomZikirOverlay.IsVisible = false;
            }
            else
            {
                CustomZikirEntry.Placeholder = "Lütfen bir zikir yazın!";
            }
        }

        private void SesTitresim_Clicked(object sender, EventArgs e)
        {
            sesDurum = !sesDurum;
            SesTitresimIcon.Text = sesDurum ? "📳" : "🔕";
            Preferences.Default.Set("SesDurum", sesDurum);
            
            // Kullanıcıya geri bildirim olarak sadece titreşim verelim, diyalog kutusu açmak yerine.
            try 
            {
               Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(50));
            } 
            catch {}
        }

        private async void Istatistik_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new IstatistikPage());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using hadis.Helpers;
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
        private readonly BackgroundService _backgroundService;
        private bool _stateRestored = false;
        private string? _currentImageName;

        private static readonly Color CyanLightColor = Color.FromArgb("#67E8F9");
        private static readonly Color EmeraldColor = Color.FromArgb("#34D399");

        public zikirmatik(StatusBarService statusBarService, BackgroundService backgroundService)
        {
            InitializeComponent();
            _statusBarService = statusBarService;
            _backgroundService = backgroundService;
            InitializeBackgroundSync();
        }

        private void InitializeBackgroundSync()
        {
            try
            {
                string savedTheme = Preferences.Default.Get(AppConstants.PREF_APP_THEME, AppConstants.THEME_SYSTEM);

                if (savedTheme == AppConstants.THEME_SYSTEM || savedTheme.StartsWith("Main"))
                {
                    var now = DateTime.Now;
                    var info = TimeBasedBackgroundConfig.GetBackgroundForTime(now.Hour, now.Minute);
                    BackgroundImage.Source = info.Image;
                    _currentImageName = info.Image;
                }

                ApplyTimeBasedBackground();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Zikirmatik Init Background Error: {ex.Message}");
            }
        }

        private void ApplyTimeBasedBackground()
        {
            string savedTheme = Preferences.Default.Get(AppConstants.PREF_APP_THEME, AppConstants.THEME_SYSTEM);

            if (savedTheme == AppConstants.THEME_CUSTOM)
            {
                string customThemeJson = Preferences.Default.Get(AppConstants.PREF_CUSTOM_THEME, string.Empty);
                if (!string.IsNullOrEmpty(customThemeJson))
                {
                    try
                    {
                        var theme = JsonSerializer.Deserialize<CustomTheme>(customThemeJson);
                        if (theme != null && !string.IsNullOrEmpty(theme.BackgroundImage))
                        {
                            _backgroundService.ApplyCustomBackground(BackgroundImage, BackgroundOverlay, theme.BackgroundImage);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Zikirmatik Custom Tema Hatası: {ex.Message}");
                    }
                }
            }

            var result = _backgroundService.SetTimeBasedBackground(BackgroundImage, BackgroundOverlay, savedTheme, _currentImageName);
            if (!string.IsNullOrEmpty(result.ImageName))
            {
                _currentImageName = result.ImageName;
            }
        }

        private void OnHeaderFrameSizeChanged(object sender, EventArgs e)
        {
            if (sender is Border frame)
            {
                double availableWidth = frame.Width - 36;
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

            if (!_stateRestored)
            {
                _stateRestored = true;
                RestorePersistedState();
            }
        }

        private void RestorePersistedState()
        {
            sayı = Preferences.Default.Get("sonSayi", 0);
            toplam = Preferences.Default.Get("Toplam", 0);
            hedef = Preferences.Default.Get("ZikirHedef", 100);
            seciliZikir = Preferences.Default.Get("SeciliZikir", "Sübhanallah");
            sesDurum = Preferences.Default.Get("SesDurum", true);

            zikirsayisi.Text = sayı.ToString();
            HedefLabel.Text = $"Hedef: {hedef}";
            UpdateSelectedDhikrLabels();
            UpdateVibrationUI();
            UpdateProgress();
        }

        private void UpdateSelectedDhikrLabels()
        {
            SeciliZikirLabel.Text = $"Seçili Zikir: {seciliZikir}";
            DhikrPillLabel.Text = seciliZikir switch
            {
                "Sübhanallah" => "سُبْحَانَ اللَّهِ  « Sübhanallah »",
                "Elhamdülillah" => "الْحَمْدُ لِلَّهِ  « Elhamdülillah »",
                "Allahu Ekber" => "اللَّهُ أَكْبَرُ  « Allahu Ekber »",
                "La ilahe illallah" => "لَا إِلَٰهَ إِلَّا اللَّهُ  « La ilahe illallah »",
                "Estağfirullah" => "أَسْتَغْفِرُ اللَّهَ  « Estağfirullah »",
                _ => $"« {seciliZikir} »"
            };
        }

        private void UpdateVibrationUI()
        {
            SesTitresimIcon.Text = sesDurum ? "📳" : "🔕";
            SesTitresimText.Text = sesDurum ? "Titreşim" : "Sessiz";
            HapticToggleBorder.Opacity = sesDurum ? 1.0 : 0.65;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ApplyTimeBasedBackground();
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
                    zikirbutton.Stroke = Color.FromArgb(theme.MainFrameBorder);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Özel tema uygulama hatası: {ex.Message}");
                ResetToDefaultStyles();
            }
        }

        private void ResetToDefaultStyles()
        {
            zikirsayisi.TextColor = Colors.White;
            ZikirBaslik.TextColor = Colors.White;
            zikirbutton.Stroke = Color.FromArgb("#38FFFFFF");
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
                    var vibrator = (Android.OS.Vibrator?)Platform.CurrentActivity?.GetSystemService(Android.Content.Context.VibratorService);
                    if (vibrator != null && vibrator.HasVibrator)
                    {
                        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q) // Android 10+
                        {
                            var effect = Android.OS.VibrationEffect.CreateOneShot(80, Android.OS.VibrationEffect.DefaultAmplitude);
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
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(80));
#endif
                }
                catch
                {
                    // Hata yut
                }
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
            if (sayı == hedef)
            {
                _ = CelebrateAchievement();

                if (sesDurum)
                {
                    try { Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500)); } catch { }
                }

                await DisplayAlert("Tebrikler! 🎉", $"{hedef} {seciliZikir} tamamlandı!", "Tamam");
            }
            else if (sayı % 33 == 0 && sesDurum)
            {
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
            double availableWidth = HeaderFrame.Width > 0 ? HeaderFrame.Width - 36 : 300;
            IlerlemeIbresi.WidthRequest = availableWidth * progress;
            int yuzde = (int)(progress * 100);
            int kalan = Math.Max(0, hedef - sayı);
            IlerlemeYuzdeLabel.Text = $"İlerleme: %{yuzde}";
            KalanLabel.Text = $"Kalan: {kalan}";
            IlerlemeYuzdeLabel.TextColor = progress >= 1.0 ? EmeraldColor : CyanLightColor;
        }

        private async Task CelebrateAchievement()
        {
            await zikirbutton.ScaleTo(1.15, 200, Easing.SpringOut);
            await zikirbutton.ScaleTo(1.0, 200, Easing.SpringIn);
        }

        private void sifirla_Clicked(object sender, EventArgs e)
        {
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
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
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
                ZikirSelectionOverlay.IsVisible = true;
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

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("//MainPage");
            });
            return true;
        }

        private async void OnSaveTarget_Clicked(object sender, EventArgs e)
        {
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
                UpdateSelectedDhikrLabels();
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
            CustomZikirEntry.IsEnabled = false;
            CustomZikirEntry.IsEnabled = true;
            CustomZikirEntry.Unfocus();

            CustomZikirOverlay.IsVisible = false;
            ZikirSelectionOverlay.IsVisible = true;
        }

        private void OnSaveCustomZikir_Clicked(object sender, EventArgs e)
        {
            CustomZikirEntry.IsEnabled = false;
            CustomZikirEntry.IsEnabled = true;
            CustomZikirEntry.Unfocus();

            if (!string.IsNullOrWhiteSpace(CustomZikirEntry.Text))
            {
                seciliZikir = CustomZikirEntry.Text.Trim();
                UpdateSelectedDhikrLabels();
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
            UpdateVibrationUI();
            Preferences.Default.Set("SesDurum", sesDurum);

            try
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(50));
            }
            catch { }
        }

        private async void Istatistik_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new IstatistikPage());
        }
    }
}

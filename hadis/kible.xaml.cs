using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using hadis.Services;
using hadis.Helpers;
using hadis.Models;
using Microsoft.Maui.ApplicationModel;

namespace hadis
{
    public partial class kible : ContentPage
    {
        private readonly Pusula compass;
        private readonly StatusBarService _statusBarService;
        private readonly TabBarService _tabBarService;
        private readonly BackgroundService _backgroundService;
        private readonly INativeCompassService _nativeCompassService;
        private readonly IImageService _imageService;

        // State flags
        private bool _isCompassRunning;
        private CompassAccuracy _currentAccuracy = CompassAccuracy.Unreliable;
        private Action? _onPermissionConfirm;
        private string? _currentImageName;

        private static readonly CultureInfo TurkishCulture = new("tr-TR");
        private static readonly Color EmeraldColor = Color.FromArgb("#34D399");
        private static readonly Color EmeraldLightColor = Color.FromArgb("#6EE7B7");
        private static readonly Color EmeraldBgColor = Color.FromArgb("#99022C22");
        private static readonly Color EmeraldStrokeColor = Color.FromArgb("#4D10B981");
        private static readonly Color AmberColor = Color.FromArgb("#FBBF24");
        private static readonly Color AmberLightColor = Color.FromArgb("#FDE68A");
        private static readonly Color AmberBgColor = Color.FromArgb("#99451A03");
        private static readonly Color AmberStrokeColor = Color.FromArgb("#4DF59E0B");

        public kible(
            StatusBarService statusBarService,
            TabBarService tabBarService,
            BackgroundService backgroundService,
            INativeCompassService nativeCompassService,
            IImageService imageService)
        {
            InitializeComponent();
            compass = new Pusula();
            compass.KonumBilgisiGuncellendi += OnCompassLocationUpdated;

            _statusBarService = statusBarService;
            _tabBarService = tabBarService;
            _backgroundService = backgroundService;
            _nativeCompassService = nativeCompassService;
            _imageService = imageService;

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
                System.Diagnostics.Debug.WriteLine($"Kible Init Background Error: {ex.Message}");
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
                        System.Diagnostics.Debug.WriteLine($"Kible Custom Tema Hatası: {ex.Message}");
                    }
                }
            }

            var result = _backgroundService.SetTimeBasedBackground(BackgroundImage, BackgroundOverlay, savedTheme, _currentImageName);
            if (!string.IsNullOrEmpty(result.ImageName))
            {
                _currentImageName = result.ImageName;
            }
        }

        private void OnBackgroundImageHandlerChanged(object? sender, EventArgs e)
        {
#if ANDROID
            if (BackgroundImage?.Handler?.PlatformView is Android.Widget.ImageView imageView &&
                OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                imageView.SetRenderEffect(
                    Android.Graphics.RenderEffect.CreateBlurEffect(28f, 28f, Android.Graphics.Shader.TileMode.Clamp!));
            }
#endif
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                ApplyTimeBasedBackground();
                UpdateLocationDisplayFromPreferences();

                // Hide calibration warning on entry; it will appear only if compass reports low accuracy
                if (CalibrationWarningFrame != null)
                {
                    CalibrationWarningFrame.IsVisible = false;
                    CalibrationWarningFrame.Opacity = 0;
                }

                await CheckAndStartCompass();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Kible OnAppearing hatası: {ex.Message}");
            }
        }

        private void OnCompassLocationUpdated()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateLocationDisplay(compass.HedefKibleAcisi, compass.MesafeKm);
            });
        }

        private void UpdateLocationDisplayFromPreferences()
        {
            if (compass.MesafeKm > 0)
            {
                UpdateLocationDisplay(compass.HedefKibleAcisi, compass.MesafeKm);
                return;
            }

            string locationName = GetDisplayLocationName();
            if (!string.IsNullOrEmpty(locationName))
            {
                LocationBearingLabel.Text = $"{locationName} • Hesaplanıyor...";
            }
        }

        private void UpdateLocationDisplay(double qiblaBearing, double distanceKm)
        {
            string locationName = GetDisplayLocationName();
            int roundedBearing = (int)Math.Round(qiblaBearing) % 360;
            string cardinal = GetCardinalDirection(roundedBearing);

            LocationBearingLabel.Text = string.IsNullOrEmpty(locationName)
                ? $"{roundedBearing}° {cardinal}"
                : $"{locationName} • {roundedBearing}° {cardinal}";

            if (distanceKm > 0)
            {
                int roundedDistance = (int)Math.Round(distanceKm);
                DistanceLabel.Text = $"{roundedDistance.ToString("N0", TurkishCulture)} km Mesafe";
            }
        }

        private static string GetDisplayLocationName()
        {
            string sehir = Preferences.Default.Get("ManuelSehir", "");
            string ilce = Preferences.Default.Get("ManuelIlce", "");

            if (!string.IsNullOrWhiteSpace(ilce) && !ilce.Equals(sehir, StringComparison.OrdinalIgnoreCase))
            {
                return $"{ilce}, {sehir}";
            }

            return !string.IsNullOrWhiteSpace(sehir) ? sehir : string.Empty;
        }

        private static string GetCardinalDirection(double angle)
        {
            string[] directions = { "K", "KD", "D", "GD", "G", "GB", "B", "KB" };
            int index = (int)Math.Round(((angle % 360) / 45.0)) % 8;
            return directions[index];
        }

        private async Task CheckAndStartCompass()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status != PermissionStatus.Granted)
            {
                LocationWarningFrame.IsVisible = true;
                kibleoku.IsVisible = false;
                AciDegeri.IsVisible = false;
                DirectionHintBorder.IsVisible = false;
                CalibrationWarningFrame.IsVisible = false;

                StopCompassLogic();
            }
            else
            {
                LocationWarningFrame.IsVisible = false;
                kibleoku.IsVisible = true;
                AciDegeri.IsVisible = true;
                DirectionHintBorder.IsVisible = true;

                StartCompassLogic();
            }
        }

        private void StartCompassLogic()
        {
            // Guard: Zaten çalışıyorsa tekrar başlatma
            if (_isCompassRunning) return;

            _nativeCompassService.AccuracyChanged += OnCompassAccuracyChanged;
            _nativeCompassService.Start();

            Task.Run(async () =>
            {
                bool success = await compass.KontrolEt();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (success)
                    {
                        compass.AciDegisti += KibleOkunuDondur;
                    }
                    else
                    {
                        ShowPermissionOverlay(
                            "Konum Alınamadı",
                            "Konum bilgisi alınamadı. Lütfen cihazınızın konum (GPS) özelliğinin açık olduğundan emin olun.",
                            "Tamam",
                            () => { },
                            "");
                    }
                });
            });

            _isCompassRunning = true;
        }

        private void StopCompassLogic()
        {
            // Guard: Zaten durmuşsa tekrar durdurma
            if (!_isCompassRunning) return;

            compass.PusulaDurdur();
            compass.AciDegisti -= KibleOkunuDondur;

            _nativeCompassService.Stop();
            _nativeCompassService.AccuracyChanged -= OnCompassAccuracyChanged;

            _isCompassRunning = false;
        }

        private async void OnRequestLocationPermission_Clicked(object sender, EventArgs e)
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
            {
                await CheckAndStartCompass();
                return;
            }

            if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
            {
                ShowPermissionOverlay(
                    "Konum İzni",
                    "Kıble yönünü doğru hesaplayabilmek için konum iznine ihtiyacımız var.",
                    "Tamam",
                    async () =>
                    {
                        var s = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                        if (s == PermissionStatus.Granted) await CheckAndStartCompass();
                    });
                return;
            }

            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
            {
                await CheckAndStartCompass();
            }
            else
            {
                ShowPermissionOverlay(
                    "İzin Gerekli",
                    "Konum izni verilmediği için kıble yönü hesaplanamıyor. Ayarlardan izin vermek ister misiniz?",
                    "Ayarlara Git",
                    () => { AppInfo.ShowSettingsUI(); },
                    "İptal");
            }
        }

        private void ShowPermissionOverlay(string title, string message, string confirmText, Action onConfirm, string cancelText = "İptal")
        {
            PermissionOverlayTitle.Text = title;
            PermissionOverlayMessage.Text = message;
            PermissionOverlayConfirmButton.Text = confirmText;
            PermissionOverlayCancelButton.Text = cancelText;

            _onPermissionConfirm = onConfirm;

            PermissionOverlayCancelButton.IsVisible = !string.IsNullOrEmpty(cancelText);

            PermissionOverlay.IsVisible = true;
        }

        private void OnPermissionOverlayConfirm_Clicked(object sender, EventArgs e)
        {
            PermissionOverlay.IsVisible = false;
            _onPermissionConfirm?.Invoke();
        }

        private void OnPermissionOverlayCancel_Clicked(object sender, EventArgs e)
        {
            PermissionOverlay.IsVisible = false;
        }

        private void OnCompassAccuracyChanged(CompassAccuracy accuracy)
        {
            _currentAccuracy = accuracy;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                UpdateAccuracyBadge(accuracy);

                bool shouldBeVisible = (accuracy != CompassAccuracy.High);

                if (CalibrationWarningFrame != null)
                {
                    if (shouldBeVisible)
                    {
                        if (!CalibrationWarningFrame.IsVisible || CalibrationWarningFrame.Opacity < 1)
                        {
                            CalibrationWarningFrame.IsVisible = true;
                            await CalibrationWarningFrame.FadeTo(1, 250);
                        }
                    }
                    else
                    {
                        if (CalibrationWarningFrame.IsVisible && CalibrationWarningFrame.Opacity > 0)
                        {
                            await CalibrationWarningFrame.FadeTo(0, 1500);
                            CalibrationWarningFrame.IsVisible = false;
                        }
                    }
                }
            });
        }

        private void UpdateAccuracyBadge(CompassAccuracy accuracy)
        {
            switch (accuracy)
            {
                case CompassAccuracy.High:
                    AccuracyIconLabel.Text = "✓";
                    AccuracyIconLabel.TextColor = EmeraldColor;
                    AccuracyTextLabel.Text = "Yüksek Doğruluk";
                    AccuracyTextLabel.TextColor = EmeraldLightColor;
                    break;
                case CompassAccuracy.Medium:
                    AccuracyIconLabel.Text = "•";
                    AccuracyIconLabel.TextColor = AmberColor;
                    AccuracyTextLabel.Text = "Orta Doğruluk";
                    AccuracyTextLabel.TextColor = AmberLightColor;
                    break;
                default:
                    AccuracyIconLabel.Text = "!";
                    AccuracyIconLabel.TextColor = AmberColor;
                    AccuracyTextLabel.Text = "Kalibrasyon Gerekli";
                    AccuracyTextLabel.TextColor = AmberLightColor;
                    break;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            StopCompassLogic();
        }

        public void KibleOkunuDondur(double gelenaci)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Optimize edilmiş smooth rotation
                kibleoku.SmoothRotateTo(gelenaci, 100);

                double rawAngle = (360 - (gelenaci % 360)) % 360;
                int displayAngle = (int)Math.Round(rawAngle);

                if (displayAngle == 360) displayAngle = 0;

                int diff = displayAngle <= 180 ? displayAngle : 360 - displayAngle;
                AciDegeri.Text = $"{displayAngle}°";

                if (diff <= 3)
                {
                    AciDegeri.TextColor = EmeraldColor;
                    DirectionHintLabel.Text = "KIBLE YÖNÜNDESİNİZ";
                    DirectionHintLabel.TextColor = EmeraldLightColor;
                    DirectionHintBorder.BackgroundColor = EmeraldBgColor;
                    DirectionHintBorder.Stroke = EmeraldStrokeColor;
                    DiffAngleLabel.Text = "0° Tam İsabet";
                    DiffAngleLabel.TextColor = EmeraldColor;
                }
                else
                {
                    AciDegeri.TextColor = Colors.White;
                    DirectionHintLabel.Text = displayAngle <= 180
                        ? $"{diff}° SOLA DÖNÜN"
                        : $"{diff}° SAĞA DÖNÜN";
                    DirectionHintLabel.TextColor = AmberLightColor;
                    DirectionHintBorder.BackgroundColor = AmberBgColor;
                    DirectionHintBorder.Stroke = AmberStrokeColor;
                    DiffAngleLabel.Text = $"{diff}° Fark";
                    DiffAngleLabel.TextColor = AmberColor;
                }

                if (diff <= 5 && QiblaCheckmark != null)
                {
                    if (QiblaCheckmark.Opacity == 0)
                    {
                        QiblaCheckmark.FadeTo(1, 200);
                    }
                }
                else if (QiblaCheckmark != null)
                {
                    if (QiblaCheckmark.Opacity > 0)
                    {
                        QiblaCheckmark.FadeTo(0, 200);
                    }
                }
            });
        }

        protected override bool OnBackButtonPressed()
        {
            // Ana sayfaya (Vakitler) dön
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("//MainPage");
            });
            return true;
        }
    }
}

using System;
using System.Threading.Tasks;
using hadis.Services;
using hadis.Helpers;

using Microsoft.Maui.ApplicationModel;

namespace hadis
{
    public partial class kible : ContentPage
    {
        private Pusula compass;
        private readonly StatusBarService _statusBarService;
        private readonly TabBarService _tabBarService;
        private readonly INativeCompassService _nativeCompassService;
        private readonly IImageService _imageService;
        
        // State flag to prevent duplicate event registration
        private bool _isCompassRunning;
        private bool _inInitialWarningPeriod;
        private CompassAccuracy _currentAccuracy = CompassAccuracy.Unreliable;
        private Action? _onPermissionConfirm;
        
        // Animasyon için element array'i (allocation optimize)
        private VisualElement[]? _kibleElements;
        private VisualElement[] KibleElements => _kibleElements ??= new VisualElement[] { kibleoku, AciDegeri };

        public kible(StatusBarService statusBarService, TabBarService tabBarService, INativeCompassService nativeCompassService, IImageService imageService)
        {
            InitializeComponent();
            compass = new Pusula();
            _statusBarService = statusBarService;
            _tabBarService = tabBarService;
            _nativeCompassService = nativeCompassService;
            _imageService = imageService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            try
            {
                _statusBarService.SetStatusBarColor("#000000");
                
                _inInitialWarningPeriod = true;
                if (CalibrationWarningFrame != null)
                    CalibrationWarningFrame.IsVisible = true;

                _ = Task.Run(async () => 
                {
                    await Task.Delay(5000);
                    _inInitialWarningPeriod = false;
                    
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnCompassAccuracyChanged(_currentAccuracy);
                    });
                });

                await CheckAndStartCompass();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Kible OnAppearing hatasý: {ex.Message}");
            }
        }

        private async Task CheckAndStartCompass()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            
            if (status != PermissionStatus.Granted)
            {
                LocationWarningFrame.IsVisible = true;
                kibleoku.IsVisible = false;
                AciDegeri.IsVisible = false;
                CalibrationWarningFrame.IsVisible = false;
                
                StopCompassLogic();
            }
            else
            {
                LocationWarningFrame.IsVisible = false;
                kibleoku.IsVisible = true;
                AciDegeri.IsVisible = true;
                
                StartCompassLogic();
            }
        }

        private void StartCompassLogic()
        {
            // Guard: Zaten çalýþýyorsa tekrar baþlatma
            if (_isCompassRunning) return;
            
            _nativeCompassService.AccuracyChanged += OnCompassAccuracyChanged;
            _nativeCompassService.Start();

            Task.Run(async () =>
            {
                await compass.KontrolEt();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    compass.AciDegisti += KýbleOkunuDondur;
                });
            });
            
            _isCompassRunning = true;
        }
        
        private void StopCompassLogic()
        {
            // Guard: Zaten durmuþsa tekrar durdurma
            if (!_isCompassRunning) return;
            
            compass.PusulaDurdur();
            compass.AciDegisti -= KýbleOkunuDondur;
            
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
                    "Konum Ýzni", 
                    "Kýble yönünü doðru hesaplayabilmek için konum iznine ihtiyacýmýz var.", 
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
                    "Ýzin Gerekli", 
                    "Konum izni verilmediði için kýble yönü hesaplanamýyor. Ayarlardan izin vermek ister misiniz?", 
                    "Ayarlara Git",
                    () => { AppInfo.ShowSettingsUI(); },
                    "Ýptal");
            }
        }

        private void ShowPermissionOverlay(string title, string message, string confirmText, Action onConfirm, string cancelText = "Ýptal")
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
                bool shouldBeVisible = _inInitialWarningPeriod || 
                    (accuracy != CompassAccuracy.High && accuracy != CompassAccuracy.Medium);

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
        
        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            await AnimateKibleEntry();
        }
        
        private async Task AnimateKibleEntry()
        {
            // Optimize edilmiþ animasyon
            AnimationHelpers.PrepareForAnimation(KibleElements);
            
            // Kýble oku önce
            await kibleoku.AnimateIn(600, 800);
            
            // Açý deðeri sonra
            await AciDegeri.AnimateIn(400, 500);
        }
        
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            StopCompassLogic();
        }
        
        protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);
            await AnimateKibleExit();
        }
        
        private async Task AnimateKibleExit()
        {
            // Optimize edilmiþ paralel çýkýþ animasyonu
            await AnimationHelpers.AnimateOutParallel(KibleElements);
        }

        public void KýbleOkunuDondur(double gelenaci)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Optimize edilmiþ smooth rotation
                kibleoku.SmoothRotateTo(gelenaci, 100);

                double rawAngle = (360 - (gelenaci % 360)) % 360;
                int displayAngle = (int)Math.Round(rawAngle);
                
                if (displayAngle == 360) displayAngle = 0;

                AciDegeri.Text = $"{displayAngle}°";
                
                if(displayAngle == 0)
                {
                    AciDegeri.TextColor = Colors.Green;
                }
                else
                {
                    AciDegeri.ClearValue(Label.TextColorProperty);
                }

                if ((displayAngle >= 355 || displayAngle <= 5) && QiblaCheckmark != null)
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
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("//MainPage");
            });
            return true;
        }
    }
}

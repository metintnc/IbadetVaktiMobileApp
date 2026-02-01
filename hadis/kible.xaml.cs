using System;
using System.Threading.Tasks;
using hadis.Services;

using Microsoft.Maui.ApplicationModel;

namespace hadis
{
    public partial class kible : ContentPage
    {
        private Pusula compass;
        private bool _animationPlayed = false;
        private readonly StatusBarService _statusBarService;
        private readonly TabBarService _tabBarService;
        private readonly INativeCompassService _nativeCompassService;
        private readonly IImageService _imageService;
        
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
            
            // Kıble sayfası için özel StatusBar ve TabBar renkleri
            _statusBarService.SetStatusBarColor("#000000"); // Siyah
            _tabBarService.SetTabBarColor("#19222B"); // Özel kıble rengi
            
            await CheckAndStartCompass();
        }

        private async Task CheckAndStartCompass()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            
            if (status != PermissionStatus.Granted)
            {
                // İzin yoksa uyarıyı göster, diğer her şeyi gizle
                LocationWarningFrame.IsVisible = true;
                kibleoku.IsVisible = false;
                AciDegeri.IsVisible = false;
                CalibrationWarningFrame.IsVisible = false;
                
                // Arka planda çalışmaması için durdur
                StopCompassLogic();
            }
            else
            {
                // İzin varsa normal akış
                LocationWarningFrame.IsVisible = false;
                kibleoku.IsVisible = true;
                AciDegeri.IsVisible = true;
                
                StartCompassLogic();
            }
        }

        private void StartCompassLogic()
        {
            // Zaten çalışıyorsa tekrar başlatma (basit kontrol)
            _nativeCompassService.AccuracyChanged -= OnCompassAccuracyChanged;
            _nativeCompassService.AccuracyChanged += OnCompassAccuracyChanged;
            _nativeCompassService.Start();

            Task.Run(async () =>
            {
                await compass.KontrolEt();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    compass.AciDegisti -= KıbleOkunuDondur; // Çift eklemeyi önle
                    compass.AciDegisti += KıbleOkunuDondur;
                });
            });
        }
        
        private void StopCompassLogic()
        {
            compass.PusulaDurdur();
            compass.AciDegisti -= KıbleOkunuDondur;
            
            _nativeCompassService.Stop();
            _nativeCompassService.AccuracyChanged -= OnCompassAccuracyChanged;
        }

        private Action _onPermissionConfirm;

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
                // Eğer izin reddedildiyse ve rasyonel gösterilmiyorsa (kalıcı ret durumu olabilir)
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
            MainThread.BeginInvokeOnMainThread(() =>
            {
                bool showWarning = false;

                switch (accuracy)
                {
                    case CompassAccuracy.High:
                    case CompassAccuracy.Medium:
                        showWarning = false;
                        break;
                    case CompassAccuracy.Low:
                    case CompassAccuracy.Unreliable:
                        showWarning = true;
                        break;
                    default:
                        showWarning = false;
                        break;
                }

                if (CalibrationWarningFrame != null)
                {
                    CalibrationWarningFrame.IsVisible = showWarning;
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
            kibleoku.Opacity = 0;
            kibleoku.Scale = 0.3;
            AciDegeri.Opacity = 0;
            AciDegeri.Scale = 0.5;
            await Task.WhenAll(
                kibleoku.FadeTo(1, 600, Easing.CubicOut),
                kibleoku.ScaleTo(1.0, 800, Easing.SpringOut)
            );
            await Task.WhenAll(
                AciDegeri.FadeTo(1, 400, Easing.CubicOut),
                AciDegeri.ScaleTo(1.0, 500, Easing.SpringOut)
            );
        }
        
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            compass.PusulaDurdur();
            compass.AciDegisti -= KıbleOkunuDondur;
            
            _nativeCompassService.Stop();
            _nativeCompassService.AccuracyChanged -= OnCompassAccuracyChanged;
        }
        
        protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);
            await AnimateKibleExit();
        }
        
        private async Task AnimateKibleExit()
        {
            var acıTask = Task.WhenAll(
                AciDegeri.FadeTo(0, 300, Easing.CubicIn),
                AciDegeri.ScaleTo(0.5, 400, Easing.CubicIn)
            );
            var okuTask = Task.WhenAll(
                kibleoku.FadeTo(0, 400, Easing.CubicIn),
                kibleoku.ScaleTo(0.3, 500, Easing.SpringIn)
            );
            await Task.WhenAll(acıTask, okuTask);
        }

        public void KıbleOkunuDondur(double gelenaci)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Mevcut açı ve hedef açı arasındaki en kısa yolu bul
                double currentRotation = kibleoku.Rotation;
                
                // Hedef açıyı 0-360 arasına normalize et (Gelen açı zaten öyledir ama garanti olsun)
                double targetRotation = gelenaci % 360; 
                if (targetRotation < 0) targetRotation += 360;

                // Farkı bul
                double diff = targetRotation - currentRotation;

                // Farkı -180 ile 180 arasına sıkıştır (En kısa yol)
                while (diff < -180) diff += 360;
                while (diff > 180) diff -= 360;

                // Yeni hedef, mevcut + fark (Böylece 350 -> 10 geçişi 350 -> 370 olur, terse dönmez)
                double finalTarget = currentRotation + diff;

                // Animasyonlu geçiş (Await etmiyoruz, yeni gelen veri eskisini iptal edip devam etsin)
                // Sensör hızı Game (20ms) olduğu için, 80-100ms arası bir animasyon yumuşaklık sağlar.
                kibleoku.RotateTo(finalTarget, 100, Easing.Linear);

                // UI Güncelleme
                double displayAngle = (360 - (gelenaci % 360)) % 360;
                AciDegeri.Text = $"{displayAngle:F0}°";
                
                int a = Convert.ToInt32(displayAngle);
                if(a == 0 || a == 360)
                {
                    AciDegeri.TextColor = Colors.Gold;
                }
                else
                {
                    AciDegeri.ClearValue(Label.TextColorProperty);
                }
            });
        }
    }
}

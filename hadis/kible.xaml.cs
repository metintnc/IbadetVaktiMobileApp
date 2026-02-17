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
        
        private bool _inInitialWarningPeriod = false;
        private CompassAccuracy _currentAccuracy = CompassAccuracy.Unreliable;

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            try
            {
                // Kıble sayfası için özel StatusBar ve TabBar renkleri
                _statusBarService.SetStatusBarColor("#000000"); // Siyah

                
                 // Her açılışta 5 saniye kalibrasyon uyarısını göster
                _inInitialWarningPeriod = true;
                if (CalibrationWarningFrame != null)
                    CalibrationWarningFrame.IsVisible = true;

                // 5 saniye sonra normal akışa dön
                _ = Task.Run(async () => 
                {
                    await Task.Delay(5000);
                    _inInitialWarningPeriod = false;
                    
                    // Süre bitince mevcut duruma göre güncelle
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnCompassAccuracyChanged(_currentAccuracy);
                    });
                });

                await CheckAndStartCompass();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Kible OnAppearing hatası: {ex.Message}");
            }
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

        // ... (Permission methods excluded for brevity if not changing) ...

        // ... Existing permission methods ...
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
            _currentAccuracy = accuracy;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                bool shouldBeVisible = false;

                if (_inInitialWarningPeriod)
                {
                    shouldBeVisible = true;
                }
                else
                {
                    switch (accuracy)
                    {
                        case CompassAccuracy.High:
                        case CompassAccuracy.Medium:
                            shouldBeVisible = false;
                            break;
                        default:
                            shouldBeVisible = true;
                            break;
                    }
                }

                if (CalibrationWarningFrame != null)
                {
                    if (shouldBeVisible)
                    {
                        // Görünür olması gerekiyorsa ve görünür değilse veya sönükse
                        if (!CalibrationWarningFrame.IsVisible || CalibrationWarningFrame.Opacity < 1)
                        {
                            CalibrationWarningFrame.IsVisible = true;
                            // Hızlıca gelsin (varsa bir önceki animasyonu da ezer)
                            await CalibrationWarningFrame.FadeTo(1, 250);
                        }
                    }
                    else
                    {
                        // Gizlenmesi gerekiyorsa ve şu an görünürse
                        if (CalibrationWarningFrame.IsVisible && CalibrationWarningFrame.Opacity > 0)
                        {
                            // Yavaş yavaş gitsin (1.5 saniye)
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

                // Kıble yönü doğrulama (355 - 5 arası)
                // displayAngle 0 ise, 355-5 aralığına girer (çünkü 0 <= 5)
                if ((displayAngle >= 355 || displayAngle <= 5) && QiblaCheckmark != null)
                {
                    // Zaten görünür değilse göster (Fade To)
                    if (QiblaCheckmark.Opacity == 0)
                    {
                        QiblaCheckmark.FadeTo(1, 200);
                    }
                }
                else if (QiblaCheckmark != null)
                {
                    // Görünürse gizle
                    if (QiblaCheckmark.Opacity > 0)
                    {
                        QiblaCheckmark.FadeTo(0, 200);
                    }
                }
            });
        }
        
        protected override bool OnBackButtonPressed()
        {
            // Geri tuşuna basıldığında Ana Sayfaya (Vakitler Sekmesine) git
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("//MainPage");
            });
            return true; // Olayı biz yönettik
        }
    }
}

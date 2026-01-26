using System;
using System.Threading.Tasks;
using hadis.Services;

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
            
            await LoadBackground();

            // Kıble sayfası için özel StatusBar ve TabBar renkleri
            _statusBarService.SetStatusBarColor("#000000"); // Siyah
            _tabBarService.SetTabBarColor("#19222B"); // Özel kıble rengi
            
            _nativeCompassService.AccuracyChanged += OnCompassAccuracyChanged;
            _nativeCompassService.Start();

            Task.Run(async () =>
            {
                await compass.KontrolEt();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    compass.AciDegisti += KıbleOkunuDondur;
                });
            });
        }

        private async Task LoadBackground()
        {
            try
            {
                string imageName = Application.Current.RequestedTheme == AppTheme.Dark ? "kiblearkaplan.png" : "bg_light.jpg";
                // Kullanıcı manuel tema seçtiyse ona bakmak gerekebilir ama basitlik için sistem temasını baz alıyoruz 
                // veya Namaz Vakti uygulamasında genelde ThemeService kullanılır. 
                // Burada basit AppTheme kontrolü yapıyoruz mevcut kod gibi.
                
                BackgroundImage.Source = await _imageService.GetOptimizedBackgroundImageAsync(imageName);
                BackgroundImage.IsVisible = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kible Background Load Error: {ex.Message}");
            }
        }

        private void OnCompassAccuracyChanged(CompassAccuracy accuracy)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                string statusText = "";
                Color statusColor = Colors.Gray;
                bool showWarning = false;

                switch (accuracy)
                {
                    case CompassAccuracy.High:
                        statusText = "Kalibrasyon: Yüksek";
                        statusColor = Colors.Green;
                        showWarning = false;
                        break;
                    case CompassAccuracy.Medium:
                        statusText = "Kalibrasyon: Orta";
                        statusColor = Colors.Orange;
                        showWarning = false;
                        break;
                    case CompassAccuracy.Low:
                        statusText = "Kalibrasyon: Düşük";
                        statusColor = Colors.Red;
                        showWarning = true;
                        break;
                    case CompassAccuracy.Unreliable:
                        statusText = "Kalibrasyon: Güvenilmez";
                        statusColor = Colors.DarkRed;
                        showWarning = true;
                        break;
                }

                AccuracyStatusLabel.Text = statusText;
                AccuracyStatusLabel.TextColor = statusColor;
                AccuracyWarningLabel.IsVisible = showWarning;
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
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if(gelenaci < 180)
                {
                    await kibleoku.RotateTo(gelenaci, 150, Easing.Linear);
                }
                else
                {
                    await kibleoku.RotateTo(gelenaci -360, 150, Easing.Linear);
                }
                AciDegeri.Text = $"{360 - gelenaci:F0}°";
                int a = Convert.ToInt32(gelenaci);
                if(360 - a == 0)
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

using hadis.Helpers;

namespace hadis.Services
{
    /// <summary>
    /// Arkaplan resmi ve renk yönetimi servisi
    /// </summary>
    public class BackgroundService
    {
        private readonly StatusBarService _statusBarService;
        private readonly TabBarService _tabBarService;
        private readonly IImageService _imageService;

        public BackgroundService(StatusBarService statusBarService, TabBarService tabBarService, IImageService imageService)
        {
            _statusBarService = statusBarService;
            _tabBarService = tabBarService;
            _imageService = imageService;
        }

        /// <summary>
        /// Saate göre otomatik arkaplan ayarlar
        /// </summary>
        public bool SetTimeBasedBackground(Image backgroundImage, Grid backgroundOverlay, string savedTheme)
        {
            // Sadece Custom tema değilse otomatik arkaplan uygula
            if (savedTheme == AppConstants.THEME_CUSTOM)
            {
                Console.WriteLine("ℹ️ Custom tema aktif - otomatik arkaplan devre dışı");
                return false; // Default to dark/false
            }

            Console.WriteLine("🎨 Zamana göre arkaplan ayarlanıyor...");

            DateTime now = DateTime.Now;
            var backgroundInfo = TimeBasedBackgroundConfig.GetBackgroundForTime(now.Hour, now.Minute);

            Console.WriteLine($"📅 Şu anki saat: {now.Hour}:{now.Minute:D2}");
            Console.WriteLine($"🖼️ Arkaplan: {backgroundInfo.Image}, Status Bar: {backgroundInfo.StatusBarColor}");

            try
            {
                // Arkaplanı uygula (Optimize edilmiş)
                MainThread.BeginInvokeOnMainThread(async () => 
                {
                    backgroundImage.Source = await _imageService.GetOptimizedBackgroundImageAsync(backgroundInfo.Image);
                });
                
                backgroundImage.IsVisible = true;

                // Overlay'i devre dışı bırak (Artık frameler değişecek)
                backgroundOverlay.IsVisible = false;
                // ApplyContrastOverlay(backgroundOverlay, backgroundInfo.Image);

                // Status bar rengini ayarla
                _statusBarService.SetStatusBarColor(backgroundInfo.StatusBarColor);
                
                // TabBar rengini ayarla
                _tabBarService.SetTabBarColor(backgroundInfo.TabBarColor);

                Console.WriteLine("✅ Arkaplan, status bar ve TabBar başarıyla ayarlandı!");

                // Arkaplan parlak mı?
                return TimeBasedBackgroundConfig.IsBackgroundBright(backgroundInfo.Image);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Arkaplan ayarlama hatası: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Özel tema arkaplanını uygular
        /// </summary>
        public void ApplyCustomBackground(Image backgroundImage, Grid backgroundOverlay, string backgroundValue)
        {
            if (string.IsNullOrEmpty(backgroundValue))
                return;

            try
            {
                if (backgroundValue.EndsWith(".jpg") || backgroundValue.EndsWith(".png"))
                {
                    backgroundImage.Source = ImageSource.FromFile(backgroundValue);
                    backgroundImage.IsVisible = true;
                    ApplyContrastOverlay(backgroundOverlay, backgroundValue);
                }
                else if (backgroundValue.StartsWith("gradient_"))
                {
                    backgroundImage.IsVisible = false;
                    ApplyContrastOverlay(backgroundOverlay, backgroundValue);
                }
                else if (backgroundValue.StartsWith("#"))
                {
                    backgroundImage.IsVisible = false;
                    ApplyContrastOverlay(backgroundOverlay, backgroundValue);
                }

                // Status bar rengini ayarla
                string statusBarColor = TimeBasedBackgroundConfig.GetStatusBarColorForCustomBackground(backgroundValue);
                _statusBarService.SetStatusBarColor(statusBarColor);
                
                // TabBar rengini ayarla
                string tabBarColor = TimeBasedBackgroundConfig.GetTabBarColorForCustomBackground(backgroundValue);
                _tabBarService.SetTabBarColor(tabBarColor);

                Console.WriteLine($"✅ Özel arkaplan, status bar ve TabBar uygulandı: {backgroundValue}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Özel arkaplan uygulama hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Arkaplan parlaklığına göre kontrast overlay uygular
        /// </summary>
        private void ApplyContrastOverlay(Grid backgroundOverlay, string backgroundValue)
        {
            bool isBright = TimeBasedBackgroundConfig.IsBackgroundBright(backgroundValue);

            float overlayOpacity = isBright 
                ? AppConstants.BRIGHT_OVERLAY_OPACITY 
                : AppConstants.DARK_OVERLAY_OPACITY;

            backgroundOverlay.IsVisible = true;
            backgroundOverlay.Background = new SolidColorBrush(Colors.Black.WithAlpha(overlayOpacity));

            Console.WriteLine($"🎨 Overlay uygulandı - Parlak: {isBright}, Opacity: {overlayOpacity}");
        }
    }
}

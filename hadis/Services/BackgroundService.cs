using hadis.Helpers;

namespace hadis.Services
{
    /// <summary>
    /// Arkaplan resmi ve renk yönetimi servisi
    /// </summary>
    public class BackgroundService
    {
        private readonly StatusBarService _statusBarService;

        public BackgroundService(StatusBarService statusBarService)
        {
            _statusBarService = statusBarService;
        }

        /// <summary>
        /// Saate göre otomatik arkaplan ayarlar
        /// </summary>
        public void SetTimeBasedBackground(Image backgroundImage, Grid backgroundOverlay, string savedTheme)
        {
            // Sadece Custom tema değilse otomatik arkaplan uygula
            if (savedTheme == AppConstants.THEME_CUSTOM)
            {
                Console.WriteLine("ℹ️ Custom tema aktif - otomatik arkaplan devre dışı");
                return;
            }

            Console.WriteLine("🎨 Zamana göre arkaplan ayarlanıyor...");

            DateTime now = DateTime.Now;
            var backgroundInfo = TimeBasedBackgroundConfig.GetBackgroundForTime(now.Hour, now.Minute);

            Console.WriteLine($"📅 Şu anki saat: {now.Hour}:{now.Minute:D2}");
            Console.WriteLine($"🖼️ Arkaplan: {backgroundInfo.Image}, Status Bar: {backgroundInfo.StatusBarColor}");

            try
            {
                // Arkaplanı uygula
                backgroundImage.Source = ImageSource.FromFile(backgroundInfo.Image);
                backgroundImage.IsVisible = true;

                // Overlay'i uygula (kontrast kontrolü)
                ApplyContrastOverlay(backgroundOverlay, backgroundInfo.Image);

                // Status bar rengini ayarla
                _statusBarService.SetStatusBarColor(backgroundInfo.StatusBarColor);

                Console.WriteLine("✅ Arkaplan ve status bar başarıyla ayarlandı!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Arkaplan ayarlama hatası: {ex.Message}");
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

                Console.WriteLine($"✅ Özel arkaplan uygulandı: {backgroundValue}");
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

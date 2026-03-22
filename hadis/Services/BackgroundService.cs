using hadis.Helpers;

namespace hadis.Services
{
    /// <summary>
    /// Arkaplan resmi ve renk yÃ¶netimi servisi
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
        /// Saate gÃ¶re otomatik arkaplan ayarlar
        /// </summary>
        public (bool IsBright, string ImageName) SetTimeBasedBackground(Image backgroundImage, Grid backgroundOverlay, string savedTheme, string? currentImageName = null)
        {
            // 1. Ã–zel Tema: Otomatik arkaplanÄ± atla
            if (savedTheme == "Custom")
            {
                System.Diagnostics.Debug.WriteLine("â„¹ï¸ Custom tema aktif - otomatik arkaplan devre dÄ±ÅŸÄ±");
                return (false, string.Empty); 
            }

            // 2. AÃ§Ä±k (Sabit) Tema
            if (savedTheme == "Light")
            {
                try
                {
                    string targetImage = "kuran_light.png";
                    if (currentImageName != targetImage)
                    {
                        backgroundImage.Source = targetImage;
                        backgroundImage.IsVisible = true;
                    }
                    backgroundOverlay.IsVisible = false;
                    
                    _statusBarService.SetStatusBarColor("#FFFFFF"); // White Status Bar
                    _tabBarService.SetTabBarColor("#FFFFFF");       // Light Tab Bar
                    
                    return (true, targetImage); // Parlak
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"âŒ AÃ§Ä±k tema arkaplan hatasÄ±: {ex.Message}");
                    return (true, "kuran_light.png");
                }
            }

            // 3. Simsiyah (Sabit) Tema
            if (savedTheme == "PitchBlack")
            {
                 try
                {
                    string targetImage = "kuranarkaplan.png";
                    if (currentImageName != targetImage)
                    {
                        backgroundImage.Source = targetImage;
                        backgroundImage.IsVisible = true;
                    }
                    
                    // Kuran sayfasÄ±ndaki siyah overlay efekti (Opacity 0.45)
                    backgroundOverlay.IsVisible = true;
                    backgroundOverlay.Background = new SolidColorBrush(Colors.Black.WithAlpha(0.45f));
                    
                    _statusBarService.SetStatusBarColor("#000000"); // Black Status Bar
                    _tabBarService.SetTabBarColor("#000000");       // Black Tab Bar
                    
                    return (false, targetImage); // Koyu
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"âŒ Simsiyah tema arkaplan hatasÄ±: {ex.Message}");
                    return (false, "kuranarkaplan.png");
                }
            }

            // 4. Ana Temalar (MainLight, MainDark, Main, System): Dinamik Arkaplan
            System.Diagnostics.Debug.WriteLine("ğŸ¨ Zamana gÃ¶re arkaplan ayarlanÄ±yor...");

            DateTime now = DateTime.Now;
            var backgroundInfo = TimeBasedBackgroundConfig.GetBackgroundForTime(now.Hour, now.Minute);

            System.Diagnostics.Debug.WriteLine($"ğŸ“… Åu anki saat: {now.Hour}:{now.Minute:D2}");
            System.Diagnostics.Debug.WriteLine($"ğŸ–¼ï¸ Arkaplan: {backgroundInfo.Image}, Status Bar: {backgroundInfo.StatusBarColor}");

            try
            {
                // ArkaplanÄ± uygula (EÄŸer deÄŸiÅŸtiyse)
                if (currentImageName != backgroundInfo.Image)
                {
                    MainThread.BeginInvokeOnMainThread(async () => 
                    {
                        backgroundImage.Source = await _imageService.GetOptimizedBackgroundImageAsync(backgroundInfo.Image);
                        backgroundImage.IsVisible = true;
                    });
                }
                else
                {
                     System.Diagnostics.Debug.WriteLine("â„¹ï¸ Arkaplan resmi aynÄ±, gÃ¼ncelleme atlandÄ±.");
                     backgroundImage.IsVisible = true;
                }
                
                // Overlay'i devre dÄ±ÅŸÄ± bÄ±rak
                backgroundOverlay.IsVisible = false;

                // Status bar rengini ayarla
                _statusBarService.SetStatusBarColor(backgroundInfo.StatusBarColor);
                
                // TabBar rengini ayarla
                _tabBarService.SetTabBarColor(backgroundInfo.TabBarColor);

                System.Diagnostics.Debug.WriteLine("âœ… Arkaplan, status bar ve TabBar baÅŸarÄ±yla ayarlandÄ±!");

                // Arkaplan parlak mÄ±?
                bool isBright = TimeBasedBackgroundConfig.IsBackgroundBright(backgroundInfo.Image);
                return (isBright, backgroundInfo.Image);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"âŒ Arkaplan ayarlama hatasÄ±: {ex.Message}");
                return (false, backgroundInfo.Image);
            }
        }

        /// <summary>
        /// Ã–zel tema arkaplanÄ±nÄ± uygular
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

                System.Diagnostics.Debug.WriteLine($"âœ… Ã–zel arkaplan, status bar ve TabBar uygulandÄ±: {backgroundValue}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"âŒ Ã–zel arkaplan uygulama hatasÄ±: {ex.Message}");
            }
        }

        /// <summary>
        /// Arkaplan parlaklÄ±ÄŸÄ±na gÃ¶re kontrast overlay uygular
        /// </summary>
        private void ApplyContrastOverlay(Grid backgroundOverlay, string backgroundValue)
        {
            bool isBright = TimeBasedBackgroundConfig.IsBackgroundBright(backgroundValue);

            float overlayOpacity = isBright 
                ? AppConstants.BRIGHT_OVERLAY_OPACITY 
                : AppConstants.DARK_OVERLAY_OPACITY;

            backgroundOverlay.IsVisible = true;
            backgroundOverlay.Background = new SolidColorBrush(Colors.Black.WithAlpha(overlayOpacity));

            System.Diagnostics.Debug.WriteLine($"ğŸ¨ Overlay uygulandÄ± - Parlak: {isBright}, Opacity: {overlayOpacity}");
        }
    }
}



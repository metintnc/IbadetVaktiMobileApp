namespace hadis.Helpers
{
    /// <summary>
    /// Uygulama genelinde kullanılan sabit değerler
    /// </summary>
    public static class AppConstants
    {
        // Timer Ayarları
        public const int TIMER_INTERVAL_MS = 500;

        // Parlaklık Hesaplama
        public const int BRIGHTNESS_THRESHOLD = 128;
        public const double RED_LUMINANCE_COEFFICIENT = 0.299;
        public const double GREEN_LUMINANCE_COEFFICIENT = 0.587;
        public const double BLUE_LUMINANCE_COEFFICIENT = 0.114;

        // Opacity Değerleri
        public const float BRIGHT_OVERLAY_OPACITY = 0.35f;
        public const float DARK_OVERLAY_OPACITY = 0.20f;
        public const double DEFAULT_BACKGROUND_OPACITY = 0.3;

        // Glassmorphism Ayarları
        public const float GLASSMORPHISM_ALPHA_START = 0.3f;
        public const float GLASSMORPHISM_ALPHA_END = 0.2f;

        // Parlaklık Eşikleri
        public const int BRIGHT_COLOR_THRESHOLD = 180;

        // Preferences Keys
        public const string PREF_APP_THEME = "AppTheme";
        public const string PREF_CUSTOM_THEME = "CustomTheme";
        public const string PREF_BACKGROUND_OPACITY = "BackgroundOpacity";

        // Tema Değerleri
        public const string THEME_SYSTEM = "System";
        public const string THEME_CUSTOM = "Custom";
        public const string THEME_LIGHT = "Light";
        public const string THEME_DARK = "Dark";
    }
}

using hadis.Helpers;
using hadis.Models;
using System.Text.Json;

namespace hadis.Services
{
    /// <summary>
    /// Tema yönetimi ve uygulama servisi
    /// </summary>
    public class ThemeService
    {
        private readonly StatusBarService _statusBarService;

        // Tema renk sabitleri - static readonly ile tek seferlik allocation
        private static readonly Color DarkBorderColor = Color.FromArgb("#80FFFFFF");
        private static readonly Color DarkBaseColor = Color.FromArgb("#30FFFFFF");
        private static readonly Color DarkTextColor = Colors.White;

        private static readonly Color LightBorderColor = Color.FromArgb("#80009688");
        private static readonly Color LightBaseColor = Color.FromArgb("#40FFFFFF");
        private static readonly Color LightTextColor = Color.FromArgb("#00796B");
        private static readonly Color LightAyetBorderColor = Color.FromArgb("#8000796B");

        // Adaptive tema renkleri
        private static readonly Color BrightBgBaseColor = Color.FromArgb("#80000000");
        private static readonly Color BrightBgBorderColor = Color.FromArgb("#99FFFFFF");
        private static readonly Color DarkBgBaseColor = Color.FromArgb("#30FFFFFF");
        private static readonly Color DarkBgBorderColor = Color.FromArgb("#50FFFFFF");

        public ThemeService(StatusBarService statusBarService)
        {
            _statusBarService = statusBarService;
        }

        /// <summary>
        /// Özel temayı tüm border'lara uygular
        /// </summary>
        public void ApplyCustomTheme(
            Border mainBorder, Label namazIsmi, Label kalan, Label konum,
            Border imsakBorder, Label imsakYazi, Label imsakVakit,
            Border gunesBorder, Label gunesYazi, Label gunesVakit,
            Border ogleBorder, Label ogleYazi, Label ogleVakit,
            Border ikindiBorder, Label ikindiYazi, Label ikindiVakit,
            Border aksamBorder, Label aksamYazi, Label aksamVakit,
            Border yatsiBorder, Label yatsiYazi, Label yatsiVakit,
            Border ayetBorder, Label gununAyeti)
        {
            string customThemeJson = Preferences.Default.Get(AppConstants.PREF_CUSTOM_THEME, string.Empty);

            if (string.IsNullOrEmpty(customThemeJson))
            {
                ResetToDefaultStyles(mainBorder, namazIsmi, kalan, konum,
                    imsakBorder, imsakYazi, imsakVakit,
                    gunesBorder, gunesYazi, gunesVakit,
                    ogleBorder, ogleYazi, ogleVakit,
                    ikindiBorder, ikindiYazi, ikindiVakit,
                    aksamBorder, aksamYazi, aksamVakit,
                    yatsiBorder, yatsiYazi, yatsiVakit,
                    ayetBorder, gununAyeti);
                return;
            }

            try
            {
                var theme = JsonSerializer.Deserialize<CustomTheme>(customThemeJson);
                if (theme != null)
                {
                    // Ana Border
                    ApplyMainFrameTheme(mainBorder, namazIsmi, kalan, konum, theme);

                    // Namaz vakitleri border'ları
                    var smallBaseColor = Color.FromArgb(theme.SmallFrameBackground);
                    var smallBorderColor = Color.FromArgb(theme.SmallFrameBorder);
                    var smallTextColor = Color.FromArgb(theme.SmallFrameText);

                    ApplyPrayerTimeBorders(
                        imsakBorder, imsakYazi, imsakVakit,
                        gunesBorder, gunesYazi, gunesVakit,
                        ogleBorder, ogleYazi, ogleVakit,
                        ikindiBorder, ikindiYazi, ikindiVakit,
                        aksamBorder, aksamYazi, aksamVakit,
                        yatsiBorder, yatsiYazi, yatsiVakit,
                        smallBorderColor, smallTextColor, smallBaseColor);

                    // Ayet Border
                    ApplyAyetFrameTheme(ayetBorder, gununAyeti, theme);

                    System.Diagnostics.Debug.WriteLine("✅ Özel tema başarıyla uygulandı");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Özel tema uygulama hatası: {ex.Message}");
                ResetToDefaultStyles(mainBorder, namazIsmi, kalan, konum,
                    imsakBorder, imsakYazi, imsakVakit,
                    gunesBorder, gunesYazi, gunesVakit,
                    ogleBorder, ogleYazi, ogleVakit,
                    ikindiBorder, ikindiYazi, ikindiVakit,
                    aksamBorder, aksamYazi, aksamVakit,
                    yatsiBorder, yatsiYazi, yatsiVakit,
                    ayetBorder, gununAyeti);
            }
        }

        /// <summary>
        /// Ana border'a tema uygular
        /// </summary>
        private static void ApplyMainFrameTheme(Border mainBorder, Label namazIsmi, Label kalan, Label konum, CustomTheme theme)
        {
            mainBorder.Stroke = Color.FromArgb(theme.MainFrameBorder);
            var mainBaseColor = Color.FromArgb(theme.MainFrameBackground);
            mainBorder.ApplyGlassmorphism(mainBaseColor, Color.FromArgb(theme.MainFrameBorder));

            var mainTextColor = Color.FromArgb(theme.MainFrameText);
            namazIsmi.TextColor = mainTextColor;
            kalan.TextColor = mainTextColor;
            konum.TextColor = mainTextColor;
        }

        /// <summary>
        /// Ayet border'ına tema uygular
        /// </summary>
        private static void ApplyAyetFrameTheme(Border ayetBorder, Label gununAyeti, CustomTheme theme)
        {
            ayetBorder.Stroke = Color.FromArgb(theme.AyetFrameBorder);
            var baseColor = Color.FromArgb(theme.AyetFrameBackground);
            ayetBorder.ApplyGlassmorphism(baseColor, Color.FromArgb(theme.AyetFrameBorder));
            gununAyeti.TextColor = Color.FromArgb(theme.AyetFrameText);
        }

        /// <summary>
        /// Varsayılan (sistem) tema stillerini uygular
        /// </summary>
        public void ResetToDefaultStyles(
            Border mainBorder, Label namazIsmi, Label kalan, Label konum,
            Border imsakBorder, Label imsakYazi, Label imsakVakit,
            Border gunesBorder, Label gunesYazi, Label gunesVakit,
            Border ogleBorder, Label ogleYazi, Label ogleVakit,
            Border ikindiBorder, Label ikindiYazi, Label ikindiVakit,
            Border aksamBorder, Label aksamYazi, Label aksamVakit,
            Border yatsiBorder, Label yatsiYazi, Label yatsiVakit,
            Border ayetBorder, Label gununAyeti)
        {
            var currentTheme = Application.Current?.UserAppTheme == AppTheme.Unspecified
                ? Application.Current?.RequestedTheme ?? AppTheme.Light
                : Application.Current?.UserAppTheme ?? AppTheme.Light;

            bool isDark = currentTheme == AppTheme.Dark;

            ApplyThemeColors(isDark,
                mainBorder, namazIsmi, kalan, konum,
                imsakBorder, imsakYazi, imsakVakit,
                gunesBorder, gunesYazi, gunesVakit,
                ogleBorder, ogleYazi, ogleVakit,
                ikindiBorder, ikindiYazi, ikindiVakit,
                aksamBorder, aksamYazi, aksamVakit,
                yatsiBorder, yatsiYazi, yatsiVakit,
                ayetBorder, gununAyeti);
        }

        /// <summary>
        /// Tema renklerini uygular (Dark/Light birleştirilmiş metod)
        /// </summary>
        private static void ApplyThemeColors(
            bool isDark,
            Border mainBorder, Label namazIsmi, Label kalan, Label konum,
            Border imsakBorder, Label imsakYazi, Label imsakVakit,
            Border gunesBorder, Label gunesYazi, Label gunesVakit,
            Border ogleBorder, Label ogleYazi, Label ogleVakit,
            Border ikindiBorder, Label ikindiYazi, Label ikindiVakit,
            Border aksamBorder, Label aksamYazi, Label aksamVakit,
            Border yatsiBorder, Label yatsiYazi, Label yatsiVakit,
            Border ayetBorder, Label gununAyeti)
        {
            var borderColor = isDark ? DarkBorderColor : LightBorderColor;
            var baseColor = isDark ? DarkBaseColor : LightBaseColor;
            var textColor = isDark ? DarkTextColor : LightTextColor;

            // Ana border
            mainBorder.ApplyGlassmorphism(baseColor, borderColor);
            namazIsmi.TextColor = textColor;
            kalan.TextColor = textColor;
            konum.TextColor = textColor;

            // Namaz vakitleri border'ları
            ApplyPrayerTimeBorders(
                imsakBorder, imsakYazi, imsakVakit,
                gunesBorder, gunesYazi, gunesVakit,
                ogleBorder, ogleYazi, ogleVakit,
                ikindiBorder, ikindiYazi, ikindiVakit,
                aksamBorder, aksamYazi, aksamVakit,
                yatsiBorder, yatsiYazi, yatsiVakit,
                borderColor, textColor, baseColor);

            // Ayet border
            var ayetBorderColor = isDark ? borderColor : LightAyetBorderColor;
            ayetBorder.ApplyGlassmorphism(baseColor, ayetBorderColor);
            gununAyeti.TextColor = textColor;
        }

        /// <summary>
        /// Tüm namaz vakti border'larına stil uygular (kod tekrarını önler)
        /// </summary>
        private static void ApplyPrayerTimeBorders(
            Border imsakBorder, Label imsakYazi, Label imsakVakit,
            Border gunesBorder, Label gunesYazi, Label gunesVakit,
            Border ogleBorder, Label ogleYazi, Label ogleVakit,
            Border ikindiBorder, Label ikindiYazi, Label ikindiVakit,
            Border aksamBorder, Label aksamYazi, Label aksamVakit,
            Border yatsiBorder, Label yatsiYazi, Label yatsiVakit,
            Color borderColor, Color textColor, Color baseColor)
        {
            imsakBorder.ApplyPrayerTimeStyle(imsakYazi, imsakVakit, borderColor, textColor, baseColor);
            gunesBorder.ApplyPrayerTimeStyle(gunesYazi, gunesVakit, borderColor, textColor, baseColor);
            ogleBorder.ApplyPrayerTimeStyle(ogleYazi, ogleVakit, borderColor, textColor, baseColor);
            ikindiBorder.ApplyPrayerTimeStyle(ikindiYazi, ikindiVakit, borderColor, textColor, baseColor);
            aksamBorder.ApplyPrayerTimeStyle(aksamYazi, aksamVakit, borderColor, textColor, baseColor);
            yatsiBorder.ApplyPrayerTimeStyle(yatsiYazi, yatsiVakit, borderColor, textColor, baseColor);
        }

        /// <summary>
        /// Arkaplan parlaklığına göre borderlara adaptif cam efekti uygular
        /// </summary>
        public void ApplyAdaptiveGlassTheme(
            bool isBrightBackground,
            Border mainBorder, Label namazIsmi, Label kalan, Label konum,
            Border imsakBorder, Label imsakYazi, Label imsakVakit,
            Border gunesBorder, Label gunesYazi, Label gunesVakit,
            Border ogleBorder, Label ogleYazi, Label ogleVakit,
            Border ikindiBorder, Label ikindiYazi, Label ikindiVakit,
            Border aksamBorder, Label aksamYazi, Label aksamVakit,
            Border yatsiBorder, Label yatsiYazi, Label yatsiVakit,
            Border ayetBorder, Label gununAyeti)
        {
            // Parlak arkaplan → koyu cam, Koyu arkaplan → açık cam
            var baseColor = isBrightBackground ? BrightBgBaseColor : DarkBgBaseColor;
            var borderColor = isBrightBackground ? BrightBgBorderColor : DarkBgBorderColor;
            var textColor = Colors.White;

            // Ana border
            mainBorder.ApplyGlassmorphism(baseColor, borderColor);
            namazIsmi.TextColor = textColor;
            kalan.TextColor = textColor;
            konum.TextColor = textColor;

            // Namaz vakitleri
            ApplyPrayerTimeBorders(
                imsakBorder, imsakYazi, imsakVakit,
                gunesBorder, gunesYazi, gunesVakit,
                ogleBorder, ogleYazi, ogleVakit,
                ikindiBorder, ikindiYazi, ikindiVakit,
                aksamBorder, aksamYazi, aksamVakit,
                yatsiBorder, yatsiYazi, yatsiVakit,
                borderColor, textColor, baseColor);

            // Ayet Border
            ayetBorder.ApplyGlassmorphism(baseColor, borderColor);
            gununAyeti.TextColor = textColor;
        }
    }
}


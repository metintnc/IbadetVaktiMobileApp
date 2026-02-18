using hadis.Helpers;
using hadis.Models;
using System.Text.Json;

namespace hadis.Services
{
    /// <summary>
    /// Tema yÃ¶netimi ve uygulama servisi
    /// </summary>
    public class ThemeService
    {
        private readonly StatusBarService _statusBarService;

        public ThemeService(StatusBarService statusBarService)
        {
            _statusBarService = statusBarService;
        }

        /// <summary>
        /// Ã–zel temayÄ± tÃ¼m border'lara uygular
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

                    // Namaz vakitleri border'larÄ±
                    var smallBaseColor = Color.FromArgb(theme.SmallFrameBackground);
                    var smallBorderColor = Color.FromArgb(theme.SmallFrameBorder);
                    var smallTextColor = Color.FromArgb(theme.SmallFrameText);

                    imsakBorder.ApplyPrayerTimeStyle(imsakYazi, imsakVakit, smallBorderColor, smallTextColor, smallBaseColor);
                    gunesBorder.ApplyPrayerTimeStyle(gunesYazi, gunesVakit, smallBorderColor, smallTextColor, smallBaseColor);
                    ogleBorder.ApplyPrayerTimeStyle(ogleYazi, ogleVakit, smallBorderColor, smallTextColor, smallBaseColor);
                    ikindiBorder.ApplyPrayerTimeStyle(ikindiYazi, ikindiVakit, smallBorderColor, smallTextColor, smallBaseColor);
                    aksamBorder.ApplyPrayerTimeStyle(aksamYazi, aksamVakit, smallBorderColor, smallTextColor, smallBaseColor);
                    yatsiBorder.ApplyPrayerTimeStyle(yatsiYazi, yatsiVakit, smallBorderColor, smallTextColor, smallBaseColor);

                    // Ayet Border
                    ApplyAyetFrameTheme(ayetBorder, gununAyeti, theme);

                    System.Diagnostics.Debug.WriteLine("âœ… Ã–zel tema baÅŸarÄ±yla uygulandÄ±");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"âŒ Ã–zel tema uygulama hatasÄ±: {ex.Message}");
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
        private void ApplyMainFrameTheme(Border mainBorder, Label namazIsmi, Label kalan, Label konum, CustomTheme theme)
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
        /// Ayet border'Ä±na tema uygular
        /// </summary>
        private void ApplyAyetFrameTheme(Border ayetBorder, Label gununAyeti, CustomTheme theme)
        {
            ayetBorder.Stroke = Color.FromArgb(theme.AyetFrameBorder);
            var baseColor = Color.FromArgb(theme.AyetFrameBackground);
            ayetBorder.ApplyGlassmorphism(baseColor, Color.FromArgb(theme.AyetFrameBorder));
            gununAyeti.TextColor = Color.FromArgb(theme.AyetFrameText);
        }

        /// <summary>
        /// VarsayÄ±lan (sistem) tema stillerini uygular
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

            if (currentTheme == AppTheme.Dark)
            {
                ApplyDarkTheme(mainBorder, namazIsmi, kalan, konum,
                    imsakBorder, imsakYazi, imsakVakit,
                    gunesBorder, gunesYazi, gunesVakit,
                    ogleBorder, ogleYazi, ogleVakit,
                    ikindiBorder, ikindiYazi, ikindiVakit,
                    aksamBorder, aksamYazi, aksamVakit,
                    yatsiBorder, yatsiYazi, yatsiVakit,
                    ayetBorder, gununAyeti);
            }
            else
            {
                ApplyLightTheme(mainBorder, namazIsmi, kalan, konum,
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
        /// Koyu tema stillerini uygular
        /// </summary>
        private void ApplyDarkTheme(
            Border mainBorder, Label namazIsmi, Label kalan, Label konum,
            Border imsakBorder, Label imsakYazi, Label imsakVakit,
            Border gunesBorder, Label gunesYazi, Label gunesVakit,
            Border ogleBorder, Label ogleYazi, Label ogleVakit,
            Border ikindiBorder, Label ikindiYazi, Label ikindiVakit,
            Border aksamBorder, Label aksamYazi, Label aksamVakit,
            Border yatsiBorder, Label yatsiYazi, Label yatsiVakit,
            Border ayetBorder, Label gununAyeti)
        {
            var borderColor = Color.FromArgb("#80FFFFFF");
            var baseColor = Color.FromArgb("#30FFFFFF");
            var textColor = Colors.White;

            // Ana border
            mainBorder.ApplyGlassmorphism(baseColor, borderColor);
            namazIsmi.TextColor = textColor;
            kalan.TextColor = textColor;
            konum.TextColor = textColor;

            // Namaz vakitleri border'larÄ±
            imsakBorder.ApplyPrayerTimeStyle(imsakYazi, imsakVakit, borderColor, textColor, baseColor);
            gunesBorder.ApplyPrayerTimeStyle(gunesYazi, gunesVakit, borderColor, textColor, baseColor);
            ogleBorder.ApplyPrayerTimeStyle(ogleYazi, ogleVakit, borderColor, textColor, baseColor);
            ikindiBorder.ApplyPrayerTimeStyle(ikindiYazi, ikindiVakit, borderColor, textColor, baseColor);
            aksamBorder.ApplyPrayerTimeStyle(aksamYazi, aksamVakit, borderColor, textColor, baseColor);
            yatsiBorder.ApplyPrayerTimeStyle(yatsiYazi, yatsiVakit, borderColor, textColor, baseColor);

            // Ayet border
            ayetBorder.ApplyGlassmorphism(baseColor, borderColor);
            gununAyeti.TextColor = textColor;
        }

        /// <summary>
        /// AÃ§Ä±k tema stillerini uygular
        /// </summary>
        private void ApplyLightTheme(
            Border mainBorder, Label namazIsmi, Label kalan, Label konum,
            Border imsakBorder, Label imsakYazi, Label imsakVakit,
            Border gunesBorder, Label gunesYazi, Label gunesVakit,
            Border ogleBorder, Label ogleYazi, Label ogleVakit,
            Border ikindiBorder, Label ikindiYazi, Label ikindiVakit,
            Border aksamBorder, Label aksamYazi, Label aksamVakit,
            Border yatsiBorder, Label yatsiYazi, Label yatsiVakit,
            Border ayetBorder, Label gununAyeti)
        {
            var borderColor = Color.FromArgb("#80009688");
            var baseColor = Color.FromArgb("#40FFFFFF");
            var textColor = Color.FromArgb("#00796B");

            // Ana border
            mainBorder.ApplyGlassmorphism(baseColor, borderColor);
            namazIsmi.TextColor = textColor;
            kalan.TextColor = textColor;
            konum.TextColor = textColor;

            // Namaz vakitleri border'larÄ±
            imsakBorder.ApplyPrayerTimeStyle(imsakYazi, imsakVakit, borderColor, textColor, baseColor);
            gunesBorder.ApplyPrayerTimeStyle(gunesYazi, gunesVakit, borderColor, textColor, baseColor);
            ogleBorder.ApplyPrayerTimeStyle(ogleYazi, ogleVakit, borderColor, textColor, baseColor);
            ikindiBorder.ApplyPrayerTimeStyle(ikindiYazi, ikindiVakit, borderColor, textColor, baseColor);
            aksamBorder.ApplyPrayerTimeStyle(aksamYazi, aksamVakit, borderColor, textColor, baseColor);
            yatsiBorder.ApplyPrayerTimeStyle(yatsiYazi, yatsiVakit, borderColor, textColor, baseColor);

            // Ayet border
            ayetBorder.Stroke = Color.FromArgb("#8000796B");
            ayetBorder.ApplyGlassmorphism(baseColor, Color.FromArgb("#8000796B"));
            gununAyeti.TextColor = textColor;
        }
        /// <summary>
        /// Arkaplan parlaklÄ±ÄŸÄ±na gÃ¶re borderlara adaptif cam efekti uygular
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
            Color baseColor;
            Color borderColor;
            Color textColor;

            if (isBrightBackground)
            {
                // Arkaplan parlak ise (GÃ¼ndÃ¼z vb.) -> KOYU CAM (Dark Glass) kullan ki gÃ¶rÃ¼nsÃ¼n
                // Ã–rn: Siyah %50 opaklÄ±k
                baseColor = Color.FromArgb("#80000000"); 
                borderColor = Color.FromArgb("#99FFFFFF"); // BeyazÄ±msÄ± sÄ±nÄ±r
                textColor = Colors.White;
            }
            else
            {
                // Arkaplan karanlÄ±k ise (Gece vb.) -> AÃ‡IK CAM (Light Glass) kullan
                // Ã–rn: Beyaz %20-30 opaklÄ±k
                baseColor = Color.FromArgb("#40FFFFFF");
                borderColor = Color.FromArgb("#80FFFFFF");
                textColor = Colors.White; // Koyu arkaplanda beyaz yazÄ± genelde iyidir ama frame aÃ§Ä±k renkse?
                // Light Glass (BeyazÄ±msÄ±) Ã¼stÃ¼ne Beyaz yazÄ± okunmayabilir.
                // EÄŸer frame beyazÄ±msÄ± ise yazÄ± koyu olmalÄ±.
                // Veya "Light Glass" tamamen ÅŸeffaf beyaz ise...
                // VarsayÄ±lan tasarÄ±mÄ±mÄ±zda gece arkaplan koyu, frameler beyazÄ±msÄ±, yazÄ±lar KOYU (ThemeService.ApplyLightTheme'deki gibi).
                
                // Ancak kullanÄ±cÄ± "Gece" modunda genelde beyaz yazÄ± bekler.
                // Mevcut tasarÄ±m: Koyu arkaplanlarda beyaz yazÄ±, aÃ§Ä±k frameler.
                // Hadi Light Theme renklerini baz alalÄ±m ama text rengine dikkat.
                
                // Revize:
                // Gece (Koyu BG) -> Frame: #30FFFFFF (Hafif Beyaz), Text: White. (ApplyDarkTheme gibi)
                baseColor = Color.FromArgb("#30FFFFFF");
                borderColor = Color.FromArgb("#50FFFFFF");
                textColor = Colors.White;
            }

            // Ana border
            mainBorder.ApplyGlassmorphism(baseColor, borderColor);
            namazIsmi.TextColor = textColor;
            kalan.TextColor = textColor;
            konum.TextColor = textColor;

            // Namaz vakitleri
            void ApplyStyle(Border b, Label l1, Label l2)
            {
                b.ApplyGlassmorphism(baseColor, borderColor);
                l1.TextColor = textColor;
                l2.TextColor = textColor;
            }

            ApplyStyle(imsakBorder, imsakYazi, imsakVakit);
            ApplyStyle(gunesBorder, gunesYazi, gunesVakit);
            ApplyStyle(ogleBorder, ogleYazi, ogleVakit);
            ApplyStyle(ikindiBorder, ikindiYazi, ikindiVakit);
            ApplyStyle(aksamBorder, aksamYazi, aksamVakit);
            ApplyStyle(yatsiBorder, yatsiYazi, yatsiVakit);

            // Ayet Border
            ayetBorder.ApplyGlassmorphism(baseColor, borderColor);
            gununAyeti.TextColor = textColor;
        }
    }
}


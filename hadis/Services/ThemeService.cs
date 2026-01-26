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

        public ThemeService(StatusBarService statusBarService)
        {
            _statusBarService = statusBarService;
        }

        /// <summary>
        /// Özel temayı tüm frame'lere uygular
        /// </summary>
        public void ApplyCustomTheme(
            Frame mainFrame, Label namazIsmi, Label kalan, Label konum,
            Frame imsakFrame, Label imsakYazi, Label imsakVakit,
            Frame gunesFrame, Label gunesYazi, Label gunesVakit,
            Frame ogleFrame, Label ogleYazi, Label ogleVakit,
            Frame ikindiFrame, Label ikindiYazi, Label ikindiVakit,
            Frame aksamFrame, Label aksamYazi, Label aksamVakit,
            Frame yatsiFrame, Label yatsiYazi, Label yatsiVakit,
            Frame ayetFrame, Label gununAyeti)
        {
            string customThemeJson = Preferences.Default.Get(AppConstants.PREF_CUSTOM_THEME, string.Empty);

            if (string.IsNullOrEmpty(customThemeJson))
            {
                ResetToDefaultStyles(mainFrame, namazIsmi, kalan, konum,
                    imsakFrame, imsakYazi, imsakVakit,
                    gunesFrame, gunesYazi, gunesVakit,
                    ogleFrame, ogleYazi, ogleVakit,
                    ikindiFrame, ikindiYazi, ikindiVakit,
                    aksamFrame, aksamYazi, aksamVakit,
                    yatsiFrame, yatsiYazi, yatsiVakit,
                    ayetFrame, gununAyeti);
                return;
            }

            try
            {
                var theme = JsonSerializer.Deserialize<CustomTheme>(customThemeJson);
                if (theme != null)
                {
                    // Ana Frame
                    ApplyMainFrameTheme(mainFrame, namazIsmi, kalan, konum, theme);

                    // Namaz vakitleri frame'leri
                    var smallBaseColor = Color.FromArgb(theme.SmallFrameBackground);
                    var smallBorderColor = Color.FromArgb(theme.SmallFrameBorder);
                    var smallTextColor = Color.FromArgb(theme.SmallFrameText);

                    imsakFrame.ApplyPrayerTimeStyle(imsakYazi, imsakVakit, smallBorderColor, smallTextColor, smallBaseColor);
                    gunesFrame.ApplyPrayerTimeStyle(gunesYazi, gunesVakit, smallBorderColor, smallTextColor, smallBaseColor);
                    ogleFrame.ApplyPrayerTimeStyle(ogleYazi, ogleVakit, smallBorderColor, smallTextColor, smallBaseColor);
                    ikindiFrame.ApplyPrayerTimeStyle(ikindiYazi, ikindiVakit, smallBorderColor, smallTextColor, smallBaseColor);
                    aksamFrame.ApplyPrayerTimeStyle(aksamYazi, aksamVakit, smallBorderColor, smallTextColor, smallBaseColor);
                    yatsiFrame.ApplyPrayerTimeStyle(yatsiYazi, yatsiVakit, smallBorderColor, smallTextColor, smallBaseColor);

                    // Ayet Frame
                    ApplyAyetFrameTheme(ayetFrame, gununAyeti, theme);

                    Console.WriteLine("✅ Özel tema başarıyla uygulandı");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Özel tema uygulama hatası: {ex.Message}");
                ResetToDefaultStyles(mainFrame, namazIsmi, kalan, konum,
                    imsakFrame, imsakYazi, imsakVakit,
                    gunesFrame, gunesYazi, gunesVakit,
                    ogleFrame, ogleYazi, ogleVakit,
                    ikindiFrame, ikindiYazi, ikindiVakit,
                    aksamFrame, aksamYazi, aksamVakit,
                    yatsiFrame, yatsiYazi, yatsiVakit,
                    ayetFrame, gununAyeti);
            }
        }

        /// <summary>
        /// Ana frame'e tema uygular
        /// </summary>
        private void ApplyMainFrameTheme(Frame mainFrame, Label namazIsmi, Label kalan, Label konum, CustomTheme theme)
        {
            mainFrame.BorderColor = Color.FromArgb(theme.MainFrameBorder);
            var mainBaseColor = Color.FromArgb(theme.MainFrameBackground);
            mainFrame.ApplyGlassmorphism(mainBaseColor, Color.FromArgb(theme.MainFrameBorder));

            var mainTextColor = Color.FromArgb(theme.MainFrameText);
            namazIsmi.TextColor = mainTextColor;
            kalan.TextColor = mainTextColor;
            konum.TextColor = mainTextColor;
        }

        /// <summary>
        /// Ayet frame'ine tema uygular
        /// </summary>
        private void ApplyAyetFrameTheme(Frame ayetFrame, Label gununAyeti, CustomTheme theme)
        {
            ayetFrame.BorderColor = Color.FromArgb(theme.AyetFrameBorder);
            var baseColor = Color.FromArgb(theme.AyetFrameBackground);
            ayetFrame.ApplyGlassmorphism(baseColor, Color.FromArgb(theme.AyetFrameBorder));
            gununAyeti.TextColor = Color.FromArgb(theme.AyetFrameText);
        }

        /// <summary>
        /// Varsayılan (sistem) tema stillerini uygular
        /// </summary>
        public void ResetToDefaultStyles(
            Frame mainFrame, Label namazIsmi, Label kalan, Label konum,
            Frame imsakFrame, Label imsakYazi, Label imsakVakit,
            Frame gunesFrame, Label gunesYazi, Label gunesVakit,
            Frame ogleFrame, Label ogleYazi, Label ogleVakit,
            Frame ikindiFrame, Label ikindiYazi, Label ikindiVakit,
            Frame aksamFrame, Label aksamYazi, Label aksamVakit,
            Frame yatsiFrame, Label yatsiYazi, Label yatsiVakit,
            Frame ayetFrame, Label gununAyeti)
        {
            var currentTheme = Application.Current?.UserAppTheme == AppTheme.Unspecified
                ? Application.Current?.RequestedTheme ?? AppTheme.Light
                : Application.Current?.UserAppTheme ?? AppTheme.Light;

            if (currentTheme == AppTheme.Dark)
            {
                ApplyDarkTheme(mainFrame, namazIsmi, kalan, konum,
                    imsakFrame, imsakYazi, imsakVakit,
                    gunesFrame, gunesYazi, gunesVakit,
                    ogleFrame, ogleYazi, ogleVakit,
                    ikindiFrame, ikindiYazi, ikindiVakit,
                    aksamFrame, aksamYazi, aksamVakit,
                    yatsiFrame, yatsiYazi, yatsiVakit,
                    ayetFrame, gununAyeti);
            }
            else
            {
                ApplyLightTheme(mainFrame, namazIsmi, kalan, konum,
                    imsakFrame, imsakYazi, imsakVakit,
                    gunesFrame, gunesYazi, gunesVakit,
                    ogleFrame, ogleYazi, ogleVakit,
                    ikindiFrame, ikindiYazi, ikindiVakit,
                    aksamFrame, aksamYazi, aksamVakit,
                    yatsiFrame, yatsiYazi, yatsiVakit,
                    ayetFrame, gununAyeti);
            }
        }

        /// <summary>
        /// Koyu tema stillerini uygular
        /// </summary>
        private void ApplyDarkTheme(
            Frame mainFrame, Label namazIsmi, Label kalan, Label konum,
            Frame imsakFrame, Label imsakYazi, Label imsakVakit,
            Frame gunesFrame, Label gunesYazi, Label gunesVakit,
            Frame ogleFrame, Label ogleYazi, Label ogleVakit,
            Frame ikindiFrame, Label ikindiYazi, Label ikindiVakit,
            Frame aksamFrame, Label aksamYazi, Label aksamVakit,
            Frame yatsiFrame, Label yatsiYazi, Label yatsiVakit,
            Frame ayetFrame, Label gununAyeti)
        {
            var borderColor = Color.FromArgb("#80FFFFFF");
            var baseColor = Color.FromArgb("#30FFFFFF");
            var textColor = Colors.White;

            // Ana frame
            mainFrame.ApplyGlassmorphism(baseColor, borderColor);
            namazIsmi.TextColor = textColor;
            kalan.TextColor = textColor;
            konum.TextColor = textColor;

            // Namaz vakitleri frame'leri
            imsakFrame.ApplyPrayerTimeStyle(imsakYazi, imsakVakit, borderColor, textColor, baseColor);
            gunesFrame.ApplyPrayerTimeStyle(gunesYazi, gunesVakit, borderColor, textColor, baseColor);
            ogleFrame.ApplyPrayerTimeStyle(ogleYazi, ogleVakit, borderColor, textColor, baseColor);
            ikindiFrame.ApplyPrayerTimeStyle(ikindiYazi, ikindiVakit, borderColor, textColor, baseColor);
            aksamFrame.ApplyPrayerTimeStyle(aksamYazi, aksamVakit, borderColor, textColor, baseColor);
            yatsiFrame.ApplyPrayerTimeStyle(yatsiYazi, yatsiVakit, borderColor, textColor, baseColor);

            // Ayet frame
            ayetFrame.ApplyGlassmorphism(baseColor, borderColor);
            gununAyeti.TextColor = textColor;
        }

        /// <summary>
        /// Açık tema stillerini uygular
        /// </summary>
        private void ApplyLightTheme(
            Frame mainFrame, Label namazIsmi, Label kalan, Label konum,
            Frame imsakFrame, Label imsakYazi, Label imsakVakit,
            Frame gunesFrame, Label gunesYazi, Label gunesVakit,
            Frame ogleFrame, Label ogleYazi, Label ogleVakit,
            Frame ikindiFrame, Label ikindiYazi, Label ikindiVakit,
            Frame aksamFrame, Label aksamYazi, Label aksamVakit,
            Frame yatsiFrame, Label yatsiYazi, Label yatsiVakit,
            Frame ayetFrame, Label gununAyeti)
        {
            var borderColor = Color.FromArgb("#80009688");
            var baseColor = Color.FromArgb("#40FFFFFF");
            var textColor = Color.FromArgb("#00796B");

            // Ana frame
            mainFrame.ApplyGlassmorphism(baseColor, borderColor);
            namazIsmi.TextColor = textColor;
            kalan.TextColor = textColor;
            konum.TextColor = textColor;

            // Namaz vakitleri frame'leri
            imsakFrame.ApplyPrayerTimeStyle(imsakYazi, imsakVakit, borderColor, textColor, baseColor);
            gunesFrame.ApplyPrayerTimeStyle(gunesYazi, gunesVakit, borderColor, textColor, baseColor);
            ogleFrame.ApplyPrayerTimeStyle(ogleYazi, ogleVakit, borderColor, textColor, baseColor);
            ikindiFrame.ApplyPrayerTimeStyle(ikindiYazi, ikindiVakit, borderColor, textColor, baseColor);
            aksamFrame.ApplyPrayerTimeStyle(aksamYazi, aksamVakit, borderColor, textColor, baseColor);
            yatsiFrame.ApplyPrayerTimeStyle(yatsiYazi, yatsiVakit, borderColor, textColor, baseColor);

            // Ayet frame
            ayetFrame.BorderColor = Color.FromArgb("#8000796B");
            ayetFrame.ApplyGlassmorphism(baseColor, Color.FromArgb("#8000796B"));
            gununAyeti.TextColor = textColor;
        }
        /// <summary>
        /// Arkaplan parlaklığına göre framelere adaptif cam efekti uygular
        /// </summary>
        public void ApplyAdaptiveGlassTheme(
            bool isBrightBackground,
            Frame mainFrame, Label namazIsmi, Label kalan, Label konum,
            Frame imsakFrame, Label imsakYazi, Label imsakVakit,
            Frame gunesFrame, Label gunesYazi, Label gunesVakit,
            Frame ogleFrame, Label ogleYazi, Label ogleVakit,
            Frame ikindiFrame, Label ikindiYazi, Label ikindiVakit,
            Frame aksamFrame, Label aksamYazi, Label aksamVakit,
            Frame yatsiFrame, Label yatsiYazi, Label yatsiVakit,
            Frame ayetFrame, Label gununAyeti)
        {
            Color baseColor;
            Color borderColor;
            Color textColor;

            if (isBrightBackground)
            {
                // Arkaplan parlak ise (Gündüz vb.) -> KOYU CAM (Dark Glass) kullan ki görünsün
                // Örn: Siyah %50 opaklık
                baseColor = Color.FromArgb("#80000000"); 
                borderColor = Color.FromArgb("#99FFFFFF"); // Beyazımsı sınır
                textColor = Colors.White;
            }
            else
            {
                // Arkaplan karanlık ise (Gece vb.) -> AÇIK CAM (Light Glass) kullan
                // Örn: Beyaz %20-30 opaklık
                baseColor = Color.FromArgb("#40FFFFFF");
                borderColor = Color.FromArgb("#80FFFFFF");
                textColor = Colors.White; // Koyu arkaplanda beyaz yazı genelde iyidir ama frame açık renkse?
                // Light Glass (Beyazımsı) üstüne Beyaz yazı okunmayabilir.
                // Eğer frame beyazımsı ise yazı koyu olmalı.
                // Veya "Light Glass" tamamen şeffaf beyaz ise...
                // Varsayılan tasarımımızda gece arkaplan koyu, frameler beyazımsı, yazılar KOYU (ThemeService.ApplyLightTheme'deki gibi).
                
                // Ancak kullanıcı "Gece" modunda genelde beyaz yazı bekler.
                // Mevcut tasarım: Koyu arkaplanlarda beyaz yazı, açık frameler.
                // Hadi Light Theme renklerini baz alalım ama text rengine dikkat.
                
                // Revize:
                // Gece (Koyu BG) -> Frame: #30FFFFFF (Hafif Beyaz), Text: White. (ApplyDarkTheme gibi)
                baseColor = Color.FromArgb("#30FFFFFF");
                borderColor = Color.FromArgb("#50FFFFFF");
                textColor = Colors.White;
            }

            // Ana frame
            mainFrame.ApplyGlassmorphism(baseColor, borderColor);
            namazIsmi.TextColor = textColor;
            kalan.TextColor = textColor;
            konum.TextColor = textColor;

            // Namaz vakitleri
            void ApplyStyle(Frame f, Label l1, Label l2)
            {
                f.ApplyGlassmorphism(baseColor, borderColor);
                l1.TextColor = textColor;
                l2.TextColor = textColor;
            }

            ApplyStyle(imsakFrame, imsakYazi, imsakVakit);
            ApplyStyle(gunesFrame, gunesYazi, gunesVakit);
            ApplyStyle(ogleFrame, ogleYazi, ogleVakit);
            ApplyStyle(ikindiFrame, ikindiYazi, ikindiVakit);
            ApplyStyle(aksamFrame, aksamYazi, aksamVakit);
            ApplyStyle(yatsiFrame, yatsiYazi, yatsiVakit);

            // Ayet Frame
            ayetFrame.ApplyGlassmorphism(baseColor, borderColor);
            gununAyeti.TextColor = textColor;
        }
    }
}

using Microsoft.Maui.Graphics;

namespace hadis.Helpers
{
    /// <summary>
    /// Renk paletleri ve renk uyumu algoritmaları
    /// </summary>
    public static class ColorPaletteHelper
    {
        #region Hazır Renk Paletleri

        /// <summary>
        /// Material Design renk paleti
        /// </summary>
        public static class MaterialColors
        {
            public static readonly Color Teal = Color.FromArgb("#009688");
            public static readonly Color TealDark = Color.FromArgb("#00796B");
            public static readonly Color TealLight = Color.FromArgb("#B2DFDB");
            
            public static readonly Color Blue = Color.FromArgb("#2196F3");
            public static readonly Color BlueDark = Color.FromArgb("#1976D2");
            public static readonly Color BlueLight = Color.FromArgb("#BBDEFB");
            
            public static readonly Color Purple = Color.FromArgb("#9C27B0");
            public static readonly Color PurpleDark = Color.FromArgb("#7B1FA2");
            public static readonly Color PurpleLight = Color.FromArgb("#E1BEE7");
            
            public static readonly Color Pink = Color.FromArgb("#E91E63");
            public static readonly Color PinkDark = Color.FromArgb("#C2185B");
            public static readonly Color PinkLight = Color.FromArgb("#F8BBD0");
            
            public static readonly Color Orange = Color.FromArgb("#FF9800");
            public static readonly Color OrangeDark = Color.FromArgb("#F57C00");
            public static readonly Color OrangeLight = Color.FromArgb("#FFE0B2");
            
            public static readonly Color Green = Color.FromArgb("#4CAF50");
            public static readonly Color GreenDark = Color.FromArgb("#388E3C");
            public static readonly Color GreenLight = Color.FromArgb("#C8E6C9");
        }

        /// <summary>
        /// İslami tema renkleri
        /// </summary>
        public static class IslamicColors
        {
            public static readonly Color Gold = Color.FromArgb("#D4AF37");
            public static readonly Color GoldDark = Color.FromArgb("#B8860B");
            public static readonly Color GoldLight = Color.FromArgb("#F0E68C");
            
            public static readonly Color IslamicGreen = Color.FromArgb("#009900");
            public static readonly Color IslamicGreenDark = Color.FromArgb("#006600");
            public static readonly Color IslamicGreenLight = Color.FromArgb("#90EE90");
            
            public static readonly Color Pearl = Color.FromArgb("#F0EAD6");
            public static readonly Color Turquoise = Color.FromArgb("#40E0D0");
            public static readonly Color Burgundy = Color.FromArgb("#800020");
        }

        /// <summary>
        /// Koyu tema renkleri
        /// </summary>
        public static class DarkThemeColors
        {
            public static readonly Color Background = Color.FromArgb("#121212");
            public static readonly Color Surface = Color.FromArgb("#1E1E1E");
            public static readonly Color SurfaceVariant = Color.FromArgb("#2C2C2C");
            
            public static readonly Color TextPrimary = Color.FromArgb("#FFFFFF");
            public static readonly Color TextSecondary = Color.FromArgb("#B3B3B3");
            
            public static readonly Color Accent = Color.FromArgb("#BB86FC");
            public static readonly Color AccentVariant = Color.FromArgb("#3700B3");
        }

        /// <summary>
        /// Açık tema renkleri
        /// </summary>
        public static class LightThemeColors
        {
            public static readonly Color Background = Color.FromArgb("#FAFAFA");
            public static readonly Color Surface = Color.FromArgb("#FFFFFF");
            public static readonly Color SurfaceVariant = Color.FromArgb("#F5F5F5");
            
            public static readonly Color TextPrimary = Color.FromArgb("#212121");
            public static readonly Color TextSecondary = Color.FromArgb("#757575");
            
            public static readonly Color Accent = Color.FromArgb("#6200EE");
            public static readonly Color AccentVariant = Color.FromArgb("#3700B3");
        }

        #endregion

        #region Renk Uyumu Algoritmaları

        /// <summary>
        /// RGB rengini HSL (Hue, Saturation, Lightness) formatına çevirir
        /// </summary>
        public static (float Hue, float Saturation, float Lightness) RgbToHsl(Color color)
        {
            float r = color.Red;
            float g = color.Green;
            float b = color.Blue;

            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;

            float hue = 0;
            float saturation = 0;
            float lightness = (max + min) / 2f;

            if (delta != 0)
            {
                saturation = lightness > 0.5f ? delta / (2f - max - min) : delta / (max + min);

                if (max == r)
                    hue = ((g - b) / delta + (g < b ? 6 : 0)) / 6f;
                else if (max == g)
                    hue = ((b - r) / delta + 2) / 6f;
                else
                    hue = ((r - g) / delta + 4) / 6f;
            }

            return (hue * 360f, saturation, lightness);
        }

        /// <summary>
        /// HSL rengini RGB formatına çevirir
        /// </summary>
        public static Color HslToRgb(float hue, float saturation, float lightness)
        {
            hue = hue / 360f;

            if (saturation == 0)
            {
                return new Color(lightness, lightness, lightness);
            }

            float q = lightness < 0.5f ? lightness * (1 + saturation) : lightness + saturation - lightness * saturation;
            float p = 2 * lightness - q;

            float r = HueToRgb(p, q, hue + 1f / 3f);
            float g = HueToRgb(p, q, hue);
            float b = HueToRgb(p, q, hue - 1f / 3f);

            return new Color(r, g, b);
        }

        private static float HueToRgb(float p, float q, float t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1f / 6f) return p + (q - p) * 6 * t;
            if (t < 1f / 2f) return q;
            if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6;
            return p;
        }

        /// <summary>
        /// Complementary (Karşıt) renk - 180 derece karşısındaki renk
        /// </summary>
        public static Color GetComplementaryColor(Color baseColor)
        {
            var (hue, sat, light) = RgbToHsl(baseColor);
            float newHue = (hue + 180) % 360;
            return HslToRgb(newHue, sat, light);
        }

        /// <summary>
        /// Analogous (Komşu) renkler - Yan yana 3 renk
        /// </summary>
        public static List<Color> GetAnalogousColors(Color baseColor)
        {
            var (hue, sat, light) = RgbToHsl(baseColor);
            
            return new List<Color>
            {
                HslToRgb((hue - 30 + 360) % 360, sat, light),
                baseColor,
                HslToRgb((hue + 30) % 360, sat, light)
            };
        }

        /// <summary>
        /// Triadic (Üçgen) renkler - 120 derece ara ile 3 renk
        /// </summary>
        public static List<Color> GetTriadicColors(Color baseColor)
        {
            var (hue, sat, light) = RgbToHsl(baseColor);
            
            return new List<Color>
            {
                baseColor,
                HslToRgb((hue + 120) % 360, sat, light),
                HslToRgb((hue + 240) % 360, sat, light)
            };
        }

        /// <summary>
        /// Monochromatic (Tek renk) tonları - Aynı rengin farklı tonları
        /// </summary>
        public static List<Color> GetMonochromaticColors(Color baseColor)
        {
            var (hue, sat, light) = RgbToHsl(baseColor);
            
            return new List<Color>
            {
                HslToRgb(hue, sat, Math.Max(0.1f, light - 0.2f)), // Koyu
                HslToRgb(hue, sat, Math.Max(0.2f, light - 0.1f)),
                baseColor,
                HslToRgb(hue, sat, Math.Min(0.9f, light + 0.1f)),
                HslToRgb(hue, sat, Math.Min(1.0f, light + 0.2f))  // Açık
            };
        }

        /// <summary>
        /// Tetradic (Dörtgen) renkler - Kare şeklinde 4 renk
        /// </summary>
        public static List<Color> GetTetradicColors(Color baseColor)
        {
            var (hue, sat, light) = RgbToHsl(baseColor);
            
            return new List<Color>
            {
                baseColor,
                HslToRgb((hue + 90) % 360, sat, light),
                HslToRgb((hue + 180) % 360, sat, light),
                HslToRgb((hue + 270) % 360, sat, light)
            };
        }

        #endregion

        #region Gradient Helpers

        /// <summary>
        /// İki renk arasında gradient oluşturur
        /// </summary>
        public static List<Color> CreateGradient(Color startColor, Color endColor, int steps = 5)
        {
            var colors = new List<Color>();
            
            for (int i = 0; i < steps; i++)
            {
                float ratio = (float)i / (steps - 1);
                
                float r = startColor.Red + (endColor.Red - startColor.Red) * ratio;
                float g = startColor.Green + (endColor.Green - startColor.Green) * ratio;
                float b = startColor.Blue + (endColor.Blue - startColor.Blue) * ratio;
                float a = startColor.Alpha + (endColor.Alpha - startColor.Alpha) * ratio;
                
                colors.Add(new Color(r, g, b, a));
            }
            
            return colors;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Rengin koyu mu açık mı olduğunu belirler
        /// </summary>
        public static bool IsDarkColor(Color color)
        {
            double luminance = 0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue;
            return luminance < 0.5;
        }

        /// <summary>
        /// Verilen renk üzerinde okunabilir bir yazı rengi döndürür
        /// </summary>
        public static Color GetContrastingTextColor(Color backgroundColor)
        {
            return IsDarkColor(backgroundColor) ? Colors.White : Colors.Black;
        }

        #endregion
    }
}

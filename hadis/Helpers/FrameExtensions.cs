namespace hadis.Helpers
{
    /// <summary>
    /// Border ve UI elementleri için extension metodları
    /// </summary>
    public static class FrameExtensions
    {
        /// <summary>
        /// Border'a glassmorphism efekti uygular
        /// </summary>
        public static void ApplyGlassmorphism(this Border border, Color baseColor, Color borderColor)
        {
            border.Stroke = borderColor;
            border.Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop 
                    { 
                        Color = baseColor.WithAlpha(AppConstants.GLASSMORPHISM_ALPHA_START), 
                        Offset = 0.0f 
                    },
                    new GradientStop 
                    { 
                        Color = baseColor.WithAlpha(AppConstants.GLASSMORPHISM_ALPHA_END), 
                        Offset = 1.0f 
                    }
                }
            };
        }

        /// <summary>
        /// Namaz vakti border'larına stil uygular
        /// </summary>
        public static void ApplyPrayerTimeStyle(
            this Border border, 
            Label titleLabel, 
            Label timeLabel,
            Color borderColor,
            Color textColor,
            Color baseColor)
        {
            border.ApplyGlassmorphism(baseColor, borderColor);
            titleLabel.TextColor = textColor;
            timeLabel.TextColor = textColor;
        }
    }

    /// <summary>
    /// Color helper metodları
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Hex renk kodundan parlaklık hesaplar
        /// </summary>
        public static bool IsLightColor(this string hexColor)
        {
            try
            {
                hexColor = hexColor.Replace("#", "");
                int r = Convert.ToInt32(hexColor.Substring(0, 2), 16);
                int g = Convert.ToInt32(hexColor.Substring(2, 2), 16);
                int b = Convert.ToInt32(hexColor.Substring(4, 2), 16);

                double brightness = (r * AppConstants.RED_LUMINANCE_COEFFICIENT + 
                                   g * AppConstants.GREEN_LUMINANCE_COEFFICIENT + 
                                   b * AppConstants.BLUE_LUMINANCE_COEFFICIENT);

                return brightness > AppConstants.BRIGHTNESS_THRESHOLD;
            }
            catch
            {
                return false; // Hata durumunda koyu kabul et
            }
        }
    }
}

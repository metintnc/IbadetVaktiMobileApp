namespace hadis.Helpers
{
    /// <summary>
    /// Frame ve UI elementleri için extension metodları
    /// </summary>
    public static class FrameExtensions
    {
        /// <summary>
        /// Frame'e glassmorphism efekti uygular
        /// </summary>
        public static void ApplyGlassmorphism(this Frame frame, Color baseColor, Color borderColor)
        {
            frame.BorderColor = borderColor;
            frame.Background = new LinearGradientBrush
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
        /// Namaz vakti frame'lerine stil uygular
        /// </summary>
        public static void ApplyPrayerTimeStyle(
            this Frame frame, 
            Label titleLabel, 
            Label timeLabel,
            Color borderColor,
            Color textColor,
            Color baseColor)
        {
            frame.ApplyGlassmorphism(baseColor, borderColor);
            titleLabel.TextColor = textColor;
            timeLabel.TextColor = textColor;
        }

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
        /// Namaz vakti border'larina stil uygular
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

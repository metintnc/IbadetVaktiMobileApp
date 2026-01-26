namespace hadis.Helpers
{
    /// <summary>
    /// Saatlere göre arkaplan resmi ve status bar renk konfigürasyonu
    /// </summary>
    public static class TimeBasedBackgroundConfig
    {
        public record BackgroundInfo(string Image, string StatusBarColor, string TabBarColor);

        /// <summary>
        /// Saatlere göre arkaplan resmi, status bar rengi ve TabBar rengi
        /// </summary>
        public static BackgroundInfo GetBackgroundForTime(int hour, int minute)
        {
            // Gece 00:00 - 04:59
            if (hour >= 0 && hour < 5)
            {
                return new BackgroundInfo("sun_01.png", "#05051B", "#000115");
            }
            // Sabah erkeni 05:00 - 05:29
            else if (hour == 5 && minute < 30)
            {
                return new BackgroundInfo("sun_02.png", "#060723", "#040519");
            }
            // Şafak 05:30 - 06:59
            else if ((hour == 5 && minute >= 30) || (hour == 6))
            {
                return new BackgroundInfo("sun_03.png", "#4B427E", "#0C0718");
            }
            // Sabah 07:00 - 08:59
            else if (hour >= 7 && hour < 9)
            {
                return new BackgroundInfo("sun_04.png", "#4077D9", "#181F3D");
            }
            // Kuşluk 09:00 - 10:59
            else if (hour >= 9 && hour < 11)
            {
                return new BackgroundInfo("sun_05.png", "#2F71E4", "#13254F");
            }
            // Öğle 11:00 - 12:59
            else if (hour >= 11 && hour < 13)
            {
                return new BackgroundInfo("sun_06.png", "#5E92F3", "#14255D");
            }
            // Öğleden sonra 13:00 - 14:59
            else if (hour >= 13 && hour < 15)
            {
                return new BackgroundInfo("sun_07.png", "#5C89F2", "#192143");
            }
            // İkindi 15:00 - 16:59
            else if (hour >= 15 && hour < 17)
            {
                return new BackgroundInfo("sun_08.png", "#6376C6", "#271C2F");
            }
            // Akşam 17:00 - 18:59
            else if (hour >= 17 && hour < 19)
            {
                return new BackgroundInfo("sun_09.png", "#22133A", "#251334");
            }
            // Gece 19:00 - 23:59
            else // hour >= 19 && hour < 24
            {
                return new BackgroundInfo("sun_10.png", "#08091D", "#0C0D2A");
            }
        }

        /// <summary>
        /// Özel tema arkaplanları için status bar rengi
        /// </summary>
        public static string GetStatusBarColorForCustomBackground(string backgroundValue)
        {
            if (string.IsNullOrEmpty(backgroundValue))
                return "#000000";

            // Resim dosyaları için
            if (backgroundValue.EndsWith(".jpg") || backgroundValue.EndsWith(".png"))
            {
                if (backgroundValue.Contains("sun_01")) return "#0D1B2A";
                if (backgroundValue.Contains("sun_02")) return "#1B263B";
                if (backgroundValue.Contains("sun_03")) return "#415A77";
                if (backgroundValue.Contains("sun_04")) return "#E07A5F";
                if (backgroundValue.Contains("sun_05")) return "#81B29A";
                if (backgroundValue.Contains("sun_06")) return "#3D5A80";
                if (backgroundValue.Contains("sun_07")) return "#4A90A4";
                if (backgroundValue.Contains("sun_08")) return "#98C1D9";
                if (backgroundValue.Contains("sun_09")) return "#EE6C4D";
                if (backgroundValue.Contains("sun_10")) return "#293241";
                return "#1A1A1A"; // Diğer resimler için koyu gri
            }

            // Gradient'ler için
            if (backgroundValue.StartsWith("gradient_"))
            {
                return backgroundValue switch
                {
                    "gradient_blue" => "#1e3c72",
                    "gradient_green" => "#134E5E",
                    "gradient_dark_blue" => "#2C3E50",
                    "gradient_night" => "#141E30",
                    _ => "#000000"
                };
            }

            // Hex renk kodu ise direkt kullan
            if (backgroundValue.StartsWith("#"))
            {
                return backgroundValue;
            }

            return "#000000";
        }

        /// <summary>
        /// Özel tema arkaplanları için TabBar rengi
        /// </summary>
        public static string GetTabBarColorForCustomBackground(string backgroundValue)
        {
            if (string.IsNullOrEmpty(backgroundValue))
                return "#1A1A1A";

            // Resim dosyaları için
            if (backgroundValue.EndsWith(".jpg") || backgroundValue.EndsWith(".png"))
            {
                if (backgroundValue.Contains("sun_01")) return "#000115";
                if (backgroundValue.Contains("sun_02")) return "#040519";
                if (backgroundValue.Contains("sun_03")) return "#0C0718";
                if (backgroundValue.Contains("sun_04")) return "#181F3D";
                if (backgroundValue.Contains("sun_05")) return "#13254F";
                if (backgroundValue.Contains("sun_06")) return "#14255D";
                if (backgroundValue.Contains("sun_07")) return "#192143";
                if (backgroundValue.Contains("sun_08")) return "#271C2F";
                if (backgroundValue.Contains("sun_09")) return "#251334";
                if (backgroundValue.Contains("sun_10")) return "#0C0D2A";
                return "#1A1A1A"; // Diğer resimler için koyu gri
            }

            // Gradient'ler için
            if (backgroundValue.StartsWith("gradient_"))
            {
                return backgroundValue switch
                {
                    "gradient_blue" => "#1e3c72",
                    "gradient_green" => "#134E5E",
                    "gradient_dark_blue" => "#2C3E50",
                    "gradient_night" => "#141E30",
                    _ => "#1A1A1A"
                };
            }

            // Hex renk kodu ise
            if (backgroundValue.StartsWith("#"))
            {
                // Aynı rengi kullan
                return backgroundValue;
            }

            return "#1A1A1A";
        }

        /// <summary>
        /// Arkaplan parlak mı diye kontrol eder
        /// </summary>
        public static bool IsBackgroundBright(string backgroundValue)
        {
            // Gündüz saatleri için parlak kabul edilen arkaplanlar
            if (backgroundValue.Contains("sun_04") || 
                backgroundValue.Contains("sun_05") || 
                backgroundValue.Contains("sun_06") || 
                backgroundValue.Contains("sun_07") || 
                backgroundValue.Contains("sun_08") || 
                backgroundValue.Contains("sun_09"))
            {
                return true;
            }

            // Gradientler için
            if (backgroundValue == "gradient_blue" || backgroundValue == "gradient_green")
            {
                return true;
            }

            // Hex renk kodu ise parlaklık hesapla
            if (backgroundValue.StartsWith("#") && backgroundValue.Length == 7)
            {
                try
                {
                    int r = Convert.ToInt32(backgroundValue.Substring(1, 2), 16);
                    int g = Convert.ToInt32(backgroundValue.Substring(3, 2), 16);
                    int b = Convert.ToInt32(backgroundValue.Substring(5, 2), 16);
                    double brightness = (r * AppConstants.RED_LUMINANCE_COEFFICIENT + 
                                       g * AppConstants.GREEN_LUMINANCE_COEFFICIENT + 
                                       b * AppConstants.BLUE_LUMINANCE_COEFFICIENT);
                    return brightness > AppConstants.BRIGHT_COLOR_THRESHOLD;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }
}

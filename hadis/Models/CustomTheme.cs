namespace hadis.Models
{
    public class CustomTheme
    {
        public string Name { get; set; } = "Ozel Tema";
        
        // Ana Frame Renkleri
        public string MainFrameBackground { get; set; } = "#00000000";
        public string MainFrameBorder { get; set; } = "#00796B";
        public string MainFrameText { get; set; } = "#212121";
        
        // Kucuk Frame'ler Renkleri
        public string SmallFrameBackground { get; set; } = "#00000000";
        public string SmallFrameBorder { get; set; } = "#00796B";
        public string SmallFrameText { get; set; } = "#212121";
        
        // Ayet Frame Renkleri
        public string AyetFrameBackground { get; set; } = "#00000000";
        public string AyetFrameBorder { get; set; } = "#00796B";
        public string AyetFrameText { get; set; } = "#212121";
        
        // Arkaplan
        public string BackgroundImage { get; set; } = "bg_dark.jpg";
        public double BackgroundOpacity { get; set; } = 0.3;

        public CustomTheme Clone()
        {
            return new CustomTheme
            {
                Name = this.Name,
                MainFrameBackground = this.MainFrameBackground,
                MainFrameBorder = this.MainFrameBorder,
                MainFrameText = this.MainFrameText,
                SmallFrameBackground = this.SmallFrameBackground,
                SmallFrameBorder = this.SmallFrameBorder,
                SmallFrameText = this.SmallFrameText,
                AyetFrameBackground = this.AyetFrameBackground,
                AyetFrameBorder = this.AyetFrameBorder,
                AyetFrameText = this.AyetFrameText,
                BackgroundImage = this.BackgroundImage,
                BackgroundOpacity = this.BackgroundOpacity
            };
        }
    }
}

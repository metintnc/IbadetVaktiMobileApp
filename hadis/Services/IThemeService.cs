namespace hadis.Services
{
    /// <summary>
    /// Tema yönetimi için interface
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// Özel temayı tüm frame'lere uygular
        /// </summary>
        void ApplyCustomTheme(
            Frame mainFrame, Label namazIsmi, Label kalan, Label konum,
            Frame imsakFrame, Label imsakYazi, Label imsakVakit,
            Frame gunesFrame, Label gunesYazi, Label gunesVakit,
            Frame ogleFrame, Label ogleYazi, Label ogleVakit,
            Frame ikindiFrame, Label ikindiYazi, Label ikindiVakit,
            Frame aksamFrame, Label aksamYazi, Label aksamVakit,
            Frame yatsiFrame, Label yatsiYazi, Label yatsiVakit,
            Frame ayetFrame, Label gununAyeti);

        /// <summary>
        /// Varsayılan (sistem) tema stillerini uygular
        /// </summary>
        void ResetToDefaultStyles(
            Frame mainFrame, Label namazIsmi, Label kalan, Label konum,
            Frame imsakFrame, Label imsakYazi, Label imsakVakit,
            Frame gunesFrame, Label gunesYazi, Label gunesVakit,
            Frame ogleFrame, Label ogleYazi, Label ogleVakit,
            Frame ikindiFrame, Label ikindiYazi, Label ikindiVakit,
            Frame aksamFrame, Label aksamYazi, Label aksamVakit,
            Frame yatsiFrame, Label yatsiYazi, Label yatsiVakit,
            Frame ayetFrame, Label gununAyeti);
    }
}

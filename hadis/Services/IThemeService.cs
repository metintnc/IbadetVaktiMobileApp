namespace hadis.Services
{
    /// <summary>
    /// Tema yönetimi için interface
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// Özel temayı tüm border'lara uygular
        /// </summary>
        void ApplyCustomTheme(
            Border mainBorder, Label namazIsmi, Label kalan, Label konum,
            Border imsakBorder, Label imsakYazi, Label imsakVakit,
            Border gunesBorder, Label gunesYazi, Label gunesVakit,
            Border ogleBorder, Label ogleYazi, Label ogleVakit,
            Border ikindiBorder, Label ikindiYazi, Label ikindiVakit,
            Border aksamBorder, Label aksamYazi, Label aksamVakit,
            Border yatsiBorder, Label yatsiYazi, Label yatsiVakit,
            Border ayetBorder, Label gununAyeti);

        /// <summary>
        /// Varsayılan (sistem) tema stillerini uygular
        /// </summary>
        void ResetToDefaultStyles(
            Border mainBorder, Label namazIsmi, Label kalan, Label konum,
            Border imsakBorder, Label imsakYazi, Label imsakVakit,
            Border gunesBorder, Label gunesYazi, Label gunesVakit,
            Border ogleBorder, Label ogleYazi, Label ogleVakit,
            Border ikindiBorder, Label ikindiYazi, Label ikindiVakit,
            Border aksamBorder, Label aksamYazi, Label aksamVakit,
            Border yatsiBorder, Label yatsiYazi, Label yatsiVakit,
            Border ayetBorder, Label gununAyeti);
    }
}

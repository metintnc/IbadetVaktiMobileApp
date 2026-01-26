namespace hadis.Services
{
    /// <summary>
    /// Arkaplan yönetimi için interface
    /// </summary>
    public interface IBackgroundService
    {
        /// <summary>
        /// Saate göre otomatik arkaplan ayarlar
        /// </summary>
        void SetTimeBasedBackground(Image backgroundImage, Grid backgroundOverlay, string savedTheme);

        /// <summary>
        /// Özel tema arkaplanını uygular
        /// </summary>
        void ApplyCustomBackground(Image backgroundImage, Grid backgroundOverlay, string backgroundValue);
    }
}

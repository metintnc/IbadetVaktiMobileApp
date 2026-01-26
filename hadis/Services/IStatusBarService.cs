namespace hadis.Services
{
    /// <summary>
    /// Status bar yönetimi için interface
    /// </summary>
    public interface IStatusBarService
    {
        /// <summary>
        /// Status bar rengini ayarlar
        /// </summary>
        /// <param name="hexColor">Hex formatında renk kodu (örn: #FF0000)</param>
        void SetStatusBarColor(string hexColor);
    }
}

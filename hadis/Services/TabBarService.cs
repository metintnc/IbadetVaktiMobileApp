namespace hadis.Services
{
    /// <summary>
    /// TabBar renk yÃ¶netimi servisi
    /// </summary>
    public class TabBarService
    {
        /// <summary>
        /// TabBar rengini ayarlar (tÃ¼m platformlar)
        /// </summary>
        public void SetTabBarColor(string hexColor)
        {
            try
            {
                if (Application.Current?.MainPage == null)
                {
                    System.Diagnostics.Debug.WriteLine("âš ï¸ MainPage null - TabBar rengi ayarlanamÄ±yor");
                    return;
                }

                // Shell'i bul
                if (Shell.Current != null)
                {
                    Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            // TabBar rengini ayarla
                            Shell.SetTabBarBackgroundColor(Shell.Current, Color.FromArgb(hexColor));
                            
                            System.Diagnostics.Debug.WriteLine($"âœ… TabBar rengi deÄŸiÅŸtirildi: {hexColor}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"âŒ TabBar renk ayarlama hatasÄ± (Main thread): {ex.Message}");
                        }
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("âš ï¸ Shell.Current null - TabBar rengi ayarlanamÄ±yor");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"âŒ TabBar renk ayarlama hatasÄ±: {ex.Message}");
            }
        }
    }
}


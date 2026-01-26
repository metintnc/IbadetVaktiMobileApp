namespace hadis.Services
{
    /// <summary>
    /// TabBar renk yönetimi servisi
    /// </summary>
    public class TabBarService
    {
        /// <summary>
        /// TabBar rengini ayarlar (tüm platformlar)
        /// </summary>
        public void SetTabBarColor(string hexColor)
        {
            try
            {
                if (Application.Current?.MainPage == null)
                {
                    Console.WriteLine("⚠️ MainPage null - TabBar rengi ayarlanamıyor");
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
                            
                            Console.WriteLine($"✅ TabBar rengi değiştirildi: {hexColor}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ TabBar renk ayarlama hatası (Main thread): {ex.Message}");
                        }
                    });
                }
                else
                {
                    Console.WriteLine("⚠️ Shell.Current null - TabBar rengi ayarlanamıyor");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ TabBar renk ayarlama hatası: {ex.Message}");
            }
        }
    }
}

namespace hadis.Services
{
    /// <summary>
    /// TabBar renk yönetimi servisi
    /// </summary>
    public class TabBarService
    {
        // Sık kullanılan renkler için cache
        private static readonly Dictionary<string, Color> _colorCache = new();
        private static readonly object _cacheLock = new();

        /// <summary>
        /// Hex string'i Color'a çevirir (cache'li)
        /// </summary>
        private static Color GetCachedColor(string hexColor)
        {
            lock (_cacheLock)
            {
                if (!_colorCache.TryGetValue(hexColor, out var color))
                {
                    color = Color.FromArgb(hexColor);
                    _colorCache[hexColor] = color;
                }
                return color;
            }
        }

        /// <summary>
        /// TabBar rengini ayarlar (tüm platformlar)
        /// </summary>
        public void SetTabBarColor(string hexColor)
        {
            try
            {
                if (Application.Current?.MainPage == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ MainPage null - TabBar rengi ayarlanamıyor");
                    return;
                }

                if (Shell.Current != null)
                {
                    // Cache'li renk kullan
                    var color = GetCachedColor(hexColor);
                    
                    Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            Shell.SetTabBarBackgroundColor(Shell.Current, color);
                            System.Diagnostics.Debug.WriteLine($"✅ TabBar rengi değiştirildi: {hexColor}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ TabBar renk ayarlama hatası (Main thread): {ex.Message}");
                        }
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Shell.Current null - TabBar rengi ayarlanamıyor");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ TabBar renk ayarlama hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// TabBar rengini Color ile ayarlar (parsing yok)
        /// </summary>
        public void SetTabBarColor(Color color)
        {
            try
            {
                if (Shell.Current != null)
                {
                    Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                    {
                        try
                        {
                            Shell.SetTabBarBackgroundColor(Shell.Current, color);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ TabBar renk ayarlama hatası: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ TabBar renk ayarlama hatası: {ex.Message}");
            }
        }
    }
}


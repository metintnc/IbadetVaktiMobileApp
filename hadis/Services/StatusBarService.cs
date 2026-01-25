using hadis.Helpers;

#if ANDROID
using Android.OS;
using Android.Views;
#endif

namespace hadis.Services
{
    /// <summary>
    /// Android status bar renk ve görünüm yönetimi
    /// </summary>
    public class StatusBarService
    {
        /// <summary>
        /// Status bar rengini ayarlar (Android)
        /// </summary>
        public void SetStatusBarColor(string hexColor)
        {
#if ANDROID
            try
            {
                var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                if (activity?.Window == null)
                {
                    Console.WriteLine("⚠️ Activity veya Window null - status bar ayarlanamıyor");
                    return;
                }

                activity.RunOnUiThread(() =>
                {
                    try
                    {
                        // Hex rengini Android Color'a çevir
                        var color = Android.Graphics.Color.ParseColor(hexColor);
                        activity.Window.SetStatusBarColor(color);

                        Console.WriteLine($"✅ Status bar rengi değiştirildi: {hexColor}");

                        // Rengin açık mı koyu mu olduğunu hesapla
                        bool isLightColor = hexColor.IsLightColor();

                        // Android 6.0 ve üzeri için icon rengini ayarla
                        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                        {
                            var decorView = activity.Window.DecorView;
                            var systemUiVisibility = decorView.SystemUiVisibility;

                            if (isLightColor)
                            {
                                // Açık renk için koyu iconlar
                                decorView.SystemUiVisibility = (StatusBarVisibility)
                                    ((int)systemUiVisibility | (int)SystemUiFlags.LightStatusBar);
                                Console.WriteLine("✅ Status bar iconları koyu yapıldı (açık arkaplan için)");
                            }
                            else
                            {
                                // Koyu renk için açık iconlar
                                decorView.SystemUiVisibility = (StatusBarVisibility)
                                    ((int)systemUiVisibility & ~(int)SystemUiFlags.LightStatusBar);
                                Console.WriteLine("✅ Status bar iconları açık yapıldı (koyu arkaplan için)");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Status bar renk ayarlama hatası (UI thread): {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Status bar renk ayarlama hatası: {ex.Message}");
            }
#else
            // iOS ve diğer platformlar için şimdilik boş
            Console.WriteLine($"ℹ️ Status bar rengi sadece Android'de destekleniyor: {hexColor}");
#endif
        }
    }
}

using hadis.Helpers;

#if ANDROID
using Android.OS;
using Android.Views;
#endif

namespace hadis.Services
{
    /// <summary>
    /// Android status bar renk ve gÃ¶rÃ¼nÃ¼m yÃ¶netimi
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
                    System.Diagnostics.Debug.WriteLine("âš ï¸ Activity veya Window null - status bar ayarlanamÄ±yor");
                    return;
                }

                activity.RunOnUiThread(() =>
                {
                    try
                    {
                        // Hex rengini Android Color'a Ã§evir
                        var color = Android.Graphics.Color.ParseColor(hexColor);
                        activity.Window.SetStatusBarColor(color);

                        System.Diagnostics.Debug.WriteLine($"âœ… Status bar rengi deÄŸiÅŸtirildi: {hexColor}");

                        // Rengin aÃ§Ä±k mÄ± koyu mu olduÄŸunu hesapla
                        bool isLightColor = hexColor.IsLightColor();

                        // Android 6.0 ve Ã¼zeri iÃ§in icon rengini ayarla
                        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                        {
                            var decorView = activity.Window.DecorView;
                            var systemUiVisibility = decorView.SystemUiVisibility;

                            if (isLightColor)
                            {
                                // AÃ§Ä±k renk iÃ§in koyu iconlar
                                decorView.SystemUiVisibility = (StatusBarVisibility)
                                    ((int)systemUiVisibility | (int)SystemUiFlags.LightStatusBar);
                                System.Diagnostics.Debug.WriteLine("âœ… Status bar iconlarÄ± koyu yapÄ±ldÄ± (aÃ§Ä±k arkaplan iÃ§in)");
                            }
                            else
                            {
                                // Koyu renk iÃ§in aÃ§Ä±k iconlar
                                decorView.SystemUiVisibility = (StatusBarVisibility)
                                    ((int)systemUiVisibility & ~(int)SystemUiFlags.LightStatusBar);
                                System.Diagnostics.Debug.WriteLine("âœ… Status bar iconlarÄ± aÃ§Ä±k yapÄ±ldÄ± (koyu arkaplan iÃ§in)");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"âŒ Status bar renk ayarlama hatasÄ± (UI thread): {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"âŒ Status bar renk ayarlama hatasÄ±: {ex.Message}");
            }
#else
            // iOS ve diÄŸer platformlar iÃ§in ÅŸimdilik boÅŸ
            System.Diagnostics.Debug.WriteLine($"â„¹ï¸ Status bar rengi sadece Android'de destekleniyor: {hexColor}");
#endif
        }
    }
}


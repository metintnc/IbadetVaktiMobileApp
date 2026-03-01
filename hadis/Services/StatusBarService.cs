using hadis.Helpers;

#if ANDROID
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
#endif

namespace hadis.Services
{
    /// <summary>
    /// Android status bar renk ve görünüm yönetimi
    /// Android 15+ için edge-to-edge uyumlu
    /// </summary>
    public class StatusBarService
    {
        /// <summary>
        /// Status bar rengini ayarlar (Android)
        /// Android 15+ için WindowInsetsController kullanır
        /// </summary>
        public void SetStatusBarColor(string hexColor)
        {
#if ANDROID
            try
            {
                var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                if (activity?.Window == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Activity veya Window null - status bar ayarlanamıyor");
                    return;
                }

                activity.RunOnUiThread(() =>
                {
                    try
                    {
                        // Rengin açık mı koyu mu olduğunu hesapla
                        bool isLightColor = hexColor.IsLightColor();

                        // Android 15+ (API 35) için yeni edge-to-edge API kullan
                        if (Build.VERSION.SdkInt >= BuildVersionCodes.VanillaIceCream) // Android 15 = VanillaIceCream (API 35)
                        {
                            // Android 15'te SetStatusBarColor kullanılamıyor
                            // Bunun yerine WindowInsetsController ile sadece icon rengi ayarlanabilir
                            ConfigureStatusBarAppearance(activity, isLightColor);
                            System.Diagnostics.Debug.WriteLine($"✅ Android 15+ Status bar appearance ayarlandı (edge-to-edge): isLight={isLightColor}");
                        }
                        else
                        {
                            // Android 14 ve altı için eski API'yi kullan
                            SetStatusBarColorLegacy(activity, hexColor, isLightColor);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Status bar renk ayarlama hatası (UI thread): {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Status bar renk ayarlama hatası: {ex.Message}");
            }
#else
            // iOS ve diğer platformlar için şimdilik boş
            System.Diagnostics.Debug.WriteLine($"ℹ️ Status bar rengi sadece Android'de destekleniyor: {hexColor}");
#endif
        }

#if ANDROID
        /// <summary>
        /// Android 15+ için WindowInsetsController kullanarak status bar görünümünü ayarlar
        /// </summary>
        private void ConfigureStatusBarAppearance(Android.App.Activity activity, bool isLightStatusBar)
        {
            try
            {
                var window = activity.Window;
                if (window == null) return;

                // WindowInsetsControllerCompat kullan (AndroidX)
                var insetsController = WindowCompat.GetInsetsController(window, window.DecorView);
                if (insetsController != null)
                {
                    // Açık arka plan için koyu iconlar, koyu arka plan için açık iconlar
                    insetsController.AppearanceLightStatusBars = isLightStatusBar;
                    System.Diagnostics.Debug.WriteLine($"✅ WindowInsetsController ile status bar iconları ayarlandı: LightStatusBars={isLightStatusBar}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ WindowInsetsController hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Android 14 ve altı için eski (legacy) API kullanarak status bar rengini ayarlar
        /// </summary>
        private void SetStatusBarColorLegacy(Android.App.Activity activity, string hexColor, bool isLightColor)
        {
            try
            {
                var window = activity.Window;
                if (window == null) return;

                // Hex rengini Android Color'a çevir
                var color = Android.Graphics.Color.ParseColor(hexColor);
                
#pragma warning disable CA1422 // Android 15'te deprecated ama eski sürümler için gerekli
                window.SetStatusBarColor(color);
#pragma warning restore CA1422

                System.Diagnostics.Debug.WriteLine($"✅ Status bar rengi değiştirildi: {hexColor}");

                // WindowInsetsControllerCompat kullan (daha güvenli)
                var insetsController = WindowCompat.GetInsetsController(window, window.DecorView);
                if (insetsController != null)
                {
                    insetsController.AppearanceLightStatusBars = isLightColor;
                    System.Diagnostics.Debug.WriteLine($"✅ Status bar iconları ayarlandı: LightStatusBars={isLightColor}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Legacy status bar ayarlama hatası: {ex.Message}");
            }
        }
#endif
    }
}


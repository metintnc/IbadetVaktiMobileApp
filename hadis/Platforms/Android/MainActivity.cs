using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Content;
using AndroidX.Core.View;
using Android.Content.Res;

namespace hadis
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Notification tap handling
            Plugin.LocalNotification.LocalNotificationCenter.NotifyNotificationTapped(Intent);

            // Ekran yönünü cihaz tipine göre ayarla
            ConfigureScreenOrientation();

            // Edge-to-edge desteğini etkinleştir (Android 15+ için zorunlu)
            ConfigureEdgeToEdge();

            // Android 12+ için exact alarm izni kontrol et
            RequestExactAlarmPermission();

            // Günlük bildirim yenileme alarmını kur (gece 03:00)
            try
            {
                hadis.Platforms.Android.Services.NotificationAlarmHelper.ScheduleDailyAlarm(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Alarm kurma hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Cihaz tipine göre ekran yönünü ayarlar
        /// Telefonlar: Portrait kilidi
        /// Tabletler/Katlanabilir: Serbest (Android 16 uyumluluğu için)
        /// </summary>
        private void ConfigureScreenOrientation()
        {
            try
            {
                // Cihazın tablet mi telefon mu olduğunu kontrol et
                bool isTablet = IsTabletDevice();
                
                if (isTablet)
                {
                    // Tablet veya büyük ekranlı cihaz - yön serbest
                    RequestedOrientation = ScreenOrientation.Unspecified;
                    System.Diagnostics.Debug.WriteLine("✅ Tablet algılandı - ekran yönü serbest");
                }
                else
                {
                    // Telefon - portrait kilidi
                    RequestedOrientation = ScreenOrientation.Portrait;
                    System.Diagnostics.Debug.WriteLine("✅ Telefon algılandı - portrait kilidi uygulandı");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Ekran yönü ayarlama hatası: {ex.Message}");
                // Hata durumunda portrait'e kilitle
                RequestedOrientation = ScreenOrientation.Portrait;
            }
        }

        /// <summary>
        /// Cihazın tablet olup olmadığını kontrol eder
        /// 600dp ve üzeri genişlik = tablet
        /// </summary>
        private bool IsTabletDevice()
        {
            try
            {
                // smallestScreenWidthDp kullanarak tablet kontrolü
                // 600dp ve üzeri genellikle tablet olarak kabul edilir
                var configuration = Resources?.Configuration;
                if (configuration != null)
                {
                    int smallestWidth = configuration.SmallestScreenWidthDp;
                    return smallestWidth >= 600;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Tablet kontrolü hatası: {ex.Message}");
            }
            
            return false; // Varsayılan olarak telefon kabul et
        }

        /// <summary>
        /// Edge-to-edge modunu yapılandırır
        /// Android 15+ için zorunlu, önceki sürümler için de önerilir
        /// </summary>
        private void ConfigureEdgeToEdge()
        {
            try
            {
                if (Window == null) return;

                // Edge-to-edge etkinleştir (içerik status bar ve navigation bar altına uzanır)
                WindowCompat.SetDecorFitsSystemWindows(Window, false);

                // Status bar ve navigation bar'ı yarı saydam yap
                Window.SetStatusBarColor(Android.Graphics.Color.Transparent);
                Window.SetNavigationBarColor(Android.Graphics.Color.Transparent);

                // Android 15+ için ek yapılandırma
                if (Build.VERSION.SdkInt >= BuildVersionCodes.VanillaIceCream)
                {
                    // Android 15'te edge-to-edge varsayılan olarak etkin
                    // Status bar icon rengini ayarla (koyu arka plan için açık iconlar)
                    var insetsController = WindowCompat.GetInsetsController(Window, Window.DecorView);
                    if (insetsController != null)
                    {
                        insetsController.AppearanceLightStatusBars = false; // Açık renkli iconlar (koyu arka plan için)
                        insetsController.AppearanceLightNavigationBars = false;
                    }
                    
                    System.Diagnostics.Debug.WriteLine("✅ Android 15+ edge-to-edge yapılandırıldı");
                }
                else
                {
                    // Android 14 ve altı için eski flag'ler
                    Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
                    Window.ClearFlags(WindowManagerFlags.TranslucentStatus);
                    
                    System.Diagnostics.Debug.WriteLine("✅ Android 14- edge-to-edge yapılandırıldı");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Edge-to-edge yapılandırma hatası: {ex.Message}");
            }
        }

        private void RequestExactAlarmPermission()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                try
                {
                    var alarmManager = (AlarmManager?)GetSystemService(AlarmService);
                    if (alarmManager != null && !alarmManager.CanScheduleExactAlarms())
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ SCHEDULE_EXACT_ALARM izni yok, izin sayfasına yönlendiriliyor...");
                        
                        // Kullanıcıyı ayarlar sayfasına yönlendir
                        var intent = new Intent(Android.Provider.Settings.ActionRequestScheduleExactAlarm);
                        intent.SetData(Android.Net.Uri.Parse($"package:{PackageName}"));
                        StartActivity(intent);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("✅ SCHEDULE_EXACT_ALARM izni mevcut");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Exact alarm izin kontrolü hatası: {ex.Message}");
                }
            }
        }

        protected override void OnNewIntent(Android.Content.Intent? intent)
        {
            Plugin.LocalNotification.LocalNotificationCenter.NotifyNotificationTapped(intent);
            base.OnNewIntent(intent);
        }
    }
}



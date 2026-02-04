using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Content;

namespace hadis
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density, ScreenOrientation = ScreenOrientation.Portrait)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Notification tap handling
            Plugin.LocalNotification.LocalNotificationCenter.NotifyNotificationTapped(Intent);

            // Window flag'lerini ayarla - status bar'ın rengini değiştirmek için gerekli
            if (Window != null)
            {
                Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
                Window.ClearFlags(WindowManagerFlags.TranslucentStatus);
            }

            // Android 12+ için exact alarm izni kontrol et
            RequestExactAlarmPermission();
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
                        System.Console.WriteLine("⚠️ SCHEDULE_EXACT_ALARM izni yok, izin sayfasına yönlendiriliyor...");
                        
                        // Kullanıcıyı ayarlar sayfasına yönlendir
                        var intent = new Intent(Android.Provider.Settings.ActionRequestScheduleExactAlarm);
                        intent.SetData(Android.Net.Uri.Parse($"package:{PackageName}"));
                        StartActivity(intent);
                    }
                    else
                    {
                        System.Console.WriteLine("✅ SCHEDULE_EXACT_ALARM izni mevcut");
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"⚠️ Exact alarm izin kontrolü hatası: {ex.Message}");
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

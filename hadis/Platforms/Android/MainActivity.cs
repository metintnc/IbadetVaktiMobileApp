using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

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
        }

        protected override void OnNewIntent(Android.Content.Intent? intent)
        {
            Plugin.LocalNotification.LocalNotificationCenter.NotifyNotificationTapped(intent);
            base.OnNewIntent(intent);
        }
    }
}

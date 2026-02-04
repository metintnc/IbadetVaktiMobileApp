using Android.App;
using Android.Runtime;

namespace hadis
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override void OnCreate()
        {
            base.OnCreate();
            CreateNotificationChannels();
        }

        private void CreateNotificationChannels()
        {
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                // Prayer Times Notification Channel
                var prayerChannel = new NotificationChannel(
                    "prayer_times_channel",
                    "Namaz Vakitleri",
                    NotificationImportance.High)
                {
                    Description = "Namaz vakti bildirimleri"
                };
                prayerChannel.EnableVibration(true);
                prayerChannel.EnableLights(true);

                // Persistent Notification Channel
                var persistentChannel = new NotificationChannel(
                    "persistent_channel",
                    "Sürekli Bildirim",
                    NotificationImportance.Low)
                {
                    Description = "Vakitleri gösteren sürekli bildirim"
                };
                persistentChannel.EnableVibration(false);
                persistentChannel.SetSound(null, null);

                var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
                notificationManager?.CreateNotificationChannel(prayerChannel);
                notificationManager?.CreateNotificationChannel(persistentChannel);

                System.Console.WriteLine("✅ Notification channels oluşturuldu");
            }
        }
    }
}

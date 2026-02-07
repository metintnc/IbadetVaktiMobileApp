using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace hadis.Platforms.Android.Services
{
    [Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeDataSync)]
    public class PersistentNotificationService : Service
    {
        private const int NOTIFICATION_ID = 9999;
        private const string CHANNEL_ID = "persistent_channel";

        public override IBinder? OnBind(Intent? intent)
        {
            return null;
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            if (intent?.Action == "STOP_SERVICE")
            {
                StopForeground(true);
                StopSelf();
                return StartCommandResult.NotSticky;
            }

            CreateNotificationChannel();
            
            var notification = CreateNotification(
                intent?.GetStringExtra("title") ?? "Namaz Vakitleri",
                intent?.GetStringExtra("message") ?? "Vakitler yükleniyor..."
            );

            StartForeground(NOTIFICATION_ID, notification);

            return StartCommandResult.Sticky;
        }

        private void CreateNotificationChannel()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                var channel = new NotificationChannel(
                    CHANNEL_ID,
                    "Sürekli Bildirim",
                    NotificationImportance.Low)
                {
                    Description = "Namaz vakitlerini gösteren sürekli bildirim",
                    LockscreenVisibility = NotificationVisibility.Public
                };
                channel.SetSound(null, null);
                channel.SetShowBadge(false);

                var notificationManager = (NotificationManager?)GetSystemService(NotificationService);
                notificationManager?.CreateNotificationChannel(channel);
            }
        }

        private Notification CreateNotification(string title, string message)
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);
            
            var pendingIntent = PendingIntent.GetActivity(
                this, 
                0, 
                intent, 
                PendingIntentFlags.Immutable
            );

            var builder = new NotificationCompat.Builder(this, CHANNEL_ID)
                .SetContentTitle(title)
                .SetContentText(message)
                .SetStyle(new NotificationCompat.BigTextStyle().BigText(message))
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetContentIntent(pendingIntent)
                .SetOngoing(true)
                .SetAutoCancel(false)
                .SetPriority(NotificationCompat.PriorityLow)
                .SetCategory(NotificationCompat.CategoryService)
                .SetVisibility(NotificationCompat.VisibilityPublic);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                builder.SetForegroundServiceBehavior(NotificationCompat.ForegroundServiceImmediate);
            }

            return builder.Build();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            StopForeground(true);
        }
    }
}


using Android.Content;
using hadis.Services;

namespace hadis.Platforms.Android.Services
{
    /// <summary>
    /// AlarmManager tarafından günde 1 kez tetiklenen BroadcastReceiver.
    /// Bildirimleri arka planda yeniden zamanlar.
    /// WorkManager kullanmaz – crash riski yok.
    /// </summary>
    [BroadcastReceiver(Enabled = true, Exported = false)]
    public class NotificationAlarmReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine("⏰ Günlük bildirim alarmı tetiklendi");

                // Bildirimleri arka planda yeniden zamanla
                Task.Run(async () =>
                {
                    try
                    {
                        // DI'dan servisleri al
                        var services = App.Current?.Handler?.MauiContext?.Services;
                        if (services == null)
                        {
                            System.Diagnostics.Debug.WriteLine("❌ DI container kullanılamıyor");
                            return;
                        }

                        var namazVaktiApiService = services.GetService<NamazVaktiApiService>();
                        var prayerTimesService = services.GetService<PrayerTimesService>();
                        var notificationService = services.GetService<IAppNotificationService>();

                        if (prayerTimesService != null && notificationService != null)
                        {
                            var typedNotificationService = notificationService as NotificationService;
                            if (typedNotificationService != null)
                            {
                                await typedNotificationService.ScheduleMultiDayNotificationsAsync(7);
                                System.Diagnostics.Debug.WriteLine("✅ Arka plan bildirim zamanlaması tamamlandı");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("❌ Servisler bulunamadı");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Arka plan zamanlama hatası: {ex.Message}");
                    }
                });

                // Bir sonraki günün alarmını kur
                NotificationAlarmHelper.ScheduleDailyAlarm(context);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ AlarmReceiver hatası: {ex.Message}");
            }
        }
    }
}

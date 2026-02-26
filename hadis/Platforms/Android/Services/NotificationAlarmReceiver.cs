using Android.Content;
using hadis.Services;

namespace hadis.Platforms.Android.Services
{
    /// <summary>
    /// AlarmManager tarafından günde 1 kez tetiklenen BroadcastReceiver.
    /// Bildirimleri arka planda yeniden zamanlar.
    /// WorkManager kullanmaz — crash riski yok.
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
                        // DI kullanmadan doğrudan PrayerTimesService ile çalış
                        var httpClient = new System.Net.Http.HttpClient();
                        var factory = new SimpleHttpClientFactory(httpClient);
                        var prayerTimesService = new PrayerTimesService(factory);
                        var notificationService = new NotificationService(prayerTimesService);

                        await notificationService.ScheduleMultiDayNotificationsAsync(7);
                        System.Diagnostics.Debug.WriteLine("✅ Arka plan bildirim zamanlaması tamamlandı");
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

    /// <summary>
    /// DI olmadan IHttpClientFactory oluşturmak için minimal implementasyon
    /// </summary>
    internal class SimpleHttpClientFactory : IHttpClientFactory
    {
        private readonly System.Net.Http.HttpClient _client;

        public SimpleHttpClientFactory(System.Net.Http.HttpClient client)
        {
            _client = client;
        }

        public System.Net.Http.HttpClient CreateClient(string name)
        {
            return _client;
        }
    }
}

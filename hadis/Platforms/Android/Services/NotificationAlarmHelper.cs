using Android.App;
using Android.Content;
using Android.OS;

namespace hadis.Platforms.Android.Services
{
    /// <summary>
    /// AlarmManager ile günlük bildirim yenileme alarmı kurar.
    /// WorkManager kullanmadan, hafif ve güvenilir arka plan zamanlaması.
    /// </summary>
    public static class NotificationAlarmHelper
    {
        private const int ALARM_REQUEST_CODE = 7777;

        /// <summary>
        /// Günlük tekrarlanan alarm kurar (gece 03:00'te tetiklenir)
        /// </summary>
        public static void ScheduleDailyAlarm(Context context)
        {
            try
            {
                var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
                if (alarmManager == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ AlarmManager alınamadı");
                    return;
                }

                var intent = new Intent(context, typeof(NotificationAlarmReceiver));
                var pendingIntent = PendingIntent.GetBroadcast(
                    context,
                    ALARM_REQUEST_CODE,
                    intent,
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

                if (pendingIntent == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ PendingIntent oluşturulamadı");
                    return;
                }

                // Bir sonraki gece 03:00'ü hesapla
                var now = DateTime.Now;
                var nextAlarm = now.Date.AddDays(1).AddHours(3); // Yarın saat 03:00
                
                // Eğer şimdi 03:00'ten önceyse bugünü kullan
                if (now.Hour < 3)
                {
                    nextAlarm = now.Date.AddHours(3); // Bugün saat 03:00
                }

                long triggerAtMillis = new DateTimeOffset(nextAlarm).ToUnixTimeMilliseconds();

                // Android sürümüne göre alarm kur
                if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
                {
                    // Android 12+ — exact alarm izni kontrol et
                    if (alarmManager.CanScheduleExactAlarms())
                    {
                        alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
                    }
                    else
                    {
                        // Exact alarm izni yoksa inexact alarm kullan
                        alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
                    }
                }
                else if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    // Android 6.0+ — Doze mode desteği
                    alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
                }
                else
                {
                    alarmManager.SetExact(AlarmType.RtcWakeup, triggerAtMillis, pendingIntent);
                }

                System.Diagnostics.Debug.WriteLine($"✅ Günlük alarm kuruldu: {nextAlarm:yyyy-MM-dd HH:mm}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Alarm kurma hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Alarmı iptal eder
        /// </summary>
        public static void CancelAlarm(Context context)
        {
            try
            {
                var alarmManager = (AlarmManager?)context.GetSystemService(Context.AlarmService);
                var intent = new Intent(context, typeof(NotificationAlarmReceiver));
                var pendingIntent = PendingIntent.GetBroadcast(
                    context,
                    ALARM_REQUEST_CODE,
                    intent,
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

                if (pendingIntent != null)
                {
                    alarmManager?.Cancel(pendingIntent);
                    System.Diagnostics.Debug.WriteLine("🗑️ Günlük alarm iptal edildi");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Alarm iptal hatası: {ex.Message}");
            }
        }
    }
}

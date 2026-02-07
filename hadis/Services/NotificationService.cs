using Plugin.LocalNotification;
using hadis.Models;
using hadis.Helpers;

namespace hadis.Services
{
    public class NotificationService : IAppNotificationService
    {
        private const int ID_IMSAK = 1001;
        private const int ID_GUNES = 1002;
        private const int ID_OGLE = 1003;
        private const int ID_IKINDI = 1004;
        private const int ID_AKSAM = 1005;
        private const int ID_YATSI = 1006;
        private const int ID_PERSISTENT = 9999;

        private static Dictionary<string, DateTime>? _cachedPrayerTimes;

        public async Task InitializeAsync()
        {
            try
            {
#if ANDROID || IOS
                if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
                {
                    await LocalNotificationCenter.Current.RequestNotificationPermission();
                }
                Console.WriteLine("âœ… Bildirim izinleri kontrol edildi.");
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"âš ï¸ Notification Initialize HatasÄ±: {ex.Message}");
            }
        }

        public async Task ScheduleNotificationsAsync(Dictionary<string, DateTime> prayerTimes)
        {
            Console.WriteLine("ğŸ“¢ ScheduleNotificationsAsync Ã§aÄŸrÄ±ldÄ±");
            
            // Vakitleri cache'le
            _cachedPrayerTimes = prayerTimes;
            
            if (!Preferences.Default.Get("NotificationsEnabled", true))
            {
                Console.WriteLine("âš ï¸ Bildirimler kapalÄ± (NotificationsEnabled = false)");
                CancelAllNotifications();
                return;
            }

            try
            {
#if ANDROID || IOS
                // Ensure permissions
                if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
                {
                    Console.WriteLine("âš ï¸ Bildirim izni yok, izin isteniyor...");
                    await LocalNotificationCenter.Current.RequestNotificationPermission();
                }
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"âš ï¸ Notification Permission HatasÄ±: {ex.Message}");
            }

            int scheduledCount = 0;
            int skippedCount = 0;

            foreach (var prayer in prayerTimes)
            {
                string key = prayer.Key;
                DateTime time = prayer.Value;
                
                Console.WriteLine($"ğŸ•Œ {key} vakti: {time:HH:mm}");
                
                int notificationId = GetNotificationId(key);
                if (notificationId == 0)
                {
                    Console.WriteLine($"âš ï¸ {key} iÃ§in ID bulunamadÄ±, atlanÄ±yor");
                    continue;
                }

                string canonicalKey = GetCanonicalKey(notificationId);
                string prefKey = $"Notification_{canonicalKey}";
                string offsetKey = $"NotificationOffset_{canonicalKey}";

                bool isEnabled = Preferences.Default.Get(prefKey, true);
                Console.WriteLine($"   ğŸ“Œ {canonicalKey} bildirimi: {(isEnabled ? "AÃ‡IK" : "KAPALI")}\n");
                
                if (!isEnabled)
                {
                    LocalNotificationCenter.Current.Cancel(notificationId);
                    skippedCount++;
                    continue;
                }

                int offsetMinutes = Preferences.Default.Get(offsetKey, 0);
                DateTime notifyTime = time.AddMinutes(-offsetMinutes);
                
                Console.WriteLine($"   â° Offset: {offsetMinutes} dk, Bildirim zamanÄ±: {notifyTime:HH:mm:ss}");

                if (notifyTime < DateTime.Now)
                {
                    Console.WriteLine($"   â­ï¸ Zaman geÃ§miÅŸ, atlanÄ±yor (Åimdi: {DateTime.Now:HH:mm:ss})");
                    skippedCount++;
                    continue; 
                }

                string description;
                if (offsetMinutes > 0)
                {
                    description = $"{key} vaktine {offsetMinutes} dakika kaldÄ±.";
                }
                else if (offsetMinutes < 0)
                {
                    description = $"{key} vaktinden {Math.Abs(offsetMinutes)} dakika geÃ§ti.";
                }
                else
                {
                    description = $"{key} vakti girdi.";
                }

                var request = new NotificationRequest
                {
                    NotificationId = notificationId,
                    Title = "Namaz Vakti",
                    Description = description,
                    ReturningData = key,
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = notifyTime,
                        RepeatType = NotificationRepeat.No
                    },
#if ANDROID
                    Android = new Plugin.LocalNotification.AndroidOption.AndroidOptions
                    {
                        ChannelId = "prayer_times_channel",
                        Priority = Plugin.LocalNotification.AndroidOption.AndroidPriority.High,
                        AutoCancel = true
                    }
#endif
                };

                try
                {
                    await LocalNotificationCenter.Current.Show(request);
                    scheduledCount++;
                    Console.WriteLine($"   âœ… Bildirim zamanlandÄ±: ID={notificationId}, Zaman={notifyTime:HH:mm:ss}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   âŒ Notification Show HatasÄ± ({key}): {ex.Message}");
                }
            }

            Console.WriteLine($"ğŸ“Š Toplam: {scheduledCount} bildirim zamanlandÄ±, {skippedCount} atlandÄ±");
            
            // Persistent notification'Ä± gÃ¼ncelle
            if (Preferences.Default.Get("PersistentNotificationEnabled", false))
            {
                await UpdatePersistentNotification(prayerTimes);
            }
        }

        private async Task UpdatePersistentNotification(Dictionary<string, DateTime> prayerTimes)
        {
            try
            {
                var now = DateTime.Now;
                string nextPrayerName = "";
                TimeSpan timeRemaining = TimeSpan.Zero;

                // Bir sonraki namazÄ± bul
                if (prayerTimes["Ä°msak"] > now)
                {
                    nextPrayerName = "Ä°msak";
                    timeRemaining = prayerTimes["Ä°msak"] - now;
                }
                else if (prayerTimes["gunes"] > now)
                {
                    nextPrayerName = "GÃ¼neÅŸ";
                    timeRemaining = prayerTimes["gunes"] - now;
                }
                else if (prayerTimes["Ogle"] > now)
                {
                    nextPrayerName = "Ã–ÄŸle";
                    timeRemaining = prayerTimes["Ogle"] - now;
                }
                else if (prayerTimes["Ä°kindi"] > now)
                {
                    nextPrayerName = "Ä°kindi";
                    timeRemaining = prayerTimes["Ä°kindi"] - now;
                }
                else if (prayerTimes["Aksam"] > now)
                {
                    nextPrayerName = "AkÅŸam";
                    timeRemaining = prayerTimes["Aksam"] - now;
                }
                else if (prayerTimes["Yatsi"] > now)
                {
                    nextPrayerName = "YatsÄ±";
                    timeRemaining = prayerTimes["Yatsi"] - now;
                }
                else
                {
                    nextPrayerName = "Ä°msak";
                    timeRemaining = prayerTimes["Ä°msak"].AddDays(1) - now;
                }

                string title = "Namaz Vakitleri";
                string message = $"{nextPrayerName}: {timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2} | " +
                                $"Ä°msak {prayerTimes["Ä°msak"]:HH:mm} | " +
                                $"GÃ¼neÅŸ {prayerTimes["gunes"]:HH:mm} | " +
                                $"Ã–ÄŸle {prayerTimes["Ogle"]:HH:mm} | " +
                                $"Ä°kindi {prayerTimes["Ä°kindi"]:HH:mm} | " +
                                $"AkÅŸam {prayerTimes["Aksam"]:HH:mm} | " +
                                $"YatsÄ± {prayerTimes["Yatsi"]:HH:mm}";

                await ShowPersistentNotificationAsync(title, message);
                Console.WriteLine($"ğŸ“Œ SÃ¼rekli bildirim gÃ¼ncellendi: {nextPrayerName} vaktine {timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"âš ï¸ Persistent notification gÃ¼ncelleme hatasÄ±: {ex.Message}");
            }
        }

        public void CancelAllNotifications()
        {
            LocalNotificationCenter.Current.CancelAll();
            Console.WriteLine("ğŸ—‘ï¸ TÃ¼m bildirimler iptal edildi");
        }

        public async Task RescheduleAllAsync()
        {
             if (!Preferences.Default.Get("NotificationsEnabled", true))
            {
                CancelAllNotifications();
            }
        }

        public async Task ShowPersistentNotificationAsync(string title, string message)
        {
            try
            {
#if ANDROID
                var context = global::Android.App.Application.Context;
                var intent = new global::Android.Content.Intent(context, typeof(Platforms.Android.Services.PersistentNotificationService));
                intent.PutExtra("title", title);
                intent.PutExtra("message", message);
                
                if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
                {
                    context.StartForegroundService(intent);
                }
                else
                {
                    context.StartService(intent);
                }
                
                Console.WriteLine($"ğŸ“Œ Foreground service baÅŸlatÄ±ldÄ±: {title}");
#elif IOS
                if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
                {
                    await LocalNotificationCenter.Current.RequestNotificationPermission();
                }

                var request = new NotificationRequest
                {
                    NotificationId = ID_PERSISTENT,
                    Title = title,
                    Description = message,
                    Sound = null,
                };

                await LocalNotificationCenter.Current.Show(request);
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"âš ï¸ Persistent Notification HatasÄ±: {ex.Message}");
            }
        }

        public void CancelPersistentNotification()
        {
#if ANDROID
            try
            {
                var context = global::Android.App.Application.Context;
                var intent = new global::Android.Content.Intent(context, typeof(Platforms.Android.Services.PersistentNotificationService));
                intent.SetAction("STOP_SERVICE");
                context.StartService(intent);
                Console.WriteLine("ğŸ—‘ï¸ Foreground service durduruldu");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"âš ï¸ Service durdurma hatasÄ±: {ex.Message}");
            }
#else
            LocalNotificationCenter.Current.Cancel(ID_PERSISTENT);
            Console.WriteLine("ğŸ—‘ï¸ SÃ¼rekli bildirim iptal edildi");
#endif
        }

        private int GetNotificationId(string prayerName)
        {
            var lower = prayerName.ToLower();
            if (lower.Contains("imsak") || lower.Contains("Ä°msak")) return ID_IMSAK;
            if (lower.Contains("gunes") || lower.Contains("gÃ¼neÅŸ")) return ID_GUNES;
            if (lower.Contains("ogle") || lower.Contains("Ã¶ÄŸle")) return ID_OGLE;
            if (lower.Contains("ikindi") || lower.Contains("Ä°kindi")) return ID_IKINDI;
            if (lower.Contains("aksam") || lower.Contains("akÅŸam")) return ID_AKSAM;
            if (lower.Contains("yatsi") || lower.Contains("yatsÄ±")) return ID_YATSI;
            
            return 0;
        }

        private string GetCanonicalKey(int notificationId)
        {
            switch (notificationId)
            {
                case ID_IMSAK: return "Imsak";
                case ID_GUNES: return "Gunes";
                case ID_OGLE: return "Ogle";
                case ID_IKINDI: return "Ikindi";
                case ID_AKSAM: return "Aksam";
                case ID_YATSI: return "Yatsi";
                default: return "";
            }
        }
    }
}


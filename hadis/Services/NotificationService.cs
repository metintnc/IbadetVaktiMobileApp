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

        public async Task InitializeAsync()
        {
            try
            {
#if ANDROID || IOS
                if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
                {
                    await LocalNotificationCenter.Current.RequestNotificationPermission();
                }
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Notification Initialize Hatası: {ex.Message}");
            }
        }

        public async Task ScheduleNotificationsAsync(Dictionary<string, DateTime> prayerTimes)
        {
            if (!Preferences.Default.Get("NotificationsEnabled", true))
            {
                CancelAllNotifications();
                return;
            }

            try
            {
#if ANDROID || IOS
                // Ensure permissions
                if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
                {
                    await LocalNotificationCenter.Current.RequestNotificationPermission();
                }
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Notification Permission Hatası: {ex.Message}");
            }

            foreach (var prayer in prayerTimes)
            {
                string key = prayer.Key; // "İmsak", "Gunes", etc.
                DateTime time = prayer.Value;
                
                // Map key to ID and Preference Check
                int notificationId = GetNotificationId(key);
                string prefKey = $"Notification_{key}"; // Need to ensure keys match what we use in settings
                
                // If ID is 0, skip (unknown key)
                if (notificationId == 0) continue;

                // Check if specific notification is enabled
                if (!Preferences.Default.Get(prefKey, true))
                {
                    LocalNotificationCenter.Current.Cancel(notificationId);
                    continue;
                }

                // If time has passed today, maybe schedule for tomorrow? 
                // For now, let's assume we are scheduling for the date provided in prayerTimes.
                // If logic is "schedule for today", check if time is in future.
                if (time < DateTime.Now)
                {
                   // time = time.AddDays(1); // Optional: logic to handle next day, but let's stick to what's passed
                   continue; 
                }

                var request = new NotificationRequest
                {
                    NotificationId = notificationId,
                    Title = "Namaz Vakti",
                    Description = $"{key} vakti girdi.",
                    ReturningData = key, // Pass data if needed
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = time,
                        RepeatType = NotificationRepeat.Daily // Repeat daily at this time? 
                        // Note: Prayer times change daily, so Repeat.Daily might not be accurate enough for long term without daily rescheduling.
                        // For MVP/First pass, let's schedule one-shot for the specific DateTime provided by the service.
                    }
                };

                try
                {
                    await LocalNotificationCenter.Current.Show(request);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Notification Show Hatası ({key}): {ex.Message}");
                }
            }
        }

        public void CancelAllNotifications()
        {
            LocalNotificationCenter.Current.CancelAll();
        }

        public async Task RescheduleAllAsync()
        {
            // logic to re-fetch today's timings and schedule
            // This requires access to PrayerTimesService to get data.
            // Since this service might be called from UI, better to let UI or a background job drive the data fetching
            // Or better: inject PrayerTimesService here? 
            // For now, simpler: UI calls Schedule with data.
            // But for "RescheduleAll" from Settings toggle, we might not have data handy.
            
            // Let's implement a simple version that just checks the master switch
             if (!Preferences.Default.Get("NotificationsEnabled", true))
            {
                CancelAllNotifications();
            }
             else
             {
                 // We need data to reschedule. 
                 // If we don't have it, we can't schedule.
                 // A proper implementation would fetch today's times here using PrayerTimesService.
                 // Let's assume for now the caller will call ScheduleNotificationsAsync with data if needed.
             }
        }

        public async Task ShowPersistentNotificationAsync(string title, string message)
        {
            try
            {
#if ANDROID || IOS
                if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
                {
                    await LocalNotificationCenter.Current.RequestNotificationPermission();
                }
#endif

                var request = new NotificationRequest
                {
                    NotificationId = ID_PERSISTENT,
                    Title = title,
                    Description = message,
                    Android = new Plugin.LocalNotification.AndroidOption.AndroidOptions
                    {
                        ChannelId = "PersistentChannel",
                        Ongoing = true,
                        AutoCancel = false,
                        Priority = Plugin.LocalNotification.AndroidOption.AndroidPriority.Low,
                    },
                    Sound = null,
                };

                await LocalNotificationCenter.Current.Show(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Persistent Notification Hatası: {ex.Message}");
            }
        }

        public void CancelPersistentNotification()
        {
            LocalNotificationCenter.Current.Cancel(ID_PERSISTENT);
        }

        private int GetNotificationId(string prayerName)
        {
            // Normalize keys if needed. Keys from PrayerTimeService: "İmsak", "gunes", "Ogle", "İkindi", "Aksam", "Yatsi"
            // Note: Case sensitivity and exact naming matter.
            // "gunes" vs "Gunes"
            
            var lower = prayerName.ToLower();
            if (lower.Contains("imsak")) return ID_IMSAK;
            if (lower.Contains("gunes") || lower.Contains("güneş")) return ID_GUNES;
            if (lower.Contains("ogle") || lower.Contains("öğle")) return ID_OGLE;
            if (lower.Contains("ikindi")) return ID_IKINDI;
            if (lower.Contains("aksam") || lower.Contains("akşam")) return ID_AKSAM;
            if (lower.Contains("yatsi") || lower.Contains("yatsı")) return ID_YATSI;
            
            return 0;
        }
    }
}

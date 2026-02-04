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
                Console.WriteLine("✅ Bildirim izinleri kontrol edildi.");
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Notification Initialize Hatası: {ex.Message}");
            }
        }

        public async Task ScheduleNotificationsAsync(Dictionary<string, DateTime> prayerTimes)
        {
            Console.WriteLine("📢 ScheduleNotificationsAsync çağrıldı");
            
            if (!Preferences.Default.Get("NotificationsEnabled", true))
            {
                Console.WriteLine("⚠️ Bildirimler kapalı (NotificationsEnabled = false)");
                CancelAllNotifications();
                return;
            }

            try
            {
#if ANDROID || IOS
                // Ensure permissions
                if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
                {
                    Console.WriteLine("⚠️ Bildirim izni yok, izin isteniyor...");
                    await LocalNotificationCenter.Current.RequestNotificationPermission();
                }
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Notification Permission Hatası: {ex.Message}");
            }

            int scheduledCount = 0;
            int skippedCount = 0;

            foreach (var prayer in prayerTimes)
            {
                string key = prayer.Key; // "İmsak", "Gunes", etc.
                DateTime time = prayer.Value;
                
                Console.WriteLine($"🕌 {key} vakti: {time:HH:mm}");
                
                // Map key to ID and Preference Check
                int notificationId = GetNotificationId(key);
                if (notificationId == 0)
                {
                    Console.WriteLine($"⚠️ {key} için ID bulunamadı, atlanıyor");
                    continue;
                }

                // Determine canonical key for Preferences (matches Settings keys)
                string canonicalKey = GetCanonicalKey(notificationId);
                string prefKey = $"Notification_{canonicalKey}";
                string offsetKey = $"NotificationOffset_{canonicalKey}";

                // Check if specific notification is enabled
                bool isEnabled = Preferences.Default.Get(prefKey, true);
                Console.WriteLine($"   📌 {canonicalKey} bildirimi: {(isEnabled ? "AÇIK" : "KAPALI")}\n");
                
                if (!isEnabled)
                {
                    LocalNotificationCenter.Current.Cancel(notificationId);
                    skippedCount++;
                    continue;
                }

                // Apply Offset (negatif olarak uygula - vakitten önce bildirim için)
                int offsetMinutes = Preferences.Default.Get(offsetKey, 0);
                DateTime notifyTime = time.AddMinutes(-offsetMinutes);
                
                Console.WriteLine($"   ⏰ Offset: {offsetMinutes} dk, Bildirim zamanı: {notifyTime:HH:mm:ss}");

                // If time has passed today, skip
                if (notifyTime < DateTime.Now)
                {
                    Console.WriteLine($"   ⏭️ Zaman geçmiş, atlanıyor (Şimdi: {DateTime.Now:HH:mm:ss})");
                    skippedCount++;
                    continue; 
                }

                // Bildirim mesajını oluştur
                string description;
                if (offsetMinutes > 0)
                {
                    description = $"{key} vaktine {offsetMinutes} dakika kaldı.";
                }
                else if (offsetMinutes < 0)
                {
                    description = $"{key} vaktinden {Math.Abs(offsetMinutes)} dakika geçti.";
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
                    Console.WriteLine($"   ✅ Bildirim zamanlandı: ID={notificationId}, Zaman={notifyTime:HH:mm:ss}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ Notification Show Hatası ({key}): {ex.Message}");
                }
            }

            Console.WriteLine($"📊 Toplam: {scheduledCount} bildirim zamanlandı, {skippedCount} atlandı");
        }

        public void CancelAllNotifications()
        {
            LocalNotificationCenter.Current.CancelAll();
            Console.WriteLine("🗑️ Tüm bildirimler iptal edildi");
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
                        ChannelId = "persistent_channel",
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
            var lower = prayerName.ToLower();
            if (lower.Contains("imsak") || lower.Contains("İmsak")) return ID_IMSAK;
            if (lower.Contains("gunes") || lower.Contains("güneş")) return ID_GUNES;
            if (lower.Contains("ogle") || lower.Contains("öğle")) return ID_OGLE;
            if (lower.Contains("ikindi") || lower.Contains("İkindi")) return ID_IKINDI;
            if (lower.Contains("aksam") || lower.Contains("akşam")) return ID_AKSAM;
            if (lower.Contains("yatsi") || lower.Contains("yatsı")) return ID_YATSI;
            
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

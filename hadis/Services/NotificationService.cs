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
        private readonly PrayerTimesService _prayerTimesService;

        public NotificationService(PrayerTimesService prayerTimesService)
        {
            _prayerTimesService = prayerTimesService;
        }

        public async Task InitializeAsync()
        {
            try
            {
#if ANDROID || IOS
                if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
                {
                    await LocalNotificationCenter.Current.RequestNotificationPermission();
                }
                System.Diagnostics.Debug.WriteLine("✅ Bildirim izinleri kontrol edildi.");
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Notification Initialize Hatası: {ex.Message}");
            }
        }

        public async Task ScheduleNotificationsAsync(Dictionary<string, DateTime> prayerTimes, int dayOffset = 0)
        {
            System.Diagnostics.Debug.WriteLine($"📢 ScheduleNotificationsAsync çağrıldı (dayOffset={dayOffset})");
            
            // Vakitleri cache'le
            _cachedPrayerTimes = prayerTimes;
            
            if (!Preferences.Default.Get("NotificationsEnabled", true))
            {
                System.Diagnostics.Debug.WriteLine("⚠️ Bildirimler kapalı (NotificationsEnabled = false)");
                CancelAllNotifications();
                return;
            }

            try
            {
#if ANDROID || IOS
                // Ensure permissions
                if (await LocalNotificationCenter.Current.AreNotificationsEnabled() == false)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Bildirim izni yok, izin isteniyor...");
                    await LocalNotificationCenter.Current.RequestNotificationPermission();
                }
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Notification Permission Hatası: {ex.Message}");
            }

            int scheduledCount = 0;
            int skippedCount = 0;

            foreach (var prayer in prayerTimes)
            {
                string key = prayer.Key;
                DateTime time = prayer.Value;
                
                System.Diagnostics.Debug.WriteLine($"🕌 {key} vakti: {time:HH:mm}");
                
                int baseNotifId = GetNotificationId(key);
                int notificationId = baseNotifId + (dayOffset * 100);
                if (baseNotifId == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ {key} için ID bulunamadı, atlanıyor");
                    continue;
                }

                string canonicalKey = GetCanonicalKey(baseNotifId);
                string prefKey = $"Notification_{canonicalKey}";
                string offsetKey = $"NotificationOffset_{canonicalKey}";

                bool isEnabled = Preferences.Default.Get(prefKey, true);
                System.Diagnostics.Debug.WriteLine($"   📌 {canonicalKey} bildirimi: {(isEnabled ? "AÇIK" : "KAPALI")}\n");
                
                if (!isEnabled)
                {
                    LocalNotificationCenter.Current.Cancel(notificationId);
                    skippedCount++;
                    continue;
                }

                int offsetMinutes = Preferences.Default.Get(offsetKey, 0);
                DateTime notifyTime = time.AddMinutes(-offsetMinutes);
                
                System.Diagnostics.Debug.WriteLine($"   ⏰ Offset: {offsetMinutes} dk, Bildirim zamanı: {notifyTime:HH:mm:ss}");

                if (notifyTime < DateTime.Now)
                {
                    System.Diagnostics.Debug.WriteLine($"   ⭐ Zaman geçmiş, atlanıyor (Şimdi: {DateTime.Now:HH:mm:ss})");
                    skippedCount++;
                    continue; 
                }

                string description;
                string displayName = GetTurkishDisplayName(key);
                if (offsetMinutes > 0)
                {
                    description = $"{displayName} vaktine {offsetMinutes} dakika kaldı.";
                }
                else if (offsetMinutes < 0)
                {
                    description = $"{displayName} vaktinden {Math.Abs(offsetMinutes)} dakika geçti.";
                }
                else
                {
                    int baseId = GetNotificationId(key);
                    if (baseId == ID_IMSAK)
                        description = "İmsak Vakti!";
                    else if (baseId == ID_GUNES)
                        description = "Güneş Doğdu!";
                    else
                        description = $"{displayName} Ezanı!";
                }

                var request = new NotificationRequest
                {
                    NotificationId = notificationId,
                    Title = "İbadet Vakti",
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
                    System.Diagnostics.Debug.WriteLine($"   ✅ Bildirim zamanlandı: ID={notificationId}, Zaman={notifyTime:HH:mm:ss}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"   ❌ Notification Show Hatası ({key}): {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"📊 Toplam: {scheduledCount} bildirim zamanlandı, {skippedCount} atlandı");
            
            // Persistent notification'ı güncelle
            if (Preferences.Default.Get("PersistentNotificationEnabled", false))
            {
                await UpdatePersistentNotification(prayerTimes);
            }
        }

        private async Task UpdatePersistentNotification(Dictionary<string, DateTime> prayerTimes)
        {
            try
            {
                var (title, message) = PrayerTimeHelper.BuildPersistentNotificationContent(prayerTimes);
                await ShowPersistentNotificationAsync(title, message);
                System.Diagnostics.Debug.WriteLine($"📌 Sürekli bildirim güncellendi: {title}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Persistent notification güncelleme hatası: {ex.Message}");
            }
        }

        public void CancelAllNotifications()
        {
            LocalNotificationCenter.Current.CancelAll();
            System.Diagnostics.Debug.WriteLine("🗑️ Tüm bildirimler iptal edildi");
        }

        public async Task RescheduleAllAsync()
        {
             if (!Preferences.Default.Get("NotificationsEnabled", true))
            {
                CancelAllNotifications();
                return;
            }
            await ScheduleMultiDayNotificationsAsync(7);
        }

        public async Task ScheduleMultiDayNotificationsAsync(int days = 3)
        {
            System.Diagnostics.Debug.WriteLine($"📢 ScheduleMultiDayNotificationsAsync çağrıldı ({days} gün)");

            if (!Preferences.Default.Get("NotificationsEnabled", true))
            {
                CancelAllNotifications();
                return;
            }

            try
            {
                // Konum bilgilerini Preferences'tan al
                string sehir = Preferences.Default.Get("ManuelSehir", "");
                string ilce = Preferences.Default.Get("ManuelIlce", "");
                double lat = Preferences.Default.Get("ManuelLatitude", 0.0);
                double lon = Preferences.Default.Get("ManuelLongitude", 0.0);

                if (string.IsNullOrEmpty(sehir) && lat == 0.0 && lon == 0.0)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Konum bilgisi bulunamadı, çoklu gün zamanlama atlanıyor");
                    return;
                }

                for (int dayOffset = 0; dayOffset < days; dayOffset++)
                {
                    DateTime targetDate = DateTime.Now.Date.AddDays(dayOffset);
                    try
                    {
                        var vakitler = await _prayerTimesService.GetPrayerTimesForDateAsync(
                            targetDate, ilce, sehir, lat, lon);

                        if (vakitler != null)
                        {
                            await ScheduleNotificationsAsync(vakitler, dayOffset);
                            System.Diagnostics.Debug.WriteLine($"✅ {targetDate:yyyy-MM-dd} bildirimleri zamanlandı (offset={dayOffset})");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ {targetDate:yyyy-MM-dd} vakitleri alınamadı");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ {targetDate:yyyy-MM-dd} zamanlama hatası: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ScheduleMultiDayNotificationsAsync hatası: {ex.Message}");
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
                
                System.Diagnostics.Debug.WriteLine($"📌 Foreground service başlatıldı: {title}");
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
                System.Diagnostics.Debug.WriteLine($"⚠️ Persistent Notification Hatası: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine("🗑️ Foreground service durduruldu");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Service durdurma hatası: {ex.Message}");
            }
#else
            LocalNotificationCenter.Current.Cancel(ID_PERSISTENT);
            System.Diagnostics.Debug.WriteLine("🗑️ Sürekli bildirim iptal edildi");
#endif
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

        private string GetTurkishDisplayName(string key)
        {
            var lower = key.ToLower(new System.Globalization.CultureInfo("tr-TR"));
            if (lower.Contains("imsak") || lower.Contains("İmsak".ToLower(new System.Globalization.CultureInfo("tr-TR")))) return "İmsak";
            if (lower.Contains("gunes") || lower.Contains("güneş")) return "Güneş";
            if (lower.Contains("ogle") || lower.Contains("öğle")) return "Öğle";
            if (lower.Contains("ikindi") || lower.Contains("İkindi".ToLower(new System.Globalization.CultureInfo("tr-TR")))) return "İkindi";
            if (lower.Contains("aksam") || lower.Contains("akşam")) return "Akşam";
            if (lower.Contains("yatsi") || lower.Contains("yatsı")) return "Yatsı";
            return key;
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


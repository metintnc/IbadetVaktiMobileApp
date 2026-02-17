using System.Timers;

namespace hadis.Services
{
    public class PersistentNotificationUpdater
    {
        private static System.Timers.Timer? _updateTimer;
        private static IAppNotificationService? _notificationService;
        private static Dictionary<string, DateTime>? _prayerTimes;
        private static readonly object _lock = new object();

        public static void StartUpdating(IAppNotificationService notificationService, Dictionary<string, DateTime> prayerTimes)
        {
            lock (_lock)
            {
                // Önceki timer'ı temizle
                StopUpdating();

                _notificationService = notificationService;
                _prayerTimes = prayerTimes;

                // Her dakika güncelle
                _updateTimer = new System.Timers.Timer(60000); // 60 saniye
                _updateTimer.Elapsed += OnTimerElapsed;
                _updateTimer.AutoReset = true;
                _updateTimer.Start();

                System.Diagnostics.Debug.WriteLine("📢 Sürekli bildirim güncelleyici başlatıldı (60 saniye aralıkla)");
            }
        }

        public static void StopUpdating()
        {
            lock (_lock)
            {
                if (_updateTimer != null)
                {
                    _updateTimer.Stop();
                    _updateTimer.Elapsed -= OnTimerElapsed;
                    _updateTimer.Dispose();
                    _updateTimer = null;
                    System.Diagnostics.Debug.WriteLine("🛑 Sürekli bildirim güncelleyici durduruldu");
                }
            }
        }

        public static void UpdatePrayerTimes(Dictionary<string, DateTime> prayerTimes)
        {
            lock (_lock)
            {
                _prayerTimes = prayerTimes;
                System.Diagnostics.Debug.WriteLine("🔄 Sürekli bildirim için vakitler güncellendi");
            }
        }

        private static async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            IAppNotificationService? service;
            Dictionary<string, DateTime>? times;

            // Thread-safe copy
            lock (_lock)
            {
                service = _notificationService;
                times = _prayerTimes;
            }

            if (service == null || times == null)
                return;

            if (!Preferences.Default.Get("PersistentNotificationEnabled", false))
                return;

            try
            {
                var (title, message) = hadis.Helpers.PrayerTimeHelper.BuildPersistentNotificationContent(times);

                await service.ShowPersistentNotificationAsync(title, message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Sürekli bildirim güncelleme hatası: {ex.Message}");
            }
        }
    }
}


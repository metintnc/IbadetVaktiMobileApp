using System.Timers;

namespace hadis.Services
{
    public class PersistentNotificationUpdater
    {
        private static System.Timers.Timer? _updateTimer;
        private static IAppNotificationService? _notificationService;
        private static Dictionary<string, DateTime>? _prayerTimes;

        public static void StartUpdating(IAppNotificationService notificationService, Dictionary<string, DateTime> prayerTimes)
        {
            _notificationService = notificationService;
            _prayerTimes = prayerTimes;

            // Her dakika güncelle
            _updateTimer = new System.Timers.Timer(60000); // 60 saniye
            _updateTimer.Elapsed += OnTimerElapsed;
            _updateTimer.AutoReset = true;
            _updateTimer.Start();

            Console.WriteLine("?? Sürekli bildirim güncelleyici baþlatýldý (60 saniye aralýkla)");
        }

        public static void StopUpdating()
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            _updateTimer = null;
            Console.WriteLine("?? Sürekli bildirim güncelleyici durduruldu");
        }

        public static void UpdatePrayerTimes(Dictionary<string, DateTime> prayerTimes)
        {
            _prayerTimes = prayerTimes;
            Console.WriteLine("?? Sürekli bildirim için vakitler güncellendi");
        }

        private static async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_notificationService == null || _prayerTimes == null)
                return;

            if (!Preferences.Default.Get("PersistentNotificationEnabled", false))
                return;

            try
            {
                var now = DateTime.Now;
                string nextPrayerName = "";
                TimeSpan timeRemaining = TimeSpan.Zero;

                // Bir sonraki namazý bul
                if (_prayerTimes["Ýmsak"] > now)
                {
                    nextPrayerName = "Ýmsak";
                    timeRemaining = _prayerTimes["Ýmsak"] - now;
                }
                else if (_prayerTimes["gunes"] > now)
                {
                    nextPrayerName = "Güneþ";
                    timeRemaining = _prayerTimes["gunes"] - now;
                }
                else if (_prayerTimes["Ogle"] > now)
                {
                    nextPrayerName = "Öðle";
                    timeRemaining = _prayerTimes["Ogle"] - now;
                }
                else if (_prayerTimes["Ýkindi"] > now)
                {
                    nextPrayerName = "Ýkindi";
                    timeRemaining = _prayerTimes["Ýkindi"] - now;
                }
                else if (_prayerTimes["Aksam"] > now)
                {
                    nextPrayerName = "Akþam";
                    timeRemaining = _prayerTimes["Aksam"] - now;
                }
                else if (_prayerTimes["Yatsi"] > now)
                {
                    nextPrayerName = "Yatsý";
                    timeRemaining = _prayerTimes["Yatsi"] - now;
                }
                else
                {
                    nextPrayerName = "Ýmsak";
                    timeRemaining = _prayerTimes["Ýmsak"].AddDays(1) - now;
                }

                string title = "Namaz Vakitleri";
                string message = $"{nextPrayerName}: {timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2} | " +
                                $"Ýmsak {_prayerTimes["Ýmsak"]:HH:mm} | " +
                                $"Güneþ {_prayerTimes["gunes"]:HH:mm} | " +
                                $"Öðle {_prayerTimes["Ogle"]:HH:mm} | " +
                                $"Ýkindi {_prayerTimes["Ýkindi"]:HH:mm} | " +
                                $"Akþam {_prayerTimes["Aksam"]:HH:mm} | " +
                                $"Yatsý {_prayerTimes["Yatsi"]:HH:mm}";

                await _notificationService.ShowPersistentNotificationAsync(title, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? Sürekli bildirim güncelleme hatasý: {ex.Message}");
            }
        }
    }
}

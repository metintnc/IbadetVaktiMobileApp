using hadis.Models;
using hadis.Services;
using System.Collections.ObjectModel;

namespace hadis
{
    public partial class BildirimAyarlari : ContentPage
    {
        private readonly hadis.Services.IAppNotificationService _notificationService;
        private bool _isInitialized = false;

        public BildirimAyarlari(hadis.Services.IAppNotificationService notificationService)
        {
            InitializeComponent();
            _notificationService = notificationService;
        }
        
        // Parameterless constructor for XAML preview if needed, though strictly dependency injection is preferred
        public BildirimAyarlari() : this(new hadis.Services.NotificationService())
        {
            // Fallback for previewer or manual instantiation if DI fails
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await InitializeSettings();
        }

        private async Task InitializeSettings()
        {
            _isInitialized = false;
            
            // Check permissions
            await _notificationService.InitializeAsync();

            // Load Preferences
            MasterSwitch.IsToggled = Preferences.Default.Get("NotificationsEnabled", true);
            PersistentSwitch.IsToggled = Preferences.Default.Get("PersistentNotificationEnabled", false);
            
            SwitchImsak.IsToggled = Preferences.Default.Get("Notification_Imsak", true);
            SwitchGunes.IsToggled = Preferences.Default.Get("Notification_Gunes", true);
            SwitchOgle.IsToggled = Preferences.Default.Get("Notification_Ogle", true);
            SwitchIkindi.IsToggled = Preferences.Default.Get("Notification_Ikindi", true);
            SwitchAksam.IsToggled = Preferences.Default.Get("Notification_Aksam", true);
            SwitchYatsi.IsToggled = Preferences.Default.Get("Notification_Yatsi", true);


            UpdateOffsetLabels();
            UpdateUIState();
            _isInitialized = true;
        }

        private string FormatOffset(int offset)
        {
            if (offset == 0) return "0 dk";
            return offset > 0 ? $"{offset} dk önce" : $"{Math.Abs(offset)} dk sonra";
        }

        private void UpdateOffsetLabels()
        {
            LabelOffsetImsak.Text = FormatOffset(Preferences.Default.Get("NotificationOffset_Imsak", 0));
            LabelOffsetGunes.Text = FormatOffset(Preferences.Default.Get("NotificationOffset_Gunes", 0));
            LabelOffsetOgle.Text = FormatOffset(Preferences.Default.Get("NotificationOffset_Ogle", 0));
            LabelOffsetIkindi.Text = FormatOffset(Preferences.Default.Get("NotificationOffset_Ikindi", 0));
            LabelOffsetAksam.Text = FormatOffset(Preferences.Default.Get("NotificationOffset_Aksam", 0));
            LabelOffsetYatsi.Text = FormatOffset(Preferences.Default.Get("NotificationOffset_Yatsi", 0));
        }

        private async void OffsetMinus_Clicked(object sender, EventArgs e)
        {
            if (!_isInitialized) return;
            if (sender is Button btn && btn.CommandParameter is string keyPart)
            {
                string key = $"NotificationOffset_{keyPart}";
                int currentOffset = Preferences.Default.Get(key, 0);
                currentOffset--;
                Preferences.Default.Set(key, currentOffset);
                UpdateOffsetLabels();
                await RescheduleNotificationsAsync();
            }
        }

        private async void OffsetPlus_Clicked(object sender, EventArgs e)
        {
            if (!_isInitialized) return;
            if (sender is Button btn && btn.CommandParameter is string keyPart)
            {
                string key = $"NotificationOffset_{keyPart}";
                int currentOffset = Preferences.Default.Get(key, 0);
                currentOffset++;
                Preferences.Default.Set(key, currentOffset);
                UpdateOffsetLabels();
                await RescheduleNotificationsAsync();
            }
        }

        private async Task RescheduleNotificationsAsync()
        {
            try
            {
                // Konum bilgilerini al
                bool otomatikKonum = Preferences.Default.Get("OtomatikKonum", true);
                string sehir = Preferences.Default.Get("ManuelSehir", "");
                string ilce = Preferences.Default.Get("ManuelIlce", "");

                if (string.IsNullOrEmpty(sehir) || string.IsNullOrEmpty(ilce))
                {
                    Console.WriteLine("⚠️ Bildirim yeniden zamanlanamadı: Konum bilgisi yok");
                    return;
                }

                // Bugünün vakitlerini al
                var vakitler = await PrayerTimesService.GetPrayerTimesForDateAsync(DateTime.Now, ilce, sehir);
                
                if (vakitler != null)
                {
                    await _notificationService.ScheduleNotificationsAsync(vakitler);
                    Console.WriteLine("✅ Bildirimler yeniden zamanlandı");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Bildirim yeniden zamanlama hatası: {ex.Message}");
            }
        }

        private void UpdateUIState()
        {
            // Dim the settings if master switch is off
            SettingsStack.Opacity = MasterSwitch.IsToggled ? 1.0 : 0.5;
            SettingsStack.IsEnabled = MasterSwitch.IsToggled;
        }

        private async void MasterSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (!_isInitialized) return;

            bool isEnabled = e.Value;
            Preferences.Default.Set("NotificationsEnabled", isEnabled);
            UpdateUIState();

            if (!isEnabled)
            {
                _notificationService.CancelAllNotifications();
            }
            else
            {
                await RescheduleNotificationsAsync();
            }
        }

        private async void PersistentSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (!_isInitialized) return;

            bool isEnabled = e.Value;
            Preferences.Default.Set("PersistentNotificationEnabled", isEnabled);

            if (isEnabled)
            {
                // Vakitleri al ve gerçek bilgilerle göster
                try
                {
                    string sehir = Preferences.Default.Get("ManuelSehir", "");
                    string ilce = Preferences.Default.Get("ManuelIlce", "");

                    if (!string.IsNullOrEmpty(sehir) && !string.IsNullOrEmpty(ilce))
                    {
                        var vakitler = await PrayerTimesService.GetPrayerTimesForDateAsync(DateTime.Now, ilce, sehir);
                        
                        if (vakitler != null)
                        {
                            var now = DateTime.Now;
                            string nextPrayerName = "";
                            TimeSpan timeRemaining = TimeSpan.Zero;

                            // Bir sonraki namazı bul
                            if (vakitler["İmsak"] > now)
                            {
                                nextPrayerName = "İmsak";
                                timeRemaining = vakitler["İmsak"] - now;
                            }
                            else if (vakitler["gunes"] > now)
                            {
                                nextPrayerName = "Güneş";
                                timeRemaining = vakitler["gunes"] - now;
                            }
                            else if (vakitler["Ogle"] > now)
                            {
                                nextPrayerName = "Öğle";
                                timeRemaining = vakitler["Ogle"] - now;
                            }
                            else if (vakitler["İkindi"] > now)
                            {
                                nextPrayerName = "İkindi";
                                timeRemaining = vakitler["İkindi"] - now;
                            }
                            else if (vakitler["Aksam"] > now)
                            {
                                nextPrayerName = "Akşam";
                                timeRemaining = vakitler["Aksam"] - now;
                            }
                            else if (vakitler["Yatsi"] > now)
                            {
                                nextPrayerName = "Yatsı";
                                timeRemaining = vakitler["Yatsi"] - now;
                            }
                            else
                            {
                                nextPrayerName = "İmsak";
                                timeRemaining = vakitler["İmsak"].AddDays(1) - now;
                            }

                            string title = "Namaz Vakitleri";
                            string message = $"{nextPrayerName}: {timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2} | " +
                                            $"İmsak {vakitler["İmsak"]:HH:mm} | " +
                                            $"Güneş {vakitler["gunes"]:HH:mm} | " +
                                            $"Öğle {vakitler["Ogle"]:HH:mm} | " +
                                            $"İkindi {vakitler["İkindi"]:HH:mm} | " +
                                            $"Akşam {vakitler["Aksam"]:HH:mm} | " +
                                            $"Yatsı {vakitler["Yatsi"]:HH:mm}";

                            await _notificationService.ShowPersistentNotificationAsync(title, message);
                            
                            // Güncelleyiciyi başlat
                            Services.PersistentNotificationUpdater.StartUpdating(_notificationService, vakitler);
                        }
                        else
                        {
                            await _notificationService.ShowPersistentNotificationAsync("Namaz Vakti", "Vakitler yükleniyor...");
                        }
                    }
                    else
                    {
                        await _notificationService.ShowPersistentNotificationAsync("Namaz Vakti", "Konum seçiniz");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Persistent notification hatası: {ex.Message}");
                    await _notificationService.ShowPersistentNotificationAsync("Namaz Vakti", "Vakitler yükleniyor...");
                }
            }
            else
            {
                _notificationService.CancelPersistentNotification();
                // Güncelleyiciyi durdur
                Services.PersistentNotificationUpdater.StopUpdating();
            }
        }

        private async void Switch_Toggled(object sender, ToggledEventArgs e)
        {
            if (!_isInitialized) return;

            if (sender is Switch s)
            {
                string key = "";
                if (s == SwitchImsak) key = "Notification_Imsak";
                else if (s == SwitchGunes) key = "Notification_Gunes";
                else if (s == SwitchOgle) key = "Notification_Ogle";
                else if (s == SwitchIkindi) key = "Notification_Ikindi";
                else if (s == SwitchAksam) key = "Notification_Aksam";
                else if (s == SwitchYatsi) key = "Notification_Yatsi";

                if (!string.IsNullOrEmpty(key))
                {
                    Preferences.Default.Set(key, e.Value);
                    await RescheduleNotificationsAsync();
                }
            }
        }
    }
}

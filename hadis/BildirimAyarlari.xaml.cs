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

        private void UpdateOffsetLabels()
        {
            LabelOffsetImsak.Text = $"{Preferences.Default.Get("NotificationOffset_Imsak", 0)} dk";
            LabelOffsetGunes.Text = $"{Preferences.Default.Get("NotificationOffset_Gunes", 0)} dk";
            LabelOffsetOgle.Text = $"{Preferences.Default.Get("NotificationOffset_Ogle", 0)} dk";
            LabelOffsetIkindi.Text = $"{Preferences.Default.Get("NotificationOffset_Ikindi", 0)} dk";
            LabelOffsetAksam.Text = $"{Preferences.Default.Get("NotificationOffset_Aksam", 0)} dk";
            LabelOffsetYatsi.Text = $"{Preferences.Default.Get("NotificationOffset_Yatsi", 0)} dk";
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
                // Show notification immediately with generic text
                // Ideally this should fetch real prayer times.
                // For now, let's show a static message or "Yukleniyor..."
                await _notificationService.ShowPersistentNotificationAsync("Namaz Vakti", "Vakitler yükleniyor...");
            }
            else
            {
                _notificationService.CancelPersistentNotification();
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

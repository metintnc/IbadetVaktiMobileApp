using hadis.Models;
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

            UpdateUIState();
            _isInitialized = true;
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
                // Re-schedule based on existing preferences
                // For now, we rely on the periodic scheduler or app start. 
                // But we can trigger a refresh if we had data suitable for it.
                // Since NotificationService.RescheduleAllAsync logic is limited currently, let's just ensure state is saved.
                await _notificationService.RescheduleAllAsync();
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

        private void Switch_Toggled(object sender, ToggledEventArgs e)
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
                    
                    // We should also trigger a rescheduling or cancellation for this specific item
                    // But for simplicity in this pass, we rely on next schedule cycle or RescheduleAll
                    _notificationService.RescheduleAllAsync(); 
                }
            }
        }
    }
}

using hadis.Models;
using hadis.Helpers;
using System.Text.Json;

namespace hadis
{
    public partial class TemaAyarlari : ContentPage
    {
        private const string ThemePreferenceKey = AppConstants.PREF_APP_THEME;

        public TemaAyarlari()
        {
            InitializeComponent();
            LoadCurrentTheme();
            LoadCustomThemeInfo();
        }

        private void LoadCurrentTheme()
        {
            // Default to "MainDark" (Standard Dark)
            string savedTheme = Preferences.Default.Get(ThemePreferenceKey, "MainDark");
            UpdateThemeUI(savedTheme);

            // Set "Ana Tema" preview images based on current time
            try
            {
                var now = DateTime.Now;
                var bgInfo = TimeBasedBackgroundConfig.GetBackgroundForTime(now.Hour, now.Minute);
                
                if (AnaAcikImage != null) AnaAcikImage.Source = bgInfo.Image;
                if (AnaKoyuImage != null) AnaKoyuImage.Source = bgInfo.Image;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting AnaTemaImage: {ex.Message}");
            }
        }

        private void LoadCustomThemeInfo()
        {
            // Kayitli ozel tema var mi kontrol et
            string customThemeJson = Preferences.Default.Get(AppConstants.PREF_CUSTOM_THEME, string.Empty);
            bool hasCustomTheme = false;
            
            if (!string.IsNullOrEmpty(customThemeJson))
            {
                try
                {
                    var theme = JsonSerializer.Deserialize<CustomTheme>(customThemeJson);
                    if (theme != null)
                    {
                        hasCustomTheme = true;
                        
                        // Set preview image if it is an image file
                        if (!string.IsNullOrEmpty(theme.BackgroundImage) && 
                           (theme.BackgroundImage.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                            theme.BackgroundImage.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
                        {
                            if (OzelTemaImage != null)
                            {
                                OzelTemaImage.Source = theme.BackgroundImage;
                            }
                        }
                    }
                }
                catch { }
            }

            // Ozel tema varsa belirtecini aktif et (Opacity 1.0)
            if (OzelFrame != null)
            {
                OzelFrame.Opacity = hasCustomTheme ? 1.0 : 0.5;
            }
        }

        private void OnThemeCardTapped(object sender, EventArgs e)
        {
            if (sender is View view && view.GestureRecognizers.Count > 0)
            {
                 var tapGesture = view.GestureRecognizers[0] as TapGestureRecognizer;
                 if (tapGesture != null && tapGesture.CommandParameter is string theme)
                 {
                     // Ozel tema secildiyse ve kayitli tema yoksa uyari ver
                     if (theme == "Custom")
                     {
                         string customThemeJson = Preferences.Default.Get(AppConstants.PREF_CUSTOM_THEME, string.Empty);
                         if (string.IsNullOrEmpty(customThemeJson))
                         {
                             DisplayAlert("Uyarı", "Önce özel bir tema oluşturmalısınız.", "Tamam");
                             return;
                         }
                     }

                     ApplyTheme(theme);
                 }
            }
        }

        private void ApplyTheme(string selectedTheme)
        {
            if (Application.Current == null) return;

            switch (selectedTheme)
            {
                case "MainLight": // Ana Tema (Açık) - Dynamic
                    Application.Current.UserAppTheme = AppTheme.Light;
                    break;
                case "MainDark": // Ana Tema (Koyu) - Dynamic
                    Application.Current.UserAppTheme = AppTheme.Dark;
                    break;
                case "Light": // Açık (Sabit)
                    Application.Current.UserAppTheme = AppTheme.Light;
                    break;
                case "PitchBlack": // Simsiyah (Sabit)
                    Application.Current.UserAppTheme = AppTheme.Dark;
                    break;
                case "Custom":
                    Application.Current.UserAppTheme = AppTheme.Dark; 
                    break;
                default: 
                    // Fallback
                    Application.Current.UserAppTheme = AppTheme.Dark;
                    break;
            }

            Preferences.Default.Set(ThemePreferenceKey, selectedTheme);
            UpdateStatusBarColor(selectedTheme);
            UpdateThemeUI(selectedTheme);
        }

        private void UpdateThemeUI(string selectedTheme)
        {
            // Reset All (Visual State: Unselected)
            SetFrameState(AnaAcikFrame, AnaAcikStatus, false);
            SetFrameState(AnaKoyuFrame, AnaKoyuStatus, false);
            SetFrameState(AcikFrame, AcikStatus, false);
            SetFrameState(SimsiyahFrame, SimsiyahStatus, false);
            SetFrameState(OzelFrame, OzelStatus, false);

            // Set Selected
            switch (selectedTheme)
            {
                case "MainLight":
                    SetFrameState(AnaAcikFrame, AnaAcikStatus, true);
                    break;
                case "MainDark":
                    SetFrameState(AnaKoyuFrame, AnaKoyuStatus, true);
                    break;
                case "Main": // Legacy mapping
                    SetFrameState(AnaKoyuFrame, AnaKoyuStatus, true);
                    break;
                case "System": // Legacy mapping
                    SetFrameState(AnaKoyuFrame, AnaKoyuStatus, true);
                    break;
                case "Light":
                    SetFrameState(AcikFrame, AcikStatus, true);
                    break;
                case "PitchBlack":
                    SetFrameState(SimsiyahFrame, SimsiyahStatus, true);
                    break;
                case "Dark": // Legacy mapping -> PitchBlack or MainDark? 
                             // Let's map old "Dark" to "Simsiyah" (PitchBlack) as user likely wanted dark mode.
                             // Or stick to "MainDark" if that was the default. 
                             // Given "Koyu" was the old placeholder, PitchBlack is safe.
                    SetFrameState(SimsiyahFrame, SimsiyahStatus, true);
                    break;
                case "Custom":
                    SetFrameState(OzelFrame, OzelStatus, true);
                    break;
            }
        }

        private void SetFrameState(Frame frame, Label statusLabel, bool isSelected)
        {
            if (frame == null) return;

            if (isSelected)
            {
                // Highlight: Primary Color
                frame.SetAppThemeColor(Microsoft.Maui.Controls.Frame.BorderColorProperty, 
                    Color.FromArgb("#00796B"), Color.FromArgb("#80CBC4"));
                
                // Show Status Label
                if (statusLabel != null) statusLabel.IsVisible = true;
            }
            else
            {
                // Default: Gray
                frame.SetAppThemeColor(Microsoft.Maui.Controls.Frame.BorderColorProperty, 
                    Color.FromArgb("#E0E0E0"), Color.FromArgb("#333333"));
                
                // Hide Status Label
                if (statusLabel != null) statusLabel.IsVisible = false;
            }
        }

        private async void OzelTemaOlustur_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new OzelTemaOlustur());
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadCurrentTheme(); 
            LoadCustomThemeInfo();
        }

        private void UpdateStatusBarColor(string theme)
        {
            if (Application.Current == null) return;

            var currentTheme = theme == "System" 
                ? Application.Current.RequestedTheme 
                : (theme == "Dark" || theme == "Custom" ? AppTheme.Dark : AppTheme.Light);

#if ANDROID
            Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.RunOnUiThread(() =>
            {
                var window = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window;
                if (window != null)
                {
                    if (currentTheme == AppTheme.Dark)
                    {
                        window.SetStatusBarColor(Android.Graphics.Color.Black);
                        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
                        {
                            window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)
                                Android.Views.SystemUiFlags.Visible;
                        }
                    }
                    else
                    {
                        window.SetStatusBarColor(Android.Graphics.Color.White);
                        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
                        {
                            window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)
                                (Android.Views.SystemUiFlags.LightStatusBar);
                        }
                    }
                }
            });
#endif
        }
    }
}

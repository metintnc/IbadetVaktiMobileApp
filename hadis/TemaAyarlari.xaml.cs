using hadis.Models;
using System.Text.Json;

namespace hadis
{
    public partial class TemaAyarlari : ContentPage
    {
        private const string ThemePreferenceKey = "AppTheme";

        public TemaAyarlari()
        {
            InitializeComponent();
            LoadCurrentTheme();
            LoadCustomThemeInfo();
        }

        private void LoadCurrentTheme()
        {
            // Kaydedilmis tema tercihini yukle
            string savedTheme = Preferences.Default.Get(ThemePreferenceKey, "System");
            
            switch (savedTheme)
            {
                case "System":
                    SistemTemaRadio.IsChecked = true;
                    break;
                case "Light":
                    AcikTemaRadio.IsChecked = true;
                    break;
                case "Dark":
                    KoyuTemaRadio.IsChecked = true;
                    break;
                case "Custom":
                    OzelTemaRadio.IsChecked = true;
                    break;
                default:
                    SistemTemaRadio.IsChecked = true;
                    break;
            }
        }

        private void LoadCustomThemeInfo()
        {
            // Kayitli ozel tema var mi kontrol et
            string customThemeJson = Preferences.Default.Get("CustomTheme", string.Empty);
            
            if (!string.IsNullOrEmpty(customThemeJson))
            {
                try
                {
                    var theme = JsonSerializer.Deserialize<CustomTheme>(customThemeJson);
                    if (theme != null)
                    {
                        OzelTemaRadio.Content = $"Ozel Tema ({theme.Name})";
                        OzelTemaAciklama.Text = "Kaydedilmis ozel temanizi kullanir";
                        OzelTemaRadio.IsEnabled = true;
                    }
                }
                catch
                {
                    OzelTemaRadio.IsEnabled = false;
                }
            }
            else
            {
                OzelTemaRadio.IsEnabled = false;
            }
        }

        private void OnThemeChanged(object sender, CheckedChangedEventArgs e)
        {
            if (!e.Value || Application.Current == null)
                return;

            string selectedTheme;
            
            if (sender == SistemTemaRadio)
            {
                Application.Current.UserAppTheme = AppTheme.Unspecified;
                selectedTheme = "System";
            }
            else if (sender == AcikTemaRadio)
            {
                Application.Current.UserAppTheme = AppTheme.Light;
                selectedTheme = "Light";
            }
            else if (sender == KoyuTemaRadio)
            {
                Application.Current.UserAppTheme = AppTheme.Dark;
                selectedTheme = "Dark";
            }
            else if (sender == OzelTemaRadio)
            {
                // Ozel tema secildi - koyu temaya ayarla (ozel renkler MainPage'de uygulanacak)
                Application.Current.UserAppTheme = AppTheme.Dark;
                selectedTheme = "Custom";
            }
            else
            {
                return;
            }
            
            Preferences.Default.Set(ThemePreferenceKey, selectedTheme);
            UpdateStatusBarColor(selectedTheme);
        }

        private async void OzelTemaOlustur_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new OzelTemaOlustur());
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Sayfa gorunur oldugunda ozel tema bilgisini yeniden yukle
            LoadCustomThemeInfo();
        }

        private void UpdateStatusBarColor(string theme)
        {
            // Aktif temayi belirle
            var currentTheme = theme == "System" 
                ? Application.Current?.RequestedTheme ?? AppTheme.Light 
                : (theme == "Dark" ? AppTheme.Dark : AppTheme.Light);

#if ANDROID
            Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.RunOnUiThread(() =>
            {
                var window = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window;
                if (window != null)
                {
                    if (currentTheme == AppTheme.Dark)
                    {
                        // Koyu tema - siyah status bar
                        window.SetStatusBarColor(Android.Graphics.Color.Black);
                        
                        // Android 6.0 ve uzeri icin metin rengini ayarla
                        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
                        {
                            // Koyu tema icin acik renkli iconlar
                            window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)
                                Android.Views.SystemUiFlags.Visible;
                        }
                    }
                    else
                    {
                        // Acik tema - beyaz status bar
                        window.SetStatusBarColor(Android.Graphics.Color.White);
                        
                        // Android 6.0 ve uzeri icin metin rengini ayarla
                        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
                        {
                            // Acik tema icin koyu renkli iconlar
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

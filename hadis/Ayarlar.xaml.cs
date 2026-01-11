using Microsoft.Maui.ApplicationModel;

namespace hadis
{
    public partial class Ayarlar : ContentPage
    {
        private const string ThemePreferenceKey = "AppTheme";
        private const string OtomatikKonumKey = "OtomatikKonum";
        private const string ManuelLatitudeKey = "ManuelLatitude";
        private const string ManuelLongitudeKey = "ManuelLongitude";
        private const string ManuelSehirKey = "ManuelSehir";
        
        public Ayarlar()
        {
            InitializeComponent();
            LoadSettings();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            BildirimSwitch.Toggled += (s, e) => Preferences.Default.Set("BildirimAktif", e.Value);
            
            // Þehir seçim sayfasýndan dönüldüðünde konum etiketini güncelle
            UpdateKonumLabel();
        }
        
        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            
            // Tab ile gelince soldan saða kayma animasyonu
            await AnimateFromLeft();
        }
        
        private async Task AnimateFromLeft()
        {
            // Baþlýk ve tüm frame'leri solda baþlat
            AyarlarTitle.Opacity = 0;
            AyarlarTitle.TranslationX = -300;
            
            BildirimFrame.Opacity = 0;
            BildirimFrame.TranslationX = -300;
            
            KonumFrame.Opacity = 0;
            KonumFrame.TranslationX = -300;
            
            GorunumFrame.Opacity = 0;
            GorunumFrame.TranslationX = -300;
            
            VeriYonetimiFrame.Opacity = 0;
            VeriYonetimiFrame.TranslationX = -300;
            
            HakkindaFrame.Opacity = 0;
            HakkindaFrame.TranslationX = -300;
            
            // 1. Baþlýk soldan kayarak gelsin
            await Task.WhenAll(
                AyarlarTitle.FadeTo(1, 400, Easing.CubicOut),
                AyarlarTitle.TranslateTo(0, 0, 500, Easing.CubicOut)
            );
            
            // 2. Bildirim frame'i
            var bildirimTask = Task.WhenAll(
                BildirimFrame.FadeTo(1, 400, Easing.CubicOut),
                BildirimFrame.TranslateTo(0, 0, 500, Easing.CubicOut)
            );
            
            await Task.Delay(80);
            
            // 3. Konum frame'i
            var konumTask = Task.WhenAll(
                KonumFrame.FadeTo(1, 400, Easing.CubicOut),
                KonumFrame.TranslateTo(0, 0, 500, Easing.CubicOut)
            );
            
            await Task.Delay(80);
            
            // 4. Görünüm frame'i
            var gorunumTask = Task.WhenAll(
                GorunumFrame.FadeTo(1, 400, Easing.CubicOut),
                GorunumFrame.TranslateTo(0, 0, 500, Easing.CubicOut)
            );
            
            await Task.Delay(80);
            
            // 5. Veri yönetimi frame'i
            var veriTask = Task.WhenAll(
                VeriYonetimiFrame.FadeTo(1, 400, Easing.CubicOut),
                VeriYonetimiFrame.TranslateTo(0, 0, 500, Easing.CubicOut)
            );
            
            await Task.Delay(80);
            
            // 6. Hakkýnda frame'i
            await Task.WhenAll(
                HakkindaFrame.FadeTo(1, 400, Easing.CubicOut),
                HakkindaFrame.TranslateTo(0, 0, 500, Easing.CubicOut)
            );
        }
        
        protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);
            
            // Tab deðiþirken saða kayma
            await Task.WhenAll(
                AyarlarTitle.TranslateTo(300, 0, 250, Easing.CubicIn),
                BildirimFrame.TranslateTo(300, 0, 250, Easing.CubicIn),
                KonumFrame.TranslateTo(300, 0, 250, Easing.CubicIn),
                GorunumFrame.TranslateTo(300, 0, 250, Easing.CubicIn),
                VeriYonetimiFrame.TranslateTo(300, 0, 250, Easing.CubicIn),
                HakkindaFrame.TranslateTo(300, 0, 250, Easing.CubicIn),
                
                AyarlarTitle.FadeTo(0, 200, Easing.CubicIn),
                BildirimFrame.FadeTo(0, 200, Easing.CubicIn),
                KonumFrame.FadeTo(0, 200, Easing.CubicIn),
                GorunumFrame.FadeTo(0, 200, Easing.CubicIn),
                VeriYonetimiFrame.FadeTo(0, 200, Easing.CubicIn),
                HakkindaFrame.FadeTo(0, 200, Easing.CubicIn)
            );
        }

        private void LoadSettings()
        {
            BildirimSwitch.IsToggled = Preferences.Default.Get("BildirimAktif", true);
            VersionLabel.Text = $"Versiyon: {AppInfo.VersionString}";
            
            // Kaydedilmiþ tema tercihini yükle
            string savedTheme = Preferences.Default.Get(ThemePreferenceKey, "System");
            
            // Picker'da doðru seçeneði seç
            switch (savedTheme)
            {
                case "System":
                    ThemePicker.SelectedIndex = 0;
                    break;
                case "Light":
                    ThemePicker.SelectedIndex = 1;
                    break;
                case "Dark":
                    ThemePicker.SelectedIndex = 2;
                    break;
                default:
                    ThemePicker.SelectedIndex = 0;
                    break;
            }
            
            // Konum etiketini güncelle
            UpdateKonumLabel();
        }

        private void UpdateKonumLabel()
        {
            bool otomatikKonum = Preferences.Default.Get(OtomatikKonumKey, true);
            
            if (otomatikKonum)
            {
                SeciliKonumLabel.Text = "Mevcut Konum: Otomatik (GPS)";
            }
            else
            {
                string manuelSehir = Preferences.Default.Get(ManuelSehirKey, "");
                if (!string.IsNullOrEmpty(manuelSehir))
                {
                    SeciliKonumLabel.Text = $"Mevcut Konum: {manuelSehir}";
                }
                else
                {
                    SeciliKonumLabel.Text = "Mevcut Konum: Otomatik (GPS)";
                }
            }
        }

        private async void SehirSecButton_Clicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new SehirSecim());
        }
        
        private void ThemePicker_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (ThemePicker.SelectedIndex == -1 || Application.Current == null)
                return;
            
            string selectedTheme;
            
            switch (ThemePicker.SelectedIndex)
            {
                case 0: // Sistem Temasý
                    Application.Current.UserAppTheme = AppTheme.Unspecified;
                    selectedTheme = "System";
                    break;
                case 1: // Açýk Tema
                    Application.Current.UserAppTheme = AppTheme.Light;
                    selectedTheme = "Light";
                    break;
                case 2: // Koyu Tema
                    Application.Current.UserAppTheme = AppTheme.Dark;
                    selectedTheme = "Dark";
                    break;
                default:
                    return;
            }
            
            Preferences.Default.Set(ThemePreferenceKey, selectedTheme);
            
            // Status bar rengini güncelle
            UpdateStatusBarColor(selectedTheme);
        }
        
        private void UpdateStatusBarColor(string theme)
        {
            // Aktif temayý belirle
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
                        
                        // Android 6.0 ve üzeri için metin rengini ayarla
                        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
                        {
                            // Koyu tema için açýk renkli iconlar
                            window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)
                                Android.Views.SystemUiFlags.Visible;
                        }
                    }
                    else
                    {
                        // Açýk tema - beyaz status bar
                        window.SetStatusBarColor(Android.Graphics.Color.White);
                        
                        // Android 6.0 ve üzeri için metin rengini ayarla
                        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
                        {
                            // Açýk tema için koyu renkli iconlar
                            window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)
                                (Android.Views.SystemUiFlags.LightStatusBar);
                        }
                    }
                }
            });
#endif
        }

        private async void ClearCacheButton_Clicked(object? sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Onbellegi Temizle",
                "Kuran PDFi ve diger onbellek verileri silinecek. Devam etmek istiyor musunuz?",
                "Evet", "Hayir");

            if (answer)
            {
                try
                {
                    string pdfPath = Path.Combine(FileSystem.AppDataDirectory, "kuran.pdf");
                    if (File.Exists(pdfPath))
                    {
                        File.Delete(pdfPath);
                    }
                    await DisplayAlert("Basarili", "Onbellek temizlendi.", "Tamam");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Hata", $"Onbellek temizlenirken bir hata olustu: {ex.Message}", "Tamam");
                }
            }
        }

        private async void ResetSettingsButton_Clicked(object? sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Ayarlari Sifirla",
                "Tum ayarlar varsayilan degerlere donecek. Devam etmek istiyor musunuz?",
                "Evet", "Hayir");

            if (answer)
            {
                Preferences.Default.Clear();
                LoadSettings();
                await DisplayAlert("Basarili", "Ayarlar varsayilan degerlere sifirlandi.", "Tamam");
            }
        }
    }
}

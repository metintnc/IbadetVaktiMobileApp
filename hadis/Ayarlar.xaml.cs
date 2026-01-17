using Microsoft.Maui.ApplicationModel;
using System.Text.Json;
using hadis.Models;

namespace hadis
{
    public partial class Ayarlar : ContentPage
    {
        private const string OtomatikKonumKey = "OtomatikKonum";
        private const string ManuelSehirKey = "ManuelSehir";

        public Ayarlar()
        {
            InitializeComponent();
            LoadSettings();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Ozel tema varsa uygula
            ApplyCustomTheme();
            
            // Sehir secim sayfasindan donulduðunde konum etiketini guncelle
            UpdateKonumLabel();
        }

        private void ApplyCustomTheme()
        {
            // Kayitli tema tercihini kontrol et
            string savedTheme = Preferences.Default.Get("AppTheme", "System");
            
            // Eðer Custom tema seçili deðilse, varsayýlan stillere dön
            if (savedTheme != "Custom")
            {
                ResetToDefaultStyles();
                return;
            }
            
            // Ozel tema yukle
            string customThemeJson = Preferences.Default.Get("CustomTheme", string.Empty);
            
            if (string.IsNullOrEmpty(customThemeJson))
            {
                ResetToDefaultStyles();
                return;
            }
            
            try
            {
                var theme = JsonSerializer.Deserialize<CustomTheme>(customThemeJson);
                if (theme != null)
                {
                    // Tüm frame'lerin çerçeve rengini SmallFrameBorder ile ayarla
                    Color borderColor = Color.FromArgb(theme.SmallFrameBorder);
                    Color textColor = Color.FromArgb(theme.SmallFrameText);
                    Color bgColor = Color.FromArgb(theme.SmallFrameBackground).WithAlpha(0.2f);
                    
                    // Header
                    HeaderFrame.BorderColor = borderColor;
                    HeaderFrame.BackgroundColor = bgColor;
                    AyarlarTitle.TextColor = textColor;
                    
                    // Main cards
                    TemaFrame.BorderColor = borderColor;
                    TemaFrame.BackgroundColor = bgColor;
                    TemaTitle.TextColor = textColor;
                    TemaSubtitle.TextColor = textColor.WithAlpha(0.7f);
                    
                    BildirimFrame.BorderColor = borderColor;
                    BildirimFrame.BackgroundColor = bgColor;
                    BildirimTitle.TextColor = textColor;
                    BildirimSubtitle.TextColor = textColor.WithAlpha(0.7f);
                    
                    WidgetFrame.BorderColor = borderColor;
                    WidgetFrame.BackgroundColor = bgColor;
                    WidgetTitle.TextColor = textColor;
                    WidgetSubtitle.TextColor = textColor.WithAlpha(0.7f);
                    
                    KonumFrame.BorderColor = borderColor;
                    KonumFrame.BackgroundColor = bgColor;
                    KonumTitle.TextColor = textColor;
                    SeciliKonumLabel.TextColor = textColor.WithAlpha(0.7f);
                    
                    // Bottom sections
                    VeriYonetimiFrame.BorderColor = borderColor;
                    VeriYonetimiFrame.BackgroundColor = bgColor;
                    VeriYonetimiTitle.TextColor = textColor;
                    
                    HakkindaFrame.BorderColor = borderColor;
                    HakkindaFrame.BackgroundColor = bgColor;
                    HakkindaTitle.TextColor = textColor;
                    VersionLabel.TextColor = textColor.WithAlpha(0.7f);
                    CopyrightLabel.TextColor = textColor.WithAlpha(0.5f);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ozel tema uygulama hatasi: {ex.Message}");
                ResetToDefaultStyles();
            }
        }

        private void ResetToDefaultStyles()
        {
            // Aktif temayý al
            var currentTheme = Application.Current?.UserAppTheme == AppTheme.Unspecified 
                ? Application.Current?.RequestedTheme ?? AppTheme.Light 
                : Application.Current?.UserAppTheme ?? AppTheme.Light;

            if (currentTheme == AppTheme.Dark)
            {
                // Dark tema varsayýlan renkleri
                Color borderColor = Color.FromArgb("#80FFFFFF");
                Color textColor = Color.FromArgb("#FFFFFF");
                Color bgColor = Color.FromArgb("#33000000");
                
                // Header
                HeaderFrame.BorderColor = borderColor;
                HeaderFrame.BackgroundColor = bgColor;
                AyarlarTitle.TextColor = textColor;
                
                // Main cards
                TemaFrame.BorderColor = borderColor;
                TemaFrame.BackgroundColor = bgColor;
                TemaTitle.TextColor = textColor;
                TemaSubtitle.TextColor = Color.FromArgb("#BDBDBD");
                
                BildirimFrame.BorderColor = borderColor;
                BildirimFrame.BackgroundColor = bgColor;
                BildirimTitle.TextColor = textColor;
                BildirimSubtitle.TextColor = Color.FromArgb("#BDBDBD");
                
                WidgetFrame.BorderColor = borderColor;
                WidgetFrame.BackgroundColor = bgColor;
                WidgetTitle.TextColor = textColor;
                WidgetSubtitle.TextColor = Color.FromArgb("#BDBDBD");
                
                KonumFrame.BorderColor = borderColor;
                KonumFrame.BackgroundColor = bgColor;
                KonumTitle.TextColor = textColor;
                SeciliKonumLabel.TextColor = Color.FromArgb("#BDBDBD");
                
                // Bottom sections
                VeriYonetimiFrame.BorderColor = borderColor;
                VeriYonetimiFrame.BackgroundColor = bgColor;
                VeriYonetimiTitle.TextColor = textColor;
                
                HakkindaFrame.BorderColor = borderColor;
                HakkindaFrame.BackgroundColor = bgColor;
                HakkindaTitle.TextColor = textColor;
                VersionLabel.TextColor = Color.FromArgb("#BDBDBD");
                CopyrightLabel.TextColor = Color.FromArgb("#757575");
            }
            else
            {
                // Light tema varsayýlan renkleri
                Color borderColor = Color.FromArgb("#80009688");
                Color textColor = Color.FromArgb("#00796B");
                Color bgColor = Color.FromArgb("#22FFFFFF");
                
                // Header
                HeaderFrame.BorderColor = borderColor;
                HeaderFrame.BackgroundColor = bgColor;
                AyarlarTitle.TextColor = textColor;
                
                // Main cards
                TemaFrame.BorderColor = borderColor;
                TemaFrame.BackgroundColor = bgColor;
                TemaTitle.TextColor = textColor;
                TemaSubtitle.TextColor = Color.FromArgb("#757575");
                
                BildirimFrame.BorderColor = borderColor;
                BildirimFrame.BackgroundColor = bgColor;
                BildirimTitle.TextColor = textColor;
                BildirimSubtitle.TextColor = Color.FromArgb("#757575");
                
                WidgetFrame.BorderColor = borderColor;
                WidgetFrame.BackgroundColor = bgColor;
                WidgetTitle.TextColor = textColor;
                WidgetSubtitle.TextColor = Color.FromArgb("#757575");
                
                KonumFrame.BorderColor = borderColor;
                KonumFrame.BackgroundColor = bgColor;
                KonumTitle.TextColor = textColor;
                SeciliKonumLabel.TextColor = Color.FromArgb("#757575");
                
                // Bottom sections
                VeriYonetimiFrame.BorderColor = borderColor;
                VeriYonetimiFrame.BackgroundColor = bgColor;
                VeriYonetimiTitle.TextColor = textColor;
                
                HakkindaFrame.BorderColor = borderColor;
                HakkindaFrame.BackgroundColor = bgColor;
                HakkindaTitle.TextColor = textColor;
                VersionLabel.TextColor = Color.FromArgb("#757575");
                CopyrightLabel.TextColor = Color.FromArgb("#9E9E9E");
            }
        }

        private void LoadSettings()
        {
            VersionLabel.Text = $"Versiyon: {AppInfo.VersionString}";
            
            // Konum etiketini guncelle
            UpdateKonumLabel();
        }

        private void UpdateKonumLabel()
        {
            bool otomatikKonum = Preferences.Default.Get(OtomatikKonumKey, true);
            
            if (otomatikKonum)
            {
                SeciliKonumLabel.Text = "Þehir ve GPS";
            }
            else
            {
                string manuelSehir = Preferences.Default.Get(ManuelSehirKey, "");
                if (!string.IsNullOrEmpty(manuelSehir))
                {
                    SeciliKonumLabel.Text = manuelSehir;
                }
                else
                {
                    SeciliKonumLabel.Text = "Þehir ve GPS";
                }
            }
        }

        private async void BildirimButton_Clicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new BildirimAyarlari());
        }

        private async void TemaButton_Clicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new TemaAyarlari());
        }

        private async void WidgetButton_Clicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new WidgetAyarlari());
        }

        private async void KonumButton_Clicked(object? sender, EventArgs e)
        {
            await Navigation.PushAsync(new SehirSecim());
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

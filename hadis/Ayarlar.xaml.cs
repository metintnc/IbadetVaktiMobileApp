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
            
            // Özel tema varsa uygula
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
                    
                    BildirimFrame.BorderColor = borderColor;
                    TemaFrame.BorderColor = borderColor;
                    WidgetFrame.BorderColor = borderColor;
                    KonumFrame.BorderColor = borderColor;
                    VeriYonetimiFrame.BorderColor = borderColor;
                    HakkindaFrame.BorderColor = borderColor;
                    
                    // Background'larý transparent tut
                    BildirimFrame.BackgroundColor = Colors.Transparent;
                    TemaFrame.BackgroundColor = Colors.Transparent;
                    WidgetFrame.BackgroundColor = Colors.Transparent;
                    KonumFrame.BackgroundColor = Colors.Transparent;
                    VeriYonetimiFrame.BackgroundColor = Colors.Transparent;
                    HakkindaFrame.BackgroundColor = Colors.Transparent;
                    
                    // Text renklerini ayarla
                    Color textColor = Color.FromArgb(theme.SmallFrameText);
                    
                    BildirimButton.TextColor = textColor;
                    TemaButton.TextColor = textColor;
                    WidgetButton.TextColor = textColor;
                    KonumButton.TextColor = textColor;
                    VeriYonetimiTitle.TextColor = textColor;
                    HakkindaTitle.TextColor = textColor;
                    AyarlarTitle.TextColor = textColor;
                    
                    // Alt label'lar için biraz daha soluk renk
                    SeciliKonumLabel.TextColor = textColor.WithAlpha(0.7f);
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

            // Varsayýlan renklere dön
            if (currentTheme == AppTheme.Dark)
            {
                // Dark tema varsayýlan renkleri
                Color borderColor = Color.FromArgb("#26A69A");
                
                // Tüm frame çerçeveleri
                BildirimFrame.BorderColor = borderColor;
                TemaFrame.BorderColor = borderColor;
                WidgetFrame.BorderColor = borderColor;
                KonumFrame.BorderColor = borderColor;
                VeriYonetimiFrame.BorderColor = borderColor;
                HakkindaFrame.BorderColor = borderColor;
                
                // Background'lar
                BildirimFrame.BackgroundColor = Colors.Transparent;
                TemaFrame.BackgroundColor = Colors.Transparent;
                WidgetFrame.BackgroundColor = Colors.Transparent;
                KonumFrame.BackgroundColor = Colors.Transparent;
                VeriYonetimiFrame.BackgroundColor = Colors.Transparent;
                HakkindaFrame.BackgroundColor = Colors.Transparent;
                
                // Text renkleri
                BildirimButton.TextColor = Color.FromArgb("#81C784");
                TemaButton.TextColor = Color.FromArgb("#81C784");
                WidgetButton.TextColor = Color.FromArgb("#81C784");
                KonumButton.TextColor = Color.FromArgb("#81C784");
                VeriYonetimiTitle.TextColor = Color.FromArgb("#81C784");
                HakkindaTitle.TextColor = Color.FromArgb("#81C784");
                AyarlarTitle.TextColor = Color.FromArgb("#81C784");
                
                SeciliKonumLabel.TextColor = Color.FromArgb("#BDBDBD");
                VersionLabel.TextColor = Color.FromArgb("#BDBDBD");
                CopyrightLabel.TextColor = Color.FromArgb("#757575");
            }
            else
            {
                // Light tema varsayýlan renkleri
                Color borderColor = Color.FromArgb("#009688");
                
                // Tüm frame çerçeveleri
                BildirimFrame.BorderColor = borderColor;
                TemaFrame.BorderColor = borderColor;
                WidgetFrame.BorderColor = borderColor;
                KonumFrame.BorderColor = borderColor;
                VeriYonetimiFrame.BorderColor = borderColor;
                HakkindaFrame.BorderColor = borderColor;
                
                // Background'lar
                BildirimFrame.BackgroundColor = Color.FromArgb("#FAFFFFFF");
                TemaFrame.BackgroundColor = Color.FromArgb("#FAFFFFFF");
                WidgetFrame.BackgroundColor = Color.FromArgb("#FAFFFFFF");
                KonumFrame.BackgroundColor = Color.FromArgb("#FAFFFFFF");
                VeriYonetimiFrame.BackgroundColor = Color.FromArgb("#FAFFFFFF");
                HakkindaFrame.BackgroundColor = Color.FromArgb("#FAFFFFFF");
                
                // Text renkleri
                BildirimButton.TextColor = Color.FromArgb("#00796B");
                TemaButton.TextColor = Color.FromArgb("#00796B");
                WidgetButton.TextColor = Color.FromArgb("#00796B");
                KonumButton.TextColor = Color.FromArgb("#00796B");
                VeriYonetimiTitle.TextColor = Color.FromArgb("#00796B");
                HakkindaTitle.TextColor = Color.FromArgb("#00796B");
                AyarlarTitle.TextColor = Color.FromArgb("#00796B");
                
                SeciliKonumLabel.TextColor = Color.FromArgb("#757575");
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
                SeciliKonumLabel.Text = "Mevcut: Otomatik (GPS)";
            }
            else
            {
                string manuelSehir = Preferences.Default.Get(ManuelSehirKey, "");
                if (!string.IsNullOrEmpty(manuelSehir))
                {
                    SeciliKonumLabel.Text = $"Mevcut: {manuelSehir}";
                }
                else
                {
                    SeciliKonumLabel.Text = "Mevcut: Otomatik (GPS)";
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

using Syncfusion.Maui.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using hadis.Models;

namespace hadis
{
    public partial class zikirmatik : ContentPage
    {
        private int sayı = 0;
        private int toplam = 0;
        private int hedef = 100;
        private string seciliZikir = "Sübhanallah";
        private bool sesDurum = true;
        
        public zikirmatik()
        {
            InitializeComponent();
            
            // Kayıtlı değerleri yükle
            sayı = Preferences.Default.Get("sonSayi", 0);
            toplam = Preferences.Default.Get("Toplam", 0);
            hedef = Preferences.Default.Get("ZikirHedef", 100);
            seciliZikir = Preferences.Default.Get("SeciliZikir", "Sübhanallah");
            sesDurum = Preferences.Default.Get("SesDurum", true);
            
            // UI'ı güncelle
            zikirsayisi.Text = sayı.ToString();
            SeciliZikirLabel.Text = $"Seçili Zikir: {seciliZikir}";
            HedefLabel.Text = $"Hedef: {hedef}";
            SesTitresimIcon.Text = sesDurum ? "🔊" : "🔇";
            
            // İlerleme güncelle
            UpdateProgress();
            
            // Layout değişikliğinde ilerleme çubuğu genişliğini ayarla
            HeaderFrame.SizeChanged += OnHeaderFrameSizeChanged;
        }

        private void OnHeaderFrameSizeChanged(object sender, EventArgs e)
        {
            if (sender is Frame frame)
            {
                // Progress bar için maksimum genişliği ayarla
                double availableWidth = frame.Width - 48; // Padding için
                if (availableWidth > 0)
                {
                    double progress = Math.Min((double)sayı / (double)hedef, 1.0);
                    IlerlemeIbresi.WidthRequest = availableWidth * progress;
                }
            }
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            
            // Özel tema varsa uygula
            ApplyCustomTheme();
            
            // Giriş animasyonu
            await AnimateZikirEntry();
        }

        private void ApplyCustomTheme()
        {
            // Kayıtlı tema tercihini kontrol et
            string savedTheme = Preferences.Default.Get("AppTheme", "System");
            
            // Eğer Custom tema seçili değilse, varsayılan stillere dön
            if (savedTheme != "Custom")
            {
                ResetToDefaultStyles();
                return;
            }
            
            // Özel tema yükle
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
                    // Ana sayaç rengini uygula
                    zikirsayisi.TextColor = Color.FromArgb(theme.MainFrameText);
                    ZikirBaslik.TextColor = Color.FromArgb(theme.MainFrameText);
                    
                    // Border renkleri
                    zikirbutton.BorderColor = Color.FromArgb(theme.MainFrameBorder);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Özel tema uygulama hatası: {ex.Message}");
                ResetToDefaultStyles();
            }
        }

        private void ResetToDefaultStyles()
        {
            // Aktif temayı al
            var currentTheme = Application.Current?.UserAppTheme == AppTheme.Unspecified 
                ? Application.Current?.RequestedTheme ?? AppTheme.Light 
                : Application.Current?.UserAppTheme ?? AppTheme.Light;

            // Varsayılan renklere dön
            if (currentTheme == AppTheme.Dark)
            {
                zikirsayisi.TextColor = Colors.White;
                ZikirBaslik.TextColor = Colors.White;
                zikirbutton.BorderColor = Color.FromArgb("#80FFFFFF");
            }
            else
            {
                zikirsayisi.TextColor = Color.FromArgb("#00796B");
                ZikirBaslik.TextColor = Color.FromArgb("#00796B");
                zikirbutton.BorderColor = Color.FromArgb("#80009688");
            }
        }
        
        private async Task AnimateZikirEntry()
        {
            // Ana daire animasyonu
            zikirbutton.Opacity = 0;
            zikirbutton.Scale = 0.5;
            
            await Task.WhenAll(
                zikirbutton.FadeTo(1, 500, Easing.CubicOut),
                zikirbutton.ScaleTo(1.0, 600, Easing.SpringOut)
            );
        }
        
        protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);
            
            // Çıkış animasyonu
            await Task.WhenAll(
                zikirbutton.FadeTo(0, 300, Easing.CubicIn),
                zikirbutton.ScaleTo(0.5, 400, Easing.CubicIn)
            );
        }

        private async void zikirbutton_Clicked(object sender, EventArgs e)
        {
            sayı++;
            toplam++;
            
            zikirsayisi.Text = sayı.ToString();
            Preferences.Default.Set("sonSayi", sayı);
            Preferences.Default.Set("Toplam", toplam);
            
            // İlerleme güncelle
            UpdateProgress();
            
            // Hedef kontrolü
            if(sayı == hedef)
            {
                await CelebrateAchievement();
                await DisplayAlert("Tebrikler! 🎉", $"{hedef} {seciliZikir} tamamlandı!", "Tamam");
                
                if (sesDurum)
                {
                    try
                    {
                        Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(500));
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
            else if(sayı % 33 == 0 && sesDurum)
            {
                try
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            else if(sesDurum)
            {
                try
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(30));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            
            // Buton animasyonu
            await AnimateButton();
        }

        private async Task AnimateButton()
        {
            await zikirbutton.ScaleTo(0.95, 50, Easing.CubicOut);
            await zikirbutton.ScaleTo(1.0, 100, Easing.SpringOut);
        }

        private void UpdateProgress()
        {
            // Hedefe göre ilerleme
            double progress = Math.Min((double)sayı / (double)hedef, 1.0);
            IlerlemeBar.Progress = progress;
            
            // İlerleme ibresi genişliği (BoxView için)
            double availableWidth = HeaderFrame.Width > 0 ? HeaderFrame.Width - 48 : 300;
            IlerlemeIbresi.WidthRequest = availableWidth * progress;
            
            // Yüzde ve kalan bilgileri
            int yuzde = (int)(progress * 100);
            int kalan = Math.Max(0, hedef - sayı);
            
            IlerlemeYuzdeLabel.Text = $"İlerleme: {yuzde}%";
            KalanLabel.Text = $"(Kalan: {kalan})";
            
            // İlerleme tamamlandığında renk değişimi
            if (progress >= 1.0)
            {
                IlerlemeYuzdeLabel.TextColor = Colors.Green;
            }
            else
            {
                var currentTheme = Application.Current?.UserAppTheme == AppTheme.Unspecified 
                    ? Application.Current?.RequestedTheme ?? AppTheme.Light 
                    : Application.Current?.UserAppTheme ?? AppTheme.Light;
                    
                IlerlemeYuzdeLabel.TextColor = currentTheme == AppTheme.Dark 
                    ? Colors.White 
                    : Color.FromArgb("#00796B");
            }
        }

        private async Task CelebrateAchievement()
        {
            // Başarı animasyonu
            await zikirbutton.ScaleTo(1.2, 200, Easing.SpringOut);
            await zikirbutton.ScaleTo(1.0, 200, Easing.SpringIn);
        }

        private async void sifirla_Clicked(object sender, EventArgs e)
        {
            bool cevap = await DisplayAlert("Emin misiniz?", "Zikir sayacını sıfırlamak istediğimize emin misiniz?", "Evet, Sıfırla", "Hayır");
            if (cevap)
            {
                sayı = 0;
                zikirsayisi.Text = sayı.ToString();
                Preferences.Default.Set("sonSayi", sayı);
                
                UpdateProgress();
                
                if (sesDurum)
                {
                    try
                    {
                        Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(100));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
        }

        private async void HedefAyarla_Clicked(object sender, EventArgs e)
        {
            string result = await DisplayPromptAsync(
                "Hedef Ayarla",
                "Yeni hedef değerini girin (33, 66, 99, 100, vb.)",
                initialValue: hedef.ToString(),
                keyboard: Keyboard.Numeric);
            
            if (!string.IsNullOrEmpty(result) && int.TryParse(result, out int yeniHedef) && yeniHedef > 0)
            {
                hedef = yeniHedef;
                HedefLabel.Text = $"Hedef: {hedef}";
                Preferences.Default.Set("ZikirHedef", hedef);
                UpdateProgress();
                
                // İlerleme çubuğunu animasyonlu güncelle
                await AnimateProgressUpdate();
            }
        }

        private async Task AnimateProgressUpdate()
        {
            await IlerlemeIbresi.ScaleTo(1.05, 200, Easing.CubicOut);
            await IlerlemeIbresi.ScaleTo(1.0, 200, Easing.CubicIn);
        }

        private async void ZikirSec_Clicked(object sender, EventArgs e)
        {
            string[] zikirler = new string[]
            {
                "Sübhanallah",
                "Elhamdülillah",
                "Allahu Ekber",
                "La ilahe illallah",
                "Estağfirullah",
                "Sübhanallahi ve bihamdihi",
                "La havle vela kuvvete illa billah"
            };
            
            string secim = await DisplayActionSheet("Zikir Seçin", "İptal", null, zikirler);
            
            if (!string.IsNullOrEmpty(secim) && secim != "İptal")
            {
                seciliZikir = secim;
                SeciliZikirLabel.Text = $"Seçili Zikir: {seciliZikir}";
                Preferences.Default.Set("SeciliZikir", seciliZikir);
            }
        }

        private async void SesTitresim_Clicked(object sender, EventArgs e)
        {
            sesDurum = !sesDurum;
            SesTitresimIcon.Text = sesDurum ? "🔊" : "🔇";
            Preferences.Default.Set("SesDurum", sesDurum);
            
            string mesaj = sesDurum ? "Ses/Titreşim Açık" : "Ses/Titreşim Kapalı";
            await DisplayAlert("Bilgi", mesaj, "Tamam");
        }
    }
}

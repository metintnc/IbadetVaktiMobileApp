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
        
        public zikirmatik()
        {
            InitializeComponent();
            sayı = Preferences.Default.Get("sonSayi", 0);
            zikirsayisi.Text = sayı.ToString();
            toplam = Preferences.Default.Get("Toplam", 0);
            ToplamZikir.Text = toplam.ToString();
            
            // Ilerleme guncelemesi
            UpdateProgress();
            UpdateHedefProgress();
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            
            // Özel tema varsa uygula
            ApplyCustomTheme();
            
            // Tab ile gelince scale animasyon
            await AnimateZikirEntry();
        }

        private void ApplyCustomTheme()
        {
            // Kayitli tema tercihini kontrol et
            string savedTheme = Preferences.Default.Get("AppTheme", "System");
            
            // Eğer Custom tema seçili değilse, varsayılan stillere dön
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
                    // Ana frame text renkleri kullan (Zikir sayısı için)
                    zikirsayisi.TextColor = Color.FromArgb(theme.MainFrameText);
                    ZikirBaslik.TextColor = Color.FromArgb(theme.MainFrameText);
                    
                    // Buton renkleri (Ana frame border rengi kullan)
                    zikirbutton.BorderColor = Color.FromArgb(theme.MainFrameBorder);
                    sifirla.BackgroundColor = Color.FromArgb(theme.MainFrameBorder);
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
            // Aktif temayı al
            var currentTheme = Application.Current?.UserAppTheme == AppTheme.Unspecified 
                ? Application.Current?.RequestedTheme ?? AppTheme.Light 
                : Application.Current?.UserAppTheme ?? AppTheme.Light;

            // Varsayılan renklere dön
            if (currentTheme == AppTheme.Dark)
            {
                // Dark tema varsayılan renkleri
                zikirsayisi.TextColor = Colors.White;
                ZikirBaslik.TextColor = Colors.White;
                zikirbutton.BorderColor = Color.FromArgb("#26A69A");
                sifirla.BackgroundColor = Color.FromArgb("#26A69A");
            }
            else
            {
                // Light tema varsayılan renkleri
                zikirsayisi.TextColor = Color.FromArgb("#00796B");
                ZikirBaslik.TextColor = Colors.Black;
                zikirbutton.BorderColor = Color.FromArgb("#00796B");
                sifirla.BackgroundColor = Color.FromArgb("#00796B");
            }
        }
        
        private async Task AnimateZikirEntry()
        {
            // Zikir butonu
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
            
            // Tab değişirken scale out
            await Task.WhenAll(
                zikirbutton.FadeTo(0, 300, Easing.CubicIn),
                zikirbutton.ScaleTo(0.5, 400, Easing.CubicIn)
            );
        }

        private async void zikirbutton_Clicked(object sender, EventArgs e)
        {
            toplam++;
            sayı++;
            ToplamZikir.Text = toplam.ToString();
            zikirsayisi.Text = sayı.ToString();
            Preferences.Default.Set("sonSayi", sayı);
            Preferences.Default.Set("Toplam", toplam);
            
            // Ilerleme guncelle
            UpdateProgress();
            UpdateHedefProgress();
            
            if(sayı == 33 || sayı == 66 || sayı == 99)
            {
                // Basari animasyonu
                await CelebrateAchievement();
                
                try
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(300));
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            else
            {
                try
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(50));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            
            // Buton animasyonu
            zikirbutton.BackgroundColor = Colors.DarkGray;
            await Task.Delay(50);
            zikirbutton.BackgroundColor = Colors.Transparent;
        }

        private void UpdateProgress()
        {
            // 99'a gore ilerleme
            double progress = Math.Min((double)sayı / 99.0, 1.0);
            IlerlemeBar.Progress = progress;
            IlerlemeYuzde.Text = $"{(int)(progress * 100)}%";
            
            // Tur gosterimi
            MevcutTur.Text = $"{sayı} / 99";
        }

        private void UpdateHedefProgress()
        {
            // 33 hedefi
            if (sayı >= 33)
                Hedef33.Text = "✓";
            else
                Hedef33.Text = $"{sayı}/33";
            
            // 66 hedefi
            if (sayı >= 66)
                Hedef66.Text = "✓";
            else
                Hedef66.Text = $"{sayı}/66";
            
            // 99 hedefi
            if (sayı >= 99)
                Hedef99.Text = "✓";
            else
                Hedef99.Text = $"{sayı}/99";
        }

        private async Task CelebrateAchievement()
        {
            // Buton buyume animasyonu
            await zikirbutton.ScaleTo(1.15, 200, Easing.SpringOut);
            await zikirbutton.ScaleTo(1.0, 200, Easing.SpringIn);
        }

        private async void sifirla_Clicked(object sender, EventArgs e)
        {
            bool cevap = await DisplayAlert("Emin misiniz?", "Zikir sayacını sıfırlamak istediğinize emin misiniz?", "Evet, Sıfırla", "Hayır");
            if (cevap)
            {
                sayı = 0;
                zikirsayisi.Text = sayı.ToString();
                Preferences.Default.Set("sonSayi", sayı);
                
                // Ilerleme guncelle
                UpdateProgress();
                UpdateHedefProgress();
                
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

        private void zikirbutton_SizeChanged(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            btn.WidthRequest = btn.Height;
        }
    }
}

using Syncfusion.Maui.Themes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hadis
{
    public  partial class zikirmatik : ContentPage
    {
        private int sayı = 0;
        private int toplam = 0;
        
        public zikirmatik()
        {
            InitializeComponent();
            sayı = Preferences.Default.Get("sonSayi", 0);
            zikirsayisi.Text = sayı.ToString();
            toplam = Preferences.Default.Get("Toplam",0);
            ToplamZikir.Text = toplam.ToString();
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            
            // Tab ile gelince scale animasyon
            await AnimateZikirEntry();
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
            
            if(sayı ==33|| sayı == 66 || sayı == 99)
            {
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
            zikirbutton.BackgroundColor = Colors.DarkGray;
            await Task.Delay(50);
            zikirbutton.BackgroundColor = Colors.Transparent;
        }

        private async void sifirla_Clicked(object sender, EventArgs e)
        {
            bool cevap = await DisplayAlert("Emin misiniz?","Zikir sayacını sıfırlamak istediğinize emin misiniz?","Evet, Sıfırla","Hayır");
            if (cevap)
            {
                sayı = 0;
                zikirsayisi.Text = sayı.ToString();
                Preferences.Default.Set("sonSayi", sayı);
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

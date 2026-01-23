using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hadis
{
    public partial class kible : ContentPage
    {
        private Pusula compass;
        private bool _animationPlayed = false;
        
        public kible()
        {
            InitializeComponent();
            compass = new Pusula();
        }
        
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Sensör başlatmayı arka planda başlat
            Task.Run(async () =>
            {
                await compass.KontrolEt();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    compass.AciDegisti += KıbleOkunuDondur;
                });
            });
        }
        
        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            
            // Tab ile gelince animasyon oynat
            await AnimateKibleEntry();
        }
        
        private async Task AnimateKibleEntry()
        {
            // Kıble okunu görünmez ve küçük yap
            kibleoku.Opacity = 0;
            kibleoku.Scale = 0.3;
            
            // Açı değerini görünmez yap
            AciDegeri.Opacity = 0;
            AciDegeri.Scale = 0.5;
            
            // Önce kıble oku büyüsün ve belirsin - Spring efekti ile elastik animasyon
            await Task.WhenAll(
                kibleoku.FadeTo(1, 600, Easing.CubicOut),
                kibleoku.ScaleTo(1.0, 800, Easing.SpringOut)
            );
            
            // Sonra açı değeri belirsin
            await Task.WhenAll(
                AciDegeri.FadeTo(1, 400, Easing.CubicOut),
                AciDegeri.ScaleTo(1.0, 500, Easing.SpringOut)
            );
        }
        
        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            compass.PusulaDurdur();
            compass.AciDegisti -= KıbleOkunuDondur;
        }
        
        protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);
            
            // Tab değişirken animasyon oynat
            await AnimateKibleExit();
        }
        
        private async Task AnimateKibleExit()
        {
            // Önce açı değeri küçülsün ve kaybolsun
            var acıTask = Task.WhenAll(
                AciDegeri.FadeTo(0, 300, Easing.CubicIn),
                AciDegeri.ScaleTo(0.5, 400, Easing.CubicIn)
            );
            
            // Sonra kıble oku küçülsün ve kaybolsun - Spring efekti ile
            var okuTask = Task.WhenAll(
                kibleoku.FadeTo(0, 400, Easing.CubicIn),
                kibleoku.ScaleTo(0.3, 500, Easing.SpringIn)
            );
            
            await Task.WhenAll(acıTask, okuTask);
        }

        public void KıbleOkunuDondur(double gelenaci)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if(gelenaci < 180)
                {
                    await kibleoku.RotateTo(gelenaci, 150, Easing.Linear);
                }
                else
                {
                    await kibleoku.RotateTo(gelenaci -360, 150, Easing.Linear);
                }
                AciDegeri.Text = $"{360 - gelenaci:F0}°";
                int a = Convert.ToInt32(gelenaci);
                if(360 - a == 0)
                {
                    AciDegeri.TextColor = Colors.Gold;
                }
                else
                {
                    AciDegeri.ClearValue(Label.TextColorProperty);
                }
            });
        }
    }
}

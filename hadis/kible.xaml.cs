using System;
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
            await AnimateKibleEntry();
        }
        
        private async Task AnimateKibleEntry()
        {
            kibleoku.Opacity = 0;
            kibleoku.Scale = 0.3;
            AciDegeri.Opacity = 0;
            AciDegeri.Scale = 0.5;
            await Task.WhenAll(
                kibleoku.FadeTo(1, 600, Easing.CubicOut),
                kibleoku.ScaleTo(1.0, 800, Easing.SpringOut)
            );
            await Task.WhenAll(
                AciDegeri.FadeTo(1, 400, Easing.CubicOut),
                AciDegeri.ScaleTo(1.0, 500, Easing.SpringOut)
            );
        }
        
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            compass.PusulaDurdur();
            compass.AciDegisti -= KıbleOkunuDondur;
        }
        
        protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);
            await AnimateKibleExit();
        }
        
        private async Task AnimateKibleExit()
        {
            var acıTask = Task.WhenAll(
                AciDegeri.FadeTo(0, 300, Easing.CubicIn),
                AciDegeri.ScaleTo(0.5, 400, Easing.CubicIn)
            );
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

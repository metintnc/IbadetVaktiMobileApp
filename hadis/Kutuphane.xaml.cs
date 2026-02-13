namespace hadis
{
    public partial class Kutuphane : ContentPage
    {
        public Kutuphane()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetTabBarIsVisible(this, false);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Shell.SetTabBarIsVisible(this, true);
        }

        private async void ArapcaKuranButton_Clicked(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new KuranOkumaPage(1));
        }

        private async void IlmihalButton_Clicked(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new Ilmihal());
        }

        private async void NamazHocasiButton_Clicked(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new NamazHocasi());
        }

        protected override bool OnBackButtonPressed()
        {
            if (Navigation.NavigationStack.Count > 1)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PopAsync();
                });
                return true;
            }
            return base.OnBackButtonPressed();
        }
    }
}

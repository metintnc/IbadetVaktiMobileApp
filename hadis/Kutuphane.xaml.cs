using hadis.Services;

namespace hadis
{
    public partial class Kutuphane : ContentPage
    {
        private readonly IServiceProvider _serviceProvider;

        public Kutuphane(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
        }

        private async void KuranMealButton_Clicked(object sender, TappedEventArgs e)
        {
            var page = _serviceProvider.GetRequiredService<Kuran>();
            await Navigation.PushAsync(page);
        }

        private async void ArapcaKuranButton_Clicked(object sender, TappedEventArgs e)
        {
            var quranApi = _serviceProvider.GetRequiredService<QuranApiService>();
            await Navigation.PushAsync(new KuranOkumaPage(quranApi, 1));
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
            // Ana sayfaya (Vakitler) dön
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("//MainPage");
            });
            return true;
        }
    }
}

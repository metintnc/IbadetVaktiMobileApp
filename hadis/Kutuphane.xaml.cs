using hadis.Services;
using hadis.Helpers;

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

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (MainContentGrid != null)
            {
                MainContentGrid.Opacity = 0;
                MainContentGrid.TranslationY = 15;
                _ = Task.WhenAll(
                    MainContentGrid.FadeTo(1, 180, Easing.CubicOut),
                    MainContentGrid.TranslateTo(0, 0, 180, Easing.CubicOut)
                );
            }
        }

        private async void KuranMealButton_Clicked(object sender, TappedEventArgs e)
        {
            if (sender is VisualElement element)
                await element.TapBounce(0.94, 80);

            var page = _serviceProvider.GetRequiredService<Kuran>();
            await Navigation.PushAsync(page, false);
        }

        private async void ArapcaKuranButton_Clicked(object sender, TappedEventArgs e)
        {
            if (sender is VisualElement element)
                await element.TapBounce(0.94, 80);

            var quranApi = _serviceProvider.GetRequiredService<QuranApiService>();
            await Navigation.PushAsync(new KuranOkumaPage(quranApi, 1), false);
        }

        private async void IlmihalButton_Clicked(object sender, TappedEventArgs e)
        {
            if (sender is VisualElement element)
                await element.TapBounce(0.94, 80);

            await Navigation.PushAsync(new Ilmihal(), false);
        }

        private async void NamazHocasiButton_Clicked(object sender, TappedEventArgs e)
        {
            if (sender is VisualElement element)
                await element.TapBounce(0.94, 80);

            await Navigation.PushAsync(new NamazHocasi(), false);
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

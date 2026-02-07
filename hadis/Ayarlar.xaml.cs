using hadis.Helpers;

namespace hadis
{
    public partial class Ayarlar : ContentPage
    {
        private readonly hadis.Services.IImageService _imageService;

        public Ayarlar(hadis.Services.IImageService imageService)
        {
            InitializeComponent();
            _imageService = imageService;
            UpdateVersionInfo();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        private void UpdateVersionInfo()
        {
            VersionLabel.Text = $"Versiyon: {AppInfo.VersionString}";
            CopyrightLabel.Text = $"© {DateTime.Now.Year} Namaz Vakti Uygulaması";
        }

        private async void TemaButton_Clicked(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new TemaAyarlari());
        }

        private async void BildirimButton_Clicked(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new BildirimAyarlari());
        }

        private async void WidgetButton_Clicked(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new WidgetAyarlari());
        }

        private async void KonumButton_Clicked(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new SehirSecim());
        }

        protected override bool OnBackButtonPressed()
        {
            // Geri tuşuna basıldığında Ana Sayfaya (Vakitler Sekmesine) git
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("//MainPage");
            });
            return true; // Olayı biz yönettik
        }
    }
}

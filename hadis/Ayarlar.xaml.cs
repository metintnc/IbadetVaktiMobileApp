using hadis.Helpers;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Extensions.DependencyInjection;

namespace hadis
{
    public partial class Ayarlar : ContentPage
    {
        private readonly hadis.Services.IImageService _imageService;
        private readonly IServiceProvider _serviceProvider;

        public Ayarlar(hadis.Services.IImageService imageService, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _imageService = imageService;
            _serviceProvider = serviceProvider;
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
            var page = _serviceProvider.GetRequiredService<TemaAyarlari>();
            await Navigation.PushAsync(page);
        }

        private async void BildirimButton_Clicked(object sender, TappedEventArgs e)
        {
            var page = _serviceProvider.GetRequiredService<BildirimAyarlari>();
            await Navigation.PushAsync(page);
        }

        private async void WidgetButton_Clicked(object sender, TappedEventArgs e)
        {
            await DisplayAlert("Bilgi", "Widget özelliği yapım aşamasındadır. Yakında eklenecektir!", "Tamam");
        }

        private async void KonumButton_Clicked(object sender, TappedEventArgs e)
        {
            var page = _serviceProvider.GetRequiredService<SehirSecim>();
            await Navigation.PushAsync(page);
        }

        private async void GizlilikPolitikasi_Tapped(object sender, TappedEventArgs e)
        {
            // TODO: Kendi gizlilik politikası URL'nizi buraya ekleyin
            string privacyUrl = "https://metintnc.github.io/namazvakti-privacy";
            try
            {
                await Browser.Default.OpenAsync(privacyUrl, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", "Gizlilik politikası sayfası açılamadı.", "Tamam");
            }
        }

        private async void YakinCamilerButton_Clicked(object sender, TappedEventArgs e)
        {
            var page = _serviceProvider.GetRequiredService<YakindakiCamiler>();
            await Navigation.PushAsync(page);
        }

        private async void HicriTakvimButton_Clicked(object sender, TappedEventArgs e)
        {
            var page = _serviceProvider.GetRequiredService<HicriTakvim>();
            await Navigation.PushAsync(page);
        }

        private async void KutuphaneButton_Clicked(object sender, TappedEventArgs e)
        {
            var page = _serviceProvider.GetRequiredService<Kutuphane>();
            await Navigation.PushAsync(page);
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

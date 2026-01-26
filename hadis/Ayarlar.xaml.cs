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

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadBackground();
        }

        private async Task LoadBackground()
        {
            try
            {
                string imageName = Application.Current.RequestedTheme == AppTheme.Dark ? "ayarlararkaplan.png" : "bg_light.jpg";
                BackgroundImage.Source = await _imageService.GetOptimizedBackgroundImageAsync(imageName);
                BackgroundImage.IsVisible = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ayarlar Background Load Error: {ex.Message}");
            }
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

        private async void ClearCacheButton_Clicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Önbellek Temizle", "Uygulama önbelleğini temizlemek istiyor musunuz?", "Evet", "Hayır");
            if (answer)
            {
                // Önbellek temizleme işlemleri
                Preferences.Default.Remove("KuranSonSureNo");
                Preferences.Default.Remove("KuranSonAyetNo");
                await DisplayAlert("Başarılı", "Önbellek temizlendi.", "Tamam");
            }
        }

        private async void ResetSettingsButton_Clicked(object sender, EventArgs e)
        {
            bool answer = await DisplayAlert("Ayarları Sıfırla", "Tüm ayarlar varsayılan değerlere dönecek. Emin misiniz?", "Evet", "Hayır");
            if (answer)
            {
                Preferences.Default.Clear();
                await DisplayAlert("Başarılı", "Ayarlar sıfırlandı.", "Tamam");
            }
        }
    }
}

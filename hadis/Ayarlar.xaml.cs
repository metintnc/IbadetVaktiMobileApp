using System.Text.Json;
using hadis.Helpers;
using hadis.Models;
using hadis.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace hadis
{
    public partial class Ayarlar : ContentPage
    {
        private readonly IImageService _imageService;
        private readonly BackgroundService _backgroundService;
        private readonly IServiceProvider _serviceProvider;
        private string? _currentImageName;

        public Ayarlar(
            IImageService imageService,
            BackgroundService backgroundService,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _imageService = imageService;
            _backgroundService = backgroundService;
            _serviceProvider = serviceProvider;

            UpdateVersionInfo();
            InitializeBackgroundSync();
        }

        private void InitializeBackgroundSync()
        {
            try
            {
                string savedTheme = Preferences.Default.Get(AppConstants.PREF_APP_THEME, AppConstants.THEME_SYSTEM);

                if (savedTheme == AppConstants.THEME_SYSTEM || savedTheme.StartsWith("Main"))
                {
                    var now = DateTime.Now;
                    var info = TimeBasedBackgroundConfig.GetBackgroundForTime(now.Hour, now.Minute);
                    BackgroundImage.Source = info.Image;
                    _currentImageName = info.Image;
                }

                ApplyTimeBasedBackground();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ayarlar Init Background Error: {ex.Message}");
            }
        }

        private void ApplyTimeBasedBackground()
        {
            string savedTheme = Preferences.Default.Get(AppConstants.PREF_APP_THEME, AppConstants.THEME_SYSTEM);

            if (savedTheme == AppConstants.THEME_CUSTOM)
            {
                string customThemeJson = Preferences.Default.Get(AppConstants.PREF_CUSTOM_THEME, string.Empty);
                if (!string.IsNullOrEmpty(customThemeJson))
                {
                    try
                    {
                        var theme = JsonSerializer.Deserialize<CustomTheme>(customThemeJson);
                        if (theme != null && !string.IsNullOrEmpty(theme.BackgroundImage))
                        {
                            _backgroundService.ApplyCustomBackground(BackgroundImage, BackgroundOverlay, theme.BackgroundImage);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ayarlar Custom Tema Hatası: {ex.Message}");
                    }
                }
            }

            var result = _backgroundService.SetTimeBasedBackground(BackgroundImage, BackgroundOverlay, savedTheme, _currentImageName);
            if (!string.IsNullOrEmpty(result.ImageName))
            {
                _currentImageName = result.ImageName;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ApplyTimeBasedBackground();
            UpdateLocationSummary();
        }

        private void UpdateLocationSummary()
        {
            bool otomatik = Preferences.Default.Get("OtomatikKonum", false);
            string sehir = Preferences.Default.Get("ManuelSehir", "");
            string ilce = Preferences.Default.Get("ManuelIlce", "");

            string konumText;
            if (!string.IsNullOrWhiteSpace(ilce) && !ilce.Equals(sehir, StringComparison.OrdinalIgnoreCase))
            {
                konumText = $"{ilce}, {sehir}";
            }
            else if (!string.IsNullOrWhiteSpace(sehir))
            {
                konumText = sehir;
            }
            else
            {
                konumText = "Seçilmedi";
            }

            SeciliKonumLabel.Text = konumText;
            HeaderLocationLabel.Text = konumText == "Seçilmedi" ? "Konum Seç" : konumText;
            GpsStatusLabel.Text = otomatik ? "Otomatik GPS" : "Manuel Seçim";
        }

        private void UpdateVersionInfo()
        {
            VersionLabel.Text = $"İbadet Vakti • Versiyon {AppInfo.VersionString}";
            CopyrightLabel.Text = $"© {DateTime.Now.Year} İbadet Vakti Uygulaması";
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
            var page = _serviceProvider.GetRequiredService<KonumPage>();
            await Navigation.PushAsync(page);
        }

        private async void GizlilikPolitikasi_Tapped(object sender, TappedEventArgs e)
        {
            string privacyUrl = "https://metintnc.github.io/namazvakti-privacy";
            try
            {
                await Browser.Default.OpenAsync(privacyUrl, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception)
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

        private async void KuranButton_Clicked(object sender, TappedEventArgs e)
        {
            var page = _serviceProvider.GetRequiredService<Kuran>();
            await Navigation.PushAsync(page);
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

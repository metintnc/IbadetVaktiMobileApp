using hadis.Models;
using hadis.Services;
using System.Linq;

namespace hadis
{
    public partial class Kuran : ContentPage
    {
        private List<Sure> _tumSureler;
        private List<Sure> _filtreSureler;
        private readonly StatusBarService _statusBarService;
        private readonly TabBarService _tabBarService;

        private readonly IImageService _imageService;

        public Kuran(StatusBarService statusBarService, TabBarService tabBarService, IImageService imageService)
        {
            InitializeComponent();
            _statusBarService = statusBarService;
            _tabBarService = tabBarService;
            _imageService = imageService;
            _tumSureler = KuranDataService.GetSureler();
            _filtreSureler = _tumSureler;
            SureListesi.ItemsSource = _filtreSureler;
            SonOkunanYukle();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadBackground();
            _statusBarService.SetStatusBarColor("#000000");
            _tabBarService.SetTabBarColor("#000000");
            SonOkunanYukle();
        }

        private async Task LoadBackground()
        {
            try
            {
                string imageName = Application.Current.RequestedTheme == AppTheme.Dark ? "kuranarkaplan.png" : "bg_light.jpg";
                BackgroundImage.Source = await _imageService.GetOptimizedBackgroundImageAsync(imageName);
                BackgroundImage.IsVisible = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kuran Background Load Error: {ex.Message}");
            }
        }

        private void SonOkunanYukle()
        {
            var sonSureNo = Preferences.Default.Get("KuranSonSureNo", 0);
            var sonAyet = Preferences.Default.Get("KuranSonAyetNo", 1);

            var sure = KuranDataService.GetSureByNo(sonSureNo);
            if (sure != null && sonSureNo > 0)
            {
                SonOkunanFrame.IsVisible = true;
                SonOkunanSureLabel.Text = sure.Ad;
                SonOkunanAyetLabel.Text = $"Ayet {sonAyet}";
                // Yüzdeyi hesapla ve label'a yaz
                int toplamAyet = sure.AyetSayisi > 0 ? sure.AyetSayisi : 1;
                int yuzde = (int)Math.Round((double)sonAyet * 100 / toplamAyet);
                SonOkunanAyetYuzdeLabel.Text = $"%{yuzde}";
            }
            else
            {
                SonOkunanFrame.IsVisible = false;
            }
        }

        private async void OkumayaDevamEt_Clicked(object sender, EventArgs e)
        {
            var sonSureNo = Preferences.Default.Get("KuranSonSureNo", 0);
            if (sonSureNo > 0)
            {
                await Navigation.PushAsync(new SurePage(sonSureNo));
            }
            else
            {
                await DisplayAlert("Uyarı", "Son okunan sure bulunamadı.", "Tamam");
            }
        }

        private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            var aramaMetni = e.NewTextValue?.ToLower() ?? "";
            if (string.IsNullOrWhiteSpace(aramaMetni))
            {
                _filtreSureler = _tumSureler;
            }
            else
            {
                _filtreSureler = _tumSureler.Where(s =>
                    s.Ad.ToLower().Contains(aramaMetni) ||
                    s.SureNo.ToString().Contains(aramaMetni) ||
                    s.Inis.ToLower().Contains(aramaMetni)
                ).ToList();
            }
            SureListesi.ItemsSource = _filtreSureler;
        }

        private async void SureItem_Tapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is int sureNo)
            {
                Preferences.Default.Set("KuranSonSureNo", sureNo);
                Preferences.Default.Set("KuranSonAyetNo", 1); // ilk ayet
                SonOkunanYukle();
                await Navigation.PushAsync(new SurePage(sureNo));
            }
        }

        private async void SureOku_Clicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int sureNo)
            {
                Preferences.Default.Set("KuranSonSureNo", sureNo);
                Preferences.Default.Set("KuranSonAyetNo", 1); // ilk ayet
                SonOkunanYukle();
                await Navigation.PushAsync(new SurePage(sureNo));
            }
        }

        private async void KaydedilenlerButonu_Clicked(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new KaydedilenlerPage());
        }
    }
}

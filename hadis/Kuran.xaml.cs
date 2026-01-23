using hadis.Models;
using hadis.Services;

namespace hadis
{
    public partial class Kuran : ContentPage
    {
        private List<Sure> _tumSureler;
        private List<Sure> _filtreSureler;

        public Kuran()
        {
            InitializeComponent();
            _tumSureler = KuranDataService.GetSureler();
            _filtreSureler = _tumSureler;
            SureListesi.ItemsSource = _filtreSureler;
            
            SonOkunanYukle();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Son okunan yükleme iþlemini arka planda baþlat
            Task.Run(() =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SonOkunanYukle();
                });
            });
        }

        private void SonOkunanYukle()
        {
            var sonSureNo = Preferences.Default.Get("KuranSonSureNo", 0);
            var sonSayfa = Preferences.Default.Get("KuranSonSayfa", 1);

            if (sonSureNo > 0)
            {
                var sure = KuranDataService.GetSureByNo(sonSureNo);
                if (sure != null)
                {
                    SonOkunanFrame.IsVisible = true;
                    SonOkunanSureLabel.Text = sure.Ad;
                    SonOkunanAyetLabel.Text = $"Sayfa {sonSayfa}";
                }
            }
        }

        private async void OkumayaDevamEt_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//KuranPDF");
        }

        private async void Cuzler_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert("Cuzler", "Cuzler sayfasi yakinda eklenecek!", "Tamam");
        }

        private void Sureler_Clicked(object sender, EventArgs e)
        {
            SearchBar.Text = "";
            SearchBar.Focus();
        }

        private async void Favoriler_Clicked(object sender, EventArgs e)
        {
            var favoriler = KuranDataService.GetFavorites();
            if (favoriler.Count == 0)
            {
                await DisplayAlert("Favoriler", "Henuz favori sureniz yok!", "Tamam");
            }
            else
            {
                _filtreSureler = favoriler;
                SureListesi.ItemsSource = _filtreSureler;
            }
        }

        private async void Konular_Clicked(object sender, EventArgs e)
        {
            await DisplayAlert("Konular", "Konular sayfasi yakinda eklenecek!", "Tamam");
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
                var sure = KuranDataService.GetSureByNo(sureNo);
                if (sure != null)
                {
                    Preferences.Default.Set("KuranSonSureNo", sureNo);
                    Preferences.Default.Set("KuranSonSayfa", sure.BaslangicSayfasi);
                    
                    await Shell.Current.GoToAsync("//KuranPDF");
                }
            }
        }

        private async void SureOku_Clicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is int sureNo)
            {
                var sure = KuranDataService.GetSureByNo(sureNo);
                if (sure != null)
                {
                    Preferences.Default.Set("KuranSonSureNo", sureNo);
                    Preferences.Default.Set("KuranSonSayfa", sure.BaslangicSayfasi);
                    
                    await Shell.Current.GoToAsync("//KuranPDF");
                }
            }
        }
    }
}

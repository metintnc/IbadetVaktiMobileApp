using hadis.Models;
using hadis.Services;
using System.Linq;

namespace hadis
{
    public partial class Kuran : ContentPage
    {
        private List<Sure> _tumSureler = new();
        private List<Sure> _filtreSureler = new();
        private readonly StatusBarService _statusBarService;
        private readonly TabBarService _tabBarService;
        private readonly IImageService _imageService;
        private bool _isInitialized = false;

        public Kuran(StatusBarService statusBarService, TabBarService tabBarService, IImageService imageService)
        {
            InitializeComponent();
            _statusBarService = statusBarService;
            _tabBarService = tabBarService;
            _imageService = imageService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _statusBarService.SetStatusBarColor("#000000"); // Hızlı, UI thread'i bloklamaz
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            if (!_isInitialized)
            {
                _isInitialized = true;
                // Ağır senkron işlemi arka plan thread'inde çalıştır
                await Task.Run(() => _tumSureler = KuranDataService.GetSureler());
                _filtreSureler = _tumSureler;
                SureListesi.ItemsSource = _filtreSureler;
                CheckDownloadStatus();
            }

            // Her gezinişte taze 'son okunan' verisini göster (Preferences okuma hızlı)
            SonOkunanYukle();
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

        private void CheckDownloadStatus()
        {
            var service = new QuranApiService();
            bool isDownloaded = service.CheckCacheStatus();
            UpdateDownloadUI(isDownloaded);
        }

        private void UpdateDownloadUI(bool isDownloaded)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (isDownloaded)
                {
                    DownloadButton.Text = "İndirildi (İnternetsiz Kullanıma Hazır) ✅";
                    DownloadButton.IsEnabled = false; // Tekrar indirmeye gerek yok
                    DownloadButton.BackgroundColor = Colors.Gray;
                    DownloadStatusLabel.Text = "Tüm sureler cihazınıza kaydedildi.";
                    DownloadIndicator.IsVisible = false;
                    DownloadIndicator.IsRunning = false;
                    
                    // İndirme tamamlandığında kısayol ikonunu gizle
                    if (ScrollToBottomIcon != null)
                        ScrollToBottomIcon.IsVisible = false;
                }
                else
                {
                    DownloadButton.Text = "İnternetsiz Kullanım İçin İndir 📥";
                    DownloadButton.IsEnabled = true;
                    DownloadButton.BackgroundColor = Colors.Teal; // Safe fallback
                    DownloadStatusLabel.Text = "Kur'anı indirerek internetsiz okuyabilirsiniz (~10MB)";
                    DownloadIndicator.IsVisible = false;
                    DownloadIndicator.IsRunning = false;
                    
                    if (ScrollToBottomIcon != null)
                        ScrollToBottomIcon.IsVisible = true;
                }
            });
        }

        private async void ScrollToBottom_Clicked(object sender, EventArgs e)
        {
            if (_filtreSureler != null && _filtreSureler.Count > 0)
            {
                // Listeyi en sona kaydır (Footer'ı görmek için son elemana kaydırıyoruz)
                // Position.Start son elemanı en üste alır, böylece altındaki footer görünür olur.
                SureListesi.ScrollTo(_filtreSureler.Count - 1, position: ScrollToPosition.Start, animate: true);
            }
        }

        private async void DownloadButton_Clicked(object sender, EventArgs e)
        {
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await DisplayAlert("İnternet Yok", "İndirme yapabilmek için internet bağlantısı gereklidir.", "Tamam");
                return;
            }

            // Başlangıç Durumu
            DownloadButton.IsEnabled = false;
            DownloadButton.Text = "İndiriliyor...";
            DownloadIndicator.IsVisible = true;
            DownloadIndicator.IsRunning = true;

            var service = new QuranApiService();
            var progress = new Progress<string>(message => 
            {
                MainThread.BeginInvokeOnMainThread(() => DownloadStatusLabel.Text = message);
            });

            try
            {
                await Task.Run(async () => await service.DownloadAndCacheFullQuranAsync(progress));
                
                // Başarılı
                UpdateDownloadUI(true);
                await DisplayAlert("Tamamlandı", "Kur'an verileri başarıyla indirildi. Artık internetsiz de okuyabilirsiniz.", "Tamam");
            }
            catch (Exception ex)
            {
                // Hata
                MainThread.BeginInvokeOnMainThread(() => 
                {
                    DownloadStatusLabel.Text = "Hata oluştu, tekrar deneyin.";
                    DownloadButton.Text = "Tekrar Dene 📥";
                    DownloadButton.IsEnabled = true;
                    DownloadIndicator.IsVisible = false;
                    DownloadIndicator.IsRunning = false;
                });
                await DisplayAlert("Hata", $"İndirme sırasında bir sorun oluştu: {ex.Message}", "Tamam");
            }
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

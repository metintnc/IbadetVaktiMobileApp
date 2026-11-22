using hadis.Models;
using hadis.Services;
using System.Linq;

namespace hadis
{
    public class CuzItem
    {
        public int CuzNo { get; set; }
        public int BaslangicSayfasi { get; set; }
        public string Baslik => $"{CuzNo}. Cüz";
        public string Aciklama { get; set; } = "";
    }

    public partial class Kuran : ContentPage
    {
        private IReadOnlyList<Sure> _tumSureler = Array.Empty<Sure>();
        private List<Sure> _filtreSureler = new();
        private List<CuzItem> _tumCuzler = new();
        private List<CuzItem> _filtreCuzler = new();
        private string _activeTab = "Sureler";

        private readonly StatusBarService _statusBarService;
        private readonly QuranApiService _quranApiService;
        private bool _isInitialized = false;

        private static readonly Dictionary<string, string> MealIsimleri = new()
        {
            { "11", "Diyanet İşleri" },
            { "14", "Elmalılı Hamdi Yazır (Orijinal)" },
            { "15", "Elmalılı Hamdi Yazır (Sadeleştirilmiş)" },
            { "27", "Süleyman Ateş" },
            { "8", "Bayraktar Bayraklı" }
        };

        public Kuran(StatusBarService statusBarService, QuranApiService quranApiService)
        {
            InitializeComponent();
            _statusBarService = statusBarService;
            _quranApiService = quranApiService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _statusBarService.SetStatusBarColor("#000000");
            Shell.SetTabBarIsVisible(this, false);

            if (!_isInitialized)
            {
                _isInitialized = true;
                _tumSureler = KuranDataService.GetSureler();
                _filtreSureler = _tumSureler.ToList();
                SureListesi.ItemsSource = _filtreSureler;

                BuildCuzler();
                CheckDownloadStatus();
            }

            SonOkunanYukle();

            var authorId = Preferences.Default.Get("MealAuthorId", "11");
            var mealAdi = MealIsimleri.TryGetValue(authorId, out var name) ? name : "Diyanet İşleri";
            AktifMealLabel.Text = $"Aktif: {mealAdi}";
            AktifMealTabLabel.Text = mealAdi;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Shell.SetTabBarIsVisible(this, true);
        }

        private void BuildCuzler()
        {
            var list = new List<CuzItem>(30);
            for (int cuz = 1; cuz <= 30; cuz++)
            {
                int startPage = cuz == 1 ? 1 : (cuz - 1) * 20 + 2;
                var sure = KuranDataService.GetSureFromPage(startPage);
                list.Add(new CuzItem
                {
                    CuzNo = cuz,
                    BaslangicSayfasi = startPage,
                    Aciklama = $"Sayfa {startPage} • {sure.Ad} Suresi"
                });
            }
            _tumCuzler = list;
            _filtreCuzler = list;
            CuzListesi.ItemsSource = _filtreCuzler;
        }

        private void SonOkunanYukle()
        {
            var sonSureNo = Preferences.Default.Get("KuranSonSureNo", 0);
            var sonAyet = Preferences.Default.Get("KuranSonAyetNo", 1);

            // Henüz okuma yapılmadıysa Fâtiha 1. Ayet ile göster
            if (sonSureNo <= 0)
            {
                sonSureNo = 1;
                sonAyet = 1;
            }

            var sure = KuranDataService.GetSureByNo(sonSureNo) ?? _tumSureler.FirstOrDefault();
            if (sure != null)
            {
                SonOkunanFrame.IsVisible = true;
                SonOkunanSureLabel.Text = $"{sure.Ad} Suresi";
                SonOkunanArapcaLabel.Text = sure.AdArapca;
                SonOkunanInisLabel.Text = sure.InisKisa;

                int sayfa = KuranDataService.GetBaslangicSayfasi(sure.SureNo);
                int cuz = KuranDataService.GetCuzNo(sayfa);
                SonOkunanAyetLabel.Text = $"{sonAyet}. Ayet • {cuz}. Cüz, Sayfa {sayfa}";

                int toplamAyet = sure.AyetSayisi > 0 ? sure.AyetSayisi : 1;
                int yuzde = (int)Math.Round((double)sonAyet * 100 / toplamAyet);
                SonOkunanYuzdeLabel.Text = yuzde > 1
                    ? $"Kaldığın yerden devam et (%{yuzde})"
                    : "Kaldığın yerden devam et";
            }
        }

        private async void OkumayaDevamEt_Clicked(object sender, EventArgs e)
        {
            var sonSureNo = Preferences.Default.Get("KuranSonSureNo", 1);
            if (sonSureNo <= 0) sonSureNo = 1;
            await Navigation.PushAsync(new SurePage(sonSureNo, _quranApiService));
        }

        private void SearchEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            var aramaMetni = e.NewTextValue?.Trim().ToLower() ?? "";

            if (_activeTab == "Cuzler")
            {
                _filtreCuzler = string.IsNullOrWhiteSpace(aramaMetni)
                    ? _tumCuzler
                    : _tumCuzler.Where(c =>
                        c.Baslik.ToLower().Contains(aramaMetni) ||
                        c.Aciklama.ToLower().Contains(aramaMetni) ||
                        c.CuzNo.ToString().Contains(aramaMetni)).ToList();
                CuzListesi.ItemsSource = _filtreCuzler;
                return;
            }

            if (string.IsNullOrWhiteSpace(aramaMetni))
            {
                _filtreSureler = _tumSureler.ToList();
            }
            else
            {
                _filtreSureler = _tumSureler.Where(s =>
                    s.Ad.ToLower().Contains(aramaMetni) ||
                    s.SureNo.ToString().Contains(aramaMetni) ||
                    s.Inis.ToLower().Contains(aramaMetni) ||
                    s.InisKisa.ToLower().Contains(aramaMetni)
                ).ToList();
            }
            SureListesi.ItemsSource = _filtreSureler;
        }

        private void SegmentTab_Clicked(object sender, TappedEventArgs e)
        {
            if (e.Parameter is not string tab || tab == _activeTab) return;
            SetActiveSegmentTab(tab);
        }

        private void SetActiveSegmentTab(string tab)
        {
            _activeTab = tab;

            var inactiveText = Color.FromArgb("#B3FFFFFF");
            var activeBg = Color.FromArgb("#4DFFFFFF");
            var activeStroke = Color.FromArgb("#33FFFFFF");

            TabSureler.BackgroundColor = tab == "Sureler" ? activeBg : Colors.Transparent;
            TabSureler.Stroke = tab == "Sureler" ? activeStroke : Colors.Transparent;
            TabSureler.StrokeThickness = tab == "Sureler" ? 1 : 0;
            LblTabSureler.TextColor = tab == "Sureler" ? Colors.White : inactiveText;
            LblTabSureler.FontAttributes = tab == "Sureler" ? FontAttributes.Bold : FontAttributes.None;

            TabCuzler.BackgroundColor = tab == "Cuzler" ? activeBg : Colors.Transparent;
            TabCuzler.Stroke = tab == "Cuzler" ? activeStroke : Colors.Transparent;
            TabCuzler.StrokeThickness = tab == "Cuzler" ? 1 : 0;
            LblTabCuzler.TextColor = tab == "Cuzler" ? Colors.White : inactiveText;
            LblTabCuzler.FontAttributes = tab == "Cuzler" ? FontAttributes.Bold : FontAttributes.None;

            TabMealFavori.BackgroundColor = tab == "MealFavori" ? activeBg : Colors.Transparent;
            TabMealFavori.Stroke = tab == "MealFavori" ? activeStroke : Colors.Transparent;
            TabMealFavori.StrokeThickness = tab == "MealFavori" ? 1 : 0;
            LblTabMealFavori.TextColor = tab == "MealFavori" ? Colors.White : inactiveText;
            LblTabMealFavori.FontAttributes = tab == "MealFavori" ? FontAttributes.Bold : FontAttributes.None;

            SureListesi.IsVisible = tab == "Sureler";
            CuzListesi.IsVisible = tab == "Cuzler";
            MealFavoriView.IsVisible = tab == "MealFavori";
        }

        private async void QuickSure_Clicked(object sender, TappedEventArgs e)
        {
            if (e.Parameter is string str && int.TryParse(str, out int sureNo))
            {
                Preferences.Default.Set("KuranSonSureNo", sureNo);
                Preferences.Default.Set("KuranSonAyetNo", 1);
                SonOkunanYukle();
                await Navigation.PushAsync(new SurePage(sureNo, _quranApiService));
            }
        }

        private void QuickAll_Clicked(object sender, TappedEventArgs e)
        {
            SearchEntry.Text = string.Empty;
            SetActiveSegmentTab("Sureler");
        }

        private async void SureItem_Tapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is int sureNo)
            {
                Preferences.Default.Set("KuranSonSureNo", sureNo);
                Preferences.Default.Set("KuranSonAyetNo", 1);
                SonOkunanYukle();
                await Navigation.PushAsync(new SurePage(sureNo, _quranApiService));
            }
        }

        private async void CuzItem_Tapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is int startPage)
            {
                await Navigation.PushAsync(new KuranOkumaPage(_quranApiService, startPage));
            }
        }

        private async void MushafAc_Clicked(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new KuranOkumaPage(_quranApiService, 1));
        }

        private async void KaydedilenlerButonu_Clicked(object sender, TappedEventArgs e)
        {
            await Navigation.PushAsync(new KaydedilenlerPage(_quranApiService));
        }

        private void CheckDownloadStatus()
        {
            bool isDownloaded = _quranApiService.CheckCacheStatus();
            UpdateDownloadUI(isDownloaded);
        }

        private void UpdateDownloadUI(bool isDownloaded)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (isDownloaded)
                {
                    DownloadButton.Text = "İndirildi (İnternetsiz Kullanıma Hazır) ✅";
                    DownloadButton.IsEnabled = false;
                    DownloadButton.BackgroundColor = Color.FromArgb("#33FFFFFF");
                    DownloadButton.TextColor = Colors.White;
                    DownloadStatusLabel.Text = "Tüm sureler cihazınıza kaydedildi.";
                    DownloadIndicator.IsVisible = false;
                    DownloadIndicator.IsRunning = false;
                }
                else
                {
                    DownloadButton.Text = "İnternetsiz Kullanım İçin İndir 📥";
                    DownloadButton.IsEnabled = true;
                    DownloadButton.BackgroundColor = Color.FromArgb("#22D3EE");
                    DownloadButton.TextColor = Color.FromArgb("#082F49");
                    DownloadStatusLabel.Text = "Kur'anı indirerek internetsiz okuyabilirsiniz (~10MB)";
                    DownloadIndicator.IsVisible = false;
                    DownloadIndicator.IsRunning = false;
                }
            });
        }

        private async void DownloadButton_Clicked(object sender, EventArgs e)
        {
            if (Connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                await DisplayAlert("İnternet Yok", "İndirme yapabilmek için internet bağlantısı gereklidir.", "Tamam");
                return;
            }

            DownloadButton.IsEnabled = false;
            DownloadButton.Text = "İndiriliyor...";
            DownloadIndicator.IsVisible = true;
            DownloadIndicator.IsRunning = true;

            var progress = new Progress<string>(message =>
            {
                MainThread.BeginInvokeOnMainThread(() => DownloadStatusLabel.Text = message);
            });

            try
            {
                await Task.Run(async () => await _quranApiService.DownloadAndCacheFullQuranAsync(progress));

                UpdateDownloadUI(true);
                await DisplayAlert("Tamamlandı", "Kur'an verileri başarıyla indirildi. Artık internetsiz de okuyabilirsiniz.", "Tamam");
            }
            catch (Exception ex)
            {
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
            if (MealSelectionOverlay.IsVisible)
            {
                MealSelectionOverlay.IsVisible = false;
                return true;
            }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Navigation.PopAsync();
            });
            return true;
        }

        private void MealAyarlar_Clicked(object sender, EventArgs e)
        {
            var authorId = Preferences.Default.Get("MealAuthorId", "11");
            AktifMealLabel.Text = $"Aktif: {(MealIsimleri.TryGetValue(authorId, out var name) ? name : "Diyanet İşleri")}";
            MealSelectionOverlay.IsVisible = true;
        }

        private void OnCancelMealSelection_Clicked(object sender, EventArgs e)
        {
            MealSelectionOverlay.IsVisible = false;
        }

        private async void OnSelectMeal_Clicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is string authorId)
            {
                var currentAuthor = Preferences.Default.Get("MealAuthorId", "11");
                if (currentAuthor == authorId)
                {
                    MealSelectionOverlay.IsVisible = false;
                    return;
                }

                Preferences.Default.Set("MealAuthorId", authorId);
                var mealName = MealIsimleri.TryGetValue(authorId, out var name) ? name : "Bilinmiyor";
                AktifMealLabel.Text = $"Aktif: {mealName}";
                AktifMealTabLabel.Text = mealName;

                MealSelectionOverlay.IsVisible = false;
                CheckDownloadStatus();

                await DisplayAlert("Meal Değiştirildi", $"Meal: {mealName}\n\nSureleri açtığınızda yeni mealle yüklenecektir.", "Tamam");
            }
        }
    }
}

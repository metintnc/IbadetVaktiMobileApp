using System.Collections.ObjectModel;

namespace hadis
{
    public partial class KuranOkumaPage : ContentPage
    {
        private const int TotalPages = 604;
        private bool _isOverlayVisible = false;
        private ObservableCollection<string> _pages = new();
        private int _currentPageNumber = 1;
        private bool _initialPageLoaded = false;

        public KuranOkumaPage(int startPage = 1)
        {
            InitializeComponent();
            ShowLoadingOverlay();
            InitializePages();
            
            // If starting from default (1), try to load last saved page
            int targetPage = startPage;
            if (startPage == 1 && Preferences.ContainsKey("LastReadPage"))
            {
                targetPage = Preferences.Get("LastReadPage", 1);
            }

            // Set initial position (0-indexed)
            PageCarousel.Position = targetPage - 1;
            UpdatePageLabel(targetPage);
            UpdateTitle(targetPage);
            UpdateLoadingProgress(targetPage);
        }

        private void InitializePages()
        {
            // Standard Madini Mushaf has 604 pages
            for (int i = 1; i <= TotalPages; i++)
            {
                // GitHub üzerinden kullanıcının kendi repository'sindeki resimleri çeker
                _pages.Add($"https://raw.githubusercontent.com/metintnc/NamazVaktiMobileApp/main/hadis/kuransayfalar%C4%B1/kuran111-g%C3%B6r%C3%BCnt%C3%BCler-{i}.jpg");
            }
            PageCarousel.ItemsSource = _pages;
        }

        private void OnPositionChanged(object sender, PositionChangedEventArgs e)
        {
            int pageNumber = e.CurrentPosition + 1;
            _currentPageNumber = pageNumber;
            _initialPageLoaded = false;
            UpdatePageLabel(pageNumber);
            UpdateTitle(pageNumber);
            UpdateLoadingProgress(pageNumber);
            ShowLoadingOverlay();
            
            // Save progress
            Preferences.Set("LastReadPage", pageNumber);
        }

        private void UpdateLoadingProgress(int pageNumber)
        {
            var progress = TotalPages > 0 ? (double)pageNumber / TotalPages : 0;
            LoadingProgressLabel.Text = $"Sayfa {pageNumber} / {TotalPages}";
            LoadingProgressBar.Progress = Math.Clamp(progress, 0.0, 1.0);
        }

        private void ShowLoadingOverlay()
        {
            LoadingOverlay.IsVisible = true;
            LoadingSpinner.IsRunning = true;
            LoadingMessageLabel.Text = "Arapça Kur'an yükleniyor...";
            UpdateLoadingProgress(_currentPageNumber);
        }

        private void HideLoadingOverlay()
        {
            LoadingOverlay.IsVisible = false;
            LoadingSpinner.IsRunning = false;
        }

        private void OnPageImageLoaded(object sender, EventArgs e)
        {
            if (sender is not Image image)
            {
                return;
            }

            var imageSource = image.BindingContext as string;
            var expectedSuffix = $"-{_currentPageNumber}.jpg";

            if (string.IsNullOrWhiteSpace(imageSource) || !imageSource.EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (image.IsLoading)
            {
                ShowLoadingOverlay();
                return;
            }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (!_initialPageLoaded)
                {
                    _initialPageLoaded = true;
                    await Task.Delay(100);
                }

                HideLoadingOverlay();
            });
        }

        private void UpdatePageLabel(int pageNumber)
        {
            PageNumberLabel.Text = $"Sayfa {pageNumber}";
        }

        private void UpdateTitle(int pageNumber)
        {
            var sure = Services.KuranDataService.GetSureFromPage(pageNumber);
            int juz = Services.KuranDataService.GetCuzNo(pageNumber);

            if (sure != null)
            {
                // Update Overlay Labels
                SurahNameLabel.Text = $"{sure.Ad} Sûresi";
                JuzLabel.Text = $"{juz}. Cüz";
                
                // Update Window Title (keeping it too just in case)
                this.Title = $"{sure.Ad} - {juz}. Cüz";
            }
        }

        private void OnPageTapped(object sender, EventArgs e)
        {
            _isOverlayVisible = !_isOverlayVisible;
            OverlayGrid.IsVisible = _isOverlayVisible;
            OverlayGrid.InputTransparent = !_isOverlayVisible; // allow clicking buttons when visible
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetTabBarIsVisible(this, false);
            // On Android, we might want to hide status bar for immersive experience
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Shell.SetTabBarIsVisible(this, true);
        }
    }
}

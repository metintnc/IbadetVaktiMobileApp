using System.Collections.ObjectModel;

namespace hadis
{
    public partial class KuranOkumaPage : ContentPage
    {
        private bool _isOverlayVisible = false;
        private ObservableCollection<string> _pages;

        public KuranOkumaPage(int startPage = 1)
        {
            InitializeComponent();
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
        }

        private void InitializePages()
        {
            _pages = new ObservableCollection<string>();
            // Standard Madini Mushaf has 604 pages
            for (int i = 1; i <= 604; i++)
            {
                // Using android.quran.com source
                // Format: https://android.quran.com/data/width_1024/page{0:D3}.png
                _pages.Add($"https://android.quran.com/data/width_1024/page{i:D3}.png");
            }
            PageCarousel.ItemsSource = _pages;
        }

        private void OnPositionChanged(object sender, PositionChangedEventArgs e)
        {
            int pageNumber = e.CurrentPosition + 1;
            UpdatePageLabel(pageNumber);
            UpdateTitle(pageNumber);
            
            // Save progress
            Preferences.Set("LastReadPage", pageNumber);
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
            
            // Should hide status bar? Maybe later.
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

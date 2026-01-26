using hadis.Models;
using hadis.Services;
using System.Text.Json;
using System.Collections.ObjectModel;

namespace hadis
{
    public partial class KaydedilenlerPage : ContentPage
    {
        private readonly IImageService _imageService;
        private ObservableCollection<SavedAyah> _savedAyahs;

        public KaydedilenlerPage()
        {
            InitializeComponent();
            _imageService = new PlatformImageService();
            LoadSavedAyahs();
        }

        public KaydedilenlerPage(IImageService imageService)
        {
            InitializeComponent();
            _imageService = imageService;
            LoadSavedAyahs();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            LoadSavedAyahs(); // Refresh list on appearing in case changes occurred
            await LoadBackground();
        }

        private async Task LoadBackground()
        {
            try
            {
                if (_imageService != null)
                {
                    string imageName = Application.Current.RequestedTheme == AppTheme.Dark ? "kuranarkaplan.png" : "bg_light.jpg";
                    BackgroundImage.Source = await _imageService.GetOptimizedBackgroundImageAsync(imageName);
                    BackgroundImage.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kaydedilenler Background Load Error: {ex.Message}");
            }
        }

        private void LoadSavedAyahs()
        {
            try
            {
                string json = Preferences.Default.Get("SavedAyahs", "[]");
                var list = JsonSerializer.Deserialize<List<SavedAyah>>(json) ?? new List<SavedAyah>();
                
                // Sort by date descending
                list = list.OrderByDescending(x => x.SavedDate).ToList();
                
                _savedAyahs = new ObservableCollection<SavedAyah>(list);
                KaydedilenlerCollection.ItemsSource = _savedAyahs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading saved ayahs: {ex.Message}");
                _savedAyahs = new ObservableCollection<SavedAyah>();
                KaydedilenlerCollection.ItemsSource = _savedAyahs;
            }
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private async void OnAyahTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is SavedAyah ayah)
            {
                Preferences.Default.Set("KuranSonSureNo", ayah.SureNo);
                Preferences.Default.Set("KuranSonAyetNo", ayah.Number);
                await Navigation.PushAsync(new SurePage(ayah.SureNo));
            }
        }
    }
}

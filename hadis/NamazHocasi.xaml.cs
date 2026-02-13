using System.Text.Json;
using System.Reflection;

namespace hadis
{
    public partial class NamazHocasi : ContentPage
    {
        public NamazHocasi()
        {
            InitializeComponent();
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("namaz.json");
                using var reader = new StreamReader(stream);
                var contents = await reader.ReadToEndAsync();
                var namazlar = JsonSerializer.Deserialize<List<NamazTuru>>(contents);
                
                NamazCarousel.ItemsSource = namazlar;
            }
            catch (Exception ex)
            {
                // Fallback or error handling
                System.Diagnostics.Debug.WriteLine($"Error loading namaz data: {ex.Message}");
                await DisplayAlert("Hata", "Veri yüklenirken bir sorun oluştu.", "Tamam");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetTabBarIsVisible(this, false);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Shell.SetTabBarIsVisible(this, true);
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            if (Navigation.NavigationStack.Count > 1)
                await Navigation.PopAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            if (Navigation.NavigationStack.Count > 1)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PopAsync();
                });
                return true;
            }
            return base.OnBackButtonPressed();
        }
    }

    public class NamazTuru
    {
        public string Emoji { get; set; }
        public string Baslik { get; set; }
        public string RekatBilgisi { get; set; }
        public List<NamazAdimi> Adimlar { get; set; }
    }

    public class NamazAdimi
    {
        public string Baslik { get; set; }
        public string Aciklama { get; set; }
        public string Dua { get; set; }
        public string Okunusu { get; set; }
    }
}

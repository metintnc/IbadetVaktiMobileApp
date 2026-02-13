using System.Text.Json;
using System.Reflection;

namespace hadis
{
    public partial class Ilmihal : ContentPage
    {
        public Ilmihal()
        {
            InitializeComponent();
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("ilmihal.json");
                using var reader = new StreamReader(stream);
                var contents = await reader.ReadToEndAsync();
                var kategoriler = JsonSerializer.Deserialize<List<IlmihalKategori>>(contents);
                
                IlmihalCarousel.ItemsSource = kategoriler;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading ilmihal data: {ex.Message}");
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

    public class IlmihalKategori
    {
        public string Emoji { get; set; }
        public string Baslik { get; set; }
        public List<string> Icerikler { get; set; }
    }
}

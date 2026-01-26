using hadis.Models;
using hadis.Services;
using System.Collections.ObjectModel;

namespace hadis
{
    public partial class KaydedilenlerPage : ContentPage
    {
        private List<Sure> _tumSureler;

        public KaydedilenlerPage()
        {
            InitializeComponent();
            _tumSureler = KuranDataService.GetSureler();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadData();
        }

        private async Task LoadData()
        {
            var data = await SavedAyahsService.GetSavedAyahsAsync();
            KaydedilenlerCollection.ItemsSource = data.OrderByDescending(x => x.SavedDate).ToList();
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
             if (Navigation.NavigationStack.Count > 1)
                await Navigation.PopAsync();
        }

        private async void OnAyahTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is SavedAyah savedAyah)
            {
                var sure = _tumSureler.FirstOrDefault(s => s.SureNo == savedAyah.SureNo);
                if (sure != null && sure.AyetSayisi > 0)
                {
                    double percent = (double)(savedAyah.Number - 1) / (sure.AyetSayisi - 1);
                    Preferences.Default.Set($"KuranScrollPercent_{savedAyah.SureNo}", percent);
                }
                
                await Navigation.PushAsync(new SurePage(savedAyah.SureNo));
            }
        }
    }
}

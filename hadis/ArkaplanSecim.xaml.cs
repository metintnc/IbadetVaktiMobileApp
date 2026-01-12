namespace hadis
{
    public partial class ArkaplanSecim : ContentPage
    {
        private double _currentOpacity = 0.3;

        public ArkaplanSecim()
        {
            InitializeComponent();
            
            // Kayitli opacity degerini yukle
            _currentOpacity = Preferences.Default.Get("BackgroundOpacity", 0.3);
            OpacitySlider.Value = _currentOpacity;
        }

        private void OpacitySlider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            _currentOpacity = e.NewValue;
            OpacityLabel.Text = $"Arkaplan Seffafligi: {(int)(_currentOpacity * 100)}%";
        }

        private async void KoyuArkaplan_Clicked(object sender, EventArgs e)
        {
            await SaveAndClose("bg_dark.jpg");
        }

        private async void AcikArkaplan_Clicked(object sender, EventArgs e)
        {
            await SaveAndClose("bg_light.jpg");
        }

        private async void MaviGradient_Clicked(object sender, EventArgs e)
        {
            await SaveAndClose("gradient_blue");
        }

        private async void YesilGradient_Clicked(object sender, EventArgs e)
        {
            await SaveAndClose("gradient_green");
        }

        private async void KoyuMaviGradient_Clicked(object sender, EventArgs e)
        {
            await SaveAndClose("gradient_dark_blue");
        }

        private async void GeceMavisiGradient_Clicked(object sender, EventArgs e)
        {
            await SaveAndClose("gradient_night");
        }

        private async void TurkuazRenk_Clicked(object sender, EventArgs e)
        {
            await SaveAndClose("#00796B");
        }

        private async void KoyuGriRenk_Clicked(object sender, EventArgs e)
        {
            await SaveAndClose("#2C2C2C");
        }

        private async Task SaveAndClose(string backgroundValue)
        {
            // Arkaplan secimini gecici olarak kaydet
            Preferences.Default.Set("TempBackground", backgroundValue);
            
            // Opacity degerini kaydet
            Preferences.Default.Set("BackgroundOpacity", _currentOpacity);
            
            await DisplayAlert("Basarili", $"Arkaplan secildi (Seffaflik: {(int)(_currentOpacity * 100)}%)", "Tamam");
            await Navigation.PopAsync();
        }
    }
}

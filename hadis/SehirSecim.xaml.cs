using hadis.ViewModels;
using Microsoft.Maui.ApplicationModel;

namespace hadis
{
    public partial class SehirSecim : ContentPage
    {
        private SehirSecimViewModel _viewModel;

        public SehirSecim(SehirSecimViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
#if ANDROID
            Platform.CurrentActivity?.Window?.SetStatusBarColor(Android.Graphics.Color.Black);
#endif
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
#if ANDROID
            Platform.CurrentActivity?.Window?.SetStatusBarColor(Android.Graphics.Color.Transparent);
#endif
        }

        protected override bool OnBackButtonPressed()
        {
            // Hardware back button should follow ViewModel logic (e.g. exit district selection)
            if (_viewModel.TryHandleBack())
            {
                return true;
            }
            return base.OnBackButtonPressed();
        }
    }
}

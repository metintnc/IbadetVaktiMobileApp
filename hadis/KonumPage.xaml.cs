using System;
using Microsoft.Maui.Controls;
using hadis.ViewModels;

namespace hadis
{
    public partial class KonumPage : ContentPage
    {
        public KonumPage(KonumViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (BindingContext is KonumViewModel vm)
            {
                vm.LoadAddedLocations();
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void OnReorderCompleted(object sender, EventArgs e)
        {
            if (BindingContext is KonumViewModel vm)
            {
                vm.SaveOrder();
            }
        }
    }
}
using System.Text.Json;
using hadis.Helpers;
using hadis.Models;
using hadis.Services;

namespace hadis
{
    public partial class Kutuphane : ContentPage
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly BackgroundService _backgroundService;
        private string? _currentImageName;
        private string _activeFilter = "All";

        private static readonly Color ActiveChipBg = Color.FromArgb("#4006B6D4");
        private static readonly Color ActiveChipStroke = Color.FromArgb("#8022D3EE");
        private static readonly Color ActiveChipText = Color.FromArgb("#A5F3FC");
        private static readonly Color InactiveChipBg = Color.FromArgb("#0FFFFFFF");
        private static readonly Color InactiveChipStroke = Color.FromArgb("#1AFFFFFF");
        private static readonly Color InactiveChipText = Color.FromArgb("#CCFFFFFF");

        public Kutuphane(IServiceProvider serviceProvider, BackgroundService backgroundService)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _backgroundService = backgroundService;
            InitializeBackgroundSync();
        }

        private void InitializeBackgroundSync()
        {
            try
            {
                string savedTheme = Preferences.Default.Get(AppConstants.PREF_APP_THEME, AppConstants.THEME_SYSTEM);

                if (savedTheme == AppConstants.THEME_SYSTEM || savedTheme.StartsWith("Main"))
                {
                    var now = DateTime.Now;
                    var info = TimeBasedBackgroundConfig.GetBackgroundForTime(now.Hour, now.Minute);
                    BackgroundImage.Source = info.Image;
                    _currentImageName = info.Image;
                }

                ApplyTimeBasedBackground();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Kutuphane Init Background Error: {ex.Message}");
            }
        }

        private void ApplyTimeBasedBackground()
        {
            string savedTheme = Preferences.Default.Get(AppConstants.PREF_APP_THEME, AppConstants.THEME_SYSTEM);

            if (savedTheme == AppConstants.THEME_CUSTOM)
            {
                string customThemeJson = Preferences.Default.Get(AppConstants.PREF_CUSTOM_THEME, string.Empty);
                if (!string.IsNullOrEmpty(customThemeJson))
                {
                    try
                    {
                        var theme = JsonSerializer.Deserialize<CustomTheme>(customThemeJson);
                        if (theme != null && !string.IsNullOrEmpty(theme.BackgroundImage))
                        {
                            _backgroundService.ApplyCustomBackground(BackgroundImage, BackgroundOverlay, theme.BackgroundImage);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Kutuphane Custom Tema Hatası: {ex.Message}");
                    }
                }
            }

            var result = _backgroundService.SetTimeBasedBackground(BackgroundImage, BackgroundOverlay, savedTheme, _currentImageName);
            if (!string.IsNullOrEmpty(result.ImageName))
            {
                _currentImageName = result.ImageName;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ApplyTimeBasedBackground();
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void OnFilterTapped(object? sender, TappedEventArgs e)
        {
            if (e.Parameter is string filter)
            {
                _activeFilter = filter;
                UpdateFilterChipVisuals();
                ApplyFilters();
            }
        }

        private void UpdateFilterChipVisuals()
        {
            SetChipState(FilterAllBorder, FilterAllLabel, _activeFilter == "All");
            SetChipState(FilterQuranBorder, FilterQuranLabel, _activeFilter == "Quran");
            SetChipState(FilterIlmihalBorder, FilterIlmihalLabel, _activeFilter == "Ilmihal");
            SetChipState(FilterNamazBorder, FilterNamazLabel, _activeFilter == "Namaz");
        }

        private static void SetChipState(Border border, Label label, bool isActive)
        {
            border.BackgroundColor = isActive ? ActiveChipBg : InactiveChipBg;
            border.Stroke = isActive ? ActiveChipStroke : InactiveChipStroke;
            label.TextColor = isActive ? ActiveChipText : InactiveChipText;
            label.FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None;
        }

        private void ApplyFilters()
        {
            string query = (SearchEntry.Text ?? string.Empty).Trim().ToLowerInvariant();

            bool showKuranMeal = (_activeFilter == "All" || _activeFilter == "Quran") &&
                (string.IsNullOrEmpty(query) ||
                 "kur'an-ı kerim ve meali sureler türkçe mealler ayet arama yer işaretleri".Contains(query));

            bool showArapcaKuran = (_activeFilter == "All" || _activeFilter == "Quran") &&
                (string.IsNullOrEmpty(query) ||
                 "arapça kur'an-ı kerim orijinal medine mushafı sayfaları meal paneli".Contains(query));

            bool showIlmihal = (_activeFilter == "All" || _activeFilter == "Ilmihal") &&
                (string.IsNullOrEmpty(query) ||
                 "ilmihal rehberi temel dini bilgiler abdest gusül namaz hükümleri".Contains(query));

            bool showNamazHocasi = (_activeFilter == "All" || _activeFilter == "Namaz") &&
                (string.IsNullOrEmpty(query) ||
                 "namaz hocası adım adım görsel anlatımlı kılavuzu dualar".Contains(query));

            KuranMealFrame.IsVisible = showKuranMeal;
            ArapcaKuranFrame.IsVisible = showArapcaKuran;
            IlmihalFrame.IsVisible = showIlmihal;
            NamazHocasiFrame.IsVisible = showNamazHocasi;

            QuranSection.IsVisible = showKuranMeal || showArapcaKuran;
            GuidesSection.IsVisible = showIlmihal || showNamazHocasi;

            EmptySearchLabel.IsVisible = !QuranSection.IsVisible && !GuidesSection.IsVisible;
        }

        private async void KuranMealButton_Clicked(object sender, TappedEventArgs e)
        {
            if (sender is VisualElement element)
                await element.TapBounce(0.94, 80);

            var page = _serviceProvider.GetRequiredService<Kuran>();
            await Navigation.PushAsync(page);
        }

        private async void ArapcaKuranButton_Clicked(object sender, TappedEventArgs e)
        {
            if (sender is VisualElement element)
                await element.TapBounce(0.94, 80);

            var quranApi = _serviceProvider.GetRequiredService<QuranApiService>();
            await Navigation.PushAsync(new KuranOkumaPage(quranApi, 1));
        }

        private async void IlmihalButton_Clicked(object sender, TappedEventArgs e)
        {
            if (sender is VisualElement element)
                await element.TapBounce(0.94, 80);

            await Navigation.PushAsync(new Ilmihal());
        }

        private async void NamazHocasiButton_Clicked(object sender, TappedEventArgs e)
        {
            if (sender is VisualElement element)
                await element.TapBounce(0.94, 80);

            await Navigation.PushAsync(new NamazHocasi());
        }

        protected override bool OnBackButtonPressed()
        {
            // Ana sayfaya (Vakitler) dön
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("//MainPage");
            });
            return true;
        }
    }
}

using System;
using System.Text.Json;
using hadis.Models;
using hadis.Services;
using hadis.Helpers;
using hadis.ViewModels;
using Microsoft.Extensions.DependencyInjection;

#if ANDROID
using Android.OS;
using Android.Views;
#endif

namespace hadis
{
    public partial class MainPage : ContentPage
    {
        private readonly MainPageViewModel _viewModel;
        private readonly IServiceProvider _serviceProvider;
        private string _currentImageName;
        private bool _isDataLoaded = false;

        public MainPage(MainPageViewModel viewModel, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _serviceProvider = serviceProvider;
            BindingContext = viewModel;

            // İlk yüklemeleri SYNCHRONOUS olarak yap (Flicker önlemek için)
            InitializeBackgroundSync();

            // Widget güncelleme event'ını dinle
#if ANDROID
            _viewModel.WidgetUpdateRequested += UpdateAndroidWidget;
#endif
            // Konum hatasında şehir seçim sayfasına yönlendir
            _viewModel.NavigateToSehirSecim += OnNavigateToSehirSecim;
        }

        private async void OnNavigateToSehirSecim()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    var sehirSecimPage = _serviceProvider.GetRequiredService<SehirSecim>();
                    await Navigation.PushAsync(sehirSecimPage);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SehirSecim navigasyon hatası: {ex.Message}");
                }
            });
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
                    _viewModel.StatusBarService.SetStatusBarColor(info.StatusBarColor);
                }

                SetTimeBasedBackground();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Init Background Error: {ex.Message}");
                SetTimeBasedBackground();
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                // Tema ve arkaplan: hızlı, her gezişte yapılır
                ApplyTheme();
                SetTimeBasedBackground();

                // Connectivity event her OnAppearing'de bağlanır
                Connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;

                // Ağır veri yükleme (konum + HTTP) yalnızca ilk gezişte
                if (!_isDataLoaded)
                {
                    _isDataLoaded = true;
                    await _viewModel.LoadDataCommand.ExecuteAsync(null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnAppearing hatası: {ex.Message}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
        }

        private async void Connectivity_ConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await _viewModel.OnConnectivityChangedAsync(e.NetworkAccess);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Connectivity değişiklik hatası: {ex.Message}");
            }
        }

        // ============================================================
        // TEMA / ARKAPLAN (UI element referansı gerektiren kod)
        // ============================================================

        private void SetTimeBasedBackground()
        {
            string savedTheme = Preferences.Default.Get(AppConstants.PREF_APP_THEME, AppConstants.THEME_SYSTEM);

            var result = _viewModel.BackgroundService.SetTimeBasedBackground(BackgroundImage, BackgroundOverlay, savedTheme, _currentImageName);

            bool isBright = result.IsBright;
            if (!string.IsNullOrEmpty(result.ImageName))
            {
                _currentImageName = result.ImageName;
            }

            if (savedTheme != AppConstants.THEME_CUSTOM && savedTheme != "PitchBlack")
            {
                _viewModel.ThemeService.ApplyAdaptiveGlassTheme(isBright,
                    MainCountdownFrame, namazismi, kalan, Konum,
                    ImsakFrame, imsakyazı, imsakvakit,
                    GunesFrame, gunesyazı, gunesvakit,
                    OgleFrame, ogleyazı, oglevakit,
                    IkindiFrame, ikindiyazı, ikindivakit,
                    AksamFrame, aksamyazı, aksamvakit,
                    YatsiFrame, yatsıyazı, yatsıvakit,
                    AyetFrame, gununayeti);
            }
        }

        private void ApplyTheme()
        {
            string savedTheme = Preferences.Default.Get(AppConstants.PREF_APP_THEME, AppConstants.THEME_SYSTEM);

            if (savedTheme != AppConstants.THEME_CUSTOM)
            {
                _viewModel.ThemeService.ResetToDefaultStyles(
                    MainCountdownFrame, namazismi, kalan, Konum,
                    ImsakFrame, imsakyazı, imsakvakit,
                    GunesFrame, gunesyazı, gunesvakit,
                    OgleFrame, ogleyazı, oglevakit,
                    IkindiFrame, ikindiyazı, ikindivakit,
                    AksamFrame, aksamyazı, aksamvakit,
                    YatsiFrame, yatsıyazı, yatsıvakit,
                    AyetFrame, gununayeti);
                return;
            }

            _viewModel.ThemeService.ApplyCustomTheme(
                MainCountdownFrame, namazismi, kalan, Konum,
                ImsakFrame, imsakyazı, imsakvakit,
                GunesFrame, gunesyazı, gunesvakit,
                OgleFrame, ogleyazı, oglevakit,
                IkindiFrame, ikindiyazı, ikindivakit,
                AksamFrame, aksamyazı, aksamvakit,
                YatsiFrame, yatsıyazı, yatsıvakit,
                AyetFrame, gununayeti);

            string customThemeJson = Preferences.Default.Get(AppConstants.PREF_CUSTOM_THEME, string.Empty);
            if (!string.IsNullOrEmpty(customThemeJson))
            {
                try
                {
                    var theme = JsonSerializer.Deserialize<CustomTheme>(customThemeJson);
                    if (theme != null && !string.IsNullOrEmpty(theme.BackgroundImage))
                    {
                        _viewModel.BackgroundService.ApplyCustomBackground(BackgroundImage, BackgroundOverlay, theme.BackgroundImage);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Custom tema arkaplan hatası: {ex.Message}");
                }
            }
        }

        // ============================================================
        // ANİMASYONLAR (UI element referansı gerektiren kod)
        // ============================================================

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            try
            {
                await AnimateFrames();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Animasyon hatası: {ex.Message}");
            }
        }

        private async Task AnimateFrames()
        {
            // Önceki animasyonları iptal et (çakışma önleme)
            MainCountdownFrame.CancelAnimations();
            ImsakFrame.CancelAnimations();
            GunesFrame.CancelAnimations();
            OgleFrame.CancelAnimations();
            IkindiFrame.CancelAnimations();
            AksamFrame.CancelAnimations();
            YatsiFrame.CancelAnimations();
            AyetFrame.CancelAnimations();

            MainCountdownFrame.Opacity = 0;
            MainCountdownFrame.Scale = 0.7;

            ImsakFrame.Opacity = 0; ImsakFrame.Scale = 0.7;
            GunesFrame.Opacity = 0; GunesFrame.Scale = 0.7;
            OgleFrame.Opacity = 0; OgleFrame.Scale = 0.7;

            IkindiFrame.Opacity = 0; IkindiFrame.Scale = 0.7;
            AksamFrame.Opacity = 0; AksamFrame.Scale = 0.7;
            YatsiFrame.Opacity = 0; YatsiFrame.Scale = 0.7;

            AyetFrame.Opacity = 0; AyetFrame.Scale = 0.7;

            await Task.WhenAll(
                MainCountdownFrame.FadeTo(1, 500, Easing.CubicOut),
                MainCountdownFrame.ScaleTo(1.0, 600, Easing.SpringOut)
            );

            await Task.Delay(100);

            _ = AnimateSingleFrame(ImsakFrame);
            await Task.Delay(80);
            _ = AnimateSingleFrame(GunesFrame);
            await Task.Delay(80);
            _ = AnimateSingleFrame(OgleFrame);
            await Task.Delay(100);

            _ = AnimateSingleFrame(IkindiFrame);
            await Task.Delay(80);
            _ = AnimateSingleFrame(AksamFrame);
            await Task.Delay(80);
            _ = AnimateSingleFrame(YatsiFrame);
            await Task.Delay(150);

            await Task.WhenAll(
                AyetFrame.FadeTo(1, 500, Easing.CubicOut),
                AyetFrame.ScaleTo(1.0, 600, Easing.SpringOut)
            );
        }

        private Task AnimateSingleFrame(Border border)
        {
            return Task.WhenAll(
                border.FadeTo(1, 400, Easing.CubicOut),
                border.ScaleTo(1.0, 500, Easing.SpringOut)
            );
        }

        protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);

            try
            {
                // Önceki animasyonları iptal et
                MainCountdownFrame.CancelAnimations();
                ImsakFrame.CancelAnimations();
                GunesFrame.CancelAnimations();
                OgleFrame.CancelAnimations();
                IkindiFrame.CancelAnimations();
                AksamFrame.CancelAnimations();
                YatsiFrame.CancelAnimations();
                AyetFrame.CancelAnimations();

                // Hızlı (çıkış animasyonu beklenilmiyor, geçişi bloklamaz)
                _ = Task.WhenAll(
                    MainCountdownFrame.ScaleTo(0.7, 200, Easing.CubicIn),
                    ImsakFrame.ScaleTo(0.7, 200, Easing.CubicIn),
                    GunesFrame.ScaleTo(0.7, 200, Easing.CubicIn),
                    OgleFrame.ScaleTo(0.7, 200, Easing.CubicIn),
                    IkindiFrame.ScaleTo(0.7, 200, Easing.CubicIn),
                    AksamFrame.ScaleTo(0.7, 200, Easing.CubicIn),
                    YatsiFrame.ScaleTo(0.7, 200, Easing.CubicIn),
                    AyetFrame.ScaleTo(0.7, 200, Easing.CubicIn)
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigasyon animasyon hatası: {ex.Message}");
            }
        }

        // ============================================================
        // EVENT HANDLERS (Navigation)
        // ============================================================

        private async void Konum_Tapped(object? sender, EventArgs e)
        {
            try
            {
                var sehirSecimPage = _serviceProvider.GetRequiredService<SehirSecim>();
                await Navigation.PushAsync(sehirSecimPage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Konum navigasyon hatası: {ex.Message}");
            }
        }

        private async void OnLocationErrorRetry_Clicked(object sender, EventArgs e)
        {
            try
            {
                var sehirSecimPage = _serviceProvider.GetRequiredService<SehirSecim>();
                await Navigation.PushAsync(sehirSecimPage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Retry navigasyon hatası: {ex.Message}");
            }
        }

#if ANDROID
        private void UpdateAndroidWidget()
        {
            try
            {
                var context = Android.App.Application.Context;
                var appWidgetManager = Android.Appwidget.AppWidgetManager.GetInstance(context);
                var componentName = new Android.Content.ComponentName(context, Java.Lang.Class.FromType(typeof(hadis.Platforms.Android.ClockWeatherWidget)));
                var appWidgetIds = appWidgetManager?.GetAppWidgetIds(componentName);

                if (appWidgetIds != null && appWidgetIds.Length > 0)
                {
                    var intent = new Android.Content.Intent(context, typeof(hadis.Platforms.Android.ClockWeatherWidget));
                    intent.SetAction(Android.Appwidget.AppWidgetManager.ActionAppwidgetUpdate);
                    intent.PutExtra(Android.Appwidget.AppWidgetManager.ExtraAppwidgetIds, appWidgetIds);
                    context.SendBroadcast(intent);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Widget update trigger error: {ex.Message}");
            }
        }
#endif
    }
}


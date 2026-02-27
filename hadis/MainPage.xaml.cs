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
        private string _lastLocationKey = "";

        // Animasyon için frame array'i - her seferinde yeniden oluşturulmuyor (allocation optimize)
        private Border[]? _allFrames;
        private Border[]? _prayerFrames;

        private Border[] AllFrames => _allFrames ??= new[]
        {
            MainCountdownFrame, ImsakFrame, GunesFrame, OgleFrame,
            IkindiFrame, AksamFrame, YatsiFrame, AyetFrame
        };

        private Border[] PrayerFrames => _prayerFrames ??= new[]
        {
            ImsakFrame, GunesFrame, OgleFrame, IkindiFrame, AksamFrame, YatsiFrame
        };

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

        private void OnNavigateToSehirSecim()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
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

                // Konum değişikliği kontrolü
                var currentLocationKey = GetLocationKey();
                if (!_isDataLoaded || currentLocationKey != _lastLocationKey)
                {
                    _isDataLoaded = true;
                    _lastLocationKey = currentLocationKey;
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

        private void Connectivity_ConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await _viewModel.OnConnectivityChangedAsync(e.NetworkAccess);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Connectivity değişiklik hatası: {ex.Message}");
                }
            });
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
            // Tüm animasyonları iptal et (optimize edilmiş)
            AnimationHelpers.CancelAllAnimations(AllFrames);

            // Başlangıç durumuna getir
            AnimationHelpers.PrepareForAnimation(AllFrames);

            // Ana countdown frame'i animasyonla göster
            await MainCountdownFrame.AnimateIn(500, 600);

            await Task.Delay(100);

            // Namaz vakitlerini sırayla animasyonla göster
            await AnimationHelpers.AnimateInSequential(80, PrayerFrames);

            await Task.Delay(150);

            // Ayet frame'i animasyonla göster
            await AyetFrame.AnimateIn(500, 600);
        }

        private Task AnimateSingleFrame(Border border)
        {
            return border.AnimateIn();
        }

        protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);

            try
            {
                // Tüm animasyonları iptal et (optimize edilmiş)
                AnimationHelpers.CancelAllAnimations(AllFrames);

                // Hızlı çıkış animasyonu (fire-and-forget, bloklama yok)
                _ = AnimationHelpers.AnimateOutParallel(AllFrames);
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

        /// <summary>
        /// Konum tercihlerinden bir anahtar oluşturur; değişiklik tespiti için kullanılır.
        /// </summary>
        private string GetLocationKey()
        {
            var otomatik = Preferences.Default.Get("OtomatikKonum", true);
            var sehir = Preferences.Default.Get("ManuelSehir", "");
            var ilce = Preferences.Default.Get("ManuelIlce", "");
            var lat = Preferences.Default.Get("ManuelLatitude", 0.0);
            var lon = Preferences.Default.Get("ManuelLongitude", 0.0);
            return $"{otomatik}|{sehir}|{ilce}|{lat}|{lon}";
        }

        /// <summary>
        /// Ayet kutusuna tıklanınca küçülüp büyüme animasyonu ile ayet değiştirir
        /// </summary>
        private async void AyetFrame_Tapped(object? sender, EventArgs e)
        {
            try
            {
                // Optimize edilmiş tap bounce animasyonu
                await AyetFrame.TapBounce();

                // Ayet değiştir
                _viewModel.GununAyeti = Helpers.PrayerTimeHelper.GetRandomAyet(_viewModel.GununAyeti);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Ayet animasyon hatası: {ex.Message}");
            }
        }
    }
}


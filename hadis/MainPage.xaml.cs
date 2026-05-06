using System;
using System.Text.Json;
using hadis.Models;
using hadis.Services;
using hadis.Helpers;
using hadis.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace hadis
{
    public partial class MainPage : ContentPage
    {
        private readonly MainPageViewModel _viewModel;
        private readonly IServiceProvider _serviceProvider;
        private string _currentImageName;
        private bool _isDataLoaded = false;
        private string _lastLocationKey = "";
        private List<AddedCity> _additionalCities = new();
        private int _activeAdditionalCityIndex = -1; // -1: ana şehir
        private bool _isSwipeTransitionRunning;

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
            
            // Başlangıçta indicator kontrolü yapalım
            UpdatePageIndicator();
        }

        private void OnNavigateToSehirSecim()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var konumPage = _serviceProvider.GetRequiredService<KonumPage>();
                    await Navigation.PushAsync(konumPage);
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

                // İlk açılışta konum seçili mi kontrol et
                bool isFirstLaunch = !Preferences.Default.ContainsKey("OtomatikKonum") 
                    && string.IsNullOrEmpty(Preferences.Default.Get("ManuelSehir", ""));

                if (isFirstLaunch)
                {
                    // Artık ilk açılış olmadığını kaydet ki geri tuşuna basınca tekrar tekrar uyarı popup'ı vermesin
                    Preferences.Default.Set("OtomatikKonum", false);
                    
                    // Ana sayfadaki arka plan uyarı arayüzünü (overlay) hemen görünür yap
                    _viewModel.IsLocationErrorVisible = true;

                    // İlk kez açılıyor - konum seçtir
                    await DisplayAlert(
                        "Konum Seçimi", 
                        "Namaz vakitlerini görebilmek için lütfen bir konum seçiniz.",
                        "Tamam");

                    var konumPage = _serviceProvider.GetRequiredService<KonumPage>();
                    await Navigation.PushAsync(konumPage);
                    return;
                }

                // Her girişte veriyi tazele; cache ve son kayıt sayesinde bu hafif kalır
                var currentLocationKey = GetLocationKey();
                _isDataLoaded = true;
                _lastLocationKey = currentLocationKey;
                await _viewModel.LoadDataCommand.ExecuteAsync(null);

                if (_viewModel.IsLocationErrorVisible)
                {
                    bool redirect = await DisplayAlert(
                        "Konum Seçimi Gerekli", 
                        "Namaz vakitlerini görebilmek için bir şehir seçmelisiniz veya konum izni vermelisiniz.\nŞimdi şehir seçmek ister misiniz?", 
                        "Evet", 
                        "Hayır");

                    if (redirect)
                    {
                        var konumPage = _serviceProvider.GetRequiredService<KonumPage>();
                        await Navigation.PushAsync(konumPage);
                    }
                }

                RefreshAdditionalCities();

                // Animasyonları başlat
                _ = AnimateFrames();
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

            try
            {
                // Tüm animasyonları iptal et (optimize edilmiş)
                AnimationHelpers.CancelAllAnimations(AllFrames);

                // Hızlı çıkış animasyonu (fire-and-forget, bloklama yok)
                _ = AnimationHelpers.AnimateOutParallel(AllFrames);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnDisappearing animasyon hatası: {ex.Message}");
            }
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

        // ============================================================
        // EVENT HANDLERS (Navigation)
        // ============================================================

        private async void Konum_Tapped(object? sender, EventArgs e)
        {
            try
            {
                var konumPage = _serviceProvider.GetRequiredService<KonumPage>();
                await Navigation.PushAsync(konumPage);
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
                var konumPage = _serviceProvider.GetRequiredService<KonumPage>();
                await Navigation.PushAsync(konumPage);
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
            var otomatik = Preferences.Default.Get("OtomatikKonum", false);
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

        private async void OnPrayerFramesSwiped(object? sender, SwipedEventArgs e)
        {
            if (_isSwipeTransitionRunning || (e.Direction != SwipeDirection.Right && e.Direction != SwipeDirection.Left))
            {
                return;
            }

            try
            {
                RefreshAdditionalCities();
                bool isSwipeRight = e.Direction == SwipeDirection.Right;

                if (_additionalCities.Count == 0)
                {
                    _activeAdditionalCityIndex = -1;
                    return;
                }

                if (isSwipeRight)
                {
                    // Right swipe -> Go to previous (or circle to end)
                    if (_activeAdditionalCityIndex == -1)
                    {
                        _activeAdditionalCityIndex = _additionalCities.Count - 1;
                    }
                    else if (_activeAdditionalCityIndex > 0)
                    {
                        _activeAdditionalCityIndex--;
                    }
                    else
                    {
                        _activeAdditionalCityIndex = -1;
                        await AnimatePrayerFramesTransitionAsync(async () =>
                        {
                            await _viewModel.LoadDataCommand.ExecuteAsync(null);
                        }, true);
                        return;
                    }
                }
                else
                {
                    // Left swipe -> Go to next (or circle back to start/main)
                    if (_activeAdditionalCityIndex == -1)
                    {
                        _activeAdditionalCityIndex = 0;
                    }
                    else if (_activeAdditionalCityIndex < _additionalCities.Count - 1)
                    {
                        _activeAdditionalCityIndex++;
                    }
                    else
                    {
                        _activeAdditionalCityIndex = -1;
                        await AnimatePrayerFramesTransitionAsync(async () =>
                        {
                            await _viewModel.LoadDataCommand.ExecuteAsync(null);
                        }, false);
                        return;
                    }
                }

                var selected = _additionalCities[_activeAdditionalCityIndex];
                await AnimatePrayerFramesTransitionAsync(async () =>
                {
                    var loaded = await _viewModel.ShowPrayerTimesForCityAsync(selected.Sehir, selected.Ilce, selected.Ulke, selected.Latitude, selected.Longitude);
                    if (!loaded)
                    {
                        _viewModel.ShowAddCityPlaceholder();
                    }
                }, isSwipeRight);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Swipe şehir değiştirme hatası: {ex.Message}");
            }
        }

        private async Task AnimatePrayerFramesTransitionAsync(Func<Task> updateContent, bool slideRight)
        {
            _isSwipeTransitionRunning = true;
            UpdatePageIndicator(); // Dot'ı hemen güncelle
            try
            {
                double slideDistance = 40; // Çok daha kısa ve akıcı bir kayma
                double outX = slideRight ? slideDistance : -slideDistance;
                double inX = slideRight ? -slideDistance : slideDistance;

                // Eski içeriği kaydırıp şeffaflaştırarak gizle
                await Task.WhenAll(
                    MainContentGrid.TranslateTo(outX, 0, 180, Easing.CubicIn),
                    MainContentGrid.FadeTo(0, 180, Easing.CubicIn)
                );

                // İçeriği güncelle
                await updateContent();

                // Görünmezken ekranın ters tarafına al
                MainContentGrid.TranslationX = inX;
                
                // Olduğu konuma geri kaydırarak şeffaflığı kaldırıp görünür yap
                await Task.WhenAll(
                    MainContentGrid.TranslateTo(0, 0, 220, Easing.CubicOut),
                    MainContentGrid.FadeTo(1, 220, Easing.CubicOut)
                );
            }
            finally
            {
                MainContentGrid.TranslationX = 0;
                MainContentGrid.Opacity = 1;
                MainContentGrid.Scale = 1;
                _isSwipeTransitionRunning = false;
            }
        }

        private void RefreshAdditionalCities()
        {
            try
            {
                var currentSehir = Preferences.Default.Get("ManuelSehir", "");
                var currentIlce = Preferences.Default.Get("ManuelIlce", "");

                var json = Preferences.Default.Get(AppConstants.PREF_ADDED_CITIES, string.Empty);
                var allCities = string.IsNullOrWhiteSpace(json)
                    ? new List<AddedCity>()
                    : JsonSerializer.Deserialize<List<AddedCity>>(json) ?? new List<AddedCity>();

                _additionalCities = allCities
                    .Where(c =>
                        !string.Equals(c.Sehir, currentSehir, StringComparison.OrdinalIgnoreCase) ||
                        !string.Equals(c.Ilce, currentIlce, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (_activeAdditionalCityIndex >= _additionalCities.Count)
                {
                    _activeAdditionalCityIndex = -1;
                }
                
                UpdatePageIndicator();
            }
            catch
            {
                _additionalCities = new List<AddedCity>();
                _activeAdditionalCityIndex = -1;
                UpdatePageIndicator();
            }
        }

        private void UpdatePageIndicator()
        {
            if (PageIndicator == null) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_additionalCities == null || _additionalCities.Count == 0)
                {
                    PageIndicator.IsVisible = false;
                    PageIndicator.Children.Clear();
                    return;
                }

                PageIndicator.IsVisible = true;
                PageIndicator.Children.Clear();

                int totalPages = 1 + _additionalCities.Count;
                int activeIndex = _activeAdditionalCityIndex + 1; // -1 -> 0, 0 -> 1...

                for (int i = 0; i < totalPages; i++)
                {
                    var isCurrent = i == activeIndex;
                    var dot = new Microsoft.Maui.Controls.Shapes.Ellipse
                    {
                        WidthRequest = isCurrent ? 10 : 8,
                        HeightRequest = isCurrent ? 10 : 8,
                        Fill = isCurrent ? Colors.White : Colors.White.WithAlpha(0.4f),
                        VerticalOptions = LayoutOptions.Center
                    };

                    PageIndicator.Children.Add(dot);
                }
            });
        }
    }
}


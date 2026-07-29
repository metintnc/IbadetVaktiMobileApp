using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel;
using hadis.Models;
using hadis.Services;

namespace hadis
{
    public partial class KuranOkumaPage : ContentPage
    {
        private const int TotalPages = 604;
        private ObservableCollection<string> _pages = new();
        private int _currentPageNumber = 1;
        private int _loadingPageNumber = 0;
        private readonly HashSet<string> _prefetchedUrls = new();
        private readonly QuranApiService _quranApiService;
        private CancellationTokenSource _translationCts;

        private bool _isTranslationVisible = false;

        public KuranOkumaPage(QuranApiService quranApiService, int startPage = 1)
        {
            InitializeComponent();
            _quranApiService = quranApiService;
            InitializePages();
            
            // Restore meal panel visibility preference (default false for full-screen Mushaf)
            _isTranslationVisible = Preferences.Get("IsQuranTranslationVisible", false);
            TranslationBorder.IsVisible = _isTranslationVisible;
            MealToggleLabel.TextColor = _isTranslationVisible ? Color.FromArgb("#00BCD4") : Colors.White;

            // If starting from default (1), try to load last saved page
            int targetPage = startPage;
            if (startPage == 1 && Preferences.ContainsKey("LastReadPage"))
            {
                targetPage = Preferences.Get("LastReadPage", 1);
            }

            // Set initial position (0-indexed)
            int targetIndex = targetPage - 1;
            PageCarousel.Position = targetIndex;
            PageCarousel.ScrollTo(targetIndex, position: ScrollToPosition.Center, animate: false);
            _currentPageNumber = targetPage;
            UpdatePageLabel(targetPage);
            UpdateTitle(targetPage);
            
            // Prefetch pages around the start page
            PrefetchPages(targetPage);
            
            // Load translation if panel is visible
            if (_isTranslationVisible)
            {
                _ = LoadPageTranslationAsync(targetPage);
            }
        }

        private void InitializePages()
        {
            // Standard Madini Mushaf has 604 pages
            for (int i = 1; i <= TotalPages; i++)
            {
                _pages.Add($"https://raw.githubusercontent.com/metintnc/NamazVaktiMobileApp/main/hadis/kuransayfalar%C4%B1/kuran111-g%C3%B6r%C3%BCnt%C3%BCler-{i}.jpg");
            }
            PageCarousel.ItemsSource = _pages;
        }

        private void OnToggleTranslationClicked(object sender, EventArgs e)
        {
            _isTranslationVisible = !_isTranslationVisible;
            TranslationBorder.IsVisible = _isTranslationVisible;
            MealToggleLabel.TextColor = _isTranslationVisible ? Color.FromArgb("#00BCD4") : Colors.White;

            if (_isTranslationVisible && TranslationListContainer.Children.Count == 0)
            {
                _ = LoadPageTranslationAsync(_currentPageNumber);
            }
            Preferences.Set("IsQuranTranslationVisible", _isTranslationVisible);
        }

        private void OnPositionChanged(object sender, PositionChangedEventArgs e)
        {
            int pageNumber = e.CurrentPosition + 1;
            _currentPageNumber = pageNumber;
            UpdatePageLabel(pageNumber);
            UpdateTitle(pageNumber);
            
            // Save progress
            Preferences.Set("LastReadPage", pageNumber);

            // Prefetch pages around the current page
            PrefetchPages(pageNumber);
            
            // Load translation for the active page if panel is open
            if (_isTranslationVisible)
            {
                _ = LoadPageTranslationAsync(pageNumber);
            }
        }

        private void PrefetchPages(int centerPageNumber)
        {
            var pagesToPrefetch = new List<int>();
            for (int offset = -2; offset <= 2; offset++)
            {
                if (offset == 0) continue;
                int page = centerPageNumber + offset;
                if (page >= 1 && page <= TotalPages)
                {
                    pagesToPrefetch.Add(page);
                }
            }

            Task.Run(async () =>
            {
                foreach (int page in pagesToPrefetch)
                {
                    string url = $"https://raw.githubusercontent.com/metintnc/NamazVaktiMobileApp/main/hadis/kuransayfalar%C4%B1/kuran111-g%C3%B6r%C3%BCnt%C3%BCler-{page}.jpg";
                    
                    lock (_prefetchedUrls)
                    {
                        if (_prefetchedUrls.Contains(url))
                        {
                            continue;
                        }
                        _prefetchedUrls.Add(url);
                    }

                    try
                    {
                        var uriSource = new UriImageSource
                        {
                            Uri = new Uri(url),
                            CachingEnabled = true,
                            CacheValidity = TimeSpan.FromDays(30)
                        };

                        if (uriSource is IStreamImageSource streamImageSource)
                        {
                            using (var stream = await streamImageSource.GetStreamAsync(System.Threading.CancellationToken.None))
                            {
                                // Cached image
                            }
                        }

                        // Prefetch page translation as well if not already cached
                        if (!_quranApiService.IsPageTranslationCached(page))
                        {
                            await _quranApiService.GetPageTranslationAsync(page);
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (_prefetchedUrls)
                        {
                            _prefetchedUrls.Remove(url);
                        }
                        System.Diagnostics.Debug.WriteLine($"⚠️ Preloading failed for page {page}: {ex.Message}");
                    }
                }
            });
        }

        private void UpdatePageLabel(int pageNumber)
        {
            PageNumberLabel.Text = $"Sayfa {pageNumber}";
        }

        private void UpdateTitle(int pageNumber)
        {
            var sure = KuranDataService.GetSureFromPage(pageNumber);
            int juz = KuranDataService.GetCuzNo(pageNumber);

            if (sure != null)
            {
                SurahNameLabel.Text = $"{sure.Ad} Sûresi";
                JuzLabel.Text = $"{juz}. Cüz";
                this.Title = $"{sure.Ad} - {juz}. Cüz";
            }
        }

        private async void OnGoToPageClicked(object sender, EventArgs e)
        {
            string result = await DisplayPromptAsync("Sayfaya Git", 
                "Gitmek istediğiniz sayfa numarasını girin (1 - 604):", 
                "Git", 
                "İptal", 
                placeholder: _currentPageNumber.ToString(), 
                maxLength: 3, 
                keyboard: Keyboard.Numeric);

            if (!string.IsNullOrWhiteSpace(result) && int.TryParse(result, out int targetPage))
            {
                if (targetPage >= 1 && targetPage <= TotalPages)
                {
                    int targetIndex = targetPage - 1;
                    PageCarousel.ScrollTo(targetIndex, position: ScrollToPosition.Center, animate: false);
                    PageCarousel.Position = targetIndex;
                    _currentPageNumber = targetPage;
                    UpdatePageLabel(targetPage);
                    UpdateTitle(targetPage);
                    PrefetchPages(targetPage);
                    Preferences.Set("LastReadPage", targetPage);
                    if (_isTranslationVisible)
                    {
                        _ = LoadPageTranslationAsync(targetPage);
                    }
                }
                else
                {
                    await DisplayAlert("Hata", "Lütfen 1 ile 604 arasında geçerli bir sayfa numarası girin.", "Tamam");
                }
            }
        }

        private async Task LoadPageTranslationAsync(int pageNumber)
        {
            // Cancel previous active HTTP / I/O request on rapid swipe
            _translationCts?.Cancel();
            _translationCts?.Dispose();
            _translationCts = new CancellationTokenSource();
            var token = _translationCts.Token;

            _loadingPageNumber = pageNumber;
            
            bool isCached = _quranApiService.IsPageTranslationCached(pageNumber);

            // Only show spinner if data is NOT in cache and needs network fetch
            if (!isCached)
            {
                TranslationLoadingIndicator.IsVisible = true;
                TranslationLoadingIndicator.IsRunning = true;
                TranslationListContainer.Children.Clear();
            }
            else
            {
                TranslationLoadingIndicator.IsVisible = false;
                TranslationLoadingIndicator.IsRunning = false;
            }

            try
            {
                var verses = await _quranApiService.GetPageTranslationAsync(pageNumber, token);
                
                // If user swiped away while we were loading, discard results
                if (token.IsCancellationRequested || _loadingPageNumber != pageNumber)
                {
                    return;
                }

                TranslationListContainer.Children.Clear();

                if (verses == null || verses.Count == 0)
                {
                    var errorLabel = new Label
                    {
                        Text = "Bu sayfa meali yüklenemedi. İnternet bağlantınızı kontrol edin veya tüm sureleri indirmiş olduğunuzdan emin olun.",
                        TextColor = Colors.Red,
                        FontSize = 14,
                        HorizontalOptions = LayoutOptions.Center,
                        HorizontalTextAlignment = TextAlignment.Center,
                        Margin = new Thickness(10, 20)
                    };
                    TranslationListContainer.Children.Add(errorLabel);
                }
                else
                {
                    int lastSurahId = -1;
                    foreach (var ayah in verses)
                    {
                        // If this ayah belongs to a different Surah, show a header
                        if (ayah.SurahId != lastSurahId)
                        {
                            lastSurahId = ayah.SurahId;
                            var surahHeaderLabel = new Label
                            {
                                Text = $"{ayah.SurahName} Sûresi",
                                FontSize = 16,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = Color.FromArgb("#00BCD4"),
                                Margin = new Thickness(0, 10, 0, 5),
                                HorizontalOptions = LayoutOptions.Center
                            };
                            TranslationListContainer.Children.Add(surahHeaderLabel);
                        }

                        var verseStack = new VerticalStackLayout { Spacing = 4 };

                        // Arabic text
                        var arabicLabel = new Label
                        {
                            Text = ayah.ArabicText,
                            FontSize = 22,
                            TextColor = Color.FromArgb("#00BCD4"),
                            HorizontalOptions = LayoutOptions.End,
                            HorizontalTextAlignment = TextAlignment.End,
                            Margin = new Thickness(0, 0, 5, 0)
                        };
                        verseStack.Children.Add(arabicLabel);

                        // Translation text
                        var trLabel = new Label
                        {
                            FontSize = 14,
                            LineBreakMode = LineBreakMode.WordWrap,
                            HorizontalOptions = LayoutOptions.FillAndExpand
                        };
                        
                        var formattedText = new FormattedString();
                        formattedText.Spans.Add(new Span
                        {
                            Text = $"[{ayah.Number}] ",
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#00796B")
                        });
                        formattedText.Spans.Add(new Span
                        {
                            Text = ayah.Translation,
                            TextColor = AppInfo.Current.RequestedTheme == AppTheme.Dark ? Colors.White : Colors.Black
                        });
                        trLabel.FormattedText = formattedText;
                        
                        verseStack.Children.Add(trLabel);

                        // Separator line
                        var separator = new BoxView
                        {
                            HeightRequest = 1,
                            BackgroundColor = Color.FromArgb("#33FFFFFF"),
                            Opacity = 0.15,
                            Margin = new Thickness(0, 8, 0, 4)
                        };
                        verseStack.Children.Add(separator);

                        TranslationListContainer.Children.Add(verseStack);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Active request canceled due to page swipe - ignore cleanly
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading page translation: {ex.Message}");
            }
            finally
            {
                if (_loadingPageNumber == pageNumber)
                {
                    TranslationLoadingIndicator.IsRunning = false;
                    TranslationLoadingIndicator.IsVisible = false;
                }
            }
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Shell.SetTabBarIsVisible(this, false);
            
            // Prevent screen from sleeping/locking while reading Quran
            try
            {
                DeviceDisplay.Current.KeepScreenOn = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"KeepScreenOn activation failed: {ex.Message}");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            Shell.SetTabBarIsVisible(this, true);

            // Cancel any active background translation requests when navigating away
            _translationCts?.Cancel();
            _translationCts?.Dispose();
            _translationCts = null;
            
            // Allow screen to lock/sleep again
            try
            {
                DeviceDisplay.Current.KeepScreenOn = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"KeepScreenOn deactivation failed: {ex.Message}");
            }
        }
    }
}

namespace hadis
{
    using hadis.Helpers;
    using hadis.Services;
    using Microsoft.Extensions.DependencyInjection;

    public partial class AppShell : Shell
    {
        private readonly IServiceProvider _serviceProvider;
        
        // Tema cache - Preferences okuma overhead'ini önler
        private string? _cachedTheme;
        private DateTime _themeCacheTime;
        private static readonly TimeSpan ThemeCacheExpiry = TimeSpan.FromSeconds(5);
        
        public AppShell(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            
            // Hemen arka planda başlat - Task.Run UI thread'i bloklamaz
            _ = Task.Run(PrewarmPages);
        }

        /// <summary>
        /// Sayfaları ve servisleri arka planda önceden oluşturarak ilk açılış gecikmesini önler
        /// Thread pool'da çalışır, UI'ı etkilemez
        /// </summary>
        private void PrewarmPages()
        {
            try
            {
                // Paralel DI resolution - tüm sayfaları ve kritik servisleri aynı anda oluştur
                Parallel.Invoke(
                    // Sayfalar
                    () => _ = _serviceProvider.GetService<zikirmatik>(),
                    () => _ = _serviceProvider.GetService<kible>(),
                    () => _ = _serviceProvider.GetService<Kuran>(),
                    () => _ = _serviceProvider.GetService<Ayarlar>(),
                    // Kritik servisler (henüz resolve edilmemişse)
                    () => _ = _serviceProvider.GetService<INativeCompassService>(),
                    () => _ = _serviceProvider.GetService<QuranApiService>()
                );
                
                System.Diagnostics.Debug.WriteLine("✅ Sayfalar ve servisler ön yüklendi (prewarm - parallel)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Sayfa prewarm hatası: {ex.Message}");
            }
        }

        /// <summary>
        /// Tema cache'ini invalidate et (tema değiştiğinde çağrılmalı)
        /// </summary>
        public void InvalidateThemeCache() => _cachedTheme = null;

        /// <summary>
        /// Cache'li tema değerini al
        /// </summary>
        private string GetCachedTheme()
        {
            var now = DateTime.UtcNow;
            if (_cachedTheme == null || (now - _themeCacheTime) > ThemeCacheExpiry)
            {
                _cachedTheme = Preferences.Default.Get(AppConstants.PREF_APP_THEME, "MainDark");
                _themeCacheTime = now;
            }
            return _cachedTheme;
        }

        protected override void OnNavigated(ShellNavigatedEventArgs args)
        {
            base.OnNavigated(args);
            UpdateTabBarColor();
        }

        private void UpdateTabBarColor()
        {
            try
            {
                // Cache'li tema kullan
                string savedTheme = GetCachedTheme();
                var currentPage = Current.CurrentPage;
                bool isMainPage = currentPage is MainPage;

                if (savedTheme == "MainLight")
                {
                    if (isMainPage)
                    {
                        var now = DateTime.Now;
                        var info = TimeBasedBackgroundConfig.GetBackgroundForTime(now.Hour, now.Minute);
                        Shell.SetTabBarBackgroundColor(this, info.TabBarColorParsed);
                    }
                    else
                    {
                        Shell.SetTabBarBackgroundColor(this, Colors.White);
                    }
                }
                else if (savedTheme == "MainDark")
                {
                    if (isMainPage)
                    {
                        var now = DateTime.Now;
                        var info = TimeBasedBackgroundConfig.GetBackgroundForTime(now.Hour, now.Minute);
                        Shell.SetTabBarBackgroundColor(this, info.TabBarColorParsed);
                    }
                    else
                    {
                        Shell.SetTabBarBackgroundColor(this, Colors.Black);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TabBar update error: {ex.Message}");
            }
        }
    }
}

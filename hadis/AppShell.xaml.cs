namespace hadis
{
    using hadis.Helpers;
    using hadis.Services;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Maui.Platform;

    public partial class AppShell : Shell
    {
        private readonly IServiceProvider _serviceProvider;
        private int _lastTabIndex;
        private VisualElement? _animatingContent;
        private const string TabAnimation = "TabEntrance";
        
        public AppShell(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            
            // Ana sayfaları açılışta hemen yükle (ilk geçişteki takılmayı önlemek için)
            ContentVakitler.Content = _serviceProvider.GetRequiredService<MainPage>();
            ContentKutuphane.Content = _serviceProvider.GetRequiredService<Kutuphane>();
            ContentKible.Content = _serviceProvider.GetRequiredService<kible>();
            ContentZikirmatik.Content = _serviceProvider.GetRequiredService<zikirmatik>();
            ContentAyarlar.Content = _serviceProvider.GetRequiredService<Ayarlar>();
            
            // Servisleri arka planda önceden oluştur
            _ = Task.Run(PrewarmServices);
        }

        public void ChangeTab(string targetTab)
        {
            int index = targetTab switch
            {
                "Vakitler" => 0,
                "Kutuphane" => 1,
                "Kible" => 2,
                "Zikir" => 3,
                "Ayarlar" => 4,
                _ => -1
            };

            if (CurrentItem is { } tabs && index >= 0 && index < tabs.Items.Count)
                tabs.CurrentItem = tabs.Items[index];
        }

        protected override void OnNavigated(ShellNavigatedEventArgs args)
        {
            base.OnNavigated(args);
            if (CurrentItem is not { } tabs) return;

            int index = tabs.Items.IndexOf(tabs.CurrentItem);
            if (index < 0) return;
            int direction = Math.Sign(index - _lastTabIndex);
            _lastTabIndex = index;

            _animatingContent?.AbortAnimation(TabAnimation);
            _animatingContent = null;
            if (direction == 0 || CurrentPage?.FindByName<VisualElement>("MainContentGrid") is not { } content)
                return;

            // Only the incoming content moves. Shell can accept another tap immediately.
            double distance = direction * Math.Clamp(Width * 0.055, 16, 28);
            _animatingContent = content;
            content.TranslationX = distance;
            content.Opacity = 0.82;
            var entrance = new Animation(progress =>
            {
                content.TranslationX = distance * (1 - progress);
                content.Opacity = 0.82 + 0.18 * progress;
            });
            entrance.Commit(content, TabAnimation, length: 200, easing: Easing.CubicOut,
                finished: (_, _) =>
                {
                    content.TranslationX = 0;
                    content.Opacity = 1;
                });
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            if (Handler?.MauiContext != null)
            {
                var context = Handler.MauiContext;
                
                // Yerel platform sarmalayıcılarını (Handler/Fragment/ViewController) önceden oluşturarak
                // ilk tıklamadaki saliselik takılmayı tamamen sıfırla.
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        _serviceProvider.GetRequiredService<MainPage>().ToHandler(context);
                        _serviceProvider.GetRequiredService<Kutuphane>().ToHandler(context);
                        _serviceProvider.GetRequiredService<kible>().ToHandler(context);
                        _serviceProvider.GetRequiredService<zikirmatik>().ToHandler(context);
                        _serviceProvider.GetRequiredService<Ayarlar>().ToHandler(context);
                        
                        System.Diagnostics.Debug.WriteLine("✅ Yerel görünümler önceden başarıyla oluşturuldu.");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"⚠️ Yerel görünüm ön oluşturma hatası: {ex.Message}");
                    }
                });
            }
        }

        /// <summary>
        /// Kritik servisleri arka planda önceden oluşturarak açılış gecikmesini önler
        /// </summary>
        private void PrewarmServices()
        {
            try
            {
                Parallel.Invoke(
                    () => _ = _serviceProvider.GetService<Kuran>(),
                    () => _ = _serviceProvider.GetService<INativeCompassService>(),
                    () => _ = _serviceProvider.GetService<QuranApiService>()
                );
                
                System.Diagnostics.Debug.WriteLine("✅ Arka plan servisleri ön yüklendi (prewarm)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Servis prewarm hatası: {ex.Message}");
            }
        }
    }
}

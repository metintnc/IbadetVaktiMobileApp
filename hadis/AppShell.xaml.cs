namespace hadis
{
    using hadis.Helpers;
    using hadis.Services;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Maui.Platform;

    public partial class AppShell : Shell
    {
        private readonly IServiceProvider _serviceProvider;
        private bool _isTabTransitionRunning;
        
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

        public async Task ChangeTabAsync(string targetTab)
        {
            if (_isTabTransitionRunning || CurrentItem == null)
            {
                return;
            }

            var targetIndex = targetTab switch
            {
                "Vakitler" => 0,
                "Kutuphane" => 1,
                "Kible" => 2,
                "Zikir" => 3,
                "Ayarlar" => 4,
                _ => -1
            };

            if (targetIndex < 0 || targetIndex >= CurrentItem.Items.Count)
            {
                return;
            }

            var currentIndex = CurrentItem.Items.IndexOf(CurrentItem.CurrentItem);
            if (currentIndex == targetIndex)
            {
                return;
            }

            _isTabTransitionRunning = true;

            try
            {
                var direction = targetIndex > currentIndex ? 1 : -1;
                var previousContent = (CurrentPage as ContentPage)?.Content as VisualElement;
                var transitionWidth = GetTransitionWidth();

                if (previousContent != null)
                {
                    previousContent.CancelAnimations();
                    await Task.WhenAll(
                        previousContent.FadeTo(0.92, 140, Easing.CubicIn),
                        previousContent.TranslateTo(-direction * transitionWidth, 0, 180, Easing.CubicIn));
                }

                CurrentItem.CurrentItem = CurrentItem.Items[targetIndex];
                await MainThread.InvokeOnMainThreadAsync(() => { });

                var nextContent = (CurrentPage as ContentPage)?.Content as VisualElement;
                if (nextContent == null)
                {
                    return;
                }

                nextContent.CancelAnimations();
                nextContent.Opacity = 0.92;
                nextContent.TranslationX = direction * transitionWidth;

                await Task.WhenAll(
                    nextContent.FadeTo(1, 240, Easing.CubicOut),
                    nextContent.TranslateTo(0, 0, 280, Easing.CubicOut));
            }
            finally
            {
                _isTabTransitionRunning = false;
            }
        }

        private static double GetTransitionWidth()
        {
            var width = Shell.Current?.CurrentPage?.Width ?? 0;
            if (width > 0)
            {
                return width;
            }

            return DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density;
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

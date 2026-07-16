namespace hadis
{
    using hadis.Helpers;
    using hadis.Services;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Maui.Platform;

    public partial class AppShell : Shell
    {
        private readonly IServiceProvider _serviceProvider;
        
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

namespace hadis
{
    using hadis.Helpers;
    using hadis.Services;
    using Microsoft.Extensions.DependencyInjection;

    public partial class AppShell : Shell
    {
        private readonly IServiceProvider _serviceProvider;
        
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
    }
}

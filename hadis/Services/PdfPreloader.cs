using System.IO;

namespace hadis.Services
{
    public interface IPdfPreloader
    {
        bool IsLoaded { get; }
        MemoryStream? PdfStream { get; }
        Task PreloadAsync();
    }

    public class PdfPreloader : IPdfPreloader
    {
        private readonly object _lock = new object();
        public static PdfPreloader? Instance { get; private set; }
        public bool IsLoaded { get; private set; }
        public MemoryStream? PdfStream { get; private set; }

        public PdfPreloader()
        {
            Instance = this;
        }

        public async Task PreloadAsync()
        {
            lock (_lock)
            {
                if (IsLoaded) return;
            }

            try
            {
                using Stream assetStream = await FileSystem.OpenAppPackageFileAsync("kuran.pdf");
                var ms = new MemoryStream();
                await assetStream.CopyToAsync(ms);
                ms.Position = 0;

                // store the preloaded stream
                lock (_lock)
                {
                    PdfStream?.Dispose();
                    PdfStream = ms;
                    IsLoaded = true;
                }
            }
            catch
            {
                // swallow errors; fallback will load from disk later
            }
        }
    }
}

using hadis.Services;

namespace hadis.Services
{
    public class PlatformImageService : IImageService
    {
        public Task<ImageSource> GetOptimizedBackgroundImageAsync(string filename)
        {
            // Diğer platformlarda (veya Assets'ten okunamayan durumlarda) standart yöntem
            return Task.FromResult(ImageSource.FromFile(filename));
        }
    }
}

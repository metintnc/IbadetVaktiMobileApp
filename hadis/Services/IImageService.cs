using Microsoft.Maui.Graphics;

namespace hadis.Services
{
    public interface IImageService
    {
        Task<ImageSource> GetOptimizedBackgroundImageAsync(string filename);
    }
}

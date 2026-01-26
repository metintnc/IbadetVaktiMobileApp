using Android.Graphics;
using hadis.Services;

namespace hadis.Platforms.Android.Services
{
    public class AndroidImageService : IImageService
    {
        public Task<ImageSource> GetOptimizedBackgroundImageAsync(string filename)
        {
            return Task.Run<ImageSource>(() =>
            {
                try
                {
                    // 1. Ekran Boyutlarını Al
                    var displayMetrics = global::Android.App.Application.Context.Resources.DisplayMetrics;
                    int reqWidth = displayMetrics.WidthPixels;
                    int reqHeight = displayMetrics.HeightPixels;

                    // 2. Decode Options
                    BitmapFactory.Options options = new BitmapFactory.Options
                    {
                        InJustDecodeBounds = true
                    };

                    Bitmap bitmap = null;
                    
                    // A. Dosya Sistemi Kontrolü
                    if (File.Exists(filename))
                    {
                        BitmapFactory.DecodeFile(filename, options);
                        options.InSampleSize = CalculateInSampleSize(options, reqWidth, reqHeight);
                        options.InJustDecodeBounds = false;
                        bitmap = BitmapFactory.DecodeFile(filename, options);
                    }
                    else
                    {
                        // B. Assets Kontrolü (Maui içinde 'Resources/Raw' veya root assets olabilir)
                        // Genelde Maui 'filename'i olduğu gibi Asset olarak paketler.
                        try 
                        {
                            using (var stream = global::Android.App.Application.Context.Assets.Open(filename))
                            {
                                BitmapFactory.DecodeStream(stream, null, options);
                            }
                            
                            // Stream'i tekrar açmamız lazım (DecodeStream stream'i tüketir)
                            options.InSampleSize = CalculateInSampleSize(options, reqWidth, reqHeight);
                            options.InJustDecodeBounds = false;
                            
                            using (var stream2 = global::Android.App.Application.Context.Assets.Open(filename))
                            {
                                bitmap = BitmapFactory.DecodeStream(stream2, null, options);
                            }
                        }
                        catch
                        {
                            // Asset'te de yoksa fallback
                            return ImageSource.FromFile(filename);
                        }
                    }

                    if (bitmap != null)
                    {
                        // Bitmap'i Stream'e çevir ve ImageSource olarak döndür
                        MemoryStream ms = new MemoryStream();
                        bitmap.Compress(Bitmap.CompressFormat.Jpeg, 80, ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        return ImageSource.FromStream(() => new MemoryStream(ms.ToArray())); // Copy to avoid closed stream issues
                    }
                    
                    return ImageSource.FromFile(filename);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Image Opt Hatası: {ex.Message}");
                    return ImageSource.FromFile(filename);
                }
            });
        }

        public static int CalculateInSampleSize(BitmapFactory.Options options, int reqWidth, int reqHeight)
        {
            // Raw height and width of image
            int height = options.OutHeight;
            int width = options.OutWidth;
            int inSampleSize = 1;

            if (height > reqHeight || width > reqWidth)
            {
                int halfHeight = height / 2;
                int halfWidth = width / 2;

                // Calculate the largest inSampleSize value that is a power of 2 and keeps both
                // height and width larger than the requested height and width.
                while ((halfHeight / inSampleSize) >= reqHeight && (halfWidth / inSampleSize) >= reqWidth)
                {
                    inSampleSize *= 2;
                }
            }

            return inSampleSize;
        }
    }
}

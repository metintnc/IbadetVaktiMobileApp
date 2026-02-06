
using Microsoft.Extensions.Logging;
using hadis.Services;
using Plugin.LocalNotification;

namespace hadis
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    // fonts.AddFont("Amiri-Regular.ttf", "ArabicFontFamily"); // Font dosyası eksik olduğu için geçici olarak kapatıldı
                });

            // Servisleri kaydet
            builder.Services.AddSingleton<StatusBarService>();
            builder.Services.AddSingleton<TabBarService>();
            builder.Services.AddSingleton<BackgroundService>();
            builder.Services.AddSingleton<ThemeService>();
            
#if ANDROID
            builder.Services.AddSingleton<INativeCompassService, hadis.Platforms.Android.Services.AndroidCompassService>();
            builder.Services.AddSingleton<IImageService, hadis.Platforms.Android.Services.AndroidImageService>();
#else
            builder.Services.AddSingleton<INativeCompassService, hadis.Services.PlatformCompassService>();
            builder.Services.AddSingleton<IImageService, hadis.Services.PlatformImageService>();
#endif

            // Pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<kible>();
            builder.Services.AddTransient<zikirmatik>();
            builder.Services.AddTransient<Kuran>();
            builder.Services.AddTransient<Ayarlar>();
            builder.Services.AddTransient<BildirimAyarlari>();

            // Notification Service
            builder.Services.AddSingleton<IAppNotificationService, NotificationService>();
            
            // Local Notification
            try
            {
#if ANDROID || IOS
                builder.UseLocalNotification();
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LocalNotification initialization failed: {ex.Message}");
            }

            return builder.Build();
        }
    }
}

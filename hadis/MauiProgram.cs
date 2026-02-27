using Microsoft.Extensions.Logging;
using hadis.Services;
using hadis.ViewModels;
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
                });

            // Servisleri kaydet
            builder.Services.AddSingleton<StatusBarService>();
            builder.Services.AddSingleton<TabBarService>();
            builder.Services.AddSingleton<BackgroundService>();
            builder.Services.AddSingleton<ThemeService>();
            
            // HttpClient yapılandırması - Socket exhaustion önleme
            builder.Services.AddHttpClient();
            builder.Services.AddHttpClient("QuranApi", client =>
            {
                client.BaseAddress = new Uri("https://api.acikkuran.com/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            
            builder.Services.AddSingleton<PrayerTimesService>();
            builder.Services.AddSingleton<QuranApiService>(); // Artık DI ile yönetiliyor
            
#if ANDROID
            builder.Services.AddSingleton<INativeCompassService, hadis.Platforms.Android.Services.AndroidCompassService>();
            builder.Services.AddSingleton<IImageService, hadis.Platforms.Android.Services.AndroidImageService>();
#else
            builder.Services.AddSingleton<INativeCompassService, hadis.Services.PlatformCompassService>();
            builder.Services.AddSingleton<IImageService, hadis.Services.PlatformImageService>();
#endif

            // Pages
            builder.Services.AddTransient<MainPageViewModel>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<kible>();
            builder.Services.AddTransient<zikirmatik>();
            builder.Services.AddTransient<Kuran>();
            builder.Services.AddTransient<Ayarlar>();
            builder.Services.AddTransient<BildirimAyarlari>();
            builder.Services.AddTransient<SehirSecimViewModel>();
            builder.Services.AddTransient<SehirSecim>();
            builder.Services.AddTransient<TemaAyarlari>();
            builder.Services.AddTransient<YakindakiCamiler>();
            builder.Services.AddTransient<HicriTakvim>();
            builder.Services.AddTransient<Kutuphane>();

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

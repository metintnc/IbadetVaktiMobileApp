using Maui.PDFView;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core.Hosting;
using hadis.Services;

namespace hadis
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JFaF1cX2hIf0x0R3xbf1x1ZFBMZVlbRXdPMyBoS35Rc0RjW3xedXFQR2VaVEdxVEFc");

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureSyncfusionCore() 
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Amiri-Regular.ttf", "ArabicFontFamily");
                });

            // Servisleri kaydet
            builder.Services.AddSingleton<StatusBarService>();
            builder.Services.AddSingleton<TabBarService>();
            builder.Services.AddSingleton<BackgroundService>();
            builder.Services.AddSingleton<ThemeService>();

            builder.Services.AddSingleton<ThemeService>();
            
#if ANDROID
            builder.Services.AddSingleton<INativeCompassService, hadis.Platforms.Android.Services.AndroidCompassService>();
#else
            builder.Services.AddSingleton<INativeCompassService, hadis.Services.PlatformCompassService>();
#endif

            // Pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<kible>();
            builder.Services.AddTransient<zikirmatik>();
            builder.Services.AddTransient<Kuran>();
            builder.Services.AddTransient<Ayarlar>();

            return builder.Build();
        }
    }
}

using Maui.PDFView;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core.Hosting;

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

            return builder.Build();
        }
    }
}

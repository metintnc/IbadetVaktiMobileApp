namespace hadis
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            
            // Kaydedilmiş tema tercihini yükle
            LoadThemePreference();
            
            // Status bar rengini ayarla
            UpdateStatusBarColor();
            
            // Tema değişikliklerini dinle
            RequestedThemeChanged += OnRequestedThemeChanged;
            
            // Uygulamayı açar açmaz arkaplanda PDF'i önbelleğe al
            _ = OnbellekPdfAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
        
        private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            UpdateStatusBarColor();
        }
        
        private void UpdateStatusBarColor()
        {
            // Aktif temayı al
            var currentTheme = UserAppTheme == AppTheme.Unspecified 
                ? Application.Current?.RequestedTheme ?? AppTheme.Light 
                : UserAppTheme;

#if ANDROID
            // Activity hazır olana kadar bekle
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                // Küçük bir gecikme ekle, Activity'nin tam hazır olmasını bekle
                await Task.Delay(100);
                
                Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.RunOnUiThread(() =>
                {
                    var window = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window;
                    if (window != null)
                    {
                        if (currentTheme == AppTheme.Dark)
                        {
                            // Koyu tema - siyah status bar
                            window.SetStatusBarColor(Android.Graphics.Color.Black);
                            
                            // Android 6.0 ve üzeri için metin rengini ayarla
                            // Android 11 (API 30) ve üzeri için WindowInsetsController kullanımı
                            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
                            {
                                window.InsetsController?.SetSystemBarsAppearance(
                                    0, // Clear LightStatusBars flag (become white text)
                                    (int)Android.Views.WindowInsetsControllerAppearance.LightStatusBars);
                            }
                            else if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
                            {
#pragma warning disable CS0618 // Type or member is obsolete
                                // Koyu tema için açık renkli iconlar (SystemUiVisibility)
                                window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)
                                    Android.Views.SystemUiFlags.Visible;
#pragma warning restore CS0618 // Type or member is obsolete
                            }
                        }
                        else
                        {
                            // Açık tema - beyaz status bar
                            window.SetStatusBarColor(Android.Graphics.Color.White);
                            
                            // Android 11 (API 30) ve üzeri için WindowInsetsController kullanımı
                            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.R)
                            {
                                window.InsetsController?.SetSystemBarsAppearance(
                                    (int)Android.Views.WindowInsetsControllerAppearance.LightStatusBars,
                                    (int)Android.Views.WindowInsetsControllerAppearance.LightStatusBars);
                            }
                            else if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
                            {
#pragma warning disable CS0618 // Type or member is obsolete
                                // Açık tema için koyu renkli iconlar
                                window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)
                                    (Android.Views.SystemUiFlags.LightStatusBar);
#pragma warning restore CS0618 // Type or member is obsolete
                            }
                        }
                    }
                });
            });
#elif IOS || MACCATALYST
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                if (currentTheme == AppTheme.Dark)
                {
                    UIKit.UIApplication.SharedApplication.SetStatusBarStyle(UIKit.UIStatusBarStyle.LightContent, false);
                }
                else
                {
                    if (UIKit.UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
                    {
                        UIKit.UIApplication.SharedApplication.SetStatusBarStyle(UIKit.UIStatusBarStyle.DarkContent, false);
                    }
                    else
                    {
                        UIKit.UIApplication.SharedApplication.SetStatusBarStyle(UIKit.UIStatusBarStyle.Default, false);
                    }
                }
            });
#endif
        }
        
        private void LoadThemePreference()
        {
            // AppConstants kullanarak tema tercihini al
            string savedTheme = Preferences.Default.Get(Helpers.AppConstants.PREF_APP_THEME, "MainDark");
            
            switch (savedTheme)
            {
                case "MainLight": // Ana Tema (Açık) - Dynamic
                    UserAppTheme = AppTheme.Light;
                    break;
                case "MainDark": // Ana Tema (Koyu) - Dynamic
                    UserAppTheme = AppTheme.Dark;
                    break;
                case "Light": // Açık (Sabit)
                    UserAppTheme = AppTheme.Light;
                    break;
                case "PitchBlack": // Simsiyah (Sabit)
                    UserAppTheme = AppTheme.Dark;
                    break;
                case "Custom": // Özel Tema
                    UserAppTheme = AppTheme.Dark;
                    break;
                default:
                    // Fallback - Ana Koyu
                    UserAppTheme = AppTheme.Dark;
                    break;
            }
        }
        
        private async Task OnbellekPdfAsync()
        {
            try
            {
                await Task.Run(async () =>
                {
                    string localPdfPath = Path.Combine(FileSystem.AppDataDirectory, "kuran.pdf");
                    
                    // Dosya zaten varsa kopyalamayı atla
                    if (File.Exists(localPdfPath))
                        return;
                    
                    // PDF'i arkaplanda kopyala
                    using (Stream assetStream = await FileSystem.OpenAppPackageFileAsync("kuran.pdf"))
                    {
                        using (FileStream fileStream = new FileStream(localPdfPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                        {
                            await assetStream.CopyToAsync(fileStream, 81920);
                            await fileStream.FlushAsync();
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine("PDF önbelleğe alındı");
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PDF önbellekleme hatası: {ex.Message}");
            }
        }
    }
}
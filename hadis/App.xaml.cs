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
                            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
                            {
                                // Koyu tema için açık renkli iconlar
                                window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)
                                    Android.Views.SystemUiFlags.Visible;
                            }
                        }
                        else
                        {
                            // Açık tema - beyaz status bar
                            window.SetStatusBarColor(Android.Graphics.Color.White);
                            
                            // Android 6.0 ve üzeri için metin rengini ayarla
                            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
                            {
                                // Açık tema için koyu renkli iconlar
                                window.DecorView.SystemUiVisibility = (Android.Views.StatusBarVisibility)
                                    (Android.Views.SystemUiFlags.LightStatusBar);
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
            string savedTheme = Preferences.Default.Get("AppTheme", "System");
            
            switch (savedTheme)
            {
                case "Light":
                    UserAppTheme = AppTheme.Light;
                    break;
                case "Dark":
                    UserAppTheme = AppTheme.Dark;
                    break;
                case "System":
                default:
                    UserAppTheme = AppTheme.Unspecified;
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
using hadis.Services;
using Microsoft.Extensions.DependencyInjection;

namespace hadis
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;

        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            
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
            try
            {
                // AppShell'i DI'dan al (prewarm desteği için)
                var appShell = _serviceProvider.GetRequiredService<AppShell>();
                var window = new Window(appShell);
                
                // Uygulama lifecycle event'larını dinle - Batarya tasarrufu için
                // Lifecycle events'i ekle, ancak status bar renklendirmesini geciktime ile yap
                window.Resumed += OnWindowResumed;
                window.Stopped += OnWindowStopped;
                window.Destroying += OnWindowDestroying;
                
                // Status bar renklendirmesi window yüklendikten sonra yapılsın
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(100);
                    UpdateStatusBarColor();
                });
                
                return window;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Window oluşturma hatası: {ex.Message}");
                // Fallback: standart window
                var appShell = _serviceProvider.GetRequiredService<AppShell>();
                return new Window(appShell);
            }
        }

        /// <summary>
        /// Uygulama ön plana geldiğinde
        /// </summary>
        private void OnWindowResumed(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("📱 Uygulama ön plana geldi");
            // PersistentNotificationUpdater MainPage tarafından yeniden başlatılacak
        }

        /// <summary>
        /// Uygulama arka plana geçtiğinde - Timer'ları durdur
        /// </summary>
        private void OnWindowStopped(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("📱 Uygulama arka plana geçti - Timer'lar durduruluyor");
            PersistentNotificationUpdater.StopUpdating();
        }

        /// <summary>
        /// Uygulama kapanırken - Tüm kaynakları temizle
        /// </summary>
        private void OnWindowDestroying(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("📱 Uygulama kapanıyor - Kaynaklar temizleniyor");
            PersistentNotificationUpdater.StopUpdating();
        }
        
        private void OnRequestedThemeChanged(object? sender, AppThemeChangedEventArgs e)
        {
            try
            {
                UpdateStatusBarColor();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Tema değişikliği işleme hatası: {ex.Message}");
            }
        }
        
        private void UpdateStatusBarColor()
        {
            // Aktif temayı al
            var currentTheme = UserAppTheme == AppTheme.Unspecified 
                ? Application.Current?.RequestedTheme ?? AppTheme.Light 
                : UserAppTheme;

#if ANDROID
            // Activity hazır olana kadar bekle - MauiContext'in tamamen yüklendiğinden emin ol
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    // Daha uzun bir gecikme ekle, Window ve MauiContext'in tam hazır olmasını bekle
                    await Task.Delay(200);
                    
                    var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                    if (activity?.Window == null)
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ Activity.Window henüz hazır değil");
                        return;
                    }

                    activity.RunOnUiThread(() =>
                    {
                        try
                        {
                            var window = activity.Window;
                            if (window == null)
                                return;

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
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Status bar renk ayarlama hatası: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Status bar güncelleme hatası: {ex.Message}");
                }
            });
#elif IOS || MACCATALYST
            Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
            {
                try
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
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Status bar güncelleme hatası: {ex.Message}");
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
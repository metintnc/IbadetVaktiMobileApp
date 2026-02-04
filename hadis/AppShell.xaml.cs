namespace hadis
{
    using hadis.Helpers;
    using hadis.Services;

    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
        }

        protected override void OnNavigated(ShellNavigatedEventArgs args)
        {
            base.OnNavigated(args);
            UpdateTabBarColor();
        }

        private void UpdateTabBarColor()
        {
            try
            {
                string savedTheme = Preferences.Default.Get(AppConstants.PREF_APP_THEME, "MainDark");

                // Sadece "MainLight" (Ana Açık) teması için özel mantık
                if (savedTheme == "MainLight")
                {
                    // Şuan hangi sayfadayız?
                    var currentPage = Current.CurrentPage;

                    if (currentPage is MainPage)
                    {
                        // Ana Sayfa: Zamana göre dinamik renk
                        var now = DateTime.Now;
                        var info = TimeBasedBackgroundConfig.GetBackgroundForTime(now.Hour, now.Minute);
                        Shell.SetTabBarBackgroundColor(this, Color.FromArgb(info.TabBarColor));
                    }
                    else
                    {
                        // Diğer Sayfalar: Beyaz
                        Shell.SetTabBarBackgroundColor(this, Colors.White);
                    }
                }
                // "MainDark" (Ana Koyu) teması için özel mantık
                else if (savedTheme == "MainDark")
                {
                    var currentPage = Current.CurrentPage;

                    if (currentPage is MainPage)
                    {
                        // Ana Sayfa: Dinamik renk
                        var now = DateTime.Now;
                        var info = TimeBasedBackgroundConfig.GetBackgroundForTime(now.Hour, now.Minute);
                        Shell.SetTabBarBackgroundColor(this, Color.FromArgb(info.TabBarColor));
                    }
                    else
                    {
                        // Diğer Sayfalar: Siyah
                        Shell.SetTabBarBackgroundColor(this, Colors.Black);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TabBar update error: {ex.Message}");
            }
        }
    }
}

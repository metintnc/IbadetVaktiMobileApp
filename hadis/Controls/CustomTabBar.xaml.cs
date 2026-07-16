using Microsoft.Maui.Platform;

#if ANDROID
using Android.Widget;
#elif IOS || MACCATALYST
using UIKit;
#endif

namespace hadis.Controls
{
    public partial class CustomTabBar : ContentView
    {
        public static readonly BindableProperty CurrentTabProperty =
            BindableProperty.Create(nameof(CurrentTab), typeof(string), typeof(CustomTabBar), "Vakitler",
                propertyChanged: OnCurrentTabChanged);

        public string CurrentTab
        {
            get => (string)GetValue(CurrentTabProperty);
            set => SetValue(CurrentTabProperty, value);
        }

        public CustomTabBar()
        {
            InitializeComponent();
        }

        protected override void OnParentSet()
        {
            base.OnParentSet();
            UpdateTabStates();
        }

        private static void OnCurrentTabChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is CustomTabBar tabBar)
            {
                tabBar.UpdateTabStates();
            }
        }

        private void UpdateTabStates()
        {
            // Reset all tabs to inactive state
            ResetTab(ImgVakitler, LblVakitler, DotVakitler);
            ResetTab(ImgKutuphane, LblKutuphane, DotKutuphane);
            ResetTab(ImgKible, LblKible, DotKible);
            ResetTab(ImgZikir, LblZikir, DotZikir);
            ResetTab(ImgAyarlar, LblAyarlar, DotDotAyarlar);

            // Set current tab to active state
            switch (CurrentTab)
            {
                case "Vakitler":
                    SetActiveTab(ImgVakitler, LblVakitler, DotVakitler);
                    break;
                case "Kutuphane":
                    SetActiveTab(ImgKutuphane, LblKutuphane, DotKutuphane);
                    break;
                case "Kible":
                    SetActiveTab(ImgKible, LblKible, DotKible);
                    break;
                case "Zikir":
                    SetActiveTab(ImgZikir, LblZikir, DotZikir);
                    break;
                case "Ayarlar":
                    SetActiveTab(ImgAyarlar, LblAyarlar, DotDotAyarlar);
                    break;
            }
        }

        private void ResetTab(Image img, Label lbl, Microsoft.Maui.Controls.Shapes.Shape dot)
        {
            if (img == null || lbl == null || dot == null) return;
            img.Scale = 1.0;
            img.Opacity = 0.55;
            lbl.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#757575"), Color.FromArgb("#BDBDBD"));
            lbl.FontAttributes = FontAttributes.None;
            dot.Opacity = 0;
            ApplyTint(img, null);
        }

        private void SetActiveTab(Image img, Label lbl, Microsoft.Maui.Controls.Shapes.Shape dot)
        {
            if (img == null || lbl == null || dot == null) return;
            img.Scale = 1.12;
            img.Opacity = 1.0;
            lbl.TextColor = Color.FromArgb("#00BCD4"); // Cyan Accent
            lbl.FontAttributes = FontAttributes.Bold;
            dot.Opacity = 1;
            ApplyTint(img, Color.FromArgb("#00BCD4"));
        }

        private void OnTabTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is string targetTab)
            {
                if (targetTab == CurrentTab) return;

                int index = targetTab switch
                {
                    "Vakitler" => 0,
                    "Kutuphane" => 1,
                    "Kible" => 2,
                    "Zikir" => 3,
                    "Ayarlar" => 4,
                    _ => 0
                };

                var tabBar = Shell.Current?.CurrentItem;
                if (tabBar != null && index >= 0 && index < tabBar.Items.Count)
                {
                    tabBar.CurrentItem = tabBar.Items[index];
                }
            }
        }

        private void ApplyTint(Image img, Color? color)
        {
            if (img == null) return;

            // Apply immediately if handler is available
            SetNativeTint(img, color);

            // Also listen to HandlerChanged in case handler is not created yet
            img.HandlerChanged -= OnImageHandlerChanged;
            img.HandlerChanged += OnImageHandlerChanged;

            void OnImageHandlerChanged(object? sender, EventArgs e)
            {
                if (sender is Image senderImg)
                {
                    SetNativeTint(senderImg, color);
                }
            }
        }

        private void SetNativeTint(Image img, Color? color)
        {
#if ANDROID
            if (img.Handler?.PlatformView is ImageView imageView)
            {
                if (color == null)
                {
                    imageView.ClearColorFilter();
                }
                else
                {
                    imageView.SetColorFilter(color.ToPlatform());
                }
            }
#elif IOS || MACCATALYST
            if (img.Handler?.PlatformView is UIImageView imageView)
            {
                if (imageView.Image != null)
                {
                    if (color == null)
                    {
                        imageView.Image = imageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysOriginal);
                    }
                    else
                    {
                        imageView.Image = imageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
                        imageView.TintColor = color.ToPlatform();
                    }
                }
            }
#endif
        }
    }
}

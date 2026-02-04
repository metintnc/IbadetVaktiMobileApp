using hadis.Models;
using hadis.Helpers;
using System.Text.Json;

namespace hadis
{
    public partial class OzelTemaOlustur : ContentPage
    {
        private byte _red = 0;
        private byte _green = 121;
        private byte _blue = 107;
        private double _opacity = 1.0;

        private CustomTheme _currentTheme;

        public OzelTemaOlustur()
        {
            InitializeComponent();
            InitializeTheme();
        }

        private void InitializeTheme()
        {
            // Koyu tema renklerini baz alarak yeni tema olustur
            _currentTheme = new CustomTheme
            {
                Name = "Yeni Ozel Tema",
                MainFrameBackground = "#00000000",
                MainFrameBorder = "#00796B",
                MainFrameText = "#E0E0E0",
                SmallFrameBackground = "#00000000",
                SmallFrameBorder = "#00796B",
                SmallFrameText = "#E0E0E0",
                AyetFrameBackground = "#00000000",
                AyetFrameBorder = "#00796B",
                AyetFrameText = "#E0E0E0",
                BackgroundImage = "bg_dark.jpg"
            };

            // Onizlemeye baslangic renklerini uygula
            ApplyThemeToPreview();
        }

        private void ApplyThemeToPreview()
        {
            // Ana Frame
            OnizlemeFrame.BackgroundColor = Color.FromArgb(_currentTheme.MainFrameBackground);
            OnizlemeFrame.BorderColor = Color.FromArgb(_currentTheme.MainFrameBorder);
            NamazLabel.TextColor = Color.FromArgb(_currentTheme.MainFrameText);
            SaatLabel.TextColor = Color.FromArgb(_currentTheme.MainFrameText);
            KaldiLabel.TextColor = Color.FromArgb(_currentTheme.MainFrameText);
            KonumLabel.TextColor = Color.FromArgb(_currentTheme.MainFrameText);

            // Kucuk Frame'ler
            ImsakKucukFrame.BackgroundColor = Color.FromArgb(_currentTheme.SmallFrameBackground);
            ImsakKucukFrame.BorderColor = Color.FromArgb(_currentTheme.SmallFrameBorder);
            ImsakYaziLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);
            ImsakVakitLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);

            GunesKucukFrame.BackgroundColor = Color.FromArgb(_currentTheme.SmallFrameBackground);
            GunesKucukFrame.BorderColor = Color.FromArgb(_currentTheme.SmallFrameBorder);
            GunesYaziLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);
            GunesVakitLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);

            OgleKucukFrame.BackgroundColor = Color.FromArgb(_currentTheme.SmallFrameBackground);
            OgleKucukFrame.BorderColor = Color.FromArgb(_currentTheme.SmallFrameBorder);
            OgleYaziLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);
            OgleVakitLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);

            IkindiKucukFrame.BackgroundColor = Color.FromArgb(_currentTheme.SmallFrameBackground);
            IkindiKucukFrame.BorderColor = Color.FromArgb(_currentTheme.SmallFrameBorder);
            IkindiYaziLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);
            IkindiVakitLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);

            AksamKucukFrame.BackgroundColor = Color.FromArgb(_currentTheme.SmallFrameBackground);
            AksamKucukFrame.BorderColor = Color.FromArgb(_currentTheme.SmallFrameBorder);
            AksamYaziLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);
            AksamVakitLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);

            YatsiKucukFrame.BackgroundColor = Color.FromArgb(_currentTheme.SmallFrameBackground);
            YatsiKucukFrame.BorderColor = Color.FromArgb(_currentTheme.SmallFrameBorder);
            YatsiYaziLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);
            YatsiVakitLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);

            // Ayet Frame
            AyetFrame.BackgroundColor = Color.FromArgb(_currentTheme.AyetFrameBackground);
            AyetFrame.BorderColor = Color.FromArgb(_currentTheme.AyetFrameBorder);
            AyetLabel.TextColor = Color.FromArgb(_currentTheme.AyetFrameText);
        }

        private async void ArkaplanDegistir_Clicked(object sender, EventArgs e)
        {
            try
            {
                // Kullanıcıya seçenek sun: Galeri veya Hazır Arkaplanlar
                var choice = await DisplayActionSheet(
                    "Arkaplan Seç",
                    "İptal",
                    null,
                    "📸 Galeriden Resim Seç",
                    "🎨 Hazır Arkaplanlar"
                );

                if (choice == "📸 Galeriden Resim Seç")
                {
                    // Galeriden resim seç
                    var result = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
                    {
                        Title = "Arkaplan Resmi Seç"
                    });

                    if (result != null)
                    {
                        // Resmi uygulama dizinine kopyala
                        var fileName = $"custom_bg_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                        var localFilePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                        using (var sourceStream = await result.OpenReadAsync())
                        using (var localFileStream = File.Create(localFilePath))
                        {
                            await sourceStream.CopyToAsync(localFileStream);
                        }

                        // Temaya uygula
                        _currentTheme.BackgroundImage = localFilePath;
                        
                        await DisplayAlert("Başarılı", "Arkaplan resmi seçildi!", "Tamam");
                    }
                }
                else if (choice == "🎨 Hazır Arkaplanlar")
                {
                    // Hazır arkaplanlar sayfasına git
                    await Navigation.PushAsync(new ArkaplanSecim());
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Arkaplan seçilirken hata oluştu: {ex.Message}", "Tamam");
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Arkaplan secimi yapildiysa kaydet
            string tempBackground = Preferences.Default.Get("TempBackground", string.Empty);
            if (!string.IsNullOrEmpty(tempBackground))
            {
                _currentTheme.BackgroundImage = tempBackground;
                
                // Opacity degerini de kaydet
                _currentTheme.BackgroundOpacity = Preferences.Default.Get(AppConstants.PREF_BACKGROUND_OPACITY, AppConstants.DEFAULT_BACKGROUND_OPACITY);
                
                Preferences.Default.Remove("TempBackground");
            }
        }

        private async Task ShowBackgroundColorPicker()
        {
            var arkaplanSecimSayfasi = new ContentPage
            {
                Title = "Arkaplan Rengi Sec",
                BackgroundColor = Color.FromArgb("#2C2C2C")
            };

            var mainLayout = new VerticalStackLayout
            {
                Padding = 20,
                Spacing = 20
            };

            // Baslik
            mainLayout.Add(new Label
            {
                Text = "Arkaplan Rengi",
                FontSize = 24,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            });

            // Onizleme kutusu
            var onizlemeKutu = new BoxView
            {
                HeightRequest = 200,
                CornerRadius = 15,
                BackgroundColor = Color.FromRgba(_red / 255.0, _green / 255.0, _blue / 255.0, _opacity)
            };
            mainLayout.Add(onizlemeKutu);

            // RGB degerleri label
            var rgbLabel = new Label
            {
                Text = $"RGB({_red}, {_green}, {_blue}) - Opacity: {_opacity:P0}",
                FontSize = 16,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center
            };
            mainLayout.Add(rgbLabel);

            // Kirmizi Slider
            var redLabel = new Label { Text = $"Kirmizi: {_red}", TextColor = Colors.White };
            mainLayout.Add(redLabel);
            
            var redSlider = new Slider
            {
                Minimum = 0,
                Maximum = 255,
                Value = _red,
                MinimumTrackColor = Colors.Red,
                MaximumTrackColor = Colors.DarkRed,
                ThumbColor = Colors.Red
            };
            mainLayout.Add(redSlider);

            // Yesil Slider
            var greenLabel = new Label { Text = $"Yesil: {_green}", TextColor = Colors.White };
            mainLayout.Add(greenLabel);
            
            var greenSlider = new Slider
            {
                Minimum = 0,
                Maximum = 255,
                Value = _green,
                MinimumTrackColor = Colors.Green,
                MaximumTrackColor = Colors.DarkGreen,
                ThumbColor = Colors.Green
            };
            mainLayout.Add(greenSlider);

            // Mavi Slider
            var blueLabel = new Label { Text = $"Mavi: {_blue}", TextColor = Colors.White };
            mainLayout.Add(blueLabel);
            
            var blueSlider = new Slider
            {
                Minimum = 0,
                Maximum = 255,
                Value = _blue,
                MinimumTrackColor = Colors.Blue,
                MaximumTrackColor = Colors.DarkBlue,
                ThumbColor = Colors.Blue
            };
            mainLayout.Add(blueSlider);

            // Opacity Slider
            var opacityLabel = new Label { Text = $"Seffaflik: {_opacity:P0}", TextColor = Colors.White };
            mainLayout.Add(opacityLabel);
            
            var opacitySlider = new Slider
            {
                Minimum = 0,
                Maximum = 1,
                Value = _opacity,
                MinimumTrackColor = Colors.Gray,
                MaximumTrackColor = Colors.White,
                ThumbColor = Colors.White
            };
            mainLayout.Add(opacitySlider);

            // Slider degisim eventi
            void UpdateColor(object s, ValueChangedEventArgs e)
            {
                _red = (byte)redSlider.Value;
                _green = (byte)greenSlider.Value;
                _blue = (byte)blueSlider.Value;
                _opacity = opacitySlider.Value;

                var currentColor = Color.FromRgba(_red / 255.0, _green / 255.0, _blue / 255.0, _opacity);
                onizlemeKutu.BackgroundColor = currentColor;
                rgbLabel.Text = $"RGB({_red}, {_green}, {_blue}) - Opacity: {_opacity:P0}";
                redLabel.Text = $"Kirmizi: {_red}";
                greenLabel.Text = $"Yesil: {_green}";
                blueLabel.Text = $"Mavi: {_blue}";
                opacityLabel.Text = $"Seffaflik: {_opacity:P0}";
            }

            redSlider.ValueChanged += UpdateColor;
            greenSlider.ValueChanged += UpdateColor;
            blueSlider.ValueChanged += UpdateColor;
            opacitySlider.ValueChanged += UpdateColor;

            // Alt butonlar
            var butonLayout = new HorizontalStackLayout
            {
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Fill,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var iptalButon = new Button
            {
                Text = "Iptal",
                BackgroundColor = Colors.Gray,
                TextColor = Colors.White,
                CornerRadius = 15,
                Padding = new Thickness(15, 12),
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            iptalButon.Clicked += async (s, e) => await Navigation.PopAsync();

            var uygula = new Button
            {
                Text = "Uygula",
                BackgroundColor = Color.FromArgb("#00796B"),
                TextColor = Colors.White,
                CornerRadius = 15,
                Padding = new Thickness(15, 12),
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            uygula.Clicked += async (s, e) =>
            {
                var selectedColor = Color.FromRgba(_red / 255.0, _green / 255.0, _blue / 255.0, _opacity);
                
                // Ozel arkaplan rengi olarak kaydet
                _currentTheme.BackgroundImage = selectedColor.ToArgbHex();

                await DisplayAlert("Basarili", "Ozel arkaplan rengi secildi", "Tamam");
                await Navigation.PopAsync();
            };

            butonLayout.Add(iptalButon);
            butonLayout.Add(uygula);
            mainLayout.Add(butonLayout);

            arkaplanSecimSayfasi.Content = new ScrollView { Content = mainLayout };
            await Navigation.PushAsync(arkaplanSecimSayfasi);
        }

        private async void FrameRengiDegistir_Clicked(object sender, EventArgs e)
        {
            await ShowColorPicker("Frame Arka Plan Rengi", (color) =>
            {
                OnizlemeFrame.BackgroundColor = color;
                _currentTheme.MainFrameBackground = color.ToArgbHex();
            });
        }

        private async void CerceveRengiDegistir_Clicked(object sender, EventArgs e)
        {
            await ShowColorPicker("Cerceve Rengi", (color) =>
            {
                // Frame'in border rengini degistir
                if (OnizlemeFrame is Frame frame)
                {
                    frame.BorderColor = color;
                    _currentTheme.MainFrameBorder = color.ToArgbHex();
                }
            });
        }

        private async void YaziRengiDegistir_Clicked(object sender, EventArgs e)
        {
            await ShowColorPicker("Yazi Rengi", (color) =>
            {
                // Tum label'larin rengini degistir
                NamazLabel.TextColor = color;
                SaatLabel.TextColor = color;
                KaldiLabel.TextColor = color;
                KonumLabel.TextColor = color;
                _currentTheme.MainFrameText = color.ToArgbHex();
            });
        }

        private async void KucukFrameRengiDegistir_Clicked(object sender, EventArgs e)
        {
            await ShowColorPicker("Kucuk Frame Rengi", (color) =>
            {
                // Tum kucuk frame'lerin arka plan rengini degistir
                ImsakKucukFrame.BackgroundColor = color;
                GunesKucukFrame.BackgroundColor = color;
                OgleKucukFrame.BackgroundColor = color;
                IkindiKucukFrame.BackgroundColor = color;
                AksamKucukFrame.BackgroundColor = color;
                YatsiKucukFrame.BackgroundColor = color;
                _currentTheme.SmallFrameBackground = color.ToArgbHex();
            });
        }

        private async void KucukCerceveRengiDegistir_Clicked(object sender, EventArgs e)
        {
            await ShowColorPicker("Kucuk Cerceve Rengi", (color) =>
            {
                // Tum kucuk frame'lerin cerceve rengini degistir
                ImsakKucukFrame.BorderColor = color;
                GunesKucukFrame.BorderColor = color;
                OgleKucukFrame.BorderColor = color;
                IkindiKucukFrame.BorderColor = color;
                AksamKucukFrame.BorderColor = color;
                YatsiKucukFrame.BorderColor = color;
                _currentTheme.SmallFrameBorder = color.ToArgbHex();
            });
        }

        private async void KucukYaziRengiDegistir_Clicked(object sender, EventArgs e)
        {
            await ShowColorPicker("Kucuk Frame Yazi Rengi", (color) =>
            {
                // Tum kucuk frame'lerin yazi rengini degistir
                ImsakYaziLabel.TextColor = color;
                ImsakVakitLabel.TextColor = color;
                GunesYaziLabel.TextColor = color;
                GunesVakitLabel.TextColor = color;
                OgleYaziLabel.TextColor = color;
                OgleVakitLabel.TextColor = color;
                IkindiYaziLabel.TextColor = color;
                IkindiVakitLabel.TextColor = color;
                AksamYaziLabel.TextColor = color;
                AksamVakitLabel.TextColor = color;
                YatsiYaziLabel.TextColor = color;
                YatsiVakitLabel.TextColor = color;
                _currentTheme.SmallFrameText = color.ToArgbHex();
            });
        }

        private async void AyetFrameRengiDegistir_Clicked(object sender, EventArgs e)
        {
            await ShowColorPicker("Ayet Frame Rengi", (color) =>
            {
                AyetFrame.BackgroundColor = color;
                _currentTheme.AyetFrameBackground = color.ToArgbHex();
            });
        }

        private async void AyetCerceveRengiDegistir_Clicked(object sender, EventArgs e)
        {
            await ShowColorPicker("Ayet Cerceve Rengi", (color) =>
            {
                AyetFrame.BorderColor = color;
                _currentTheme.AyetFrameBorder = color.ToArgbHex();
            });
        }

        private async void AyetYaziRengiDegistir_Clicked(object sender, EventArgs e)
        {
            await ShowColorPicker("Ayet Yazi Rengi", (color) =>
            {
                AyetLabel.TextColor = color;
                _currentTheme.AyetFrameText = color.ToArgbHex();
            });
        }

        private async Task ShowColorPicker(string title, Action<Color> onColorSelected)
        {
            var renkSecimSayfasi = new ContentPage
            {
                Title = title,
                BackgroundColor = Color.FromArgb("#1E1E1E")
            };

            var scrollView = new ScrollView();
            var mainLayout = new VerticalStackLayout
            {
                Padding = 15,
                Spacing = 15
            };

            // Başlık
            mainLayout.Add(new Label
            {
                Text = "Gelişmiş Renk Seçici",
                FontSize = 24,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#81C784")
            });

            // Önizleme kutusu
            var onizlemeKutu = new BoxView
            {
                HeightRequest = 80,
                CornerRadius = 15,
                BackgroundColor = Color.FromRgba(_red / 255.0, _green / 255.0, _blue / 255.0, _opacity)
            };
            mainLayout.Add(onizlemeKutu);

            // RGB Label
            var rgbLabel = new Label
            {
                Text = $"RGB({_red}, {_green}, {_blue}) - Şeffaflık: {_opacity:P0}",
                FontSize = 14,
                TextColor = Colors.White,
                HorizontalOptions = LayoutOptions.Center
            };
            mainLayout.Add(rgbLabel);

            // === RENK UYUMU ===
            mainLayout.Add(new Label
            {
                Text = "🎨 Renk Uyumu Önerileri",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#81C784"),
                Margin = new Thickness(0, 10, 0, 5)
            });

            var harmonyLayout = new VerticalStackLayout { Spacing = 10 };
            
            // === TAB 3: ÖZEL RENK (RGB Sliders) ===
            // RGB Sliderları önce oluştur (renk uyumunda kullanılacak)
            mainLayout.Add(new Label
            {
                Text = "✏️ Özel Renk Oluştur",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#81C784"),
                Margin = new Thickness(0, 10, 0, 5)
            });

            // Kırmızı
            var redLabel = new Label { Text = $"Kırmızı: {_red}", TextColor = Colors.White };
            var redSlider = new Slider
            {
                Minimum = 0,
                Maximum = 255,
                Value = _red,
                MinimumTrackColor = Colors.Red,
                MaximumTrackColor = Colors.DarkRed,
                ThumbColor = Colors.Red
            };
            mainLayout.Add(redLabel);
            mainLayout.Add(redSlider);

            // Yeşil
            var greenLabel = new Label { Text = $"Yeşil: {_green}", TextColor = Colors.White };
            var greenSlider = new Slider
            {
                Minimum = 0,
                Maximum = 255,
                Value = _green,
                MinimumTrackColor = Colors.Green,
                MaximumTrackColor = Colors.DarkGreen,
                ThumbColor = Colors.Green
            };
            mainLayout.Add(greenLabel);
            mainLayout.Add(greenSlider);

            // Mavi
            var blueLabel = new Label { Text = $"Mavi: {_blue}", TextColor = Colors.White };
            var blueSlider = new Slider
            {
                Minimum = 0,
                Maximum = 255,
                Value = _blue,
                MinimumTrackColor = Colors.Blue,
                MaximumTrackColor = Colors.DarkBlue,
                ThumbColor = Colors.Blue
            };
            mainLayout.Add(blueLabel);
            mainLayout.Add(blueSlider);

            // Şeffaflık
            var opacityLabel = new Label { Text = $"Şeffaflık: {_opacity:P0}", TextColor = Colors.White };
            var opacitySlider = new Slider
            {
                Minimum = 0,
                Maximum = 1,
                Value = _opacity,
                MinimumTrackColor = Colors.Gray,
                MaximumTrackColor = Colors.White,
                ThumbColor = Colors.White
            };
            mainLayout.Add(opacityLabel);
            mainLayout.Add(opacitySlider);

            // Slider değişim eventi
            void UpdateColor(object s, ValueChangedEventArgs e)
            {
                _red = (byte)redSlider.Value;
                _green = (byte)greenSlider.Value;
                _blue = (byte)blueSlider.Value;
                _opacity = opacitySlider.Value;

                var currentColor = Color.FromRgba(_red / 255.0, _green / 255.0, _blue / 255.0, _opacity);
                onizlemeKutu.BackgroundColor = currentColor;
                rgbLabel.Text = $"RGB({_red}, {_green}, {_blue}) - Şeffaflık: {_opacity:P0}";
                redLabel.Text = $"Kırmızı: {_red}";
                greenLabel.Text = $"Yeşil: {_green}";
                blueLabel.Text = $"Mavi: {_blue}";
                opacityLabel.Text = $"Şeffaflık: {_opacity:P0}";
            }

            redSlider.ValueChanged += UpdateColor;
            greenSlider.ValueChanged += UpdateColor;
            blueSlider.ValueChanged += UpdateColor;
            opacitySlider.ValueChanged += UpdateColor;

            // Helper method to update sliders
            void UpdateSliders(Color color)
            {
                redSlider.Value = (byte)(color.Red * 255);
                greenSlider.Value = (byte)(color.Green * 255);
                blueSlider.Value = (byte)(color.Blue * 255);
                opacitySlider.Value = color.Alpha;
            }
            
            // Renk uyumu butonları (sliderlardan sonra ekle)
            // Complementary
            var compBtn = new Button
            {
                Text = "Karşıt Renk Göster",
                BackgroundColor = Color.FromArgb("#00796B"),
                TextColor = Colors.White,
                CornerRadius = 10,
                Padding = 10
            };
            var compColors = new Frame { IsVisible = false, Padding = 5, BackgroundColor = Color.FromArgb("#2C2C2C"), BorderColor = Color.FromArgb("#00796B") };
            compBtn.Clicked += (s, e) =>
            {
                var currentColor = onizlemeKutu.BackgroundColor;
                var comp = ColorPaletteHelper.GetComplementaryColor(currentColor);
                compColors.Content = CreateColorRow(new[] { currentColor, comp }, (c) => {
                    UpdateColorPreview(c, onizlemeKutu, rgbLabel);
                    UpdateSliders(c);
                });
                compColors.IsVisible = !compColors.IsVisible;
            };
            harmonyLayout.Add(compBtn);
            harmonyLayout.Add(compColors);

            // Analogous
            var analogBtn = new Button
            {
                Text = "Komşu Renkler Göster",
                BackgroundColor = Color.FromArgb("#00796B"),
                TextColor = Colors.White,
                CornerRadius = 10,
                Padding = 10
            };
            var analogColors = new Frame { IsVisible = false, Padding = 5, BackgroundColor = Color.FromArgb("#2C2C2C"), BorderColor = Color.FromArgb("#00796B") };
            analogBtn.Clicked += (s, e) =>
            {
                var colors = ColorPaletteHelper.GetAnalogousColors(onizlemeKutu.BackgroundColor);
                analogColors.Content = CreateColorRow(colors.ToArray(), (c) => {
                    UpdateColorPreview(c, onizlemeKutu, rgbLabel);
                    UpdateSliders(c);
                });
                analogColors.IsVisible = !analogColors.IsVisible;
            };
            harmonyLayout.Add(analogBtn);
            harmonyLayout.Add(analogColors);

            // Triadic
            var triadBtn = new Button
            {
                Text = "Üçgen Renk Uyumu Göster",
                BackgroundColor = Color.FromArgb("#00796B"),
                TextColor = Colors.White,
                CornerRadius = 10,
                Padding = 10
            };
            var triadColors = new Frame { IsVisible = false, Padding = 5, BackgroundColor = Color.FromArgb("#2C2C2C"), BorderColor = Color.FromArgb("#00796B") };
            triadBtn.Clicked += (s, e) =>
            {
                var colors = ColorPaletteHelper.GetTriadicColors(onizlemeKutu.BackgroundColor);
                triadColors.Content = CreateColorRow(colors.ToArray(), (c) => {
                    UpdateColorPreview(c, onizlemeKutu, rgbLabel);
                    UpdateSliders(c);
                });
                triadColors.IsVisible = !triadColors.IsVisible;
            };
            harmonyLayout.Add(triadBtn);
            harmonyLayout.Add(triadColors);

            // Monochromatic
            var monoBtn = new Button
            {
                Text = "Tek Renk Tonları Göster",
                BackgroundColor = Color.FromArgb("#00796B"),
                TextColor = Colors.White,
                CornerRadius = 10,
                Padding = 10
            };
            var monoColors = new Frame { IsVisible = false, Padding = 5, BackgroundColor = Color.FromArgb("#2C2C2C"), BorderColor = Color.FromArgb("#00796B") };
            monoBtn.Clicked += (s, e) =>
            {
                var colors = ColorPaletteHelper.GetMonochromaticColors(onizlemeKutu.BackgroundColor);
                monoColors.Content = CreateColorRow(colors.ToArray(), (c) => {
                    UpdateColorPreview(c, onizlemeKutu, rgbLabel);
                    UpdateSliders(c);
                });
                monoColors.IsVisible = !monoColors.IsVisible;
            };
            harmonyLayout.Add(monoBtn);
            harmonyLayout.Add(monoColors);

            mainLayout.Add(harmonyLayout);

            // Alt butonlar
            var butonLayout = new HorizontalStackLayout
            {
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Fill,
                Margin = new Thickness(0, 20, 0, 0)
            };

            var iptalButon = new Button
            {
                Text = "İptal",
                BackgroundColor = Colors.Gray,
                TextColor = Colors.White,
                CornerRadius = 15,
                Padding = new Thickness(15, 12),
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            iptalButon.Clicked += async (s, e) => await Navigation.PopAsync();

            var uygula = new Button
            {
                Text = "Uygula",
                BackgroundColor = Color.FromArgb("#00796B"),
                TextColor = Colors.White,
                CornerRadius = 15,
                Padding = new Thickness(15, 12),
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            uygula.Clicked += async (s, e) =>
            {
                var selectedColor = Color.FromRgba(_red / 255.0, _green / 255.0, _blue / 255.0, _opacity);
                onColorSelected?.Invoke(selectedColor);
                await Navigation.PopAsync();
            };

            butonLayout.Add(iptalButon);
            butonLayout.Add(uygula);
            mainLayout.Add(butonLayout);

            scrollView.Content = mainLayout;
            renkSecimSayfasi.Content = scrollView;
            await Navigation.PushAsync(renkSecimSayfasi);
        }

        private Frame CreateColorPaletteGrid(string title, (Color color, string name)[] colors, Action<Color, string> onColorTapped)
        {
            var frame = new Frame
            {
                Padding = 10,
                BackgroundColor = Color.FromArgb("#2C2C2C"),
                BorderColor = Color.FromArgb("#00796B"),
                CornerRadius = 10
            };

            var layout = new VerticalStackLayout { Spacing = 8 };
            
            layout.Add(new Label
            {
                Text = title,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            });

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                RowSpacing = 8,
                ColumnSpacing = 8
            };

            for (int i = 0; i < colors.Length; i++)
            {
                var (color, name) = colors[i];
                var colorBox = new Frame
                {
                    HeightRequest = 50,
                    BackgroundColor = color,
                    CornerRadius = 8,
                    Padding = 0,
                    HasShadow = true
                };

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) => onColorTapped(color, name);
                colorBox.GestureRecognizers.Add(tapGesture);

                grid.Add(colorBox, i % 3, i / 3);
            }

            layout.Add(grid);
            frame.Content = layout;
            return frame;
        }

        private HorizontalStackLayout CreateColorRow(Color[] colors, Action<Color> onColorTapped)
        {
            var layout = new HorizontalStackLayout { Spacing = 8 };
            
            foreach (var color in colors)
            {
                var colorBox = new Frame
                {
                    WidthRequest = 60,
                    HeightRequest = 60,
                    BackgroundColor = color,
                    CornerRadius = 10,
                    Padding = 0,
                    HasShadow = true,
                    HorizontalOptions = LayoutOptions.Center
                };

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) => onColorTapped(color);
                colorBox.GestureRecognizers.Add(tapGesture);

                layout.Add(colorBox);
            }

            return layout;
        }

        private void UpdateColorPreview(Color color, BoxView preview, Label label)
        {
            _red = (byte)(color.Red * 255);
            _green = (byte)(color.Green * 255);
            _blue = (byte)(color.Blue * 255);
            _opacity = color.Alpha;

            preview.BackgroundColor = color;
            label.Text = $"RGB({_red}, {_green}, {_blue}) - Şeffaflık: {_opacity:P0}";
        }

        private bool IsDarkColor(Color color)
        {
            // Rengin koyulugunu hesapla (luminance)
            double luminance = 0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue;
            return luminance < 0.5;
        }

        private async void TemaKaydet_Clicked(object sender, EventArgs e)
        {
            // Tema ismini kontrol et
            string temaIsmi = TemaIsmiEntry.Text?.Trim();
            
            if (string.IsNullOrWhiteSpace(temaIsmi))
            {
                await DisplayAlert("Uyari", "Lutfen tema ismi girin", "Tamam");
                return;
            }

            _currentTheme.Name = temaIsmi;

            // Temayi kaydet
            try
            {
                string json = JsonSerializer.Serialize(_currentTheme);
                Preferences.Default.Set(AppConstants.PREF_CUSTOM_THEME, json);
                Preferences.Default.Set(AppConstants.PREF_APP_THEME, AppConstants.THEME_CUSTOM);

                await DisplayAlert("Basarili", $"'{temaIsmi}' temasi kaydedildi ve uygulanacak", "Tamam");
                
                // Tema ayarlari sayfasina geri don
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Tema kaydedilemedi: {ex.Message}", "Tamam");
            }
        }
    }
}

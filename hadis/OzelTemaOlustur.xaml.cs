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
            OnizlemeFrame.Stroke = Color.FromArgb(_currentTheme.MainFrameBorder);
            NamazLabel.TextColor = Color.FromArgb(_currentTheme.MainFrameText);
            SaatLabel.TextColor = Color.FromArgb(_currentTheme.MainFrameText);
            KaldiLabel.TextColor = Color.FromArgb(_currentTheme.MainFrameText);
            KonumLabel.TextColor = Color.FromArgb(_currentTheme.MainFrameText);

            // Kucuk Frame'ler
            ImsakKucukFrame.BackgroundColor = Color.FromArgb(_currentTheme.SmallFrameBackground);
            ImsakKucukFrame.Stroke = Color.FromArgb(_currentTheme.SmallFrameBorder);
            ImsakYaziLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);
            ImsakVakitLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);

            GunesKucukFrame.BackgroundColor = Color.FromArgb(_currentTheme.SmallFrameBackground);
            GunesKucukFrame.Stroke = Color.FromArgb(_currentTheme.SmallFrameBorder);
            GunesYaziLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);
            GunesVakitLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);

            OgleKucukFrame.BackgroundColor = Color.FromArgb(_currentTheme.SmallFrameBackground);
            OgleKucukFrame.Stroke = Color.FromArgb(_currentTheme.SmallFrameBorder);
            OgleYaziLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);
            OgleVakitLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);

            IkindiKucukFrame.BackgroundColor = Color.FromArgb(_currentTheme.SmallFrameBackground);
            IkindiKucukFrame.Stroke = Color.FromArgb(_currentTheme.SmallFrameBorder);
            IkindiYaziLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);
            IkindiVakitLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);

            AksamKucukFrame.BackgroundColor = Color.FromArgb(_currentTheme.SmallFrameBackground);
            AksamKucukFrame.Stroke = Color.FromArgb(_currentTheme.SmallFrameBorder);
            AksamYaziLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);
            AksamVakitLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);

            YatsiKucukFrame.BackgroundColor = Color.FromArgb(_currentTheme.SmallFrameBackground);
            YatsiKucukFrame.Stroke = Color.FromArgb(_currentTheme.SmallFrameBorder);
            YatsiYaziLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);
            YatsiVakitLabel.TextColor = Color.FromArgb(_currentTheme.SmallFrameText);

            // Ayet Frame
            AyetFrame.BackgroundColor = Color.FromArgb(_currentTheme.AyetFrameBackground);
            AyetFrame.Stroke = Color.FromArgb(_currentTheme.AyetFrameBorder);
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
                if (OnizlemeFrame is Border border)
                {
                    border.Stroke = color;
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
                ImsakKucukFrame.Stroke = color;
                GunesKucukFrame.Stroke = color;
                OgleKucukFrame.Stroke = color;
                IkindiKucukFrame.Stroke = color;
                AksamKucukFrame.Stroke = color;
                YatsiKucukFrame.Stroke = color;
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
                AyetFrame.Stroke = color;
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

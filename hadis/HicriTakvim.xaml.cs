using System.Globalization;

namespace hadis
{
    public partial class HicriTakvim : ContentPage
    {
        private readonly UmAlQuraCalendar _hicriTakvim = new();
        private int _gorunenHicriYil;
        private int _gorunenHicriAy;

        private static readonly string[] HicriAylar = {
            "Muharrem", "Safer", "Rebiülevvel", "Rebiülahir",
            "Cemaziyelevvel", "Cemaziyelahir", "Recep", "Şaban",
            "Ramazan", "Şevval", "Zilkade", "Zilhicce"
        };

        // Önemli İslami günler: (Hicri Ay, Hicri Gün, İsim, Emoji)
        private static readonly List<(int Ay, int Gun, string Isim, string Emoji)> OnemliGunler = new()
        {
            // Muharrem
            (1, 1, "Hicri Yılbaşı", "🎉"),
            (1, 10, "Aşure Günü", "🤲"),

            // Safer
            (2, 1, "Safer Ayı Başlangıcı", "📿"),

            // Rebiülevvel
            (3, 12, "Mevlid Kandili", "🕯️"),

            // Recep
            (7, 1, "Üç Ayların Başlangıcı", "🌟"),
            (7, 27, "Regaip Kandili", "🕯️"),

            // Şaban
            (8, 15, "Berat Kandili", "🕯️"),

            // Ramazan
            (9, 1, "Ramazan Başlangıcı", "🌙"),
            (9, 27, "Kadir Gecesi", "✨"),

            // Şevval
            (10, 1, "Ramazan Bayramı (1. Gün)", "🎊"),
            (10, 2, "Ramazan Bayramı (2. Gün)", "🎊"),
            (10, 3, "Ramazan Bayramı (3. Gün)", "🎊"),

            // Zilhicce
            (12, 9, "Arife Günü", "🤲"),
            (12, 10, "Kurban Bayramı (1. Gün)", "🐑"),
            (12, 11, "Kurban Bayramı (2. Gün)", "🐑"),
            (12, 12, "Kurban Bayramı (3. Gün)", "🐑"),
            (12, 13, "Kurban Bayramı (4. Gün)", "🐑"),
        };

        public HicriTakvim()
        {
            InitializeComponent();

            // Bugünkü Hicri tarihi hesapla
            var bugun = DateTime.Now;
            _gorunenHicriYil = _hicriTakvim.GetYear(bugun);
            _gorunenHicriAy = _hicriTakvim.GetMonth(bugun);

            // Miladi tarihi göster
            MiladiTarihLabel.Text = bugun.ToString("dd MMMM yyyy, dddd", new CultureInfo("tr-TR"));

            // Hicri tarihi göster
            int hicriGun = _hicriTakvim.GetDayOfMonth(bugun);
            HicriTarihLabel.Text = $"🌙 {hicriGun} {HicriAylar[_gorunenHicriAy - 1]} {_gorunenHicriYil}";

            TakvimOlustur();
        }

        private void OncekiAy_Clicked(object sender, EventArgs e)
        {
            _gorunenHicriAy--;
            if (_gorunenHicriAy < 1)
            {
                _gorunenHicriAy = 12;
                _gorunenHicriYil--;
            }
            TakvimOlustur();
        }

        private void SonrakiAy_Clicked(object sender, EventArgs e)
        {
            _gorunenHicriAy++;
            if (_gorunenHicriAy > 12)
            {
                _gorunenHicriAy = 1;
                _gorunenHicriYil++;
            }
            TakvimOlustur();
        }

        private void TakvimOlustur()
        {
            try
            {
                // Ay başlığını güncelle
                AyBaslikLabel.Text = $"{HicriAylar[_gorunenHicriAy - 1]} {_gorunenHicriYil}";

                // Takvim grid'ini temizle
                TakvimGrid.Children.Clear();
                TakvimGrid.RowDefinitions.Clear();

                // Bu aydaki gün sayısı
                int gunSayisi = _hicriTakvim.GetDaysInMonth(_gorunenHicriYil, _gorunenHicriAy);

                // Ayın ilk gününün miladi karşılığını bul
                DateTime ilkGunMiladi = _hicriTakvim.ToDateTime(_gorunenHicriYil, _gorunenHicriAy, 1, 0, 0, 0, 0);

                // Haftanın hangi gününde başlıyor (Pazartesi = 0)
                int baslangicGunu = ((int)ilkGunMiladi.DayOfWeek + 6) % 7; // Pazartesi=0, Pazar=6

                // Kaç satır gerekli
                int satirSayisi = (int)Math.Ceiling((baslangicGunu + gunSayisi) / 7.0);

                for (int i = 0; i < satirSayisi; i++)
                {
                    TakvimGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                }

                // Bugünü hesapla
                var bugun = DateTime.Now;
                int bugunHicriGun = _hicriTakvim.GetDayOfMonth(bugun);
                int bugunHicriAy = _hicriTakvim.GetMonth(bugun);
                int bugunHicriYil = _hicriTakvim.GetYear(bugun);

                // Bu aydaki önemli günleri bul
                var ayinOnemliGunleri = OnemliGunler
                    .Where(g => g.Ay == _gorunenHicriAy)
                    .ToList();

                // Günleri yerleştir
                for (int gun = 1; gun <= gunSayisi; gun++)
                {
                    int konum = baslangicGunu + gun - 1;
                    int satir = konum / 7;
                    int sutun = konum % 7;

                    DateTime gunMiladi = _hicriTakvim.ToDateTime(_gorunenHicriYil, _gorunenHicriAy, gun, 0, 0, 0, 0);

                    bool bugunMu = (gun == bugunHicriGun && _gorunenHicriAy == bugunHicriAy && _gorunenHicriYil == bugunHicriYil);
                    bool onemliGunMu = ayinOnemliGunleri.Any(g => g.Gun == gun);

                    var hucre = GunHucresiOlustur(gun, gunMiladi.Day, bugunMu, onemliGunMu, sutun == 4); // Cuma = column 4

                    Grid.SetRow(hucre, satir);
                    Grid.SetColumn(hucre, sutun);
                    TakvimGrid.Children.Add(hucre);
                }

                // Önemli günleri listele
                OnemliGunleriGoster(ayinOnemliGunleri);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Takvim oluşturma hatası: {ex.Message}");
            }
        }

        private Frame GunHucresiOlustur(int hicriGun, int miladiGun, bool bugunMu, bool onemliGunMu, bool cumaMi)
        {
            Color bgColor;
            Color textColor;
            Color miladiColor;

            if (bugunMu)
            {
                bgColor = Application.Current.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#00796B")
                    : Color.FromArgb("#00796B");
                textColor = Colors.White;
                miladiColor = Color.FromArgb("#B2DFDB");
            }
            else if (onemliGunMu)
            {
                bgColor = Application.Current.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#2E3B2E")
                    : Color.FromArgb("#E8F5E9");
                textColor = Application.Current.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#A5D6A7")
                    : Color.FromArgb("#2E7D32");
                miladiColor = Application.Current.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#81C784")
                    : Color.FromArgb("#66BB6A");
            }
            else
            {
                bgColor = Colors.Transparent;
                textColor = Application.Current.RequestedTheme == AppTheme.Dark
                    ? Colors.White
                    : Color.FromArgb("#212121");
                miladiColor = Application.Current.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#757575")
                    : Color.FromArgb("#9E9E9E");
            }

            // Cuma günü özel renk (bugün/önemli değilse)
            if (cumaMi && !bugunMu && !onemliGunMu)
            {
                textColor = Application.Current.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#80CBC4")
                    : Color.FromArgb("#00796B");
            }

            var frame = new Frame
            {
                CornerRadius = 10,
                Padding = new Thickness(2),
                HasShadow = false,
                BackgroundColor = bgColor,
                BorderColor = bugunMu ? Color.FromArgb("#00BFA5") : Colors.Transparent,
                HeightRequest = 48,
                Margin = new Thickness(1)
            };

            var stack = new VerticalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Spacing = 0
            };

            var hicriLabel = new Label
            {
                Text = hicriGun.ToString(),
                FontSize = bugunMu ? 16 : 14,
                FontAttributes = bugunMu ? FontAttributes.Bold : FontAttributes.None,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = textColor
            };

            var miladiLabel = new Label
            {
                Text = miladiGun.ToString(),
                FontSize = 9,
                HorizontalOptions = LayoutOptions.Center,
                TextColor = miladiColor
            };

            stack.Children.Add(hicriLabel);
            stack.Children.Add(miladiLabel);
            frame.Content = stack;

            return frame;
        }

        private void OnemliGunleriGoster(List<(int Ay, int Gun, string Isim, string Emoji)> ayinGunleri)
        {
            OnemliGunlerStack.Children.Clear();

            if (ayinGunleri.Count == 0)
            {
                OnemliGunlerStack.Children.Add(new Label
                {
                    Text = "Bu ayda önemli bir gün bulunmamaktadır.",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Italic,
                    TextColor = Application.Current.RequestedTheme == AppTheme.Dark
                        ? Color.FromArgb("#757575")
                        : Color.FromArgb("#9E9E9E")
                });
                return;
            }

            foreach (var gun in ayinGunleri.OrderBy(g => g.Gun))
            {
                // Miladi karşılığını hesapla
                DateTime miladiTarih;
                try
                {
                    miladiTarih = _hicriTakvim.ToDateTime(_gorunenHicriYil, _gorunenHicriAy, gun.Gun, 0, 0, 0, 0);
                }
                catch
                {
                    continue;
                }

                var frame = new Frame
                {
                    CornerRadius = 12,
                    Padding = new Thickness(12, 10),
                    HasShadow = false,
                    BackgroundColor = Application.Current.RequestedTheme == AppTheme.Dark
                        ? Color.FromArgb("#1A2E2A")
                        : Color.FromArgb("#E0F2F1"),
                    BorderColor = Application.Current.RequestedTheme == AppTheme.Dark
                        ? Color.FromArgb("#2E4F47")
                        : Color.FromArgb("#B2DFDB")
                };

                var grid = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition(GridLength.Auto),
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto)
                    },
                    ColumnSpacing = 10
                };

                var emojiLabel = new Label
                {
                    Text = gun.Emoji,
                    FontSize = 24,
                    VerticalOptions = LayoutOptions.Center
                };

                var infoStack = new VerticalStackLayout { Spacing = 2 };

                var isimLabel = new Label
                {
                    Text = gun.Isim,
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Application.Current.RequestedTheme == AppTheme.Dark
                        ? Colors.White
                        : Color.FromArgb("#00796B")
                };

                var tarihLabel = new Label
                {
                    Text = $"{gun.Gun} {HicriAylar[gun.Ay - 1]} / {miladiTarih:dd MMMM yyyy}",
                    FontSize = 12,
                    TextColor = Application.Current.RequestedTheme == AppTheme.Dark
                        ? Color.FromArgb("#BDBDBD")
                        : Color.FromArgb("#757575")
                };

                infoStack.Children.Add(isimLabel);
                infoStack.Children.Add(tarihLabel);

                Grid.SetColumn(emojiLabel, 0);
                Grid.SetColumn(infoStack, 1);

                grid.Children.Add(emojiLabel);
                grid.Children.Add(infoStack);

                // Bugün bu önemli günse işaret koy
                var bugun = DateTime.Now;
                int bugunHicriGun = _hicriTakvim.GetDayOfMonth(bugun);
                int bugunHicriAy = _hicriTakvim.GetMonth(bugun);

                if (gun.Gun == bugunHicriGun && gun.Ay == bugunHicriAy)
                {
                    var bugunLabel = new Label
                    {
                        Text = "BUGÜN",
                        FontSize = 11,
                        FontAttributes = FontAttributes.Bold,
                        VerticalOptions = LayoutOptions.Center,
                        TextColor = Color.FromArgb("#00BFA5")
                    };
                    Grid.SetColumn(bugunLabel, 2);
                    grid.Children.Add(bugunLabel);
                }

                frame.Content = grid;
                OnemliGunlerStack.Children.Add(frame);
            }
        }
    }
}

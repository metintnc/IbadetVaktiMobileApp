using hadis.Models;
using System.Text.Json;
using System.Globalization;

namespace hadis
{
    public partial class IstatistikPage : ContentPage
    {
        private const string ZikirHistoryKey = "ZikirHistory";

        public IstatistikPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var json = Preferences.Default.Get(ZikirHistoryKey, string.Empty);
                var history = string.IsNullOrEmpty(json)
                    ? new Dictionary<string, Dictionary<string, int>>()
                    : JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(json);

                if (history == null) return;

                CalculateSummary(history);
                DrawWeeklyChart(history);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Veri yükleme hatası: {ex.Message}");
            }
        }

        private void CalculateSummary(Dictionary<string, Dictionary<string, int>> history)
        {
            int totalCount = 0;
            int todayCount = 0;
            Dictionary<string, int> zikirTotals = new Dictionary<string, int>();

            string today = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            foreach (var dateEntry in history)
            {
                int dailySum = 0;
                foreach (var zikirEntry in dateEntry.Value)
                {
                    dailySum += zikirEntry.Value;
                    
                    if (zikirTotals.ContainsKey(zikirEntry.Key))
                        zikirTotals[zikirEntry.Key] += zikirEntry.Value;
                    else
                        zikirTotals[zikirEntry.Key] = zikirEntry.Value;
                }
                totalCount += dailySum;

                if (dateEntry.Key == today)
                {
                    todayCount = dailySum;
                }
            }

            ToplamZikirLabel.Text = totalCount.ToString("N0", new CultureInfo("tr-TR"));
            BugunZikirLabel.Text = todayCount.ToString("N0", new CultureInfo("tr-TR"));

            // En Çok Çekilen
            if (zikirTotals.Count > 0)
            {
                var topZikir = zikirTotals.MaxBy(x => x.Value);
                EnCokZikirLabel.Text = $"{topZikir.Key} ({topZikir.Value})";
            }
            else
            {
                EnCokZikirLabel.Text = "-";
            }

            // Zincir (Streak) Hesaplama
            int streak = 0;
            DateTime checkDate = DateTime.Now.Date;
            
            // Eğer bugün henüz çekilmediyse, zincir kopmuş sayılmaz, dünden kontrol etmeye başla
            // Ancak bugün çekildiyse, bugünden başla.
            // Basit mantık: Geriye doğru git, her gün var mı bak.
            
            // Bugün çekilmiş mi?
            string todayKey = checkDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            bool todayActive = history.ContainsKey(todayKey) && history[todayKey].Values.Sum() > 0;

            if (!todayActive)
            {
                checkDate = checkDate.AddDays(-1); // Dünden başla
            }

            while (true)
            {
                string key = checkDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (history.ContainsKey(key) && history[key].Values.Sum() > 0)
                {
                    streak++;
                    checkDate = checkDate.AddDays(-1);
                }
                else
                {
                    break;
                }
            }
            ZincirLabel.Text = $"{streak} Gün";


            // Günlük Ortalama Hesaplama (Toplam / Aktif Gün Sayısı)
            // Sadece zikir çekilen günleri baz alıyoruz (0'lar ortalamayı düşürmesin mi? Genelde düşürmemesi istenir ama 'Total Average' ise düşürür.
            // Kullanıcı 'Aktif olduğu günlerde ne kadar çekiyor'u merak eder genelde.)
            // Veya toplam gün sayısı (ilk kayıttan bugüne).
            // Basit ve motive edici olan: Toplam / Aktif Gün Sayısı.
            
            int activeDays = history.Count(d => d.Value.Values.Sum() > 0);
            int average = activeDays > 0 ? totalCount / activeDays : 0;
            
            OrtalamaLabel.Text = average.ToString("N0", new CultureInfo("tr-TR"));
        }

        private void DrawWeeklyChart(Dictionary<string, Dictionary<string, int>> history)
        {
            ChartGrid.Children.Clear();
            ChartGrid.RowDefinitions.Clear();
            ChartGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            ChartGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Now.AddDays(-6 + i))
                .ToList();

            int maxVal = 1;
            foreach (var date in last7Days)
            {
                string key = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (history.ContainsKey(key))
                {
                    int sum = history[key].Values.Sum();
                    if (sum > maxVal) maxVal = sum;
                }
            }

            for (int i = 0; i < 7; i++)
            {
                var date = last7Days[i];
                string key = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                int count = history.ContainsKey(key) ? history[key].Values.Sum() : 0;
                
                double heightFactor = (double)count / maxVal;
                if (count > 0 && heightFactor < 0.05) heightFactor = 0.05;

                // Main stack for the column (Value + Bar)
                var columnStack = new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.End,
                    HorizontalOptions = LayoutOptions.Fill,
                    Spacing = 2
                };

                // Value Label (Shows the count)
                var valueLabel = new Label
                {
                    Text = count > 0 ? count.ToString() : "", // Show only if > 0 or always? User wants to see it. 0 is fine too or empty. Let's show empty for 0 to keep it clean, or 0 if explicit. User example imply seeing count. Let's show if > 0 or just keeping it clean.
                    FontSize = 10,
                    TextColor = count > 0 ? Color.FromArgb(i == 6 ? "#FFA000" : "#00796B") : Colors.Transparent, // Match bar color
                    HorizontalOptions = LayoutOptions.Center,
                    FontAttributes = FontAttributes.Bold
                };
                columnStack.Add(valueLabel);

                // Bar
                var bar = new BoxView
                {
                    Color = count > 0 ? Color.FromArgb(i == 6 ? "#FFA000" : "#00796B") : Color.FromArgb("#E0E0E0"),
                    CornerRadius = 4,
                    HeightRequest = heightFactor * 120,
                    HorizontalOptions = LayoutOptions.Fill
                };
                columnStack.Add(bar);
                
                // Day Label (Bottom axis)
                var dayLabel = new Label
                {
                    Text = date.ToString("ddd", new CultureInfo("tr-TR")),
                    FontSize = 10,
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Color.FromArgb("#757575")
                };

                ChartGrid.Add(columnStack, i, 0);
                ChartGrid.Add(dayLabel, i, 1);
            }
        }


    }
}


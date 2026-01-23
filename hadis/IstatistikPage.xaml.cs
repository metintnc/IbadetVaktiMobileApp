using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Maui.Controls;

namespace hadis
{
    public partial class IstatistikPage : ContentPage
    {
        public class ZikirDetay
        {
            public string Ad { get; set; }
            public int Adet { get; set; }
        }
        public class GunDetay
        {
            public string Tarih { get; set; }
            public List<ZikirDetay> Zikirler { get; set; }
        }

        public IstatistikPage()
        {
            InitializeComponent();
            YukuIstatistik();
        }

        private void YukuIstatistik()
        {
            int toplamZikir = Preferences.Default.Get("Toplam", 0);
            ToplamZikirLabel.Text = $"Toplam Zikir: {toplamZikir}";

            // Zikir geçmiþini yükle
            var json = Preferences.Default.Get("ZikirHistory", string.Empty);
            var history = string.IsNullOrEmpty(json)
                ? new Dictionary<string, Dictionary<string, int>>()
                : JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(json) ?? new();

            // En çok çekilen zikir
            var allZikirler = history.SelectMany(g => g.Value)
                .GroupBy(z => z.Key)
                .Select(g => new { Ad = g.Key, Adet = g.Sum(x => x.Value) })
                .OrderByDescending(z => z.Adet)
                .ToList();
            if (allZikirler.Count > 0)
                EnCokZikirLabel.Text = $"En Çok Çekilen Zikir: {allZikirler[0].Ad} ({allZikirler[0].Adet})";
            else
                EnCokZikirLabel.Text = "En Çok Çekilen Zikir: -";

            // Son 7 gün
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Now.Date.AddDays(-i))
                .OrderBy(d => d)
                .ToList();
            var gunDetayList = new List<GunDetay>();
            foreach (var day in last7Days)
            {
                string dayKey = day.ToString("yyyy-MM-dd");
                var detay = new GunDetay
                {
                    Tarih = day.ToString("dd.MM.yyyy"),
                    Zikirler = new List<ZikirDetay>()
                };
                if (history.ContainsKey(dayKey))
                {
                    foreach (var zikir in history[dayKey])
                    {
                        detay.Zikirler.Add(new ZikirDetay { Ad = zikir.Key, Adet = zikir.Value });
                    }
                }
                gunDetayList.Add(detay);
            }
            HaftaListesi.ItemsSource = gunDetayList;
        }
    }
}

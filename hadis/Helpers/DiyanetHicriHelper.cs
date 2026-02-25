using System.Globalization;

namespace hadis.Helpers
{
    /// <summary>
    /// Diyanet İşleri Başkanlığı'nın resmi Hicri takvim verilerine göre
    /// Hicri tarih hesaplayan yardımcı sınıf.
    /// Veri bulunamazsa UmAlQuraCalendar fallback olarak kullanılır.
    /// </summary>
    public static class DiyanetHicriHelper
    {
        private static readonly UmAlQuraCalendar _fallback = new();

        public static readonly string[] HicriAylar =
        {
            "Muharrem", "Safer", "Rebiülevvel", "Rebiülahir",
            "Cemaziyelevvel", "Cemaziyelahir", "Recep", "Şaban",
            "Ramazan", "Şevval", "Zilkade", "Zilhicce"
        };

        /// <summary>
        /// Diyanet İşleri Başkanlığı'nın resmi Hicri ay başlangıç tarihleri.
        /// (Hicri Yıl, Hicri Ay) → Miladi başlangıç tarihi
        /// </summary>
        private static readonly SortedList<DateTime, (int Yil, int Ay)> DiyanetTakvim = new()
        {
            // ---- 1446 ----
            { new DateTime(2024, 7, 7),   (1446, 1) },   // Muharrem
            { new DateTime(2024, 8, 5),   (1446, 2) },   // Safer
            { new DateTime(2024, 9, 4),   (1446, 3) },   // Rebiülevvel
            { new DateTime(2024, 10, 4),  (1446, 4) },   // Rebiülahir
            { new DateTime(2024, 11, 3),  (1446, 5) },   // Cemaziyelevvel
            { new DateTime(2024, 12, 2),  (1446, 6) },   // Cemaziyelahir
            { new DateTime(2025, 1, 1),   (1446, 7) },   // Recep
            { new DateTime(2025, 1, 31),  (1446, 8) },   // Şaban
            { new DateTime(2025, 3, 1),   (1446, 9) },   // Ramazan
            { new DateTime(2025, 3, 30),  (1446, 10) },  // Şevval
            { new DateTime(2025, 4, 29),  (1446, 11) },  // Zilkade
            { new DateTime(2025, 5, 28),  (1446, 12) },  // Zilhicce

            // ---- 1447 ----
            { new DateTime(2025, 6, 26),  (1447, 1) },   // Muharrem
            { new DateTime(2025, 7, 26),  (1447, 2) },   // Safer
            { new DateTime(2025, 8, 24),  (1447, 3) },   // Rebiülevvel
            { new DateTime(2025, 9, 23),  (1447, 4) },   // Rebiülahir
            { new DateTime(2025, 10, 23), (1447, 5) },   // Cemaziyelevvel
            { new DateTime(2025, 11, 21), (1447, 6) },   // Cemaziyelahir
            { new DateTime(2025, 12, 21), (1447, 7) },   // Recep
            { new DateTime(2026, 1, 20),  (1447, 8) },   // Şaban
            { new DateTime(2026, 2, 19),  (1447, 9) },   // Ramazan
            { new DateTime(2026, 3, 20),  (1447, 10) },  // Şevval
            { new DateTime(2026, 4, 18),  (1447, 11) },  // Zilkade
            { new DateTime(2026, 5, 18),  (1447, 12) },  // Zilhicce
        };

        /// <summary>
        /// Hızlı lookup: (Yıl, Ay) → Miladi başlangıç
        /// </summary>
        private static readonly Dictionary<(int Yil, int Ay), DateTime> AyBaslariLookup;

        static DiyanetHicriHelper()
        {
            AyBaslariLookup = new Dictionary<(int Yil, int Ay), DateTime>();
            foreach (var kvp in DiyanetTakvim)
            {
                AyBaslariLookup[kvp.Value] = kvp.Key;
            }
        }

        /// <summary>
        /// Miladi tarihten Diyanet Hicri tarihi hesaplar.
        /// </summary>
        public static (int Gun, int Ay, int Yil) GetHicriTarih(DateTime miladi)
        {
            var tarih = miladi.Date;

            // Sıralı listedeki son eşleşmeyi bul (tarih >= aybaşı)
            for (int i = DiyanetTakvim.Count - 1; i >= 0; i--)
            {
                if (tarih >= DiyanetTakvim.Keys[i])
                {
                    var (yil, ay) = DiyanetTakvim.Values[i];
                    int gun = (tarih - DiyanetTakvim.Keys[i]).Days + 1;
                    return (gun, ay, yil);
                }
            }

            // Fallback: UmAlQuraCalendar
            return GetFallback(miladi);
        }

        /// <summary>
        /// Formatlı Hicri tarih: "🌙 7 Ramazan 1447"
        /// </summary>
        public static string GetHicriTarihFormatli(DateTime miladi)
        {
            try
            {
                var (gun, ay, yil) = GetHicriTarih(miladi);
                string ayAdi = HicriAylar[ay - 1];
                return $"🌙 {gun} {ayAdi} {yil}";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Bir Hicri ayın gün sayısını döndürür.
        /// </summary>
        public static int GetDaysInMonth(int yil, int ay)
        {
            // Sonraki ayın başlangıcını bul
            int sonrakiAy = ay + 1;
            int sonrakiYil = yil;
            if (sonrakiAy > 12)
            {
                sonrakiAy = 1;
                sonrakiYil++;
            }

            if (AyBaslariLookup.TryGetValue((yil, ay), out var baslangic) &&
                AyBaslariLookup.TryGetValue((sonrakiYil, sonrakiAy), out var sonraki))
            {
                return (sonraki - baslangic).Days;
            }

            // Fallback
            try { return _fallback.GetDaysInMonth(yil, ay); }
            catch { return 30; }
        }

        /// <summary>
        /// Hicri tarihten Miladi tarihe çevirir.
        /// </summary>
        public static DateTime HicriToMiladi(int yil, int ay, int gun)
        {
            if (AyBaslariLookup.TryGetValue((yil, ay), out var baslangic))
            {
                return baslangic.AddDays(gun - 1);
            }

            // Fallback
            try { return _fallback.ToDateTime(yil, ay, gun, 0, 0, 0, 0); }
            catch { return DateTime.MinValue; }
        }

        private static (int Gun, int Ay, int Yil) GetFallback(DateTime miladi)
        {
            try
            {
                int gun = _fallback.GetDayOfMonth(miladi);
                int ay = _fallback.GetMonth(miladi);
                int yil = _fallback.GetYear(miladi);
                return (gun, ay, yil);
            }
            catch
            {
                return (1, 1, 1);
            }
        }
    }
}

using System.Globalization;

namespace hadis.Helpers
{
    /// <summary>
    /// UmAlQuraCalendar kullanarak dinamik ve süresiz olarak Hicri tarih hesaplayan yardımcı sınıf.
    /// Manuel yıl/ay tablosu tutma gereksinimini ortadan kaldırır.
    /// </summary>
    public static class DiyanetHicriHelper
    {
        private static readonly UmAlQuraCalendar _calendar = new();

        public static readonly string[] HicriAylar =
        {
            "Muharrem", "Safer", "Rebiülevvel", "Rebiülahir",
            "Cemaziyelevvel", "Cemaziyelahir", "Recep", "Şaban",
            "Ramazan", "Şevval", "Zilkade", "Zilhicce"
        };

        /// <summary>
        /// Miladi tarihten Hicri tarihi dinamik olarak hesaplar.
        /// </summary>
        public static (int Gun, int Ay, int Yil) GetHicriTarih(DateTime miladi)
        {
            try
            {
                int gun = _calendar.GetDayOfMonth(miladi);
                int ay = _calendar.GetMonth(miladi);
                int yil = _calendar.GetYear(miladi);
                return (gun, ay, yil);
            }
            catch
            {
                return (1, 1, 1);
            }
        }

        /// <summary>
        /// Formatlı Hicri tarih: "🌙 14 Safer 1448"
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
        /// Bir Hicri ayın gün sayısını döndürür (29 veya 30).
        /// </summary>
        public static int GetDaysInMonth(int yil, int ay)
        {
            try
            {
                return _calendar.GetDaysInMonth(yil, ay);
            }
            catch
            {
                return 30;
            }
        }

        /// <summary>
        /// Hicri tarihten Miladi tarihe çevirir.
        /// </summary>
        public static DateTime HicriToMiladi(int yil, int ay, int gun)
        {
            try
            {
                return _calendar.ToDateTime(yil, ay, gun, 0, 0, 0, 0);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
    }
}

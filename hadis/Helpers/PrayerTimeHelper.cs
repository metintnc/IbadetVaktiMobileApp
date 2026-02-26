using System.Globalization;

namespace hadis.Helpers
{
    /// <summary>
    /// Namaz vakitleriyle ilgili tekrar kullanılabilir yardımcı metotlar
    /// </summary>
    public static class PrayerTimeHelper
    {
        // Vakitlerin sıralı listesi (dictionary key'leri)
        private static readonly (string Key, string DisplayName, string ShortName)[] PrayerOrder = new[]
        {
            ("İmsak", "İmsak Vaktine", "İmsak"),
            ("gunes", "Güneşin Doğmasına", "Güneş"),
            ("Ogle", "Öğle Namazına", "Öğle"),
            ("İkindi", "İkindi Namazına", "İkindi"),
            ("Aksam", "Akşam Namazına", "Akşam"),
            ("Yatsi", "Yatsı Namazına", "Yatsı")
        };

        /// <summary>
        /// Sonraki namazı ve kalan süreyi bulur
        /// </summary>
        /// <summary>
        /// Sonraki namazı ve kalan süreyi bulur
        /// </summary>
        public static (string DisplayName, string Key, TimeSpan Remaining, int Index) GetNextPrayer(Dictionary<string, DateTime> times)
        {
            var details = GetNextPrayerDetails(times);
            return (details.DisplayName, details.Key, details.Remaining, details.Index);
        }

        /// <summary>
        /// Sonraki namazın kısa adını ve kalan süreyi döndürür (bildirimler için)
        /// </summary>
        public static (string ShortName, TimeSpan Remaining) GetNextPrayerShort(Dictionary<string, DateTime> times)
        {
            var details = GetNextPrayerDetails(times);
            return (details.ShortName, details.Remaining);
        }

        private static (string Key, string DisplayName, string ShortName, TimeSpan Remaining, int Index) GetNextPrayerDetails(Dictionary<string, DateTime> times)
        {
            DateTime now = DateTime.Now;

            for (int i = 0; i < PrayerOrder.Length; i++)
            {
                var (key, displayName, shortName) = PrayerOrder[i];
                if (times.ContainsKey(key) && times[key] > now)
                {
                    return (key, displayName, shortName, times[key] - now, i);
                }
            }

            // Tüm vakitler geçmiş → ertesi gün İmsak
            var imsakTime = times["İmsak"].AddDays(1);
            return ("İmsak", "İmsak Vaktine", "İmsak", imsakTime - now, 0);
        }

        /// <summary>
        /// Sürekli bildirim için başlık ve mesaj oluşturur
        /// </summary>
        public static (string Title, string Message) BuildPersistentNotificationContent(Dictionary<string, DateTime> times)
        {
            var (shortName, remaining) = GetNextPrayerShort(times);

            string title = $"{shortName} vaktine {remaining.Hours:D2}:{remaining.Minutes:D2} kaldı";
            string message = $"İmsak {times["İmsak"]:HH:mm} | " +
                            $"Güneş {times["gunes"]:HH:mm} | " +
                            $"Öğle {times["Ogle"]:HH:mm} | " +
                            $"İkindi {times["İkindi"]:HH:mm} | " +
                            $"Akşam {times["Aksam"]:HH:mm} | " +
                            $"Yatsı {times["Yatsi"]:HH:mm}";

            return (title, message);
        }

        /// <summary>
        /// Vakit saatini formatlı string olarak döndürür (HH:mm)
        /// </summary>
        public static string FormatTime(DateTime time) => $"{time.Hour:D2}:{time.Minute:D2}";

        /// <summary>
        /// Geri sayım süresini formatlı string olarak döndürür (HH : MM : SS)
        /// </summary>
        public static string FormatCountdown(TimeSpan remaining) =>
            $"{remaining.Hours:D2} : {remaining.Minutes:D2} : {remaining.Seconds:D2}";

        /// <summary>
        /// Diyanet'e uyumlu Hicri tarihi formatlı string olarak döndürür
        /// </summary>
        public static string GetHicriTarih()
        {
            return DiyanetHicriHelper.GetHicriTarihFormatli(DateTime.Now);
        }

        private static readonly string[] Ayetler = new string[]
        {
            "Hiç bilenlerle bilmeyenler bir olur mu? (Zümer, 9)",
            "Şüphesiz Allah sabredenlerle beraberdir. (Bakara, 153)",
            "Gerçekten güçlükle beraber bir kolaylık vardır. (İnşirah, 6)",
            "Allah, kullarına karşı çok şefkatlidir. (Şura, 19)",
            "Ey iman edenler! Sabır ve namazla Allah'tan yardım isteyin. (Bakara, 45)",
            "Göklerde ve yerde ne varsa hepsi Allah'ındır. (Bakara, 284)",
            "Zorlukla beraber bir kolaylık vardır. (İnşirah, 5)",
            "Kıyamet günü herkese amel defteri verilecektir. (İsra, 13)",
            "İyilik ve takva üzerine yardımlaşın. (Maide, 2)",
            "Şüphesiz dönüş ancak Allah'adır. (Bakara, 156)",
            "Allah'a güvenip dayanan kimseye O yeter. (Talak, 3)",
            "Rabbinizden mağfiret dileyin; O çok bağışlayıcıdır. (Nuh, 10)",
            "Kim bir iyilik yaparsa onun on katı vardır. (En'am, 160)",
            "Allah'ı çokça zikredin ki kurtuluşa eresiniz. (Cuma, 10)",
            "Rabbim! İlmimi artır. (Taha, 114)",
            "Sabret! Allah'ın vaadi gerçektir. (Rum, 60)",
            "İnsan için ancak çalıştığının karşılığı vardır. (Necm, 39)",
            "Allah güzel davrananları sever. (Ali İmran, 134)",
            "Namaz, kötülüklerden alıkoyar. (Ankebut, 45)",
            "De ki: Rabbim, beni doğru yola ilet. (Müminun, 93)"
        };

        private static readonly Random _random = new Random();

        /// <summary>
        /// Her girişte farklı rastgele ayet döndürür
        /// </summary>
        public static string GetDailyAyet()
        {
            return Ayetler[_random.Next(Ayetler.Length)];
        }

        /// <summary>
        /// Mevcut ayetten farklı rastgele bir ayet döndürür (dokunma ile değişim için)
        /// </summary>
        public static string GetRandomAyet(string currentAyet)
        {
            string newAyet;
            do
            {
                newAyet = Ayetler[_random.Next(Ayetler.Length)];
            } while (newAyet == currentAyet && Ayetler.Length > 1);

            return newAyet;
        }
    }
}

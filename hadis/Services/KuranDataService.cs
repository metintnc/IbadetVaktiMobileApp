using System.Collections.Generic;
using System.Linq;
using hadis.Models;

namespace hadis.Services
{
    public static class KuranDataService
    {
        // IReadOnlyList ile immutability garantisi
        private static readonly IReadOnlyList<Sure> _sureler = new List<Sure>
        {
            new Sure { SureNo = 1, Ad = "Fâtiha", AdArapca = "الفاتحة", AyetSayisi = 7, Inis = "Mekke" },
            new Sure { SureNo = 2, Ad = "Bakara", AdArapca = "البقرة", AyetSayisi = 286, Inis = "Medine" },
            new Sure { SureNo = 3, Ad = "Âl-i İmrân", AdArapca = "آل عمران", AyetSayisi = 200, Inis = "Medine" },
            new Sure { SureNo = 4, Ad = "Nisâ", AdArapca = "النساء", AyetSayisi = 176, Inis = "Medine" },
            new Sure { SureNo = 5, Ad = "Mâide", AdArapca = "المائدة", AyetSayisi = 120, Inis = "Medine" },
            new Sure { SureNo = 6, Ad = "En'âm", AdArapca = "الأنعام", AyetSayisi = 165, Inis = "Mekke" },
            new Sure { SureNo = 7, Ad = "A'râf", AdArapca = "الأعراف", AyetSayisi = 206, Inis = "Mekke" },
            new Sure { SureNo = 8, Ad = "Enfâl", AdArapca = "الأنفال", AyetSayisi = 75, Inis = "Medine" },
            new Sure { SureNo = 9, Ad = "Tevbe", AdArapca = "التوبة", AyetSayisi = 129, Inis = "Medine" },
            new Sure { SureNo = 10, Ad = "Yûnus", AdArapca = "يونس", AyetSayisi = 109, Inis = "Mekke" },
            new Sure { SureNo = 11, Ad = "Hûd", AdArapca = "هود", AyetSayisi = 123, Inis = "Mekke" },
            new Sure { SureNo = 12, Ad = "Yûsuf", AdArapca = "يوسف", AyetSayisi = 111, Inis = "Mekke" },
            new Sure { SureNo = 13, Ad = "Ra'd", AdArapca = "الرعد", AyetSayisi = 43, Inis = "Medine" },
            new Sure { SureNo = 14, Ad = "İbrâhîm", AdArapca = "ابراهيم", AyetSayisi = 52, Inis = "Mekke" },
            new Sure { SureNo = 15, Ad = "Hicr", AdArapca = "الحجر", AyetSayisi = 99, Inis = "Mekke" },
            new Sure { SureNo = 16, Ad = "Nahl", AdArapca = "النحل", AyetSayisi = 128, Inis = "Mekke" },
            new Sure { SureNo = 17, Ad = "İsrâ", AdArapca = "الإسراء", AyetSayisi = 111, Inis = "Mekke" },
            new Sure { SureNo = 18, Ad = "Kehf", AdArapca = "الكهف", AyetSayisi = 110, Inis = "Mekke" },
            new Sure { SureNo = 19, Ad = "Meryem", AdArapca = "مريم", AyetSayisi = 98, Inis = "Mekke" },
            new Sure { SureNo = 20, Ad = "Tâhâ", AdArapca = "طه", AyetSayisi = 135, Inis = "Mekke" },
            new Sure { SureNo = 21, Ad = "Enbiyâ", AdArapca = "الأنبياء", AyetSayisi = 112, Inis = "Mekke" },
            new Sure { SureNo = 22, Ad = "Hac", AdArapca = "الحج", AyetSayisi = 78, Inis = "Medine" },
            new Sure { SureNo = 23, Ad = "Mü'minûn", AdArapca = "المؤمنون", AyetSayisi = 118, Inis = "Mekke" },
            new Sure { SureNo = 24, Ad = "Nûr", AdArapca = "النور", AyetSayisi = 64, Inis = "Medine" },
            new Sure { SureNo = 25, Ad = "Furkan", AdArapca = "الفرقان", AyetSayisi = 77, Inis = "Mekke" },
            new Sure { SureNo = 26, Ad = "Şuarâ", AdArapca = "الشعراء", AyetSayisi = 227, Inis = "Mekke" },
            new Sure { SureNo = 27, Ad = "Neml", AdArapca = "النمل", AyetSayisi = 93, Inis = "Mekke" },
            new Sure { SureNo = 28, Ad = "Kasas", AdArapca = "القصص", AyetSayisi = 88, Inis = "Mekke" },
            new Sure { SureNo = 29, Ad = "Ankebût", AdArapca = "العنكبوت", AyetSayisi = 69, Inis = "Mekke" },
            new Sure { SureNo = 30, Ad = "Rûm", AdArapca = "الروم", AyetSayisi = 60, Inis = "Mekke" },
            new Sure { SureNo = 31, Ad = "Lokmân", AdArapca = "لقمان", AyetSayisi = 34, Inis = "Mekke" },
            new Sure { SureNo = 32, Ad = "Secde", AdArapca = "السجدة", AyetSayisi = 30, Inis = "Mekke" },
            new Sure { SureNo = 33, Ad = "Ahzâb", AdArapca = "الأحزاب", AyetSayisi = 73, Inis = "Mekke" },
            new Sure { SureNo = 34, Ad = "Sebe'", AdArapca = "سبأ", AyetSayisi = 54, Inis = "Mekke" },
            new Sure { SureNo = 35, Ad = "Fâtır", AdArapca = "فاطر", AyetSayisi = 45, Inis = "Mekke" },
            new Sure { SureNo = 36, Ad = "Yâsîn", AdArapca = "يس", AyetSayisi = 83, Inis = "Mekke" },
            new Sure { SureNo = 37, Ad = "Sâffât", AdArapca = "الصافات", AyetSayisi = 182, Inis = "Mekke" },
            new Sure { SureNo = 38, Ad = "Sâd", AdArapca = "ص", AyetSayisi = 88, Inis = "Mekke" },
            new Sure { SureNo = 39, Ad = "Zümer", AdArapca = "الزمر", AyetSayisi = 75, Inis = "Mekke" },
            new Sure { SureNo = 40, Ad = "Mü'min", AdArapca = "غافر", AyetSayisi = 85, Inis = "Mekke" },
            new Sure { SureNo = 41, Ad = "Fussilet", AdArapca = "فصلت", AyetSayisi = 54, Inis = "Mekke" },
            new Sure { SureNo = 42, Ad = "Şûrâ", AdArapca = "الشورى", AyetSayisi = 53, Inis = "Mekke" },
            new Sure { SureNo = 43, Ad = "Zuhruf", AdArapca = "الزخرف", AyetSayisi = 89, Inis = "Mekke" },
            new Sure { SureNo = 44, Ad = "Duhân", AdArapca = "الدخان", AyetSayisi = 59, Inis = "Mekke" },
            new Sure { SureNo = 45, Ad = "Câsiye", AdArapca = "الجاثية", AyetSayisi = 37, Inis = "Mekke" },
            new Sure { SureNo = 46, Ad = "Ahkâf", AdArapca = "الأحقاف", AyetSayisi = 35, Inis = "Mekke" },
            new Sure { SureNo = 47, Ad = "Muhammed", AdArapca = "محمد", AyetSayisi = 38, Inis = "Medine" },
            new Sure { SureNo = 48, Ad = "Fetih", AdArapca = "الفتح", AyetSayisi = 29, Inis = "Medine" },
            new Sure { SureNo = 49, Ad = "Hucurât", AdArapca = "الحجرات", AyetSayisi = 18, Inis = "Medine" },
            new Sure { SureNo = 50, Ad = "Kâf", AdArapca = "ق", AyetSayisi = 45, Inis = "Mekke" },
            new Sure { SureNo = 51, Ad = "Zâriyât", AdArapca = "الذاريات", AyetSayisi = 60, Inis = "Mekke" },
            new Sure { SureNo = 52, Ad = "Tûr", AdArapca = "الطور", AyetSayisi = 49, Inis = "Mekke" },
            new Sure { SureNo = 53, Ad = "Necm", AdArapca = "النجم", AyetSayisi = 62, Inis = "Mekke" },
            new Sure { SureNo = 54, Ad = "Kamer", AdArapca = "القمر", AyetSayisi = 55, Inis = "Mekke" },
            new Sure { SureNo = 55, Ad = "Rahmân", AdArapca = "الرحمن", AyetSayisi = 78, Inis = "Medine" },
            new Sure { SureNo = 56, Ad = "Vâkıa", AdArapca = "الواقعة", AyetSayisi = 96, Inis = "Mekke" },
            new Sure { SureNo = 57, Ad = "Hadîd", AdArapca = "الحديد", AyetSayisi = 29, Inis = "Medine" },
            new Sure { SureNo = 58, Ad = "Mücâdele", AdArapca = "المجادلة", AyetSayisi = 22, Inis = "Medine" },
            new Sure { SureNo = 59, Ad = "Haşr", AdArapca = "الحشر", AyetSayisi = 24, Inis = "Medine" },
            new Sure { SureNo = 60, Ad = "Mümtehine", AdArapca = "الممتحنة", AyetSayisi = 13, Inis = "Medine" },
            new Sure { SureNo = 61, Ad = "Saff", AdArapca = "الصف", AyetSayisi = 14, Inis = "Medine" },
            new Sure { SureNo = 62, Ad = "Cuma", AdArapca = "الجمعة", AyetSayisi = 11, Inis = "Medine" },
            new Sure { SureNo = 63, Ad = "Münâfikûn", AdArapca = "المنافقون", AyetSayisi = 11, Inis = "Medine" },
            new Sure { SureNo = 64, Ad = "Teğâbün", AdArapca = "التغابن", AyetSayisi = 18, Inis = "Medine" },
            new Sure { SureNo = 65, Ad = "Talâk", AdArapca = "الطلاق", AyetSayisi = 12, Inis = "Medine" },
            new Sure { SureNo = 66, Ad = "Tahrîm", AdArapca = "التحريم", AyetSayisi = 12, Inis = "Medine" },
            new Sure { SureNo = 67, Ad = "Mülk", AdArapca = "الملك", AyetSayisi = 30, Inis = "Mekke" },
            new Sure { SureNo = 68, Ad = "Kalem", AdArapca = "القلم", AyetSayisi = 52, Inis = "Mekke" },
            new Sure { SureNo = 69, Ad = "Hâkka", AdArapca = "الحاقة", AyetSayisi = 52, Inis = "Mekke" },
            new Sure { SureNo = 70, Ad = "Meâric", AdArapca = "المعارج", AyetSayisi = 44, Inis = "Mekke" },
            new Sure { SureNo = 71, Ad = "Nûh", AdArapca = "نوح", AyetSayisi = 28, Inis = "Mekke" },
            new Sure { SureNo = 72, Ad = "Cin", AdArapca = "الجن", AyetSayisi = 28, Inis = "Mekke" },
            new Sure { SureNo = 73, Ad = "Müzzemmil", AdArapca = "المزمل", AyetSayisi = 20, Inis = "Mekke" },
            new Sure { SureNo = 74, Ad = "Müddessir", AdArapca = "المدثر", AyetSayisi = 56, Inis = "Mekke" },
            new Sure { SureNo = 75, Ad = "Kıyâme", AdArapca = "القيامة", AyetSayisi = 40, Inis = "Mekke" },
            new Sure { SureNo = 76, Ad = "İnsân", AdArapca = "الانسان", AyetSayisi = 31, Inis = "Medine" },
            new Sure { SureNo = 77, Ad = "Mürselât", AdArapca = "المرسلات", AyetSayisi = 50, Inis = "Mekke" },
            new Sure { SureNo = 78, Ad = "Nebe'", AdArapca = "النبأ", AyetSayisi = 40, Inis = "Mekke" },
            new Sure { SureNo = 79, Ad = "Nâziât", AdArapca = "النازعات", AyetSayisi = 46, Inis = "Mekke" },
            new Sure { SureNo = 80, Ad = "Abese", AdArapca = "عبس", AyetSayisi = 42, Inis = "Mekke" },
            new Sure { SureNo = 81, Ad = "Tekvîr", AdArapca = "التكوير", AyetSayisi = 29, Inis = "Mekke" },
            new Sure { SureNo = 82, Ad = "İnfitâr", AdArapca = "الإنفطار", AyetSayisi = 19, Inis = "Mekke" },
            new Sure { SureNo = 83, Ad = "Mutaffifîn", AdArapca = "المطففين", AyetSayisi = 36, Inis = "Mekke" },
            new Sure { SureNo = 84, Ad = "İnşikâk", AdArapca = "الإنشقاق", AyetSayisi = 25, Inis = "Mekke" },
            new Sure { SureNo = 85, Ad = "Burûc", AdArapca = "البروج", AyetSayisi = 22, Inis = "Mekke" },
            new Sure { SureNo = 86, Ad = "Târık", AdArapca = "الطارق", AyetSayisi = 17, Inis = "Mekke" },
            new Sure { SureNo = 87, Ad = "A'lâ", AdArapca = "الأعلى", AyetSayisi = 19, Inis = "Mekke" },
            new Sure { SureNo = 88, Ad = "Gâşiye", AdArapca = "الغاشية", AyetSayisi = 26, Inis = "Mekke" },
            new Sure { SureNo = 89, Ad = "Fecr", AdArapca = "الفجر", AyetSayisi = 30, Inis = "Mekke" },
            new Sure { SureNo = 90, Ad = "Beled", AdArapca = "البلد", AyetSayisi = 20, Inis = "Mekke" },
            new Sure { SureNo = 91, Ad = "Şems", AdArapca = "الشمس", AyetSayisi = 15, Inis = "Mekke" },
            new Sure { SureNo = 92, Ad = "Leyl", AdArapca = "الليل", AyetSayisi = 21, Inis = "Mekke" },
            new Sure { SureNo = 93, Ad = "Duhâ", AdArapca = "الضحى", AyetSayisi = 11, Inis = "Mekke" },
            new Sure { SureNo = 94, Ad = "İnşirah", AdArapca = "الشرح", AyetSayisi = 8, Inis = "Mekke" },
            new Sure { SureNo = 95, Ad = "Tîn", AdArapca = "التين", AyetSayisi = 8, Inis = "Mekke" },
            new Sure { SureNo = 96, Ad = "Alak", AdArapca = "العلق", AyetSayisi = 19, Inis = "Mekke" },
            new Sure { SureNo = 97, Ad = "Kadir", AdArapca = "القدر", AyetSayisi = 5, Inis = "Mekke" },
            new Sure { SureNo = 98, Ad = "Beyyine", AdArapca = "البينة", AyetSayisi = 8, Inis = "Medine" },
            new Sure { SureNo = 99, Ad = "Zilzâl", AdArapca = "الزلزلة", AyetSayisi = 8, Inis = "Medine" },
            new Sure { SureNo = 100, Ad = "Âdiyât", AdArapca = "العاديات", AyetSayisi = 11, Inis = "Mekke" },
            new Sure { SureNo = 101, Ad = "Kâria", AdArapca = "القارعة", AyetSayisi = 11, Inis = "Mekke" },
            new Sure { SureNo = 102, Ad = "Tekâsür", AdArapca = "التكاثر", AyetSayisi = 8, Inis = "Mekke" },
            new Sure { SureNo = 103, Ad = "Asr", AdArapca = "العصر", AyetSayisi = 3, Inis = "Mekke" },
            new Sure { SureNo = 104, Ad = "Hümeze", AdArapca = "الهمزة", AyetSayisi = 9, Inis = "Mekke" },
            new Sure { SureNo = 105, Ad = "Fîl", AdArapca = "الفيل", AyetSayisi = 5, Inis = "Mekke" },
            new Sure { SureNo = 106, Ad = "Kureyş", AdArapca = "قريش", AyetSayisi = 4, Inis = "Mekke" },
            new Sure { SureNo = 107, Ad = "Mâûn", AdArapca = "الماعون", AyetSayisi = 7, Inis = "Mekke" },
            new Sure { SureNo = 108, Ad = "Kevser", AdArapca = "الكوثر", AyetSayisi = 3, Inis = "Mekke" },
            new Sure { SureNo = 109, Ad = "Kâfirûn", AdArapca = "الكافرون", AyetSayisi = 6, Inis = "Mekke" },
            new Sure { SureNo = 110, Ad = "Nasr", AdArapca = "النصر", AyetSayisi = 3, Inis = "Medine" },
            new Sure { SureNo = 111, Ad = "Tebbet", AdArapca = "المسد", AyetSayisi = 5, Inis = "Mekke" },
            new Sure { SureNo = 112, Ad = "İhlâs", AdArapca = "الإخلاص", AyetSayisi = 4, Inis = "Mekke" },
            new Sure { SureNo = 113, Ad = "Felak", AdArapca = "الفلق", AyetSayisi = 5, Inis = "Mekke" },
            new Sure { SureNo = 114, Ad = "Nâs", AdArapca = "الناس", AyetSayisi = 6, Inis = "Mekke" }
        }.AsReadOnly();

        // Standard 15-line Madani Mushaf Page Numbers for each Surah (1-114)
        public static readonly int[] SurahPageNumbers = new int[] {
            1, 2, 50, 77, 106, 128, 151, 177, 187, 208,
            221, 235, 249, 255, 262, 267, 282, 293, 305, 312,
            322, 332, 342, 350, 359, 367, 377, 385, 396, 404,
            411, 415, 418, 428, 434, 440, 446, 453, 458, 467,
            477, 483, 489, 496, 499, 502, 507, 511, 515, 518,
            520, 523, 526, 528, 531, 534, 537, 542, 545, 549,
            551, 553, 554, 556, 558, 560, 562, 564, 566, 568,
            570, 572, 574, 575, 577, 578, 580, 582, 583, 585,
            586, 587, 587, 589, 590, 591, 591, 592, 593, 594,
            595, 595, 596, 596, 597, 597, 598, 598, 599, 600,
            600, 601, 601, 602, 602, 603, 603, 604, 604
        };

        public static int GetBaslangicSayfasi(int surahNo)
        {
            if (surahNo < 1 || surahNo > 114) return 1;
            return SurahPageNumbers[surahNo - 1];
        }

        public static Sure GetSureFromPage(int page)
        {
            int surahIndex = -1;
            for (int i = 0; i < SurahPageNumbers.Length; i++)
            {
                if (SurahPageNumbers[i] <= page)
                {
                    surahIndex = i;
                }
                else
                {
                    break;
                }
            }
            
            if (surahIndex != -1)
            {
                return _sureler[surahIndex];
            }
            return _sureler[0];
        }

        public static int GetCuzNo(int page)
        {
            if (page < 1) return 1;
            int juz = (page - 2) / 20 + 1;
            if (juz > 30) juz = 30;
            if (juz < 1) juz = 1;
            return juz;
        }

        /// <summary>
        /// Tüm sureleri döndürür (immutable - değiştirilemez)
        /// </summary>
        public static IReadOnlyList<Sure> GetSureler() => _sureler;

        public static Sure? GetSureByNo(int sureNo)
        {
            return _sureler.FirstOrDefault(s => s.SureNo == sureNo);
        }
    }
}

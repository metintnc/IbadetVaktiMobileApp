using hadis.Models;
using System.Collections.Generic;
using System.Linq;

namespace hadis.Services
{
    public class KuranDataService
    {
        private static List<Sure> _sureler;

        public static List<Sure> GetSureler()
        {
            if (_sureler == null)
            {
                _sureler = new List<Sure>
                {
                    new Sure { SureNo = 1, Ad = "Fatiha Suresi", AdArapca = "???????", AyetSayisi = 7, Inis = "Mekke", BaslangicSayfasi = 1 },
                    new Sure { SureNo = 2, Ad = "Bakara Suresi", AdArapca = "??????", AyetSayisi = 286, Inis = "Medine", BaslangicSayfasi = 2 },
                    new Sure { SureNo = 3, Ad = "Al-i Ýmran Suresi", AdArapca = "?? ?????", AyetSayisi = 200, Inis = "Medine", BaslangicSayfasi = 50 },
                    new Sure { SureNo = 4, Ad = "Nisa Suresi", AdArapca = "??????", AyetSayisi = 176, Inis = "Medine", BaslangicSayfasi = 77 },
                    new Sure { SureNo = 5, Ad = "Maide Suresi", AdArapca = "???????", AyetSayisi = 120, Inis = "Medine", BaslangicSayfasi = 106 },
                    new Sure { SureNo = 6, Ad = "En'am Suresi", AdArapca = "???????", AyetSayisi = 165, Inis = "Mekke", BaslangicSayfasi = 128 },
                    new Sure { SureNo = 7, Ad = "A'raf Suresi", AdArapca = "???????", AyetSayisi = 206, Inis = "Mekke", BaslangicSayfasi = 151 },
                    new Sure { SureNo = 8, Ad = "Enfal Suresi", AdArapca = "???????", AyetSayisi = 75, Inis = "Medine", BaslangicSayfasi = 177 },
                    new Sure { SureNo = 9, Ad = "Tevbe Suresi", AdArapca = "??????", AyetSayisi = 129, Inis = "Medine", BaslangicSayfasi = 187 },
                    new Sure { SureNo = 10, Ad = "Yunus Suresi", AdArapca = "????", AyetSayisi = 109, Inis = "Mekke", BaslangicSayfasi = 208 },
                    new Sure { SureNo = 11, Ad = "Hud Suresi", AdArapca = "???", AyetSayisi = 123, Inis = "Mekke", BaslangicSayfasi = 221 },
                    new Sure { SureNo = 12, Ad = "Yusuf Suresi", AdArapca = "????", AyetSayisi = 111, Inis = "Mekke", BaslangicSayfasi = 235 },
                    new Sure { SureNo = 13, Ad = "Ra'd Suresi", AdArapca = "?????", AyetSayisi = 43, Inis = "Medine", BaslangicSayfasi = 249 },
                    new Sure { SureNo = 14, Ad = "Ýbrahim Suresi", AdArapca = "???????", AyetSayisi = 52, Inis = "Mekke", BaslangicSayfasi = 255 },
                    new Sure { SureNo = 15, Ad = "Hicr Suresi", AdArapca = "?????", AyetSayisi = 99, Inis = "Mekke", BaslangicSayfasi = 262 },
                    new Sure { SureNo = 16, Ad = "Nahl Suresi", AdArapca = "?????", AyetSayisi = 128, Inis = "Mekke", BaslangicSayfasi = 267 },
                    new Sure { SureNo = 17, Ad = "Ýsra Suresi", AdArapca = "???????", AyetSayisi = 111, Inis = "Mekke", BaslangicSayfasi = 282 },
                    new Sure { SureNo = 18, Ad = "Kehf Suresi", AdArapca = "?????", AyetSayisi = 110, Inis = "Mekke", BaslangicSayfasi = 293 },
                    new Sure { SureNo = 19, Ad = "Meryem Suresi", AdArapca = "????", AyetSayisi = 98, Inis = "Mekke", BaslangicSayfasi = 305 },
                    new Sure { SureNo = 20, Ad = "Taha Suresi", AdArapca = "??", AyetSayisi = 135, Inis = "Mekke", BaslangicSayfasi = 312 },
                    new Sure { SureNo = 21, Ad = "Enbiya Suresi", AdArapca = "????????", AyetSayisi = 112, Inis = "Mekke", BaslangicSayfasi = 322 },
                    new Sure { SureNo = 22, Ad = "Hac Suresi", AdArapca = "????", AyetSayisi = 78, Inis = "Medine", BaslangicSayfasi = 332 },
                    new Sure { SureNo = 23, Ad = "Müminun Suresi", AdArapca = "????????", AyetSayisi = 118, Inis = "Mekke", BaslangicSayfasi = 342 },
                    new Sure { SureNo = 24, Ad = "Nur Suresi", AdArapca = "?????", AyetSayisi = 64, Inis = "Medine", BaslangicSayfasi = 350 },
                    new Sure { SureNo = 25, Ad = "Furkan Suresi", AdArapca = "???????", AyetSayisi = 77, Inis = "Mekke", BaslangicSayfasi = 359 },
                    new Sure { SureNo = 26, Ad = "Þuara Suresi", AdArapca = "???????", AyetSayisi = 227, Inis = "Mekke", BaslangicSayfasi = 367 },
                    new Sure { SureNo = 27, Ad = "Neml Suresi", AdArapca = "?????", AyetSayisi = 93, Inis = "Mekke", BaslangicSayfasi = 377 },
                    new Sure { SureNo = 28, Ad = "Kasas Suresi", AdArapca = "?????", AyetSayisi = 88, Inis = "Mekke", BaslangicSayfasi = 385 },
                    new Sure { SureNo = 29, Ad = "Ankebut Suresi", AdArapca = "????????", AyetSayisi = 69, Inis = "Mekke", BaslangicSayfasi = 396 },
                    new Sure { SureNo = 30, Ad = "Rum Suresi", AdArapca = "?????", AyetSayisi = 60, Inis = "Mekke", BaslangicSayfasi = 404 },
                    new Sure { SureNo = 31, Ad = "Lokman Suresi", AdArapca = "?????", AyetSayisi = 34, Inis = "Mekke", BaslangicSayfasi = 411 },
                    new Sure { SureNo = 32, Ad = "Secde Suresi", AdArapca = "??????", AyetSayisi = 30, Inis = "Mekke", BaslangicSayfasi = 415 },
                    new Sure { SureNo = 33, Ad = "Ahzab Suresi", AdArapca = "???????", AyetSayisi = 73, Inis = "Medine", BaslangicSayfasi = 418 },
                    new Sure { SureNo = 34, Ad = "Sebe Suresi", AdArapca = "???", AyetSayisi = 54, Inis = "Mekke", BaslangicSayfasi = 428 },
                    new Sure { SureNo = 35, Ad = "Fatýr Suresi", AdArapca = "????", AyetSayisi = 45, Inis = "Mekke", BaslangicSayfasi = 434 },
                    new Sure { SureNo = 36, Ad = "Yasin Suresi", AdArapca = "??", AyetSayisi = 83, Inis = "Mekke", BaslangicSayfasi = 440 },
                    new Sure { SureNo = 37, Ad = "Saffat Suresi", AdArapca = "???????", AyetSayisi = 182, Inis = "Mekke", BaslangicSayfasi = 446 },
                    new Sure { SureNo = 38, Ad = "Sad Suresi", AdArapca = "?", AyetSayisi = 88, Inis = "Mekke", BaslangicSayfasi = 453 },
                    new Sure { SureNo = 39, Ad = "Zümer Suresi", AdArapca = "?????", AyetSayisi = 75, Inis = "Mekke", BaslangicSayfasi = 458 },
                    new Sure { SureNo = 40, Ad = "Mümin Suresi", AdArapca = "????", AyetSayisi = 85, Inis = "Mekke", BaslangicSayfasi = 467 },
                    new Sure { SureNo = 41, Ad = "Fussilet Suresi", AdArapca = "????", AyetSayisi = 54, Inis = "Mekke", BaslangicSayfasi = 477 },
                    new Sure { SureNo = 42, Ad = "Þura Suresi", AdArapca = "??????", AyetSayisi = 53, Inis = "Mekke", BaslangicSayfasi = 482 },
                    new Sure { SureNo = 43, Ad = "Zuhruf Suresi", AdArapca = "??????", AyetSayisi = 89, Inis = "Mekke", BaslangicSayfasi = 489 },
                    new Sure { SureNo = 44, Ad = "Duhan Suresi", AdArapca = "??????", AyetSayisi = 59, Inis = "Mekke", BaslangicSayfasi = 496 },
                    new Sure { SureNo = 45, Ad = "Casiye Suresi", AdArapca = "???????", AyetSayisi = 37, Inis = "Mekke", BaslangicSayfasi = 499 },
                    new Sure { SureNo = 46, Ad = "Ahkaf Suresi", AdArapca = "???????", AyetSayisi = 35, Inis = "Mekke", BaslangicSayfasi = 502 },
                    new Sure { SureNo = 47, Ad = "Muhammed Suresi", AdArapca = "????", AyetSayisi = 38, Inis = "Medine", BaslangicSayfasi = 507 },
                    new Sure { SureNo = 48, Ad = "Fetih Suresi", AdArapca = "?????", AyetSayisi = 29, Inis = "Medine", BaslangicSayfasi = 511 },
                    new Sure { SureNo = 49, Ad = "Hucurat Suresi", AdArapca = "???????", AyetSayisi = 18, Inis = "Medine", BaslangicSayfasi = 515 },
                    new Sure { SureNo = 50, Ad = "Kaf Suresi", AdArapca = "?", AyetSayisi = 45, Inis = "Mekke", BaslangicSayfasi = 518 },
                    new Sure { SureNo = 51, Ad = "Zariyat Suresi", AdArapca = "????????", AyetSayisi = 60, Inis = "Mekke", BaslangicSayfasi = 520 },
                    new Sure { SureNo = 52, Ad = "Tur Suresi", AdArapca = "?????", AyetSayisi = 49, Inis = "Mekke", BaslangicSayfasi = 523 },
                    new Sure { SureNo = 53, Ad = "Necm Suresi", AdArapca = "?????", AyetSayisi = 62, Inis = "Mekke", BaslangicSayfasi = 526 },
                    new Sure { SureNo = 54, Ad = "Kamer Suresi", AdArapca = "?????", AyetSayisi = 55, Inis = "Mekke", BaslangicSayfasi = 528 },
                    new Sure { SureNo = 55, Ad = "Rahman Suresi", AdArapca = "??????", AyetSayisi = 78, Inis = "Medine", BaslangicSayfasi = 531 },
                    new Sure { SureNo = 56, Ad = "Vakýa Suresi", AdArapca = "???????", AyetSayisi = 96, Inis = "Mekke", BaslangicSayfasi = 534 },
                    new Sure { SureNo = 57, Ad = "Hadid Suresi", AdArapca = "??????", AyetSayisi = 29, Inis = "Medine", BaslangicSayfasi = 537 },
                    new Sure { SureNo = 58, Ad = "Mücadele Suresi", AdArapca = "????????", AyetSayisi = 22, Inis = "Medine", BaslangicSayfasi = 542 },
                    new Sure { SureNo = 59, Ad = "Haþr Suresi", AdArapca = "?????", AyetSayisi = 24, Inis = "Medine", BaslangicSayfasi = 545 },
                    new Sure { SureNo = 60, Ad = "Mümtehine Suresi", AdArapca = "????????", AyetSayisi = 13, Inis = "Medine", BaslangicSayfasi = 549 },
                    new Sure { SureNo = 61, Ad = "Saff Suresi", AdArapca = "????", AyetSayisi = 14, Inis = "Medine", BaslangicSayfasi = 551 },
                    new Sure { SureNo = 62, Ad = "Cuma Suresi", AdArapca = "??????", AyetSayisi = 11, Inis = "Medine", BaslangicSayfasi = 553 },
                    new Sure { SureNo = 63, Ad = "Münafikun Suresi", AdArapca = "?????????", AyetSayisi = 11, Inis = "Medine", BaslangicSayfasi = 554 },
                    new Sure { SureNo = 64, Ad = "Tegabun Suresi", AdArapca = "???????", AyetSayisi = 18, Inis = "Medine", BaslangicSayfasi = 556 },
                    new Sure { SureNo = 65, Ad = "Talak Suresi", AdArapca = "??????", AyetSayisi = 12, Inis = "Medine", BaslangicSayfasi = 558 },
                    new Sure { SureNo = 66, Ad = "Tahrim Suresi", AdArapca = "???????", AyetSayisi = 12, Inis = "Medine", BaslangicSayfasi = 560 },
                    new Sure { SureNo = 67, Ad = "Mülk Suresi", AdArapca = "?????", AyetSayisi = 30, Inis = "Mekke", BaslangicSayfasi = 562 },
                    new Sure { SureNo = 68, Ad = "Kalem Suresi", AdArapca = "?????", AyetSayisi = 52, Inis = "Mekke", BaslangicSayfasi = 564 },
                    new Sure { SureNo = 69, Ad = "Hakka Suresi", AdArapca = "??????", AyetSayisi = 52, Inis = "Mekke", BaslangicSayfasi = 566 },
                    new Sure { SureNo = 70, Ad = "Mearic Suresi", AdArapca = "???????", AyetSayisi = 44, Inis = "Mekke", BaslangicSayfasi = 568 },
                    new Sure { SureNo = 71, Ad = "Nuh Suresi", AdArapca = "???", AyetSayisi = 28, Inis = "Mekke", BaslangicSayfasi = 570 },
                    new Sure { SureNo = 72, Ad = "Cin Suresi", AdArapca = "????", AyetSayisi = 28, Inis = "Mekke", BaslangicSayfasi = 572 },
                    new Sure { SureNo = 73, Ad = "Müzzemmil Suresi", AdArapca = "??????", AyetSayisi = 20, Inis = "Mekke", BaslangicSayfasi = 574 },
                    new Sure { SureNo = 74, Ad = "Müddessir Suresi", AdArapca = "??????", AyetSayisi = 56, Inis = "Mekke", BaslangicSayfasi = 575 },
                    new Sure { SureNo = 75, Ad = "Kýyamet Suresi", AdArapca = "???????", AyetSayisi = 40, Inis = "Mekke", BaslangicSayfasi = 577 },
                    new Sure { SureNo = 76, Ad = "Ýnsan Suresi", AdArapca = "???????", AyetSayisi = 31, Inis = "Medine", BaslangicSayfasi = 578 },
                    new Sure { SureNo = 77, Ad = "Mürselat Suresi", AdArapca = "????????", AyetSayisi = 50, Inis = "Mekke", BaslangicSayfasi = 580 },
                    new Sure { SureNo = 78, Ad = "Nebe Suresi", AdArapca = "?????", AyetSayisi = 40, Inis = "Mekke", BaslangicSayfasi = 582 },
                    new Sure { SureNo = 79, Ad = "Naziat Suresi", AdArapca = "????????", AyetSayisi = 46, Inis = "Mekke", BaslangicSayfasi = 583 },
                    new Sure { SureNo = 80, Ad = "Abese Suresi", AdArapca = "???", AyetSayisi = 42, Inis = "Mekke", BaslangicSayfasi = 584 },
                    new Sure { SureNo = 81, Ad = "Tekvir Suresi", AdArapca = "???????", AyetSayisi = 29, Inis = "Mekke", BaslangicSayfasi = 586 },
                    new Sure { SureNo = 82, Ad = "Ýnfitar Suresi", AdArapca = "????????", AyetSayisi = 19, Inis = "Mekke", BaslangicSayfasi = 587 },
                    new Sure { SureNo = 83, Ad = "Mutaffifin Suresi", AdArapca = "????????", AyetSayisi = 36, Inis = "Mekke", BaslangicSayfasi = 588 },
                    new Sure { SureNo = 84, Ad = "Ýnþikak Suresi", AdArapca = "????????", AyetSayisi = 25, Inis = "Mekke", BaslangicSayfasi = 589 },
                    new Sure { SureNo = 85, Ad = "Buruc Suresi", AdArapca = "??????", AyetSayisi = 22, Inis = "Mekke", BaslangicSayfasi = 590 },
                    new Sure { SureNo = 86, Ad = "Tarýk Suresi", AdArapca = "??????", AyetSayisi = 17, Inis = "Mekke", BaslangicSayfasi = 591 },
                    new Sure { SureNo = 87, Ad = "Ala Suresi", AdArapca = "??????", AyetSayisi = 19, Inis = "Mekke", BaslangicSayfasi = 592 },
                    new Sure { SureNo = 88, Ad = "Gaþiye Suresi", AdArapca = "???????", AyetSayisi = 26, Inis = "Mekke", BaslangicSayfasi = 593 },
                    new Sure { SureNo = 89, Ad = "Fecr Suresi", AdArapca = "?????", AyetSayisi = 30, Inis = "Mekke", BaslangicSayfasi = 594 },
                    new Sure { SureNo = 90, Ad = "Beled Suresi", AdArapca = "?????", AyetSayisi = 20, Inis = "Mekke", BaslangicSayfasi = 595 },
                    new Sure { SureNo = 91, Ad = "Þems Suresi", AdArapca = "?????", AyetSayisi = 15, Inis = "Mekke", BaslangicSayfasi = 596 },
                    new Sure { SureNo = 92, Ad = "Leyl Suresi", AdArapca = "?????", AyetSayisi = 21, Inis = "Mekke", BaslangicSayfasi = 597 },
                    new Sure { SureNo = 93, Ad = "Duha Suresi", AdArapca = "?????", AyetSayisi = 11, Inis = "Mekke", BaslangicSayfasi = 598 },
                    new Sure { SureNo = 94, Ad = "Ýnþirah Suresi", AdArapca = "?????", AyetSayisi = 8, Inis = "Mekke", BaslangicSayfasi = 599 },
                    new Sure { SureNo = 95, Ad = "Tin Suresi", AdArapca = "?????", AyetSayisi = 8, Inis = "Mekke", BaslangicSayfasi = 600 },
                    new Sure { SureNo = 96, Ad = "Alak Suresi", AdArapca = "?????", AyetSayisi = 19, Inis = "Mekke", BaslangicSayfasi = 601 },
                    new Sure { SureNo = 97, Ad = "Kadr Suresi", AdArapca = "?????", AyetSayisi = 5, Inis = "Mekke", BaslangicSayfasi = 602 },
                    new Sure { SureNo = 98, Ad = "Beyyine Suresi", AdArapca = "??????", AyetSayisi = 8, Inis = "Medine", BaslangicSayfasi = 603 },
                    new Sure { SureNo = 99, Ad = "Zilzal Suresi", AdArapca = "???????", AyetSayisi = 8, Inis = "Medine", BaslangicSayfasi = 604 },
                    new Sure { SureNo = 100, Ad = "Adiyat Suresi", AdArapca = "????????", AyetSayisi = 11, Inis = "Mekke", BaslangicSayfasi = 605 },
                    new Sure { SureNo = 101, Ad = "Karia Suresi", AdArapca = "???????", AyetSayisi = 11, Inis = "Mekke", BaslangicSayfasi = 606 },
                    new Sure { SureNo = 102, Ad = "Tekasür Suresi", AdArapca = "???????", AyetSayisi = 8, Inis = "Mekke", BaslangicSayfasi = 607 },
                    new Sure { SureNo = 103, Ad = "Asr Suresi", AdArapca = "?????", AyetSayisi = 3, Inis = "Mekke", BaslangicSayfasi = 608 },
                    new Sure { SureNo = 104, Ad = "Hümeze Suresi", AdArapca = "??????", AyetSayisi = 9, Inis = "Mekke", BaslangicSayfasi = 609 },
                    new Sure { SureNo = 105, Ad = "Fil Suresi", AdArapca = "?????", AyetSayisi = 5, Inis = "Mekke", BaslangicSayfasi = 610 },
                    new Sure { SureNo = 106, Ad = "Kureyþ Suresi", AdArapca = "????", AyetSayisi = 4, Inis = "Mekke", BaslangicSayfasi = 611 },
                    new Sure { SureNo = 107, Ad = "Maun Suresi", AdArapca = "???????", AyetSayisi = 7, Inis = "Mekke", BaslangicSayfasi = 612 },
                    new Sure { SureNo = 108, Ad = "Kevser Suresi", AdArapca = "??????", AyetSayisi = 3, Inis = "Mekke", BaslangicSayfasi = 613 },
                    new Sure { SureNo = 109, Ad = "Kafirun Suresi", AdArapca = "????????", AyetSayisi = 6, Inis = "Mekke", BaslangicSayfasi = 614 },
                    new Sure { SureNo = 110, Ad = "Nasr Suresi", AdArapca = "?????", AyetSayisi = 3, Inis = "Medine", BaslangicSayfasi = 615 },
                    new Sure { SureNo = 111, Ad = "Tebbet Suresi", AdArapca = "?????", AyetSayisi = 5, Inis = "Mekke", BaslangicSayfasi = 616 },
                    new Sure { SureNo = 112, Ad = "Ýhlas Suresi", AdArapca = "???????", AyetSayisi = 4, Inis = "Mekke", BaslangicSayfasi = 617 },
                    new Sure { SureNo = 113, Ad = "Felak Suresi", AdArapca = "?????", AyetSayisi = 5, Inis = "Mekke", BaslangicSayfasi = 618 },
                    new Sure { SureNo = 114, Ad = "Nas Suresi", AdArapca = "?????", AyetSayisi = 6, Inis = "Mekke", BaslangicSayfasi = 619 }
                };
            }
            return _sureler;
        }

        public static Sure GetSureByNo(int sureNo)
        {
            return GetSureler().FirstOrDefault(s => s.SureNo == sureNo);
        }

        public static List<Sure> GetFavorites()
        {
            var favorites = Preferences.Default.Get("FavoriteSureler", "");
            var favoriteIds = favorites.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(int.Parse)
                                      .ToList();

            return GetSureler().Where(s => favoriteIds.Contains(s.SureNo)).ToList();
        }

        public static void ToggleFavorite(int sureNo)
        {
            var favorites = Preferences.Default.Get("FavoriteSureler", "");
            var favoriteList = favorites.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (favoriteList.Contains(sureNo.ToString()))
            {
                favoriteList.Remove(sureNo.ToString());
            }
            else
            {
                favoriteList.Add(sureNo.ToString());
            }

            Preferences.Default.Set("FavoriteSureler", string.Join(",", favoriteList));
        }

        public static bool IsFavorite(int sureNo)
        {
            var favorites = Preferences.Default.Get("FavoriteSureler", "");
            return favorites.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Any(f => f == sureNo.ToString());
        }
    }
}

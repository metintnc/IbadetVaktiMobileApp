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
                    new Sure { SureNo = 3, Ad = "Al-i Imran Suresi", AdArapca = "?? ?????", AyetSayisi = 200, Inis = "Medine", BaslangicSayfasi = 50 },
                    new Sure { SureNo = 4, Ad = "Nisa Suresi", AdArapca = "??????", AyetSayisi = 176, Inis = "Medine", BaslangicSayfasi = 77 },
                    new Sure { SureNo = 5, Ad = "Maide Suresi", AdArapca = "???????", AyetSayisi = 120, Inis = "Medine", BaslangicSayfasi = 106 },
                    new Sure { SureNo = 6, Ad = "En'am Suresi", AdArapca = "???????", AyetSayisi = 165, Inis = "Mekke", BaslangicSayfasi = 128 },
                    new Sure { SureNo = 7, Ad = "A'raf Suresi", AdArapca = "???????", AyetSayisi = 206, Inis = "Mekke", BaslangicSayfasi = 151 },
                    new Sure { SureNo = 8, Ad = "Enfal Suresi", AdArapca = "???????", AyetSayisi = 75, Inis = "Medine", BaslangicSayfasi = 177 },
                    new Sure { SureNo = 9, Ad = "Tevbe Suresi", AdArapca = "??????", AyetSayisi = 129, Inis = "Medine", BaslangicSayfasi = 187 },
                    new Sure { SureNo = 10, Ad = "Yunus Suresi", AdArapca = "????", AyetSayisi = 109, Inis = "Mekke", BaslangicSayfasi = 208 }
                };
                
                // Ýlk 10 sure ile test
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

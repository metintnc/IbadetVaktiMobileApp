using System.Collections.ObjectModel;
using hadis.Models;
using hadis.Services;
using System.Threading.Tasks;

namespace hadis.ViewModels
{
    public class KuranViewModel
    {
        public ObservableCollection<Ayah> Ayahs { get; set; } = new();
        public string SureTitle { get; set; }
        public string SureTitleArabic { get; set; }
        private int _sureNo;

        public KuranViewModel(int sureNo)
        {
            _sureNo = sureNo;
            var sure = KuranDataService.GetSureByNo(sureNo);
            SureTitle = sure?.Ad ?? "";
            SureTitleArabic = sure?.AdArapca ?? "";
            _ = LoadSureWithTranslationAsync();
        }

        private async Task LoadSureWithTranslationAsync()
        {
            var service = new QuranApiService();
            var arabicAyahs = await service.GetSurahAsync(_sureNo, "ar");
            var turkishAyahs = await service.GetSurahAsync(_sureNo, "tr.diyanet");
            
            // Kaydedilenleri al
            var savedAyahs = await SavedAyahsService.GetSavedAyahsAsync();
            var savedSet = new HashSet<int>(savedAyahs.Where(x => x.SureNo == _sureNo).Select(x => x.Number));

            Ayahs.Clear();
            int localNumber = 1;
            foreach (var a in arabicAyahs)
            {
                var translation = turkishAyahs.FirstOrDefault(t => t.Number == a.Number)?.Text ?? string.Empty;
                Ayahs.Add(new Ayah
                {
                    Number = localNumber, // Her surede 1'den başlayıp artacak
                    ArabicText = a.Text,
                    Translation = translation,
                    Transliteration = string.Empty,
                    IsSaved = savedSet.Contains(localNumber)
                });
                localNumber++;
            }
        }
    }
}

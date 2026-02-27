using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using hadis.Models;
using hadis.Services;
using System.Threading.Tasks;

namespace hadis.ViewModels
{
    public partial class KuranViewModel : ObservableObject
    {
        public ObservableCollection<Ayah> Ayahs { get; set; } = new();
        public string SureTitle { get; set; }
        public string SureTitleArabic { get; set; }
        
        [ObservableProperty]
        private bool isBusy;

        private readonly int _sureNo;
        private readonly QuranApiService _quranApiService;

        public KuranViewModel(int sureNo, QuranApiService quranApiService)
        {
            _sureNo = sureNo;
            _quranApiService = quranApiService;
            
            var sure = KuranDataService.GetSureByNo(sureNo);
            SureTitle = sure?.Ad ?? "";
            SureTitleArabic = sure?.AdArapca ?? "";
            _ = LoadSureWithTranslationAsync();
        }

        private async Task LoadSureWithTranslationAsync()
        {
            IsBusy = true;
            try
            {
                // DI ile inject edilmiþ service kullan
                var fetchedAyahs = await _quranApiService.GetSurahAsync(_sureNo);
                
                // Kaydedilenleri al
                var savedAyahs = await SavedAyahsService.GetSavedAyahsAsync();
                var savedSet = new HashSet<int>(savedAyahs.Where(x => x.SureNo == _sureNo).Select(x => x.Number));

                Ayahs.Clear();
                foreach (var a in fetchedAyahs)
                {
                    a.IsSaved = savedSet.Contains(a.Number);
                    Ayahs.Add(a);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}

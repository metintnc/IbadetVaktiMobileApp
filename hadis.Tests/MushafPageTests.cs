using hadis.Services;

namespace hadis.Tests;

public class MushafPageTests
{
    [Fact]
    public void ScannedPagesCoverEveryVerseOnceInOrder()
    {
        int global = 0;
        var seen = new List<(int Surah, int Verse)>();
        for (int page = 1; page <= KuranDataService.PageCount; page++)
        {
            var ranges = KuranDataService.GetPageRanges(page).ToArray();
            Assert.NotEmpty(ranges);
            Assert.Equal(ranges[0].Surah, KuranDataService.GetSureFromPage(page));
            foreach (var (surah, first, last) in ranges)
            {
                for (int verse = first; verse <= last; verse++)
                {
                    Assert.Equal(page, KuranDataService.GetReaderPage(++global));
                    if (verse == 1) Assert.Equal(page, KuranDataService.GetBaslangicSayfasi(surah.SureNo));
                    seen.Add((surah.SureNo, verse));
                }
            }
        }
        Assert.Equal(6236, global);
        Assert.Equal(KuranDataService.GetSureler().SelectMany(s =>
            Enumerable.Range(1, s.AyetSayisi).Select(n => (s.SureNo, n))), seen);
    }

    [Fact]
    public void FinalScanContainsIhlasFelakAndNas()
    {
        Assert.Equal(605, KuranDataService.PageCount);
        var ranges = KuranDataService.GetPageRanges(605).Select(r => (r.Surah.SureNo, r.FirstVerse, r.LastVerse));
        Assert.Equal(new[] { (112, 1, 4), (113, 1, 5), (114, 1, 6) }, ranges);
    }
}

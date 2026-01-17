using System.ComponentModel;
using Syncfusion.Maui.PdfViewer;
using hadis.Services;

namespace hadis
{
    public partial class KuranPDF : ContentPage
    {
        private int Sayfa;
        private bool _yukleniyor = true;
        private readonly string _localPdfPath;
        private FileStream _pdfStream;
        private bool _sayfaYuklendi = false;
        
        public KuranPDF()
        {
            InitializeComponent();
            _localPdfPath = Path.Combine(FileSystem.AppDataDirectory, "kuran.pdf");
            pdfViewer.DocumentLoaded += PdfViewer_DocumentLoaded;
            pdfViewer.PropertyChanged += PdfViewer_PropertyChanged;
        }
        
        private void PdfViewer_DocumentLoaded(object sender, EventArgs e)
        {
            if (!_sayfaYuklendi)
            {
                Sayfa = Preferences.Default.Get("KuranSonSayfa", 1);
                pdfViewer.GoToPage(Sayfa);
                _sayfaYuklendi = true;
            }
            _yukleniyor = false;
        }
        
        private void PdfViewer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_yukleniyor) return;
            if (e.PropertyName == nameof(Syncfusion.Maui.PdfViewer.SfPdfViewer.PageNumber))
            {
                Preferences.Default.Set("KuranSonSayfa", pdfViewer.PageNumber);
                
                // Sure bilgisini güncelle
                var sure = KuranDataService.GetSureler()
                    .Where(s => s.BaslangicSayfasi <= pdfViewer.PageNumber)
                    .OrderByDescending(s => s.BaslangicSayfasi)
                    .FirstOrDefault();
                
                if (sure != null)
                {
                    Preferences.Default.Set("KuranSonSureNo", sure.SureNo);
                }
            }
        }
        
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            if (_pdfStream == null)
            {
                await KuranPDFYukleAsync();
            }
        }
        
        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            
            // Tab ile gelince fade animasyon
            pdfViewer.Opacity = 0;
            await pdfViewer.FadeTo(1, 400, Easing.CubicOut);
        }
        
        protected override async void OnNavigatedFrom(NavigatedFromEventArgs args)
        {
            base.OnNavigatedFrom(args);
            
            // Tab değişirken fade out
            await pdfViewer.FadeTo(0, 250, Easing.CubicIn);
        }

        private async Task KuranPDFYukleAsync()
        {
            try
            {
                // İlk açılışta PDF'i kopyala
                await İlkAcılıstaKopyala();
                
                // FileStream'i aç ve yükle
                _pdfStream = new FileStream(_localPdfPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                pdfViewer.LoadDocument(_pdfStream);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"PDF yüklenirken bir sorun oluştu: {ex.Message}", "Tamam");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Son sayfayı kaydet
            if (!_yukleniyor && _sayfaYuklendi)
            {
                Preferences.Default.Set("KuranSonSayfa", pdfViewer.PageNumber);
            }
        }

        private async Task İlkAcılıstaKopyala()
        {
            // Dosya zaten varsaysa kopyalamayı atla
            if (File.Exists(_localPdfPath))
                return;

            using (Stream assetStream = await FileSystem.OpenAppPackageFileAsync("kuran.pdf"))
            {
                using (FileStream fileStream = new FileStream(_localPdfPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true))
                {
                    await assetStream.CopyToAsync(fileStream, 81920);
                }
            }
        }
    }
}


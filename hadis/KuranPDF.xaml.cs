using System.ComponentModel;
using Syncfusion.Maui.PdfViewer;
namespace hadis
{
    public partial class KuranPDF : ContentPage
    {
        private int Sayfa;
        private bool _yukleniyor = true;
        private readonly string _localPdfPath;
        private FileStream _pdfStream;
        public KuranPDF()
        {
            InitializeComponent();
            _localPdfPath = Path.Combine(FileSystem.AppDataDirectory, "kuran.pdf");
            pdfViewer.DocumentLoaded += PdfViewer_DocumentLoaded;
            pdfViewer.PropertyChanged += PdfViewer_PropertyChanged;
        }
        private void PdfViewer_DocumentLoaded(object sender, EventArgs e)
        {
            _yukleniyor = true;
        }
        private void PdfViewer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_yukleniyor) return;
            if (e.PropertyName == nameof(Syncfusion.Maui.PdfViewer.SfPdfViewer.PageNumber))
            {
                Preferences.Default.Set("KuranSonSayfa", pdfViewer.PageNumber);
            }
            
        }
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await KuranPDFYukleAsync();
            Sayfa = Preferences.Default.Get("KuranSonSayfa", 1);
            pdfViewer.GoToPage(Sayfa);
        }

        private async Task KuranPDFYukleAsync()
        {
            try
            {
                await İlkAcılıstaKopyala();
                _pdfStream = new FileStream(_localPdfPath, FileMode.Open, FileAccess.Read);
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
            _pdfStream?.Dispose();
            _pdfStream = null;
            Preferences.Default.Set("KuranSonSayfa", pdfViewer.PageNumber);
        }

        private async Task İlkAcılıstaKopyala()
        {
            if (File.Exists(_localPdfPath))
                return;

            using (Stream assetStream = await FileSystem.OpenAppPackageFileAsync("kuran.pdf"))
            {
                using (FileStream fileStream = File.Create(_localPdfPath))
                {
                    await assetStream.CopyToAsync(fileStream);
                }
            }
        }
    }
}


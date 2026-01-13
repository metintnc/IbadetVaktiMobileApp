using hadis.Models;
using System.Collections.ObjectModel;

namespace hadis
{
    public partial class WidgetAyarlari : ContentPage
    {
        public WidgetAyarlari()
        {
            InitializeComponent();
        }

        private async void EkleButton_Clicked(object sender, EventArgs e)
        {
#if ANDROID
            try
            {
                // Android'de widget ekleme iþlemi kullanýcý tarafýndan yapýlýr
                // Uygulama sadece widget'ýn mevcut olduðunu gösterir
                await DisplayAlert("Widget Nasýl Eklenir?", 
                    "1. Ana ekranýnýzda boþ bir alana uzun basýn\n" +
                    "2. 'Widget'lar' veya 'Araçlar' seçeneðini seçin\n" +
                    "3. 'NamazVakti' uygulamasýný bulun\n" +
                    "4. 'Namaz Vakti Widget'ýný sürükleyip ekleyin\n\n" +
                    "Widget saat, tarih ve sonraki namaza kalan süreyi gösterir.\n" +
                    "Arkaplan %100 þeffaftýr.", 
                    "Tamam");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Bir hata oluþtu: {ex.Message}", "Tamam");
            }
#else
            await DisplayAlert("Desteklenmiyor", "Widget özelliði þu anda sadece Android'de desteklenmektedir.", "Tamam");
#endif
        }
    }
}

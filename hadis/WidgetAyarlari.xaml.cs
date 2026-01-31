using Microsoft.Maui.Controls;
using System;

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
            // Widget ekleme işlemi platform spesifik olabilir veya burada basit bir mesaj gösterilebilir.
            // Android widgetları genellikle dışarıdan (Launcher'dan) eklenir, uygulama içinden "pin" işlemi
            // Android O+ (API 26) gerektirir.
            
            bool result = await DisplayAlert("Widget Ekle", "Widget'ı ana ekrana eklemek için ana ekranınızda boş bir yere basılı tutun ve 'Widgetlar' menüsünden Hadis uygulamasını seçin.", "Tamam", "İptal");
        }
    }
}

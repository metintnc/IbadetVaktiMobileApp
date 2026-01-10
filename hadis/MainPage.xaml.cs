using System.Text.Json;
using Microsoft.Maui.Devices.Sensors;
namespace hadis
{
    public partial class MainPage : ContentPage
    {
        Dictionary<string,DateTime> _namazvakitleri;
        private System.Timers.Timer _timer;
        public MainPage()
        {
            InitializeComponent();
            _ = NamazVakitleriniÇek();
            _timer = new System.Timers.Timer(500);
            _timer.Elapsed += async (s, e) => await MainThread.InvokeOnMainThreadAsync(GeriSayımıGüncelle);
            _timer.Start();
            _ =ayetgoster();
          
          
            
        }
  
       
        public async Task ayetgoster()
        {
            string[] ayetler = new string[]
            {
            "Hiç bilenlerle bilmeyenler bir olur mu? (Zümer, 9)",
            "Şüphesiz Allah sabredenlerle beraberdir. (Bakara, 153)",
            "Gerçekten güçlükle beraber bir kolaylık vardır. (İnşirah, 6)",
            "Allah, kullarına karşı çok şefkatlidir. (Şura, 19)",
            "Ey iman edenler! Sabır ve namazla Allah’tan yardım isteyin. (Bakara, 45)",
            "Göklerde ve yerde ne varsa hepsi Allah’ındır. (Bakara, 284)",
            "Zorlukla beraber bir kolaylık vardır. (İnşirah, 5)",
            "Kıyamet günü herkese amel defteri verilecektir. (İsra, 13)",
            "İyilik ve takva üzere yardımlaşın. (Maide, 2)",
            "Şüphesiz dönüş ancak Allah’adır. (Bakara, 156)"
            };
            int gunIndex = DateTime.Now.DayOfYear % ayetler.Length;
            string bugununAyeti = ayetler[gunIndex];
            gununayeti.Text = bugununAyeti;


            string[] hadisler = new string[]
            {
            "Ameller niyetlere göredir. (Buhârî, 1)",
            "Kolaylaştırın, zorlaştırmayın. (Buhârî, 11)",
            "Güzel söz sadakadır. (Müslim, 56)",
            "Tebessüm sadakadır. (Tirmizî, Birr 36)",
            "Faydasız şeyi terk et. (Tirmizî, Zühd 11)",
            "Temizlik imanın yarısıdır. (Müslim, Tahâret 1)",
            "Allah işini sağlam yapanı sever. (Taberânî)",
            "En hayırlınız, ahlakı en güzel olandır. (Tirmizî, Birr 61)"
            };
            string bugununhadisi = hadisler[gunIndex];
            
        }
        public void GeriSayımıGüncelle()
        {
          
            if(_namazvakitleri == null || _namazvakitleri.Count == 0)
            {
                return;
            }
            TimeSpan kalansure;
            string sonraki;
            DateTime simdi = DateTime.Now;
            if (_namazvakitleri["İmsak"] > simdi)
            {
                kalansure = _namazvakitleri["İmsak"] - simdi;
                sonraki = "İmsak Vaktine";
                aksamvakit.TextColor = Colors.Silver;
                yatsıvakit.TextColor = Colors.White;
            }
            else if (_namazvakitleri["gunes"] > simdi)
            {
                kalansure = _namazvakitleri["gunes"] - simdi;
                sonraki = "Güneşin Doğmasına";
                yatsıvakit.TextColor = Colors.Silver;
                imsakvakit.TextColor = Colors.White;
            }
            else if (_namazvakitleri["Ogle"] > simdi)
            {
                kalansure = _namazvakitleri["Ogle"] - simdi;
                sonraki = "Öğle Namazına";
                imsakvakit.TextColor = Colors.Silver;
                gunesvakit.TextColor = Colors.White;
            }
            else if (_namazvakitleri["İkindi"] > simdi)
            {
                kalansure = _namazvakitleri["İkindi"] - simdi;
                sonraki = "İkindi Namazına";
                gunesvakit.TextColor = Colors.Silver;
                oglevakit.TextColor = Colors.White;
            }
            else if (_namazvakitleri["Aksam"] > simdi)
            {
                kalansure = _namazvakitleri["Aksam"] - simdi;
                sonraki = "Akşam Namazına";
                oglevakit.TextColor = Colors.Silver;
                ikindivakit.TextColor = Colors.White;
            }
            else if (_namazvakitleri["Yatsi"] > simdi)
            {
                kalansure = _namazvakitleri["Yatsi"] - simdi;
                sonraki = "Yatsı Namazına";
                ikindivakit.TextColor = Colors.Silver;
                aksamvakit.TextColor = Colors.White;
            }
            else
            {
                _namazvakitleri["İmsak"] = _namazvakitleri["İmsak"].AddDays(1);
                kalansure = _namazvakitleri["İmsak"] - simdi;
                sonraki = "İmsak Vaktine";
                aksamvakit.TextColor  = Colors.Silver;
                yatsıvakit.TextColor = Colors.White;
            }
            namazismi.Text = sonraki;
            kalan.Text = $"{kalansure.Hours:D2} : {kalansure.Minutes:D2} : {kalansure.Seconds:D2}";
            yatsıvakit.Text = $"{_namazvakitleri["Yatsi"].Hour:D2}:{_namazvakitleri["Yatsi"].Minute:D2}";
            aksamvakit.Text = $"{_namazvakitleri["Aksam"].Hour:D2} : {_namazvakitleri["Aksam"].Minute:D2}";
            ikindivakit.Text = $"{_namazvakitleri["İkindi"].Hour:D2} : {_namazvakitleri["İkindi"].Minute:D2}";
            oglevakit.Text = $"{_namazvakitleri["Ogle"].Hour:D2} : {_namazvakitleri["Ogle"].Minute:D2}";
            gunesvakit.Text = $"{_namazvakitleri["gunes"].Hour:D2} : {_namazvakitleri["gunes"].Minute:D2}";
            imsakvakit.Text = $"{_namazvakitleri["İmsak"].Hour:D2}:{_namazvakitleri["İmsak"].Minute:D2}";
        }
        public async Task NamazVakitleriniÇek()
        {
            try
            {
                
                var (latitude, longitude) = await GetKonum();
                if (latitude == 0 && longitude == 0)
                {
                    kalan.Text = "- -";
                    namazismi.Text = "";
                    return;
                }

                if (latitude == 0 && longitude == 0)
                {
                    // Konum alınamadıysa işlemi iptal et, UI çökmemiş olur
                    kalan.Text = "- -";
                    namazismi.Text = "";
                    return;
                }
                HttpClient http = new HttpClient();
                string url = $"https://api.aladhan.com/v1/timings?latitude={latitude}&longitude={longitude}&method=13";
                HttpResponseMessage response = await http.GetAsync(url);
                string vakitler = await response.Content.ReadAsStringAsync();
                var root = JsonDocument.Parse(vakitler).RootElement.GetProperty("data");
                root = root.GetProperty("timings");
                string imsak = root.GetProperty("Fajr").GetString();
                string gunes = root.GetProperty("Sunrise").GetString();
                string ogle = root.GetProperty("Dhuhr").GetString();
                string ikindi = root.GetProperty("Asr").GetString();
                string aksam = root.GetProperty("Maghrib").GetString();
                string yatsi = root.GetProperty("Isha").GetString();

                DateTime imsakvakti = DateTime.Today + TimeSpan.Parse(imsak);
                DateTime gunesvakti = DateTime.Today + TimeSpan.Parse(gunes);
                DateTime oglevakti = DateTime.Today + TimeSpan.Parse(ogle);
                DateTime ikindivakti = DateTime.Today + TimeSpan.Parse(ikindi);
                DateTime aksamvakti = DateTime.Today + TimeSpan.Parse(aksam);
                DateTime yatsivakti = DateTime.Today + TimeSpan.Parse(yatsi);
                
                _namazvakitleri = new Dictionary<string, DateTime>();
                _namazvakitleri.Add("İmsak",imsakvakti);
                _namazvakitleri.Add("gunes", gunesvakti);
                _namazvakitleri.Add("Ogle", oglevakti);
                _namazvakitleri.Add("İkindi", ikindivakti);
                _namazvakitleri.Add("Aksam", aksamvakti);
                _namazvakitleri.Add("Yatsi", yatsivakti);
               

            }
            catch(Exception e)
            {
                kalan.Text = "- -";
                yatsıvakit.Text = "- -";
                aksamvakit.Text = "- -";
                ikindivakit.Text = "- -";
                oglevakit.Text = "- -";
                gunesvakit.Text = "- -";
                imsakvakit.Text = "- -";
            }

        }

        public async Task<(double Latiude, double longitude)> GetKonum()
        {
            try
            {
                var konum = await Geolocation.GetLastKnownLocationAsync();

                if (konum == null)
                {
                    konum = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
                }

                if (konum != null)
                {

                    return (konum.Latitude, konum.Longitude);
                }
                else
                {
                    throw new Exception("- -");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Konum Hatası: {ex.Message}");
                return (0, 0);
            }
        }

        private void Buton_Clicked(object sender, EventArgs e)
        {

        }
    }
}

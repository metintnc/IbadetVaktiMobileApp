using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using System.Globalization;

namespace hadis
{
    public partial class YakindakiCamiler : ContentPage
    {
        private bool _isLoaded = false;

        public YakindakiCamiler()
        {
            InitializeComponent();
            MapWebView.Navigating += OnWebViewNavigating;
        }

        private async void OnWebViewNavigating(object sender, WebNavigatingEventArgs e)
        {
            // Google Maps veya harici linkleri yakalayıp dış tarayıcıda aç
            if (e.Url != null && (e.Url.StartsWith("https://www.google.com/maps") || 
                                  e.Url.StartsWith("https://maps.google.com") ||
                                  e.Url.StartsWith("geo:")))
            {
                e.Cancel = true; // WebView içinde açma
                try
                {
                    await Launcher.Default.OpenAsync(new Uri(e.Url));
                }
                catch
                {
                    await DisplayAlert("Hata", "Harita uygulaması açılamadı.", "Tamam");
                }
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!_isLoaded)
            {
                await LoadMapAsync();
            }
        }

        private async Task LoadMapAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("İzin Gerekli", "Yakındaki camileri görebilmek için konum izni vermeniz gerekmektedir.", "Tamam");
                    await Navigation.PopAsync();
                    return;
                }

                LoadingLabel.Text = "Konum alınıyor...";

                var location = await Geolocation.Default.GetLocationAsync(
                    new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));

                if (location == null)
                {
                    // Son bilinen konumu dene
                    location = await Geolocation.Default.GetLastKnownLocationAsync();
                }

                if (location == null)
                {
                    await DisplayAlert("Konum Bulunamadı", "GPS sinyali alınamıyor. Lütfen GPS servisinizin aktif olduğundan emin olun.", "Tamam");
                    await Navigation.PopAsync();
                    return;
                }

                LoadingLabel.Text = "Harita yükleniyor...";

                string lat = location.Latitude.ToString(CultureInfo.InvariantCulture);
                string lng = location.Longitude.ToString(CultureInfo.InvariantCulture);

                string html = GenerateMapHtml(lat, lng);
                MapWebView.Source = new HtmlWebViewSource { Html = html };

                _isLoaded = true;

                // Harita render olunca loading'i kaldır
                await Task.Delay(1500);
                LoadingOverlay.IsVisible = false;
            }
            catch (FeatureNotSupportedException)
            {
                await DisplayAlert("Hata", "Cihazınız bu özelliği desteklemiyor.", "Tamam");
                await Navigation.PopAsync();
            }
            catch (PermissionException)
            {
                await DisplayAlert("Hata", "Konum izni alınamadı.", "Tamam");
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Bir hata oluştu: {ex.Message}", "Tamam");
                await Navigation.PopAsync();
            }
        }

        private string GenerateMapHtml(string lat, string lng)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'/>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'/>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css'/>
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <style>
        * {{ margin: 0; padding: 0; }}
        html, body {{ height: 100%; width: 100%; }}
        #map {{ height: 100%; width: 100%; }}
        .mosque-popup {{
            text-align: center;
            min-width: 180px;
        }}
        .mosque-popup h3 {{
            margin: 0 0 8px 0;
            font-size: 14px;
            color: #00796B;
        }}
        .mosque-popup .distance {{
            font-size: 12px;
            color: #757575;
            margin-bottom: 10px;
        }}
        .mosque-popup .nav-btn {{
            display: inline-block;
            background: #00796B;
            color: white !important;
            padding: 8px 16px;
            border-radius: 20px;
            text-decoration: none;
            font-size: 13px;
            font-weight: bold;
            cursor: pointer;
            border: none;
        }}
        .loading-msg {{
            position: fixed;
            bottom: 20px;
            left: 50%;
            transform: translateX(-50%);
            background: rgba(0,0,0,0.75);
            color: white;
            padding: 10px 20px;
            border-radius: 25px;
            font-size: 14px;
            z-index: 1000;
            font-family: sans-serif;
        }}
    </style>
</head>
<body>
    <div id='map'></div>
    <div id='loading' class='loading-msg'>🕌 Camiler aranıyor...</div>
    <script>
        var userLat = {lat};
        var userLng = {lng};
        
        var map = L.map('map').setView([userLat, userLng], 15);
        
        L.tileLayer('https://{{s}}.tile.openstreetmap.org/{{z}}/{{x}}/{{y}}.png', {{
            attribution: '&copy; OpenStreetMap',
            maxZoom: 19
        }}).addTo(map);

        // User location marker
        var userIcon = L.divIcon({{
            html: '<div style=""width:18px;height:18px;background:#1976D2;border:3px solid white;border-radius:50%;box-shadow:0 0 8px rgba(25,118,210,0.6);""></div>',
            iconSize: [18, 18],
            iconAnchor: [9, 9],
            className: ''
        }});
        L.marker([userLat, userLng], {{icon: userIcon}}).addTo(map)
            .bindPopup('<b>📍 Konumunuz</b>');

        // Mosque icon
        var mosqueIcon = L.divIcon({{
            html: '<div style=""font-size:28px;text-shadow:0 2px 4px rgba(0,0,0,0.3);"">🕌</div>',
            iconSize: [28, 28],
            iconAnchor: [14, 28],
            popupAnchor: [0, -28],
            className: ''
        }});

        // Calculate distance
        function getDistance(lat1, lon1, lat2, lon2) {{
            var R = 6371;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.sin(dLat/2) * Math.sin(dLat/2) +
                    Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
                    Math.sin(dLon/2) * Math.sin(dLon/2);
            var c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
            return R * c;
        }}

        // Fetch mosques from Overpass API
        var query = '[out:json][timeout:15];(' +
            'nwr[amenity=place_of_worship][religion=muslim](around:5000,' + userLat + ',' + userLng + ');' +
            ');out center body;';

        var overpassUrls = [
            'https://overpass-api.de/api/interpreter?data=',
            'https://overpass.kumi.systems/api/interpreter?data='
        ];

        function tryFetch(urlIndex) {{
            if (urlIndex >= overpassUrls.length) {{
                var loadingEl = document.getElementById('loading');
                loadingEl.textContent = 'Camiler yüklenemedi. Tekrar deneyin.';
                setTimeout(function() {{ loadingEl.style.display = 'none'; }}, 3000);
                return;
            }}

            fetch(overpassUrls[urlIndex] + encodeURIComponent(query))
                .then(function(r) {{
                    if (!r.ok) throw new Error('HTTP ' + r.status);
                    return r.json();
                }})
                .then(function(data) {{
                    var count = 0;
                    data.elements.forEach(function(el) {{
                        var elLat = el.lat || (el.center && el.center.lat);
                        var elLng = el.lon || (el.center && el.center.lon);
                        if (!elLat || !elLng) return;
                        
                        var name = (el.tags && el.tags.name) || 'Cami';
                        var dist = getDistance(userLat, userLng, elLat, elLng);
                        var distText = dist < 1 ? Math.round(dist * 1000) + ' m' : dist.toFixed(1) + ' km';
                        
                        var mapsUrl = 'https://www.google.com/maps/dir/?api=1&destination=' + elLat + ',' + elLng;
                        
                        var popup = '<div class=""mosque-popup"">' +
                            '<h3>🕌 ' + name + '</h3>' +
                            '<div class=""distance"">📏 ' + distText + ' uzaklıkta</div>' +
                            '<a class=""nav-btn"" href=""' + mapsUrl + '"">🧭 Yol Tarifi Al</a>' +
                            '</div>';
                        
                        L.marker([elLat, elLng], {{icon: mosqueIcon}}).addTo(map).bindPopup(popup);
                        count++;
                    }});
                    
                    var loadingEl = document.getElementById('loading');
                    if (count > 0) {{
                        loadingEl.textContent = count + ' cami bulundu';
                        setTimeout(function() {{ loadingEl.style.display = 'none'; }}, 2000);
                    }} else {{
                        loadingEl.textContent = 'Yakında cami bulunamadı';
                        setTimeout(function() {{ loadingEl.style.display = 'none'; }}, 3000);
                    }}
                }})
                .catch(function(err) {{
                    // Failover to next server
                    tryFetch(urlIndex + 1);
                }});
        }}

        tryFetch(0);
    </script>
</body>
</html>";
        }

        private async void OnBackTapped(object sender, TappedEventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}

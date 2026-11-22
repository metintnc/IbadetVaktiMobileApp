using System;
using Microsoft.Maui.Devices.Sensors;

namespace hadis
{
    public class Pusula
    {
        private double enlem, boylam, hedefKibleAcisi;
        const double kabeenlem = 21.4225;
        const double kabeboylam = 39.8262;
        private bool _pusulaAktifMi;

        public double Enlem => enlem;
        public double Boylam => boylam;
        public double HedefKibleAcisi => hedefKibleAcisi;
        public double MesafeKm { get; private set; }

        public event Action<double>? AciDegisti;
        public event Action? KonumBilgisiGuncellendi;
        public async Task<bool> KontrolEt()
        {
            try
            {
                var konum = await Geolocation.GetLastKnownLocationAsync();
                if(konum == null)
                {
                    konum = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));
                }
                if(konum == null)
                {
                    return false;
                }
                enlem = konum.Latitude;
                boylam = konum.Longitude;
                AciyiHesapla();
                PusulaBaslat();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public void AciyiHesapla()
        {
            double _enlemRad = enlem * (Math.PI / 180);
            double _boylamRad = boylam * (Math.PI / 180);
            double _kabeenlemRad = kabeenlem * (Math.PI / 180);
            double _kabeboylamRad = kabeboylam * (Math.PI / 180);
            double boylamfark = _kabeboylamRad - _boylamRad;
            double Y = Math.Sin(boylamfark) * Math.Cos(_kabeenlemRad);
            double X = Math.Cos(_enlemRad) * Math.Sin(_kabeenlemRad) - Math.Sin(_enlemRad) * Math.Cos(_kabeenlemRad) * Math.Cos(boylamfark);
            double aci = Math.Atan2(Y, X) * 180/Math.PI;
            hedefKibleAcisi = (aci + 360) % 360;

            // Kabe'ye olan mesafe (Haversine)
            double dLat = _kabeenlemRad - _enlemRad;
            double dLon = boylamfark;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(_enlemRad) * Math.Cos(_kabeenlemRad) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            MesafeKm = 6371.0 * c;

            KonumBilgisiGuncellendi?.Invoke();
        }
        public void PusulaBaslat()
        {
            if (!Compass.Default.IsSupported || _pusulaAktifMi)
            {
                return;
            }
            Compass.Default.ReadingChanged += PusulaVerisiGeldi;
            Compass.Default.Start(SensorSpeed.UI);
            _pusulaAktifMi = true;
        }
        public void PusulaVerisiGeldi(object? sender, CompassChangedEventArgs e)
        {
            double kuzey = e.Reading.HeadingMagneticNorth;
            double donusacisi = hedefKibleAcisi - kuzey;
            donusacisi = (donusacisi + 360) % 360;
            AciDegisti?.Invoke(donusacisi);
        }
        public void PusulaDurdur()
        {
            if (!_pusulaAktifMi)
            {
                return;
            }
            Compass.Default.ReadingChanged -= PusulaVerisiGeldi;
            Compass.Default.Stop();
            _pusulaAktifMi = false;
        }
    }
}

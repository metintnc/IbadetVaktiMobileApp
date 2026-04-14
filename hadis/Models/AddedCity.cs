namespace hadis.Models
{
    public class AddedCity
    {
        public string Sehir { get; set; } = string.Empty;
        public string Ilce { get; set; } = string.Empty;
        public string Ulke { get; set; } = "Türkiye";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}

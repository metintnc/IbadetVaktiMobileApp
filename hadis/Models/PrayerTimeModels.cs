using System.Text.Json.Serialization;

namespace hadis.Models
{
    public class CalendarResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("data")]
        public List<CalendarData> Data { get; set; }
    }

    public class CalendarData
    {
        [JsonPropertyName("timings")]
        public Timings Timings { get; set; }

        [JsonPropertyName("date")]
        public DateInfo Date { get; set; }
    }

    public class Timings
    {
        [JsonPropertyName("Fajr")]
        public string Fajr { get; set; }

        [JsonPropertyName("Sunrise")]
        public string Sunrise { get; set; }

        [JsonPropertyName("Dhuhr")]
        public string Dhuhr { get; set; }

        [JsonPropertyName("Asr")]
        public string Asr { get; set; }

        [JsonPropertyName("Maghrib")]
        public string Maghrib { get; set; }

        [JsonPropertyName("Isha")]
        public string Isha { get; set; }
    }

    public class DateInfo
    {
        [JsonPropertyName("readable")]
        public string Readable { get; set; }

        [JsonPropertyName("gregorian")]
        public GregorianDate Gregorian { get; set; }
    }

    public class GregorianDate
    {
        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("format")]
        public string Format { get; set; }

        [JsonPropertyName("day")]
        public string Day { get; set; }

        [JsonPropertyName("weekday")]
        public Weekday Weekday { get; set; }

        [JsonPropertyName("month")]
        public Month Month { get; set; }

        [JsonPropertyName("year")]
        public string Year { get; set; }
    }

    public class Weekday
    {
        [JsonPropertyName("en")]
        public string En { get; set; }
    }

    public class Month
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("en")]
        public string En { get; set; }
    }
}

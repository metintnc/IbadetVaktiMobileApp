using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace hadis.Models
{
    public class AlQuranAyah
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

    public class AlQuranData
    {
        [JsonPropertyName("ayahs")]
        public List<AlQuranAyah> Ayahs { get; set; }
    }

    public class AlQuranResponse
    {
        [JsonPropertyName("data")]
        public AlQuranData Data { get; set; }
    }
}

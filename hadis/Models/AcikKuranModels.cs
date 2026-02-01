using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace hadis.Models
{
    public class AcikKuranResponse
    {
        [JsonPropertyName("data")]
        public AcikKuranData Data { get; set; }
    }

    public class AcikKuranData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("verses")]
        public List<AcikKuranVerse> Verses { get; set; }
    }

    public class AcikKuranVerse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("verse_number")]
        public int VerseNumber { get; set; }

        [JsonPropertyName("verse")]
        public string Verse { get; set; } // Arabic Text

        [JsonPropertyName("transcription")]
        public string Transcription { get; set; }

        [JsonPropertyName("translation")]
        public AcikKuranTranslation Translation { get; set; }
    }

    public class AcikKuranTranslation
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}

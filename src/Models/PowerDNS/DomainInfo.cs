using System.Text.Json.Serialization;

namespace PowerRqlite.Models.PowerDNS
{
    public class DomainInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; } = -1;

        [JsonPropertyName("zone")]
        public required string Zone { get; set; }

        [JsonPropertyName("serial")]
        public string? Serial { get; set; }
        
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = "NATIVE";

        [JsonPropertyName("notified_serial")]
        public int? NotifiedSerial { get; set; } = -1;

        [JsonPropertyName("last_check")]
        public int? LastCheck { get; set; } = 0;

        [JsonPropertyName("masters")]
        public string[]? Masters { get; set; }

    }
}

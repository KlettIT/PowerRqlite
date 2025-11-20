using System.Text.Json.Serialization;

namespace PowerRqlite.Interfaces.PowerDNS
{
    public interface IRecord
    {
        [JsonPropertyName("qtype")]
        public string QType { get; set; }
        [JsonPropertyName("qname")]
        public string QName { get; set; }
        [JsonPropertyName("content")]
        public string Content { get; set; }
        [JsonPropertyName("ttl")]
        public int TTL { get; set; }
        [JsonPropertyName("domain_id")]
        public int DomainId { get; set; }
        [JsonPropertyName("auth")]
        public int Auth { get; set; }
        [JsonPropertyName("disabled")]
        public bool Disabled { get; set; }
    }
}

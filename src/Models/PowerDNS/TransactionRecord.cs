using PowerRqlite.Enums.PowerDNS;
using PowerRqlite.Interfaces.PowerDNS;
using System.Text.Json.Serialization;

namespace PowerRqlite.Models.PowerDNS
{
    public class TransactionRecord : IRecord
    { 
        public TransactionMode TransactionMode { get; set; }
        [JsonPropertyName("qtype")]
        public required string QType { get; set; }

        [JsonPropertyName("qname")]
        public required string QName { get; set; }

        [JsonPropertyName("content")]
        public required string Content { get; set; }

        [JsonPropertyName("ttl")]
        public required int TTL { get; set; }

        [JsonPropertyName("domain_id")]
        public required int DomainId { get; set; } = -1;

        [JsonPropertyName("auth")]
        public int Auth { get; set; } = 1;

        [JsonPropertyName("disabled")]
        public bool Disabled { get; set; } = false;
    }
}

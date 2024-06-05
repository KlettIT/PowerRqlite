using System.Text.Json.Serialization;
using PowerRqlite.Interfaces.PowerDNS;

namespace PowerRqlite.Models.PowerDNS.Responses
{
    public class BoolResponse : IResponse
    {
        [JsonPropertyName("result")]
        public bool Result { get; set; }
        public List<string>? Log { get; set; }
    }
}

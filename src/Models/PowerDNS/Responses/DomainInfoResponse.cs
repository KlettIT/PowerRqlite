using PowerRqlite.Exceptions;
using PowerRqlite.Interfaces.PowerDNS;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerRqlite.Models.PowerDNS.Responses
{
    public class DomainInfoResponse : IResponse
    {
        [JsonPropertyName("result")]
        public DomainInfo? Result { get; set; }
        public List<string>? Log { get; set; }

        public static DomainInfoResponse FromValues(List<JsonElement>? value)
        {
            if (value != null)
            {
                DomainInfo domainInfo = new()
                {
                    Id = value[0].ValueKind == JsonValueKind.Number ? value[0].GetInt32() : -1,
                    Zone = value[1].GetString() ?? string.Empty,
                    Masters = value[2].ValueKind == JsonValueKind.Array ? value[2].EnumerateArray().Select(x => x.GetString()!).ToArray() : null,
                    LastCheck = value[3].ValueKind == JsonValueKind.Number ? value[3].GetInt32() : 0,
                    Kind = value[4].GetString() ?? value[4].ToString(), // raw SQLite value, not JSON-encoded
                    NotifiedSerial = value[5].ValueKind == JsonValueKind.Number ? value[5].GetInt32() : 0,
                };

                // Optional 8th column: SOA content => serial number (ns hostmaster SERIAL refresh ...)
                if (value.Count >= 8)
                {
                    string[] soa = value[7].GetString()?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
                    if (soa.Length >= 3 && int.TryParse(soa[2], out int serial))
                    {
                        domainInfo.Serial = serial;
                    }
                }

                return new DomainInfoResponse() { Result = domainInfo };

            }
            else
            {
                throw new NoValuesException();
            }
        }

        public static DomainInfoResponse FromValues(List<List<JsonElement>>? Values)
        {
            if (Values != null)
            {
                List<JsonElement> value = Values[0];

                return FromValues(value);
            }
            else
            {
                throw new NoValuesException();
            }
        }
    }
}

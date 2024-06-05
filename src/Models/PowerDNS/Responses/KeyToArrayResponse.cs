using PowerRqlite.Exceptions;
using PowerRqlite.Interfaces.PowerDNS;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerRqlite.Models.PowerDNS.Responses
{
    public class KeyToArrayResponse : IResponse
    {
        [JsonPropertyName("result")]
        public required Dictionary<string, List<string>> Result { get; set; }
        public List<string>? Log { get; set; }

        public static KeyToArrayResponse FromValues(List<List<JsonElement>>? Values)
        {
            if (Values != null)
            {

                Dictionary<string, List<string>> valuePairs = Values
                    .GroupBy(v => v[0].GetString()!)
                    .Select(group => new { Key = group.Key, Values = group.Select(v => v[1].GetString()!).ToList() })
                    .ToDictionary(g => g.Key, g => g.Values);

                return new KeyToArrayResponse() { Result = valuePairs };

            }
            else
            {
                throw new NoValuesException();
            }
        }
    }
}

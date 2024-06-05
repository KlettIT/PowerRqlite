using PowerRqlite.Exceptions;
using PowerRqlite.Interfaces.PowerDNS;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerRqlite.Models.PowerDNS.Responses
{
    public class StringArrayResponse : IResponse
    {
        [JsonPropertyName("result")]
        public required List<string> Result { get; set; }
        public List<string>? Log { get; set; }

        public static StringArrayResponse FromValues(List<List<JsonElement>>? Values)
        {
            if (Values != null)
            {
                return new StringArrayResponse() { Result = Values.SelectMany(x => x.Where(w => w.ValueKind == JsonValueKind.String).Select(y => y.GetString())).Cast<string>().ToList() };

            }
            else
            {
                throw new NoValuesException();
            }
        }
    }
}

using PowerRqlite.Exceptions;
using PowerRqlite.Interfaces.PowerDNS;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerRqlite.Models.PowerDNS.Responses
{
    public class ListResponse : IResponse
    {
        [JsonPropertyName("result")]
        public List<IRecord> Result { get; set; } = [];
        public List<string>? Log { get; set; }
        public static ListResponse FromValues (List<List<JsonElement>>? Values)
        {

            List<IRecord> records = [];

            if (Values != null)
            {
                records.AddRange(Values.AsParallel().Select(value =>
                {
                    return new Record()
                    {
                        DomainId = value[0].ValueKind == JsonValueKind.Number ? value[0].GetInt32() : -1,
                        QName = value[1].ValueKind == JsonValueKind.String ? value[1].GetString()! : string.Empty,
                        QType = value[2].ValueKind == JsonValueKind.String ? value[2].GetString()! : string.Empty,
                        Content = value[3].ValueKind == JsonValueKind.String ? value[3].GetString()! : string.Empty,
                        TTL = value[4].ValueKind == JsonValueKind.Number ? value[4].GetInt32() : 0,
                        Disabled = value[5].ValueKind == JsonValueKind.Number && Convert.ToBoolean(value[5].GetInt32()),
                        Auth = value[6].ValueKind == JsonValueKind.Number ? value[6].GetInt32() : 0,
                    };
                }).ToList());

                return new ListResponse() { Result = records };
            }
            else
            {
                throw new NoValuesException();
            } 
        }
    }
}

using PowerRqlite.Exceptions;
using PowerRqlite.Interfaces.PowerDNS;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerRqlite.Models.PowerDNS.Responses
{
    public class GetUpdatedMastersResponse : IResponse
    {
        [JsonPropertyName("result")]
        public required List<DomainInfo> Result { get; set; }
        public List<string>? Log { get; set; }

        public static GetUpdatedMastersResponse FromValues(List<List<JsonElement>>? Values)
        {
            if (Values != null)
            {

                var domainInfos = Values.AsParallel().AsOrdered().Select(DomainInfoResponse.FromValues)
                    .Where(x => x != null && x.Result != null)
                    .Select(y => y!.Result)
                    .Cast<DomainInfo>().ToList();

                return new GetUpdatedMastersResponse() { Result = domainInfos };

            }
            else
            {
                throw new NoValuesException();
            }
        }
    }
}

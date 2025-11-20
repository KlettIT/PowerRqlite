using PowerRqlite.Exceptions;
using PowerRqlite.Interfaces.PowerDNS;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerRqlite.Models.PowerDNS.Responses
{
    public class AllDomainsResponse : IResponse
    {
        public List<string>? Log { get; set; }

        [JsonPropertyName("result")]
        public List<DomainInfo>? Result { get; set; }

        public static AllDomainsResponse FromValues(List<List<JsonElement>>? Values)
        {
            if (Values != null)
            {
                List<DomainInfo> domains = Values.AsParallel().AsOrdered().Select(DomainInfoResponse.FromValues)
                    .Where(x => x.Result != null)
                    .Select(y => y!.Result)
                    .Cast<DomainInfo>().ToList();

                return new AllDomainsResponse() { Result = domains };

            }
            else
            {
                throw new NoValuesException();
            }
        }
    }
}

using PowerRqlite.Exceptions;
using PowerRqlite.Models.rqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerRqlite.Models.PowerDNS.Responses
{
    public class GetUpdatedMastersResponse : IResponse
    {
        private bool isDisposed;
        public List<DomainInfo> result { get; set; }
        public List<string> Log { get; set; }

        public static GetUpdatedMastersResponse FromValues(IList<IList<Object>> Values)
        {
            if (Values != null)
            {

                List<DomainInfo> domainInfos = new List<DomainInfo>();

                foreach (var value in Values)
                {
                    DomainInfo domainInfo = new DomainInfo
                    {
                        id = int.Parse(value[0].ToString()),
                        zone = value[1].ToString(),
                        masters = string.IsNullOrEmpty(value[2].ToString()) ? Array.Empty<string>() : new string[] { value[2].ToString() },
                        last_check = value[3] != null ? int.Parse(value[3].ToString()) : 0,
                        kind = value[4].ToString(),
                        notified_serial = value[5] != null ? int.Parse(value[5].ToString()) : 0
                    };

                    domainInfos.Add(domainInfo);
                }

                return new GetUpdatedMastersResponse() { result = domainInfos };

            }
            else
            {
                throw new NoValuesException();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                // free managed resources
            }

            isDisposed = true;
        }
    }
}

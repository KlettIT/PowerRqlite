using PowerRqlite.Exceptions;
using PowerRqlite.Models.rqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerRqlite.Models.PowerDNS.Responses
{
    public class DomainInfoResponse : IResponse
    {
        private bool isDisposed;
        public DomainInfo result { get; set; }
        public List<string> Log { get; set; }

        public static DomainInfoResponse FromValues(IList<IList<Object>> Values)
        {
            if (Values != null)
            {
                var value = Values.First();

                DomainInfo domainInfo = new DomainInfo
                {
                    id = int.Parse(value[0].ToString()),
                    zone = value[1].ToString(),
                    masters = string.IsNullOrEmpty(value[2].ToString()) ? Array.Empty<string>() : new string[] { value[2].ToString() },
                    last_check = value[3] != null ? int.Parse(value[3].ToString()) : 0,
                    kind = value[4].ToString(),
                    notified_serial = value[5] != null ? int.Parse(value[5].ToString()) : 0
                };

                return new DomainInfoResponse() { result = domainInfo };

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

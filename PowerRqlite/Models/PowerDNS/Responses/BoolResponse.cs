using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerRqlite.Models.PowerDNS.Responses
{
    public class BoolResponse : IResponse
    {
        public bool result { get; set; }
        public List<string> Log { get; set; }

        public void Dispose()
        {
           // nothing to do
        }
    }
}

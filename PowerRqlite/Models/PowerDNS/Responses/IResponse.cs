using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerRqlite.Models.PowerDNS.Responses
{
    public interface IResponse : IDisposable
    {
       public List<string> Log { get; set; }
    }
}

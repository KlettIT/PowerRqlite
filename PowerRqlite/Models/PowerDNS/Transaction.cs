using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerRqlite.Models.PowerDNS
{
    public class Transaction
    {
        public int id { get; set; }
        public int domain_id { get; set; }
        public string domain { get; set; }
    }
}

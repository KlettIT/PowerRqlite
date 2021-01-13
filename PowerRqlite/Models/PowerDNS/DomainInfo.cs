using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerRqlite.Models.PowerDNS
{
    public class DomainInfo
    {
        public int id { get; set; }

        public string zone { get; set; }

        public string kind { get; set; }

        public int? notified_serial { get; set; } = -1;

        public int? last_check { get; set; } = 0;

        public string[] masters { get; set; }

    }
}

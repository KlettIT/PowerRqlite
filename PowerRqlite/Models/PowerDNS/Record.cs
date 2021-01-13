using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerRqlite.Models.PowerDNS
{
    public class Record
    {
        public string qtype { get; set; }
        public string qname { get; set; }
        public string content { get; set; }
        public int ttl { get; set; }
        public int domain_id { get; set; } = -1;
        public bool auth { get; set; } = true;
        public bool disabled { get; set; } = false;

    }
}

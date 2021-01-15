using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerRqlite.Models.PowerDNS
{
    public interface IRecord : IFormattable
    {
        public string qtype { get; set; }
        public string qname { get; set; }
        public string content { get; set; }
        public int ttl { get; set; }
        public int domain_id { get; set; }
        public bool auth { get; set; }
        public bool disabled { get; set; }
    }
}

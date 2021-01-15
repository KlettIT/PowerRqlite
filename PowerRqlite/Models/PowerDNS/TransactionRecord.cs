using Newtonsoft.Json;
using PowerRqlite.Models.PowerDNS.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerRqlite.Models.PowerDNS
{
    public class TransactionRecord : IRecord
    { 
        public TransactionMode TransactionMode { get; set; }
        public string qtype { get; set; }
        public string qname { get; set; }
        public string content { get; set; }
        public int ttl { get; set; }
        public int domain_id { get; set; } = -1;
        public bool auth { get; set; } = true;
        public bool disabled { get; set; } = false;
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

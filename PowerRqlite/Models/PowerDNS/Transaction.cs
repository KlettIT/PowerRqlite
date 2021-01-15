using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerRqlite.Models.PowerDNS
{
    public class Transaction
    {
        public Transaction()
        {
            started = DateTime.Now;
            Queries = new List<string>();
            Records = new List<TransactionRecord>();
        }

        public DateTime started { get; private set; }
        public int id { get; set; }
        public int domain_id { get; set; }
        public string domain { get; set; }
        public List<string> Queries { get; set; }
        public List<TransactionRecord> Records {get; set;}
    }
}

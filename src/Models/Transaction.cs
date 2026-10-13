using PowerRqlite.Models.PowerDNS;
using PowerRqlite.Models.rqlite;

namespace PowerRqlite.Models
{
    public class Transaction
    {
        public Transaction()
        {
            Started = DateTime.Now;
            Queries = [];
            Records = [];
        }
        public DateTime Started { get; private set; }
        public int Id { get; set; }
        public int DomainId { get; set; }
        public required string Domain { get; set; }
        public List<QueryEntry> Queries { get; set; }
        public List<TransactionRecord> Records { get; set; }
    }
}

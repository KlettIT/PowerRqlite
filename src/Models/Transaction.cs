using PowerRqlite.Models.PowerDNS;

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
        public List<string> Queries { get; set; }
        public List<TransactionRecord> Records { get; set; }
    }
}

using PowerRqlite.Enums.PowerDNS;
using PowerRqlite.Interfaces.PowerDNS;
using PowerRqlite.Models;
using PowerRqlite.Models.rqlite;

namespace PowerRqlite.Interfaces
{
    public interface ITransactionManager
    {
        public bool StartTransaction(int id,int domain_id,string domain);
        public bool AddTransaction(int id, QueryEntry entry, int domain_id, string qname, string qtype, TransactionMode transactionMode);
        public bool AddTransaction(int id, QueryEntry entry, IRecord record, TransactionMode transactionMode);
        public bool RemoveTransaction(int id);
        public Transaction GetTransaction(int id);
        public Transaction? GetLastTransaction(string domain);
        public Transaction? GetLastTransaction(int domain_id);
        public List<QueryEntry> GetTransactionQuerys(int id);
        public List<QueryEntry> GetTransactionQuerys(string domain);
        public List<Transaction> Transactions();
        public bool TransactionExistsWith(int domain_id);
        public bool TransactionExistsWith(string qname, string qtype);
        public IRecord? GetLastTransactionRecord(string qname, string qtype);
    }
}

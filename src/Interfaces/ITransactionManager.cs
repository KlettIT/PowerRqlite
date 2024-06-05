using PowerRqlite.Enums.PowerDNS;
using PowerRqlite.Interfaces.PowerDNS;
using PowerRqlite.Models;

namespace PowerRqlite.Interfaces
{
    public interface ITransactionManager
    {
        public bool StartTransaction(int id,int domain_id,string domain);
        public bool AddTransaction(int id, string query, int domain_id, string qname, string qtype, TransactionMode transactionMode);
        public bool AddTransaction(int id, string query, IRecord record, TransactionMode transactionMode);
        public bool RemoveTransaction(int id);
        public Transaction GetTransaction(int id);
        public Transaction? GetLastTransaction(string domain);
        public Transaction? GetLastTransaction(int domain_id);
        public List<string> GetTransactionQuerys(int id);
        public List<string> GetTransactionQuerys(string domain);
        public List<Transaction> Transactions();
        public bool TransactionExistsWith(int domain_id);
        public bool TransactionExistsWith(string qname, string qtype);
        public IRecord? GetLastTransactionRecord(string qname, string qtype);
    }
}

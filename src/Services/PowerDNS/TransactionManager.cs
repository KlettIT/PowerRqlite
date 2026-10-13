using PowerRqlite.Enums.PowerDNS;
using PowerRqlite.Interfaces;
using PowerRqlite.Interfaces.PowerDNS;
using PowerRqlite.Models;
using PowerRqlite.Models.PowerDNS;
using PowerRqlite.Models.rqlite;

namespace PowerRqlite.Services.PowerDNS
{
    public class TransactionManager : ITransactionManager
    {

        private readonly List<Transaction> transactions;
        private readonly object _lock;
        public TransactionManager()
        {
            transactions = [];
            _lock = new object();
        }

        public bool StartTransaction(int id, int domain_id, string domain)
        {
            lock (_lock)
            {
                Transaction transaction = new Transaction() { Id = id, DomainId = domain_id, Domain = domain };

                if (!transactions.Exists(x => x.Id == id))
                {
                    transactions.Add(transaction);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool AddTransaction(int id, QueryEntry entry, int domain_id, string qname, string qtype, TransactionMode transactionMode)
        {

            lock (_lock)
            {

                Transaction? transaction = transactions.FirstOrDefault(x => x.Id == id);

                if (transaction != null)
                {
                    transaction.Queries.Add(entry);
                    transaction.Records.Add(new TransactionRecord() { DomainId = domain_id, QName = qname, QType = qtype, TransactionMode = transactionMode, Content = string.Empty, TTL = -1 });
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool AddTransaction(int id, QueryEntry entry, IRecord record, TransactionMode transactionMode)
        {

            lock (_lock)
            {

                Transaction? transaction = transactions.FirstOrDefault(x => x.Id == id);

                if (transaction != null)
                {
                    transaction.Queries.Add(entry);
                    transaction.Records.Add(new TransactionRecord() { Auth = record.Auth, Content = record.Content, Disabled = record.Disabled, DomainId = record.DomainId, QName = record.QName, QType = record.QType, TTL = record.TTL, TransactionMode = transactionMode });

                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public Transaction GetTransaction(int id)
        {

            lock (_lock)
            {
                Transaction? transaction = transactions.FirstOrDefault(x => x.Id == id);

                return transaction ?? throw new InvalidOperationException("Transaction does not exist!");
            }
        }

        public Transaction? GetLastTransaction(string domain)
        {

            lock (_lock)
            {
                return transactions.LastOrDefault(x => x.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase));
            }
        }

        public Transaction? GetLastTransaction(int domain_id)
        {
            lock (_lock)
            {
                return transactions.LastOrDefault(x => x.DomainId == domain_id);
            }
        }

        public bool RemoveTransaction(int id)
        {

            lock (_lock)
            {
                Transaction? transaction = transactions.FirstOrDefault(x => x.Id == id);

                if (transaction != null)
                {
                    transactions.Remove(transaction);
                    return true;
                }
                else
                {
                    throw new InvalidOperationException("Transaction does not exist!");
                }
            }
        }

        public List<QueryEntry> GetTransactionQuerys(int id)
        {
            lock (_lock)
            {
                Transaction? transaction = transactions.FirstOrDefault(x => x.Id == id);

                return transaction != null ? transaction.Queries : throw new InvalidOperationException("Transaction does not exist!");
            }
        }

        public List<QueryEntry> GetTransactionQuerys(string domain)
        {
            lock (_lock)
            {
                Transaction? transaction = transactions.LastOrDefault(x => x.Domain.Equals(domain, StringComparison.OrdinalIgnoreCase));

                return transaction != null ? transaction.Queries : throw new InvalidOperationException("Transaction does not exist!");
            }
        }

        public IRecord? GetLastTransactionRecord(string qname, string qtype)
        {
            lock (_lock)
            {
                // For "ANY" qtype, match any record with the given qname (ignore qtype in the existence check).
                bool hasMatchingRecord(Transaction t) => t.Records.Exists(y =>
                    y.QName.Equals(qname, StringComparison.OrdinalIgnoreCase) &&
                    (qtype == "ANY" || y.QType == qtype));

                Transaction? transaction = transactions.LastOrDefault(hasMatchingRecord);

                if (transaction != null)
                {
                    TransactionRecord? record = qtype == "ANY"
                        ? transaction.Records.LastOrDefault(x => x.QName.Equals(qname, StringComparison.OrdinalIgnoreCase))
                        : transaction.Records.LastOrDefault(x => x.QName.Equals(qname, StringComparison.OrdinalIgnoreCase) && x.QType == qtype);

                    if (record != null && record.TransactionMode == TransactionMode.INSERT)
                    {
                        return record;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
        }
        public bool TransactionExistsWith(string qname, string qtype)
        {
            lock (_lock)
            {
                return transactions.Exists(x => x.Records.Exists(y => y.QName.Equals(qname, StringComparison.OrdinalIgnoreCase) && y.QType == qtype));
            }
        }

        public bool TransactionExistsWith(int domain_id)
        {
            lock (_lock)
            {
                return transactions.Exists(x => x.DomainId == domain_id);
            }
        }

        public List<Transaction> Transactions()
        {
            lock (_lock)
            {
                return transactions;
            }
        }
    }
}

using PowerRqlite.Models.PowerDNS;
using PowerRqlite.Models.PowerDNS.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PowerRqlite.Services.PowerDNS
{
    public class TransactionManager : ITransactionManager
    {

        private List<Transaction> transactions;
        private object _lock;
        public TransactionManager()
        {
            transactions = new List<Transaction>();
            _lock = new object();
        }

        public bool StartTransaction(int id, int domain_id, string domain)
        {
            lock (_lock)
            {
                var transaction = new Transaction() { id = id, domain_id = domain_id, domain = domain };

                if (!transactions.Any(x => x.id == id))
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

        public bool AddTransaction(int id, string query, int domain_id, string qname, string qtype, TransactionMode transactionMode)
        {

            lock (_lock)
            {

                Transaction transaction = transactions.First(x => x.id == id);

                if (transaction != null)
                {
                    transaction.Queries.Add(query);
                    transaction.Records.Add(new TransactionRecord() { domain_id = domain_id, qname = qname, qtype = qtype, TransactionMode = transactionMode });
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool AddTransaction(int id, string query, IRecord record, TransactionMode transactionMode)
        {

            lock (_lock)
            {

                Transaction transaction = transactions.First(x => x.id == id);

                if (transaction != null)
                {
                    transaction.Queries.Add(query);
                    transaction.Records.Add(new TransactionRecord() { auth = record.auth, content = record.content, disabled = record.disabled, domain_id = record.domain_id, qname = record.qname, qtype = record.qtype, ttl = record.ttl, TransactionMode = transactionMode });

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
                Transaction transaction = transactions.First(x => x.id == id);

                if (transaction != null)
                {
                    return transaction;
                }
                else
                {
                    throw new Exception("Transaction does not exist!");
                }
            }
        }

        public Transaction GetLastTransaction(string domain)
        {

            lock (_lock)
            {
                Transaction transaction = transactions.LastOrDefault(x => x.domain.ToLower() == domain.ToLower());

                if (transaction != null)
                {
                    return transaction;
                }
                else
                {
                    throw new Exception("Transaction does not exist!");
                }
            }
        }

        public Transaction GetLastTransaction(int domain_id)
        {
            lock (_lock)
            {
                Transaction transaction = transactions.LastOrDefault(x => x.domain_id == domain_id);

                if (transaction != null)
                {
                    return transaction;
                }
                else
                {
                    throw new Exception("Transaction does not exist!");
                }
            }
        }

        public bool RemoveTransaction(int id)
        {

            lock (_lock)
            {
                Transaction transaction = transactions.First(x => x.id == id);

                if (transaction != null)
                {
                    transactions.Remove(transaction);
                    return true;
                }
                else
                {
                    throw new Exception("Transaction does not exist!");
                }
            }
        }

        public List<string> GetTransactionQuerys(int id)
        {
            lock (_lock)
            {
                Transaction transaction = transactions.First(x => x.id == id);

                if (transaction != null)
                {
                    return transaction.Queries;
                }
                else
                {
                    throw new Exception("Transaction does not exist!");
                }
            }
        }

        public List<string> GetTransactionQuerys(string domain)
        {
            lock (_lock)
            {
                Transaction transaction = transactions.First(x => x.domain.ToLower() == domain.ToLower());

                if (transaction != null)
                {
                    return transaction.Queries;
                }
                else
                {
                    throw new Exception("Transaction does not exist!");
                }
            }
        }

        public IRecord GetLastTransactionRecord(string qname, string qtype)
        {
            lock (_lock)
            {
                var transaction = transactions.Where(x => x.Records.Any(y => y.qname.ToLower() == qname.ToLower() & y.qtype == qtype)).LastOrDefault();

                if (transaction != null)
                {
                    TransactionRecord record = qtype == "ANY" ? transaction.Records.LastOrDefault(x => x.qname.ToLower() == qname.ToLower()) : transaction.Records.LastOrDefault(x => x.qname.ToLower() == qname.ToLower() & x.qtype == qtype);

                    if (record.TransactionMode == TransactionMode.INSERT)
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
            return transactions.Any(x => x.Records.Any(y => y.qname.ToLower() == qname.ToLower() & y.qtype == qtype));
        }

        public bool TransactionExistsWith(int domain_id)
        {
            return transactions.Any(x => x.domain_id == domain_id);
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
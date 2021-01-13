using PowerRqlite.Models.PowerDNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PowerRqlite.Services.PowerDNS
{
    public class TransactionManager : ITransactionManager
    {

        private Dictionary<Transaction, List<string>> transactions;
        private object _lock;
        public TransactionManager()
        {
            transactions = new Dictionary<Transaction, List<string>>();
            _lock = new object();
        }

        public bool StartTransaction(int id, int domain_id, string domain)
        {
            lock (_lock)
            {
                var transaction = new Transaction() { id = id, domain_id = domain_id, domain = domain };

                if (transactions.ContainsKey(transaction) == false)
                {
                    transactions.Add(transaction, new List<string>());
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool AddTransaction(int id, string query)
        {

            lock (_lock)
            {

                Transaction transaction = transactions.First(x => x.Key.id == id).Key;

                if (transaction != null && transactions.ContainsKey(transaction) == true)
                {
                    transactions[transaction].Add(query);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool AddTransaction(string domain, string query)
        {

            lock (_lock)
            {

                Transaction transaction = transactions.First(x => x.Key.domain == domain).Key;

                if (transaction != null && transactions.ContainsKey(transaction) == true)
                {
                    transactions[transaction].Add(query);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public List<string> GetTransaction(int id)
        {

            lock (_lock)
            {
                Transaction transaction = transactions.First(x => x.Key.id == id).Key;

                if (transaction != null && transactions.ContainsKey(transaction))
                {
                    return transactions[transaction];
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
                Transaction transaction = transactions.First(x => x.Key.id == id).Key;

                if (transaction != null &&  transactions.ContainsKey(transaction))
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
                Transaction transaction = transactions.First(x => x.Key.id == id).Key;

                if (transaction != null && transactions.ContainsKey(transaction))
                {
                    return transactions[transaction];
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
                Transaction transaction = transactions.First(x => x.Key.domain == domain).Key;

                if (transaction != null && transactions.ContainsKey(transaction))
                {
                    return transactions[transaction];
                }
                else
                {
                    throw new Exception("Transaction does not exist!");
                }
            }
        }

        public Dictionary<Transaction, List<string>> Transactions()
        {
            lock (_lock)
            {
                return transactions;
            }
        }
    }
}

using PowerRqlite.Models.PowerDNS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerRqlite.Services.PowerDNS
{
    public interface ITransactionManager
    {
        public bool StartTransaction(int id,int domain_id,string domain);
        public bool AddTransaction(int id, string query);
        public bool AddTransaction(string domian, string query);
        public bool RemoveTransaction(int id);
        public List<string> GetTransactionQuerys(int id);
        public List<string> GetTransactionQuerys(string domain);

        public Dictionary<Transaction, List<string>> Transactions();
    }
}

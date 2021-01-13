using PowerRqlite.Models.rqlite;
using PowerRqlite.Models.rqlite.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerRqlite.Services.rqlite
{
    public interface IrqliteService
    {
        public Task<QueryResult> QueryAsync(string query,ReadConsistencyLevel readConsistencyLevel = ReadConsistencyLevel.Weak, string overwriteUrl = "");
        public Task<ExecuteResult> ExecuteAsync(string query, string overwriteUrl = null);
        public Task<ExecuteResult> ExecuteBulkAsync(List<string> querys, string overwriteUrl = null);
    }
}

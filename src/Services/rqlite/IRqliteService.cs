using PowerRqlite.Enums.rqlite;
using PowerRqlite.Models.rqlite;

namespace PowerRqlite.Services.rqlite
{
    public interface IRqliteService
    {
        public Task<QueryResult> QueryAsync(string query,ReadConsistencyLevel readConsistencyLevel = ReadConsistencyLevel.Weak);
        public Task<ExecuteResult> ExecuteAsync(string query);
        public Task<ExecuteResult> ExecuteBulkAsync(List<string> querys);
    }
}

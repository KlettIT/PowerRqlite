using PowerRqlite.Enums.rqlite;
using PowerRqlite.Models.rqlite;

namespace PowerRqlite.Services.rqlite
{
    public interface IRqliteService
    {
        public Task<QueryResult> QueryAsync(string query, object?[]? args = null, ReadConsistencyLevel readConsistencyLevel = ReadConsistencyLevel.Weak);
        public Task<ExecuteResult> ExecuteAsync(string query, object?[]? args = null);
        public Task<ExecuteResult> ExecuteBulkAsync(List<string> queries, object?[][]? args = null);
    }
}

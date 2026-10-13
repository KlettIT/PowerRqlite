using Flurl.Http;
using PowerRqlite.Enums.rqlite;
using PowerRqlite.Interfaces.rqlite;
using PowerRqlite.Models.rqlite;
using Serilog;

namespace PowerRqlite.Services.rqlite
{
    public class RqliteService : IRqliteService
    {
        private readonly IFlurlClient _client;

        public RqliteService(IRqliteContext rqliteContext)
        {
            // Own client instead of the global Flurl cache, which throws when a second service registers the same URL.
            _client = new FlurlClient(rqliteContext.RqliteUrl)
                .WithAutoRedirect(true)
                .AfterCall(call =>
                {
                    Log.Debug("[rqlite] [{Method}] [{StatusCode}] {Url}", call.HttpRequestMessage.Method.ToString(), call.Response?.StatusCode, call.Request.Url.ToString());
                });
        }

        // rqlite parameterized statement format: [["SQL ?", arg1, arg2], ...]. Values are bound by SQLite, never concatenated.
        private static object?[] Statement(string sql, object?[]? args) => [sql, .. args ?? []];

        public async Task<QueryResult> QueryAsync(string query, object?[]? args = null, ReadConsistencyLevel readConsistencyLevel = ReadConsistencyLevel.Weak)
        {
            QueryResult result = await _client.Request("db", "query")
                .AppendQueryParam("level", readConsistencyLevel)
                .PostJsonAsync(new[] { Statement(query, args) })
                .ReceiveJson<QueryResult>();

            ThrowOnError(result.Results);
            return result;
        }

        public async Task<ExecuteResult> ExecuteAsync(string query, object?[]? args = null)
        {
            ExecuteResult result = await _client.Request("db", "execute")
                .PostJsonAsync(new[] { Statement(query, args) })
                .ReceiveJson<ExecuteResult>();

            ThrowOnError(result.Results);
            return result;
        }

        public async Task<ExecuteResult> ExecuteBulkAsync(List<string> queries, object?[][]? args = null)
        {
            ExecuteResult result = await _client.Request("db", "execute")
                .AppendQueryParam("transaction")
                .PostJsonAsync(queries.Select((sql, i) => Statement(sql, args?[i])).ToArray())
                .ReceiveJson<ExecuteResult>();

            ThrowOnError(result.Results);
            return result;
        }

        // rqlite answers SQL errors with HTTP 200 and an "error" per statement; never treat them as "no rows".
        private static void ThrowOnError(List<Result>? results)
        {
            string? error = results?.FirstOrDefault(r => r.Error is not null)?.Error;

            if (error is not null)
            {
                throw new InvalidOperationException($"rqlite: {error}");
            }
        }
    }
}

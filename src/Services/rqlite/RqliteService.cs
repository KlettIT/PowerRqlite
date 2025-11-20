using Flurl;
using Flurl.Http;
using PowerRqlite.Enums.rqlite;
using PowerRqlite.Interfaces.rqlite;
using PowerRqlite.Models.rqlite;
using Serilog;

namespace PowerRqlite.Services.rqlite
{
    public class RqliteService : IRqliteService
    {
        private readonly string baseUrl;

        public RqliteService(IRqliteContext rqliteContext)
        {
            baseUrl = rqliteContext.RqliteUrl;

            FlurlHttp.ConfigureClientForUrl(baseUrl)
                .WithAutoRedirect(true)
                .AfterCall(call =>
                {
                    Log.Debug("[rqlite] [{Method}] [{StatusCode}] {Url}", call.HttpRequestMessage.Method.ToString(), call.Response.StatusCode, call.Request.Url.ToString());
                });
        }

        public async Task<QueryResult> QueryAsync(string query, ReadConsistencyLevel readConsistencyLevel = ReadConsistencyLevel.Weak)
        {
            IFlurlResponse? result = await baseUrl.AppendPathSegments("db", "query")
                .AppendQueryParam("level", readConsistencyLevel)
                .PostJsonAsync(new string[] { query });

            return await result.GetJsonAsync<QueryResult>();
        }

        public async Task<ExecuteResult> ExecuteAsync(string query)
        {

            IFlurlResponse? result = await baseUrl.AppendPathSegments("db", "execute")
                .PostJsonAsync(new string[] { query });

            return await result.GetJsonAsync<ExecuteResult>();
        }

        public async Task<ExecuteResult> ExecuteBulkAsync(List<string> querys)
        {

            IFlurlResponse? result = await baseUrl.AppendPathSegments("db", "execute")
                .AppendQueryParam("transaction")
                .PostJsonAsync(querys.ToArray());

            return await result.GetJsonAsync<ExecuteResult>();
        }
    }
}

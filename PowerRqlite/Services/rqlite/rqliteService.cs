using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PowerRqlite.Models.rqlite;
using PowerRqlite.Models.rqlite.enums;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PowerRqlite.Services.rqlite
{
    public class rqliteService : IrqliteService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<rqliteService> _logger;


        public rqliteService(IrqliteContext rqliteContext, HttpClient httpClient, ILogger<rqliteService> logger, IConfiguration configRoot)
        {
            _httpClient = httpClient;
            _logger = logger;

            _httpClient.BaseAddress = new Uri(rqliteContext.rqliteUrl);
        }

        public async Task<QueryResult> QueryAsync(string query, ReadConsistencyLevel readConsistencyLevel = ReadConsistencyLevel.Weak, string overwriteUrl = null)
        {

            if (_httpClient != null)
            {

                string[] jsonArray = new string[] { query };

                var content = new StringContent(JsonConvert.SerializeObject(jsonArray), Encoding.UTF8, "application/json");
                var result = await _httpClient.PostAsync(string.IsNullOrWhiteSpace(overwriteUrl) ?  $"db/query?level={readConsistencyLevel}" : overwriteUrl, content);

                if (result.StatusCode == System.Net.HttpStatusCode.MovedPermanently)
                {
                    return await QueryAsync(query, readConsistencyLevel, result.Headers.Location.ToString());
                }
                else
                {
                    result.EnsureSuccessStatusCode();
                    var response = await result.Content.ReadAsStringAsync();
                    _logger.LogDebug($"Rqlite Response:{response}");
                    return QueryResult.FromJson(response);
                }

            }
            else
            {
                throw new Exception("HttpClient not initialized!");
            }
        }

        public async Task<ExecuteResult> ExecuteAsync(string query, string overwriteUrl = null)
        {
            if (_httpClient != null)
            {

                string[] jsonArray = new string[] { query };

                var content = new StringContent(JsonConvert.SerializeObject(jsonArray), Encoding.UTF8, "application/json");
                var result = await _httpClient.PostAsync(string.IsNullOrWhiteSpace(overwriteUrl) ? "db/execute" : overwriteUrl, content);

                if (result.StatusCode == System.Net.HttpStatusCode.MovedPermanently)
                {
                    return await ExecuteAsync(query, result.Headers.Location.ToString());
                }
                else
                {
                    result.EnsureSuccessStatusCode();

                    var response = await result.Content.ReadAsStringAsync();
                    _logger.LogDebug($"Rqlite Response:{response}");
                    return ExecuteResult.FromJson(response);
                }
            }
            else
            {
                throw new Exception("HttpClient not initialized!");
            }
        }

        public async Task<ExecuteResult> ExecuteBulkAsync(List<string> querys, string overwriteUrl = null)
        {
            if (_httpClient != null)
            {

                string[] jsonArray = querys.ToArray();

                var content = new StringContent(JsonConvert.SerializeObject(jsonArray), Encoding.UTF8, "application/json");
                var result = await _httpClient.PostAsync(string.IsNullOrWhiteSpace(overwriteUrl) ? "db/execute?transaction" : overwriteUrl, content);

                if (result.StatusCode == System.Net.HttpStatusCode.MovedPermanently)
                {
                    return await ExecuteBulkAsync(querys, result.Headers.Location.ToString());
                }
                else
                {
                    result.EnsureSuccessStatusCode();
                    var response = await result.Content.ReadAsStringAsync();
                    _logger.LogDebug($"Rqlite Response:{response}");
                    return ExecuteResult.FromJson(response);
                }
            }
            else
            {
                throw new Exception("HttpClient not initialized!");
            }
        }
    }
}

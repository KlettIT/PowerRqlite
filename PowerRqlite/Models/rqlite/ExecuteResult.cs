using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PowerRqlite.Models.rqlite
{
    public partial class ExecuteResult :IDisposable
    {
        [JsonProperty("results")]
        public List<Result> Results { get; set; }

        public void Dispose()
        {
            Results.Clear();
        }
    }

    public partial class Result
    {
        [JsonProperty("last_insert_id")]
        public long LastInsertId { get; set; }

        [JsonProperty("rows_affected")]
        public long RowsAffected { get; set; }
    }

    public partial class ExecuteResult
    {
        public static ExecuteResult FromJson(string json) => JsonConvert.DeserializeObject<ExecuteResult>(json, PowerRqlite.JSON.Converter.Settings);
    }

}

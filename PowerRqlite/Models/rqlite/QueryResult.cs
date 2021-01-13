using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace PowerRqlite.Models.rqlite
{
    public partial class QueryResult :IDisposable 
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
        [JsonProperty("columns")]
        public List<string> Columns { get; set; }

        [JsonProperty("types")]
        public List<string> Types { get; set; }

        [JsonProperty("values")]
        public IList<IList<Object>> Values { get; set; }
    }

    public partial class QueryResult
    {
        public static QueryResult FromJson(string json) => JsonConvert.DeserializeObject<QueryResult>(json, PowerRqlite.JSON.Converter.Settings);
    }
}

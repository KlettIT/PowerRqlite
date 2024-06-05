using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerRqlite.Models.rqlite
{
    public class Result
    {
        [JsonPropertyName("last_insert_id")]
        public long? LastInsertId { get; set; }

        [JsonPropertyName("rows_affected")]
        public long? RowsAffected { get; set; }

        [JsonPropertyName("columns")]
        public List<string>? Columns { get; set; }

        [JsonPropertyName("types")]
        public List<string>? Types { get; set; }

        [JsonPropertyName("values")]
        public List<List<JsonElement>>? Values { get; set; }
    }
}

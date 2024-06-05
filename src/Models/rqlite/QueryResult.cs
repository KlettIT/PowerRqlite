using System.Text.Json.Serialization;

namespace PowerRqlite.Models.rqlite
{
    public partial class QueryResult : IDisposable
    {
        [JsonPropertyName("results")]
        public List<Result>? Results { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Results?.Clear();
        }
    }
}

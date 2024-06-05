using Microsoft.Extensions.Options;
using PowerRqlite.Interfaces.rqlite;

namespace PowerRqlite.Models.rqlite
{
    public class RqliteContext(IOptions<RqliteOptions> options) : IRqliteContext
    {
        public string RqliteUrl { get; } = options.Value.Url;
    }
}

using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerRqlite.Models.rqlite
{
    public class rqliteContext : IrqliteContext
    {

        public string rqliteUrl { get; }

        public rqliteContext(IOptions<rqliteOptions> options)
        {
            rqliteUrl = options.Value.Url;
        }
        
    }
}

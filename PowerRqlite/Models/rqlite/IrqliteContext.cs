using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerRqlite.Models.rqlite
{
    public interface IrqliteContext
    {
        string rqliteUrl { get; }
    }
}

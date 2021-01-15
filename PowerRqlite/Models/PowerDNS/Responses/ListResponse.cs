using PowerRqlite.Exceptions;
using PowerRqlite.Models.rqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerRqlite.Models.PowerDNS.Responses
{
    public class ListResponse : IResponse
    {
        private bool isDisposed;
        public List<IRecord> result { get; set; } = new List<IRecord>();
        public List<string> Log { get; set; }
        public static ListResponse FromValues (IList<IList<object>> Values)
        {

            List<IRecord> records = new List<IRecord>();

            if (Values != null)
            {

                System.Threading.Tasks.Parallel.ForEach(Values, value =>
                 {
                     Record record = new Record
                     {
                         domain_id = value[0] != null ? int.Parse(value[0].ToString()) : -1,
                         qname = value[1] != null ? value[1].ToString() : string.Empty,
                         qtype = value[2] != null ? value[2].ToString() : string.Empty,
                         content = value[3] != null ? value[3].ToString() : string.Empty,
                         ttl = value[4] != null ? int.Parse(value[4].ToString()) : 0,
                         disabled = value[5] != null ? Convert.ToBoolean(Convert.ToInt16(value[5])) : false,
                         auth = value[6] != null ? Convert.ToBoolean(Convert.ToInt16(value[6])) : true
                     };

                     records.Add(record);
                 });

                return new ListResponse() { result = records };
            }
            else
            {
                throw new NoValuesException();
            } 
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                // free managed resources
            }

            isDisposed = true;
        }
    }
}

using PowerRqlite.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerRqlite.Models.PowerDNS.Responses
{
    public class StringArrayResponse : IResponse
    {
        private bool isDisposed;
        public List<string> result { get; set; }
        public List<string> Log { get; set; }

        public static StringArrayResponse FromValues(IList<IList<Object>> Values)
        {
            if (Values != null)
            {
                return new StringArrayResponse() { result = Values.Select(x => x.ToString()).ToList() };

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

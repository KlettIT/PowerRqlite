using PowerRqlite.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerRqlite.Models.PowerDNS.Responses
{
    public class KeyToArrayResponse : IResponse
    {
        private bool isDisposed;
        public Dictionary<string, List<string>> result { get; set; }
        public List<string> Log { get; set; }

        public static KeyToArrayResponse FromValues(IList<IList<Object>> Values)
        {
            if (Values != null)
            {

                Dictionary<string, List<string>> valuePairs = new Dictionary<string, List<string>>();

                foreach(var value in Values)
                {
                    string key = value[0].ToString();

                    if (valuePairs.ContainsKey(key))
                    {
                        valuePairs[key].Add(value[1].ToString());
                    }
                    else
                    {
                        valuePairs.Add(key, new List<string>() { value[1].ToString() });
                    }

                }

                return new KeyToArrayResponse() { result = valuePairs };

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

using System;

namespace PowerRqlite.Exceptions
{
    public class NoValuesException : Exception
    {
        public NoValuesException()
        {
        }

        public NoValuesException(string message)
            : base(message)
        {
        }

        public NoValuesException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}

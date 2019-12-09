using System;

namespace Archive.Data.Exceptions
{
    public class ReadException : Exception
    {
        public ReadException(string message) : base(message)
        {
        }

        public ReadException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
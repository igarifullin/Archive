using System;

namespace Archive.Data.Exceptions
{
    public class WriteException : Exception
    {
        public WriteException(string message) : base(message)
        {
        }

        public WriteException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
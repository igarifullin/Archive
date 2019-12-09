using System;

namespace Archive.Data.Exceptions
{
    public class ProcessException : Exception
    {
        public ProcessException(string message) : base(message)
        {
        }

        public ProcessException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

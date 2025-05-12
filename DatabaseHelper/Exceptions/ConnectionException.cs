using System;
using System.Runtime.Serialization;

namespace DatabaseHelper.Exceptions
{
    /// <summary>
    /// Exception thrown when there are issues with database connections
    /// </summary>
    [Serializable]
    public class ConnectionException : DatabaseException
    {
        public ConnectionException() : base()
        {
        }

        public ConnectionException(string message) : base(message)
        {
        }

        public ConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ConnectionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
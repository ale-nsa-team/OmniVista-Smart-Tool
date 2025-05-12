using System;
using System.Runtime.Serialization;

namespace DatabaseHelper.Exceptions
{
    /// <summary>
    /// Base exception class for all database-related exceptions in the application
    /// </summary>
    [Serializable]
    public class DatabaseException : Exception
    {
        public DatabaseException() : base()
        {
        }

        public DatabaseException(string message) : base(message)
        {
        }

        public DatabaseException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DatabaseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
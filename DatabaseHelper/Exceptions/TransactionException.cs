using System;
using System.Runtime.Serialization;

namespace DatabaseHelper.Exceptions
{
    /// <summary>
    /// Exception thrown when transaction operations fail
    /// </summary>
    [Serializable]
    public class TransactionException : DatabaseException
    {
        public TransactionException() : base()
        {
        }

        public TransactionException(string message) : base(message)
        {
        }

        public TransactionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TransactionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
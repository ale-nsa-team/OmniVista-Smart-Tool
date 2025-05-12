using System;
using System.Runtime.Serialization;

namespace DatabaseHelper.Exceptions
{
    /// <summary>
    /// Exception thrown when repository operations fail
    /// </summary>
    [Serializable]
    public class RepositoryException : DatabaseException
    {
        public string EntityName { get; }
        public string Operation { get; }

        public RepositoryException() : base()
        {
        }

        public RepositoryException(string message) : base(message)
        {
        }

        public RepositoryException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public RepositoryException(string message, string entityName, string operation)
            : base(message)
        {
            EntityName = entityName;
            Operation = operation;
        }

        public RepositoryException(string message, string entityName, string operation, Exception innerException)
            : base(message, innerException)
        {
            EntityName = entityName;
            Operation = operation;
        }

        protected RepositoryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
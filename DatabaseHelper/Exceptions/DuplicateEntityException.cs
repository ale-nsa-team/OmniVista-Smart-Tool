using System;

namespace DatabaseHelper.Exceptions
{
    /// <summary>
    /// Exception thrown when attempting to create an entity that already exists.
    /// </summary>
    public class DuplicateEntityException : Exception
    {
        /// <summary>
        /// Gets the entity type that has the duplicate
        /// </summary>
        public string EntityType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateEntityException"/> class.
        /// </summary>
        public DuplicateEntityException() : base("An entity with the same identifier already exists.")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateEntityException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DuplicateEntityException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateEntityException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DuplicateEntityException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateEntityException"/> class with a specified error message
        /// and the entity type that has the duplicate.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="entityType">The type of entity that has the duplicate.</param>
        public DuplicateEntityException(string message, string entityType) : base(message)
        {
            EntityType = entityType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateEntityException"/> class with a specified error message,
        /// the entity type that has the duplicate, and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="entityType">The type of entity that has the duplicate.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public DuplicateEntityException(string message, string entityType, Exception innerException) : base(message, innerException)
        {
            EntityType = entityType;
        }
    }
}
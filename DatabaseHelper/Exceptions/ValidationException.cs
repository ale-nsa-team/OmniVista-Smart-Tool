using DatabaseHelper.Validation;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DatabaseHelper.Exceptions
{
    /// <summary>
    /// Exception thrown when entity validation fails
    /// </summary>
    [Serializable]
    public class ValidationException : DatabaseException
    {
        /// <summary>
        /// Gets the validation results that caused this exception
        /// </summary>
        public IEnumerable<ValidationResult> ValidationResults { get; }

        /// <summary>
        /// Gets the entity type that failed validation
        /// </summary>
        public string EntityType { get; }

        public ValidationException() : base()
        {
        }

        public ValidationException(string message) : base(message)
        {
        }

        public ValidationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ValidationException(string message, string entityType, IEnumerable<ValidationResult> validationResults)
            : base(message)
        {
            EntityType = entityType;
            ValidationResults = validationResults;
        }

        public ValidationException(string message, string entityType, IEnumerable<ValidationResult> validationResults, Exception innerException)
            : base(message, innerException)
        {
            EntityType = entityType;
            ValidationResults = validationResults;
        }

        protected ValidationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
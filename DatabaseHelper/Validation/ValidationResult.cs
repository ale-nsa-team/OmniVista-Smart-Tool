namespace DatabaseHelper.Validation
{
    /// <summary>
    /// Represents the result of a validation operation
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets or sets the property name that failed validation
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the severity of the validation error
        /// </summary>
        public ValidationSeverity Severity { get; set; }

        /// <summary>
        /// Initializes a new instance of the ValidationResult class
        /// </summary>
        /// <param name="propertyName">The property name that failed validation</param>
        /// <param name="errorMessage">The error message</param>
        /// <param name="severity">The severity of the validation error</param>
        public ValidationResult(string propertyName, string errorMessage, ValidationSeverity severity = ValidationSeverity.Error)
        {
            PropertyName = propertyName;
            ErrorMessage = errorMessage;
            Severity = severity;
        }

        /// <summary>
        /// Returns a string representation of the validation result
        /// </summary>
        /// <returns>A string representation of the validation result</returns>
        public override string ToString()
        {
            return $"[{Severity}] {PropertyName}: {ErrorMessage}";
        }
    }

    /// <summary>
    /// Represents the severity of a validation error
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// Information only, not an error
        /// </summary>
        Info,

        /// <summary>
        /// Warning, might not be an error
        /// </summary>
        Warning,

        /// <summary>
        /// Error, validation failed
        /// </summary>
        Error,

        /// <summary>
        /// Critical error, validation failed
        /// </summary>
        Critical
    }
}
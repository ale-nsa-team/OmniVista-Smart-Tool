using DatabaseHelper.Exceptions;
using DatabaseHelper.Logging;
using System.Collections.Generic;
using System.Linq;

namespace DatabaseHelper.Validation.Validators
{
    /// <summary>
    /// Base class for entity validators
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to validate</typeparam>
    public abstract class BaseValidator<TEntity> : IValidator<TEntity> where TEntity : class
    {
        protected readonly ILogger _logger;
        protected readonly string _entityName;
        protected List<string> _validationErrors = new List<string>();

        protected BaseValidator()
        {
            _entityName = typeof(TEntity).Name;
            _logger = LoggerFactory.CreateLogger(GetType());
            _logger.Debug($"Validator for {_entityName} initialized");
        }

        /// <summary>
        /// Validates the entity and returns validation results
        /// </summary>
        /// <param name="entity">The entity to validate</param>
        /// <returns>A collection of validation results</returns>
        public abstract IEnumerable<ValidationResult> Validate(TEntity entity);

        /// <summary>
        /// Validates the entity and throws an exception if invalid
        /// </summary>
        /// <param name="entity">The entity to validate</param>
        public virtual void ValidateAndThrow(TEntity entity)
        {
            _validationErrors.Clear();

            var results = Validate(entity);
            var errors = results.Where(r => r.Severity >= ValidationSeverity.Error).ToList();

            if (errors.Any())
            {
                var errorMessages = string.Join(", ", errors.Select(r => r.ToString()));
                _logger.Error($"Validation failed for {_entityName}: {errorMessages}");
                throw new ValidationException($"Validation failed for {_entityName}");
            }
        }

        /// <summary>
        /// Adds a validation error to the internal error list
        /// </summary>
        /// <param name="errorMessage">The error message</param>
        protected void AddValidationError(string errorMessage)
        {
            _validationErrors.Add(errorMessage);
        }

        /// <summary>
        /// Throws a ValidationException if there are any validation errors
        /// </summary>
        protected void ThrowValidationExceptionIfErrors()
        {
            if (_validationErrors.Any())
            {
                var errorMessage = string.Join(", ", _validationErrors);
                _logger.Error($"Validation failed for {_entityName}: {errorMessage}");
                throw new ValidationException($"Validation failed for {_entityName}");
            }
        }

        /// <summary>
        /// Checks if the entity is valid
        /// </summary>
        /// <param name="entity">The entity to validate</param>
        /// <returns>True if the entity is valid, false otherwise</returns>
        public bool IsValid(TEntity entity)
        {
            var validationResults = Validate(entity);
            bool isValid = !validationResults.Any(r => r.Severity >= ValidationSeverity.Error);

            if (isValid)
            {
                _logger.Debug($"Entity {_entityName} passed validation");
            }
            else
            {
                var errors = string.Join(", ", validationResults.Select(r => r.ToString()));
                _logger.Warn($"Entity {_entityName} failed validation: {errors}");
            }

            return isValid;
        }

        /// <summary>
        /// Adds a validation error if the condition is true
        /// </summary>
        /// <param name="results">The collection of validation results</param>
        /// <param name="condition">The condition to check</param>
        /// <param name="propertyName">The property name</param>
        /// <param name="errorMessage">The error message</param>
        /// <param name="severity">The severity of the validation error</param>
        protected void AddErrorIf(List<ValidationResult> results, bool condition, string propertyName, string errorMessage, ValidationSeverity severity = ValidationSeverity.Error)
        {
            if (condition)
            {
                results.Add(new ValidationResult(propertyName, errorMessage, severity));
            }
        }

        /// <summary>
        /// Validates that a string property is not null or empty
        /// </summary>
        /// <param name="results">The collection of validation results</param>
        /// <param name="value">The value to check</param>
        /// <param name="propertyName">The property name</param>
        /// <param name="severity">The severity of the validation error</param>
        protected void ValidateRequired(List<ValidationResult> results, string value, string propertyName, ValidationSeverity severity = ValidationSeverity.Error)
        {
            AddErrorIf(results, string.IsNullOrEmpty(value), propertyName, $"{propertyName} is required", severity);
        }

        /// <summary>
        /// Validates that a string property does not exceed a maximum length
        /// </summary>
        /// <param name="results">The collection of validation results</param>
        /// <param name="value">The value to check</param>
        /// <param name="maxLength">The maximum allowed length</param>
        /// <param name="propertyName">The property name</param>
        /// <param name="severity">The severity of the validation error</param>
        protected void ValidateMaxLength(List<ValidationResult> results, string value, int maxLength, string propertyName, ValidationSeverity severity = ValidationSeverity.Error)
        {
            if (!string.IsNullOrEmpty(value))
            {
                AddErrorIf(results, value.Length > maxLength, propertyName, $"{propertyName} cannot exceed {maxLength} characters", severity);
            }
        }
    }
}
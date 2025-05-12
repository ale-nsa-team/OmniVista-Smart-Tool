using DatabaseHelper.Entities;
using DatabaseHelper.Exceptions;
using System;
using System.Collections.Generic;

namespace DatabaseHelper.Validation.Validators
{
    /// <summary>
    /// Validator for DatabaseVersionEntity
    /// </summary>
    public class DatabaseVersionValidator : BaseValidator<DatabaseVersionEntity>, IValidator<DatabaseVersionEntity>
    {
        /// <summary>
        /// Validates the DatabaseVersionEntity and returns validation results
        /// </summary>
        /// <param name="entity">The DatabaseVersionEntity to validate</param>
        /// <returns>A collection of validation results</returns>
        public override IEnumerable<ValidationResult> Validate(DatabaseVersionEntity entity)
        {
            var results = new List<ValidationResult>();

            if (entity == null)
            {
                results.Add(new ValidationResult("entity", "DatabaseVersion entity cannot be null", ValidationSeverity.Critical));
                return results;
            }

            // Validate ID (cannot be empty)
            if (entity.Id == Guid.Empty)
            {
                results.Add(new ValidationResult(nameof(entity.Id), "Id is required"));
            }

            // Validate version (cannot be empty)
            if (string.IsNullOrWhiteSpace(entity.Version))
            {
                results.Add(new ValidationResult(nameof(entity.Version), "Version is required"));
            }
            else if (entity.Version.Length > 50)
            {
                results.Add(new ValidationResult(nameof(entity.Version), "Version cannot exceed 50 characters"));
            }

            // Validate release date
            if (entity.ReleaseDate > DateTime.Now)
            {
                results.Add(new ValidationResult(nameof(entity.ReleaseDate), "Release date cannot be in the future"));
            }

            // Validate description (optional but limited in length)
            if (!string.IsNullOrEmpty(entity.Description) && entity.Description.Length > 1000)
            {
                results.Add(new ValidationResult(nameof(entity.Description), "Description cannot exceed 1000 characters"));
            }

            return results;
        }

        /// <summary>
        /// Validates the DatabaseVersionEntity and throws an exception if invalid
        /// </summary>
        /// <param name="entity">The DatabaseVersionEntity to validate</param>
        public override void ValidateAndThrow(DatabaseVersionEntity entity)
        {
            _validationErrors.Clear();

            if (entity == null)
            {
                throw new ValidationException("DatabaseVersionEntity cannot be null");
            }

            // Validate ID (cannot be empty)
            if (entity.Id == Guid.Empty)
            {
                AddValidationError("Id is required");
            }

            // Validate version (cannot be empty)
            if (string.IsNullOrWhiteSpace(entity.Version))
            {
                AddValidationError("Version is required");
            }
            else if (entity.Version.Length > 50)
            {
                AddValidationError("Version cannot exceed 50 characters");
            }

            // Validate release date
            if (entity.ReleaseDate > DateTime.Now)
            {
                AddValidationError("Release date cannot be in the future");
            }

            // Validate description (optional but limited in length)
            if (!string.IsNullOrEmpty(entity.Description) && entity.Description.Length > 1000)
            {
                AddValidationError("Description cannot exceed 1000 characters");
            }

            // Throw exception if validation errors
            ThrowValidationExceptionIfErrors();
        }
    }
}
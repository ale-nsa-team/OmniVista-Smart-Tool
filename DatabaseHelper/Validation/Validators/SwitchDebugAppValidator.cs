using DatabaseHelper.Entities;
using DatabaseHelper.Validation;
using System;
using System.Collections.Generic;

namespace DatabaseHelper.Validation.Validators
{
    /// <summary>
    /// Validator for SwitchDebugAppEntity
    /// </summary>
    public class SwitchDebugAppValidator : BaseValidator<SwitchDebugAppEntity>
    {
        /// <summary>
        /// Validates the SwitchDebugAppEntity and returns validation results
        /// </summary>
        /// <param name="entity">The SwitchDebugAppEntity to validate</param>
        /// <returns>A collection of validation results</returns>
        public override IEnumerable<ValidationResult> Validate(SwitchDebugAppEntity entity)
        {
            var results = new List<ValidationResult>();

            if (entity == null)
            {
                results.Add(new ValidationResult("entity", "Switch debug app entity cannot be null", ValidationSeverity.Critical));
                return results;
            }

            // Validate required fields
            if (entity.Id == Guid.Empty)
            {
                results.Add(new ValidationResult(nameof(entity.Id), "Id is required"));
            }

            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                results.Add(new ValidationResult(nameof(entity.Name), "Name is required"));
            }

            // Validate string lengths
            ValidateMaxLength(results, entity.Name, 100, nameof(entity.Name));
            ValidateMaxLength(results, entity.AppId, 50, nameof(entity.AppId));
            ValidateMaxLength(results, entity.AppIndex, 20, nameof(entity.AppIndex));
            ValidateMaxLength(results, entity.NbSubApp, 20, nameof(entity.NbSubApp));

            // Validate numeric fields
            if (entity.DebugLevel < 0 || entity.DebugLevel > 5)
            {
                results.Add(new ValidationResult(nameof(entity.DebugLevel), "Debug level must be between 0 and 5"));
            }

            // Validate that the debug app belongs to a switch
            if (entity.SwitchId == Guid.Empty)
            {
                results.Add(new ValidationResult(nameof(entity.SwitchId), "SwitchId is required"));
            }

            return results;
        }

        /// <summary>
        /// Validates that a string doesn't exceed the maximum length
        /// </summary>
        private void ValidateMaxLength(List<ValidationResult> results, string value, int maxLength, string propertyName)
        {
            if (!string.IsNullOrEmpty(value) && value.Length > maxLength)
            {
                results.Add(new ValidationResult(propertyName, $"{propertyName} exceeds the maximum length of {maxLength} characters"));
            }
        }
    }
}
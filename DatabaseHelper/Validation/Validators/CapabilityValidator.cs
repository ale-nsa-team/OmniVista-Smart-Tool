using DatabaseHelper.Entities;
using DatabaseHelper.Validation;
using System;
using System.Collections.Generic;

namespace DatabaseHelper.Validation.Validators
{
    /// <summary>
    /// Validator for CapabilityEntity
    /// </summary>
    public class CapabilityValidator : BaseValidator<CapabilityEntity>
    {
        /// <summary>
        /// Validates the CapabilityEntity and returns validation results
        /// </summary>
        /// <param name="entity">The CapabilityEntity to validate</param>
        /// <returns>A collection of validation results</returns>
        public override IEnumerable<ValidationResult> Validate(CapabilityEntity entity)
        {
            var results = new List<ValidationResult>();

            if (entity == null)
            {
                results.Add(new ValidationResult("entity", "Capability entity cannot be null", ValidationSeverity.Critical));
                return results;
            }

            // Validate required fields
            if (entity.Id == Guid.Empty)
            {
                results.Add(new ValidationResult(nameof(entity.Id), "Id is required"));
            }

            if (string.IsNullOrWhiteSpace(entity.Value))
            {
                results.Add(new ValidationResult(nameof(entity.Value), "Value is required"));
            }

            // Validate string lengths
            ValidateMaxLength(results, entity.Value, 255, nameof(entity.Value));

            // Validate that the capability belongs to an endpoint device
            if (entity.EndPointDeviceId == Guid.Empty)
            {
                results.Add(new ValidationResult(nameof(entity.EndPointDeviceId), "EndPointDeviceId is required"));
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
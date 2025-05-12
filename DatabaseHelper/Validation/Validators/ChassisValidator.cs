using DatabaseHelper.Entities;
using System.Collections.Generic;

namespace DatabaseHelper.Validation.Validators
{
    /// <summary>
    /// Validator for ChassisEntity
    /// </summary>
    public class ChassisValidator : BaseValidator<ChassisEntity>
    {
        /// <summary>
        /// Validates the ChassisEntity and returns validation results
        /// </summary>
        /// <param name="entity">The ChassisEntity to validate</param>
        /// <returns>A collection of validation results</returns>
        public override IEnumerable<ValidationResult> Validate(ChassisEntity entity)
        {
            var results = new List<ValidationResult>();

            if (entity == null)
            {
                results.Add(new ValidationResult("entity", "Chassis entity cannot be null", ValidationSeverity.Critical));
                return results;
            }

            // Validate required fields
            if (entity.Number <= 0)
            {
                results.Add(new ValidationResult(nameof(entity.Number), "Chassis number must be greater than 0"));
            }

            if (string.IsNullOrWhiteSpace(entity.Model))
            {
                results.Add(new ValidationResult(nameof(entity.Model), "Chassis model is required"));
            }

            // Validate string lengths
            ValidateMaxLength(results, entity.Model, 100, nameof(entity.Model));
            ValidateMaxLength(results, entity.Type, 50, nameof(entity.Type));
            ValidateMaxLength(results, entity.SerialNumber, 50, nameof(entity.SerialNumber));
            ValidateMaxLength(results, entity.PartNumber, 50, nameof(entity.PartNumber));
            ValidateMaxLength(results, entity.MacAddress, 17, nameof(entity.MacAddress));

            // Validate numeric fields for valid ranges
            if (entity.PowerBudget < 0)
            {
                results.Add(new ValidationResult(nameof(entity.PowerBudget), "Power budget must be non-negative"));
            }

            if (entity.PowerConsumed < 0)
            {
                results.Add(new ValidationResult(nameof(entity.PowerConsumed), "Power consumed must be non-negative"));
            }

            // Validate SwitchId
            if (entity.SwitchId == System.Guid.Empty)
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
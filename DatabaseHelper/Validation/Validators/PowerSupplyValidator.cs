using DatabaseHelper.Entities;
using DatabaseHelper.Validation;
using System;
using System.Collections.Generic;

namespace DatabaseHelper.Validation.Validators
{
    /// <summary>
    /// Validator for PowerSupplyEntity
    /// </summary>
    public class PowerSupplyValidator : BaseValidator<PowerSupplyEntity>
    {
        /// <summary>
        /// Validates the PowerSupplyEntity and returns validation results
        /// </summary>
        /// <param name="entity">The PowerSupplyEntity to validate</param>
        /// <returns>A collection of validation results</returns>
        public override IEnumerable<ValidationResult> Validate(PowerSupplyEntity entity)
        {
            var results = new List<ValidationResult>();

            if (entity == null)
            {
                results.Add(new ValidationResult("entity", "Power supply entity cannot be null", ValidationSeverity.Critical));
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
            ValidateMaxLength(results, entity.Model, 100, nameof(entity.Model));
            ValidateMaxLength(results, entity.Type, 50, nameof(entity.Type));
            ValidateMaxLength(results, entity.Location, 100, nameof(entity.Location));
            ValidateMaxLength(results, entity.Description, 500, nameof(entity.Description));
            ValidateMaxLength(results, entity.Status, 50, nameof(entity.Status));
            ValidateMaxLength(results, entity.PartNumber, 100, nameof(entity.PartNumber));
            ValidateMaxLength(results, entity.HardwareRevision, 50, nameof(entity.HardwareRevision));
            ValidateMaxLength(results, entity.SerialNumber, 100, nameof(entity.SerialNumber));
            ValidateMaxLength(results, entity.PowerProvision, 50, nameof(entity.PowerProvision));

            // Validate that the power supply belongs to a chassis
            if (entity.ChassisId == Guid.Empty)
            {
                results.Add(new ValidationResult(nameof(entity.ChassisId), "ChassisId is required"));
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
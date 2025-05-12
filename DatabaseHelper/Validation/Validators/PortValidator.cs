using DatabaseHelper.Entities;
using DatabaseHelper.Validation;
using System;
using System.Collections.Generic;
using System.Net;

namespace DatabaseHelper.Validation.Validators
{
    /// <summary>
    /// Validator for PortEntity
    /// </summary>
    public class PortValidator : BaseValidator<PortEntity>
    {
        /// <summary>
        /// Validates the PortEntity and returns validation results
        /// </summary>
        /// <param name="entity">The PortEntity to validate</param>
        /// <returns>A collection of validation results</returns>
        public override IEnumerable<ValidationResult> Validate(PortEntity entity)
        {
            var results = new List<ValidationResult>();

            if (entity == null)
            {
                results.Add(new ValidationResult("entity", "Port entity cannot be null", ValidationSeverity.Critical));
                return results;
            }

            // Validate required fields
            if (entity.Number <= 0)
            {
                results.Add(new ValidationResult(nameof(entity.Number), "Port number must be greater than 0"));
            }

            // Validate string lengths
            ValidateMaxLength(results, entity.Name, 100, nameof(entity.Name));
            ValidateMaxLength(results, entity.PortIndex, 50, nameof(entity.PortIndex));
            ValidateMaxLength(results, entity.Status, 50, nameof(entity.Status));

            // Validate power values
            if (entity.Poe < 0)
            {
                results.Add(new ValidationResult(nameof(entity.Poe), "PoE value must be non-negative"));
            }

            if (entity.Power < 0)
            {
                results.Add(new ValidationResult(nameof(entity.Power), "Power value must be non-negative"));
            }

            if (entity.MaxPower < 0)
            {
                results.Add(new ValidationResult(nameof(entity.MaxPower), "MaxPower value must be non-negative"));
            }

            // Validate IP address format if present
            if (!string.IsNullOrEmpty(entity.IpAddress) && !IPAddress.TryParse(entity.IpAddress, out _))
            {
                results.Add(new ValidationResult(nameof(entity.IpAddress), "Invalid IP address format"));
            }

            // Validate SlotId
            if (entity.SlotId == Guid.Empty)
            {
                results.Add(new ValidationResult(nameof(entity.SlotId), "SlotId is required"));
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
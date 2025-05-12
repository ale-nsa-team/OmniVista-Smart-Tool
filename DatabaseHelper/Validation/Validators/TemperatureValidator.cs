using DatabaseHelper.Entities;
using DatabaseHelper.Exceptions;
using System;
using System.Collections.Generic;

namespace DatabaseHelper.Validation.Validators
{
    /// <summary>
    /// Validator for TemperatureEntity
    /// </summary>
    public class TemperatureValidator : BaseValidator<TemperatureEntity>, IValidator<TemperatureEntity>
    {
        /// <summary>
        /// Validates the TemperatureEntity
        /// </summary>
        /// <param name="entity">The TemperatureEntity to validate</param>
        /// <returns>A collection of validation results</returns>
        public override IEnumerable<ValidationResult> Validate(TemperatureEntity entity)
        {
            var results = new List<ValidationResult>();

            if (entity == null)
            {
                results.Add(new ValidationResult("entity", "Temperature entity cannot be null", ValidationSeverity.Critical));
                return results;
            }

            // Validate device name (cannot be empty)
            if (string.IsNullOrWhiteSpace(entity.Device))
            {
                results.Add(new ValidationResult(nameof(entity.Device), "Device name is required"));
            }
            else if (entity.Device.Length > 100)
            {
                results.Add(new ValidationResult(nameof(entity.Device), "Device name cannot exceed 100 characters"));
            }

            // Validate current temperature (must be within reasonable range)
            if (entity.Current < -50 || entity.Current > 150)
            {
                results.Add(new ValidationResult(nameof(entity.Current), "Current temperature must be between -50 and 150 degrees"));
            }

            // Only validate threshold and current relationship if current is over normal temp
            // and threshold is set (non-zero)
            if (entity.Current > 40 && entity.Threshold > 0 && entity.Threshold < entity.Current)
            {
                results.Add(new ValidationResult(nameof(entity.Threshold), "Threshold must be higher than the current temperature when current is above normal"));
            }

            // Only validate danger and threshold relationship if both are set (non-zero)
            if (entity.Threshold > 0 && entity.Danger > 0 && entity.Danger < entity.Threshold)
            {
                results.Add(new ValidationResult(nameof(entity.Danger), "Danger level must be higher than the threshold level"));
            }

            // Validate chassis ID (cannot be empty)
            if (entity.ChassisId == Guid.Empty)
            {
                results.Add(new ValidationResult(nameof(entity.ChassisId), "ChassisId is required"));
            }

            return results;
        }

        /// <summary>
        /// Validates the TemperatureEntity and throws an exception if invalid
        /// </summary>
        /// <param name="entity">The TemperatureEntity to validate</param>
        public override void ValidateAndThrow(TemperatureEntity entity)
        {
            _validationErrors.Clear();

            if (entity == null)
            {
                throw new ValidationException("TemperatureEntity cannot be null");
            }

            // Validate device name (cannot be empty)
            if (string.IsNullOrWhiteSpace(entity.Device))
            {
                AddValidationError("Device name is required");
            }
            else if (entity.Device.Length > 100)
            {
                AddValidationError("Device name cannot exceed 100 characters");
            }

            // Validate current temperature (must be within reasonable range)
            if (entity.Current < -50 || entity.Current > 150)
            {
                AddValidationError("Current temperature must be between -50 and 150 degrees");
            }

            // Only validate threshold and current relationship if current is over normal temp
            // and threshold is set (non-zero)
            if (entity.Current > 40 && entity.Threshold > 0 && entity.Threshold < entity.Current)
            {
                AddValidationError("Threshold must be higher than the current temperature when current is above normal");
            }

            // Only validate danger and threshold relationship if both are set (non-zero)
            if (entity.Threshold > 0 && entity.Danger > 0 && entity.Danger < entity.Threshold)
            {
                AddValidationError("Danger level must be higher than the threshold level");
            }

            // Validate chassis ID (cannot be empty)
            if (entity.ChassisId == Guid.Empty)
            {
                AddValidationError("ChassisId is required");
            }

            // Throw exception if validation errors
            ThrowValidationExceptionIfErrors();
        }
    }
}
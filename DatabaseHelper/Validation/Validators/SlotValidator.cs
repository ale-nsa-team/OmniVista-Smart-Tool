using DatabaseHelper.Entities;
using DatabaseHelper.Exceptions;
using DatabaseHelper.Validation;
using System;
using System.Collections.Generic;

namespace DatabaseHelper.Validation.Validators
{
    /// <summary>
    /// Validator for SlotEntity
    /// </summary>
    public class SlotValidator : BaseValidator<SlotEntity>, IValidator<SlotEntity>
    {
        /// <summary>
        /// Validates the SlotEntity and returns validation results
        /// </summary>
        /// <param name="entity">The SlotEntity to validate</param>
        /// <returns>A collection of validation results</returns>
        public override IEnumerable<ValidationResult> Validate(SlotEntity entity)
        {
            var results = new List<ValidationResult>();

            if (entity == null)
            {
                results.Add(new ValidationResult("entity", "Slot entity cannot be null", ValidationSeverity.Critical));
                return results;
            }

            // Validate slot number (must be positive)
            if (entity.Number <= 0)
            {
                results.Add(new ValidationResult(nameof(entity.Number), "Number must be positive"));
            }

            // Validate name (cannot be empty)
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                results.Add(new ValidationResult(nameof(entity.Name), "Name is required"));
            }
            else if (entity.Name.Length > 100)
            {
                results.Add(new ValidationResult(nameof(entity.Name), "Name cannot exceed 100 characters"));
            }

            // Validate NbPorts (must be non-negative)
            if (entity.NbPorts < 0)
            {
                results.Add(new ValidationResult(nameof(entity.NbPorts), "Number of ports cannot be negative"));
            }

            // Validate power (must be non-negative)
            if (entity.Power < 0)
            {
                results.Add(new ValidationResult(nameof(entity.Power), "Power cannot be negative"));
            }

            // Validate budget (must be non-negative)
            if (entity.Budget < 0)
            {
                results.Add(new ValidationResult(nameof(entity.Budget), "Budget cannot be negative"));
            }

            // Validate threshold (must be non-negative)
            if (entity.Threshold < 0)
            {
                results.Add(new ValidationResult(nameof(entity.Threshold), "Threshold cannot be negative"));
            }

            // Validate chassis ID (cannot be empty)
            if (entity.ChassisId == Guid.Empty)
            {
                results.Add(new ValidationResult(nameof(entity.ChassisId), "ChassisId is required"));
            }

            return results;
        }

        public override void ValidateAndThrow(SlotEntity entity)
        {
            _validationErrors.Clear();

            if (entity == null)
            {
                throw new ValidationException("SlotEntity cannot be null");
            }

            // Validate slot number (must be positive)
            if (entity.Number <= 0)
            {
                AddValidationError("Number must be positive");
            }

            // Validate name (cannot be empty)
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                AddValidationError("Name is required");
            }
            else if (entity.Name.Length > 100)
            {
                AddValidationError("Name cannot exceed 100 characters");
            }

            // Validate NbPorts (must be non-negative)
            if (entity.NbPorts < 0)
            {
                AddValidationError("Number of ports cannot be negative");
            }

            // Validate power (must be non-negative)
            if (entity.Power < 0)
            {
                AddValidationError("Power cannot be negative");
            }

            // Validate budget (must be non-negative)
            if (entity.Budget < 0)
            {
                AddValidationError("Budget cannot be negative");
            }

            // Validate threshold (must be non-negative)
            if (entity.Threshold < 0)
            {
                AddValidationError("Threshold cannot be negative");
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
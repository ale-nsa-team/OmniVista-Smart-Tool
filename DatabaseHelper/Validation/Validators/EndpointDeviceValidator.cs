using DatabaseHelper.Entities;
using DatabaseHelper.Exceptions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

namespace DatabaseHelper.Validation.Validators
{
    /// <summary>
    /// Validator for EndpointDeviceEntity
    /// </summary>
    public class EndpointDeviceValidator : BaseValidator<EndpointDeviceEntity>, IValidator<EndpointDeviceEntity>
    {
        // Regular expression for validating MAC addresses (xx:xx:xx:xx:xx:xx format)
        private static readonly Regex _macAddressRegex = new Regex(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$", RegexOptions.Compiled);

        /// <summary>
        /// Validates the EndpointDeviceEntity and returns validation results
        /// </summary>
        /// <param name="entity">The EndpointDeviceEntity to validate</param>
        /// <returns>A collection of validation results</returns>
        public override IEnumerable<ValidationResult> Validate(EndpointDeviceEntity entity)
        {
            var results = new List<ValidationResult>();

            if (entity == null)
            {
                results.Add(new ValidationResult("entity", "EndpointDevice entity cannot be null", ValidationSeverity.Critical));
                return results;
            }

            // Validate name (cannot be empty)
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                results.Add(new ValidationResult(nameof(entity.Name), "Name is required"));
            }

            // Validate string lengths
            ValidateMaxLength(results, entity.Name, 100, nameof(entity.Name));
            ValidateMaxLength(results, entity.Vendor, 100, nameof(entity.Vendor));
            ValidateMaxLength(results, entity.Model, 100, nameof(entity.Model));
            ValidateMaxLength(results, entity.SerialNumber, 50, nameof(entity.SerialNumber));
            ValidateMaxLength(results, entity.Type, 50, nameof(entity.Type));

            // Validate IP address format if present
            if (!string.IsNullOrEmpty(entity.IpAddress) && !IPAddress.TryParse(entity.IpAddress, out _))
            {
                results.Add(new ValidationResult(nameof(entity.IpAddress), "Invalid IP address format"));
            }

            // Validate MAC address format if present
            if (!string.IsNullOrEmpty(entity.MacAddress) && !_macAddressRegex.IsMatch(entity.MacAddress))
            {
                results.Add(new ValidationResult(nameof(entity.MacAddress), "Invalid MAC address format (expected format: xx:xx:xx:xx:xx:xx)"));
            }

            // Validate PortId
            if (entity.PortId == Guid.Empty)
            {
                results.Add(new ValidationResult(nameof(entity.PortId), "PortId is required"));
            }

            return results;
        }

        /// <summary>
        /// Validates the EndpointDeviceEntity and throws an exception if invalid
        /// </summary>
        /// <param name="entity">The EndpointDeviceEntity to validate</param>
        public override void ValidateAndThrow(EndpointDeviceEntity entity)
        {
            _validationErrors.Clear();

            if (entity == null)
            {
                throw new ValidationException("EndpointDeviceEntity cannot be null");
            }

            // Validate name (cannot be empty)
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                AddValidationError("Name is required");
            }

            // Validate string lengths
            if (!string.IsNullOrEmpty(entity.Name) && entity.Name.Length > 100)
            {
                AddValidationError("Name exceeds the maximum length of 100 characters");
            }

            if (!string.IsNullOrEmpty(entity.Vendor) && entity.Vendor.Length > 100)
            {
                AddValidationError("Vendor exceeds the maximum length of 100 characters");
            }

            if (!string.IsNullOrEmpty(entity.Model) && entity.Model.Length > 100)
            {
                AddValidationError("Model exceeds the maximum length of 100 characters");
            }

            if (!string.IsNullOrEmpty(entity.SerialNumber) && entity.SerialNumber.Length > 50)
            {
                AddValidationError("SerialNumber exceeds the maximum length of 50 characters");
            }

            if (!string.IsNullOrEmpty(entity.Type) && entity.Type.Length > 50)
            {
                AddValidationError("Type exceeds the maximum length of 50 characters");
            }

            // Validate IP address format if present
            if (!string.IsNullOrEmpty(entity.IpAddress) && !IPAddress.TryParse(entity.IpAddress, out _))
            {
                AddValidationError("Invalid IP address format");
            }

            // Validate MAC address format if present
            if (!string.IsNullOrEmpty(entity.MacAddress) && !_macAddressRegex.IsMatch(entity.MacAddress))
            {
                AddValidationError("Invalid MAC address format (expected format: xx:xx:xx:xx:xx:xx)");
            }

            // Validate PortId
            if (entity.PortId == Guid.Empty)
            {
                AddValidationError("PortId is required");
            }

            // Throw exception if validation errors
            ThrowValidationExceptionIfErrors();
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
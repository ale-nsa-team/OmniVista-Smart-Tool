using DatabaseHelper.Entities;
using System.Collections.Generic;
using System.Net;

namespace DatabaseHelper.Validation.Validators
{
    /// <summary>
    /// Validator for SwitchEntity
    /// </summary>
    public class SwitchValidator : BaseValidator<SwitchEntity>
    {
        /// <summary>
        /// Validates the SwitchEntity and returns validation results
        /// </summary>
        /// <param name="entity">The SwitchEntity to validate</param>
        /// <returns>A collection of validation results</returns>
        public override IEnumerable<ValidationResult> Validate(SwitchEntity entity)
        {
            var results = new List<ValidationResult>();

            if (entity == null)
            {
                results.Add(new ValidationResult("entity", "Switch entity cannot be null", ValidationSeverity.Critical));
                return results;
            }

            // Validate required fields
            ValidateRequired(results, entity.IpAddress, nameof(entity.IpAddress));

            // Validate IP address format
            if (!string.IsNullOrEmpty(entity.IpAddress) && !IPAddress.TryParse(entity.IpAddress, out _))
            {
                results.Add(new ValidationResult(nameof(entity.IpAddress), "Invalid IP address format"));
            }

            // Validate string lengths
            ValidateMaxLength(results, entity.Name, 100, nameof(entity.Name));
            ValidateMaxLength(results, entity.NetMask, 15, nameof(entity.NetMask));
            ValidateMaxLength(results, entity.DefaultGateway, 15, nameof(entity.DefaultGateway));
            ValidateMaxLength(results, entity.MacAddress, 17, nameof(entity.MacAddress));
            ValidateMaxLength(results, entity.Login, 50, nameof(entity.Login));
            ValidateMaxLength(results, entity.Password, 50, nameof(entity.Password));

            // Validate connection timeout
            if (entity.CnxTimeout < 0 || entity.CnxTimeout > 300)
            {
                results.Add(new ValidationResult(nameof(entity.CnxTimeout), "Connection timeout must be between 0 and 300 seconds"));
            }

            return results;
        }
    }
}
using DatabaseHelper.Entities;
using DatabaseHelper.Validation.Validators;
using System;
using System.Collections.Generic;

namespace DatabaseHelper.Validation
{
    /// <summary>
    /// Factory for creating entity validators
    /// </summary>
    public static class ValidatorFactory
    {
        private static readonly Dictionary<Type, object> _validators = new Dictionary<Type, object>();

        static ValidatorFactory()
        {
            // Register all validators
            RegisterValidator<SwitchEntity>(new SwitchValidator());
            RegisterValidator<ChassisEntity>(new ChassisValidator());
            RegisterValidator<PortEntity>(new PortValidator());
            RegisterValidator<EndpointDeviceEntity>(new EndpointDeviceValidator());
            RegisterValidator<SlotEntity>(new SlotValidator());
            RegisterValidator<DatabaseVersionEntity>(new DatabaseVersionValidator());
            RegisterValidator<PowerSupplyEntity>(new PowerSupplyValidator());
            RegisterValidator<TemperatureEntity>(new TemperatureValidator());
            RegisterValidator<CapabilityEntity>(new CapabilityValidator());
            RegisterValidator<SwitchDebugAppEntity>(new SwitchDebugAppValidator());
        }

        /// <summary>
        /// Registers a validator for a specific entity type
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="validator">The validator instance</param>
        public static void RegisterValidator<TEntity>(IValidator<TEntity> validator) where TEntity : class
        {
            _validators[typeof(TEntity)] = validator;
        }

        /// <summary>
        /// Gets a validator for a specific entity type
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <returns>The validator for the entity type</returns>
        /// <exception cref="ArgumentException">Thrown when no validator is registered for the entity type</exception>
        public static IValidator<TEntity> GetValidator<TEntity>() where TEntity : class
        {
            if (_validators.TryGetValue(typeof(TEntity), out var validator))
            {
                return (IValidator<TEntity>)validator;
            }

            throw new ArgumentException($"No validator registered for type {typeof(TEntity).Name}");
        }

        /// <summary>
        /// Checks if a validator is registered for a specific entity type
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <returns>True if a validator is registered, false otherwise</returns>
        public static bool HasValidator<TEntity>() where TEntity : class
        {
            return _validators.ContainsKey(typeof(TEntity));
        }

        /// <summary>
        /// Validates an entity
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The entity to validate</param>
        /// <returns>A collection of validation results</returns>
        /// <exception cref="ArgumentException">Thrown when no validator is registered for the entity type</exception>
        public static IEnumerable<ValidationResult> Validate<TEntity>(TEntity entity) where TEntity : class
        {
            return GetValidator<TEntity>().Validate(entity);
        }

        /// <summary>
        /// Checks if an entity is valid
        /// </summary>
        /// <typeparam name="TEntity">The entity type</typeparam>
        /// <param name="entity">The entity to validate</param>
        /// <returns>True if the entity is valid, false otherwise</returns>
        /// <exception cref="ArgumentException">Thrown when no validator is registered for the entity type</exception>
        public static bool IsValid<TEntity>(TEntity entity) where TEntity : class
        {
            return GetValidator<TEntity>().IsValid(entity);
        }
    }
}
using DatabaseHelper.Validation;
using System.Collections.Generic;

namespace DatabaseHelper.Validation.Validators
{
    /// <summary>
    /// Interface for entity validators
    /// </summary>
    /// <typeparam name="TEntity">The type of entity to validate</typeparam>
    public interface IValidator<TEntity> where TEntity : class
    {
        /// <summary>
        /// Validates the entity and returns validation results
        /// </summary>
        /// <param name="entity">The entity to validate</param>
        /// <returns>A collection of validation results</returns>
        IEnumerable<ValidationResult> Validate(TEntity entity);

        /// <summary>
        /// Validates the entity and throws an exception if invalid
        /// </summary>
        /// <param name="entity">The entity to validate</param>
        void ValidateAndThrow(TEntity entity);

        /// <summary>
        /// Checks if the entity is valid
        /// </summary>
        /// <param name="entity">The entity to validate</param>
        /// <returns>True if the entity is valid, false otherwise</returns>
        bool IsValid(TEntity entity);
    }
}
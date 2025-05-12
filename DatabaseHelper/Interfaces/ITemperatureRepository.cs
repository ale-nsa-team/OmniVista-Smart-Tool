using DatabaseHelper.Entities;
using System;
using System.Threading.Tasks;

namespace DatabaseHelper.Interfaces
{
    /// <summary>
    /// Repository interface for Temperature entities with specialized query methods
    /// </summary>
    public interface ITemperatureRepository : IRepository<TemperatureEntity>
    {
        /// <summary>
        /// Gets the temperature for a specific chassis
        /// </summary>
        /// <param name="chassisId">The ID of the chassis</param>
        /// <returns>The temperature entity or null if not found</returns>
        Task<TemperatureEntity> GetByChassisIdAsync(Guid chassisId);
    }
}
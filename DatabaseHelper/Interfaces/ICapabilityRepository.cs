using DatabaseHelper.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseHelper.Interfaces
{
    /// <summary>
    /// Repository interface for Capability entities with specialized query methods
    /// </summary>
    public interface ICapabilityRepository : IRepository<CapabilityEntity>
    {
        /// <summary>
        /// Gets all capabilities for a specific end point device
        /// </summary>
        /// <param name="deviceId">The ID of the end point device</param>
        /// <returns>A collection of capability entities</returns>
        Task<IEnumerable<CapabilityEntity>> GetByDeviceIdAsync(Guid deviceId);
    }
}
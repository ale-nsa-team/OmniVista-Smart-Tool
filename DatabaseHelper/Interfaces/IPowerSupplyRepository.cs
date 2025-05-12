using DatabaseHelper.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseHelper.Interfaces
{
    /// <summary>
    /// Repository interface for PowerSupply entities with specialized query methods
    /// </summary>
    public interface IPowerSupplyRepository : IRepository<PowerSupplyEntity>
    {
        /// <summary>
        /// Gets all power supplies for a specific chassis
        /// </summary>
        /// <param name="chassisId">The ID of the chassis</param>
        /// <returns>A collection of power supply entities</returns>
        Task<IEnumerable<PowerSupplyEntity>> GetByChassisIdAsync(Guid chassisId);
    }
}
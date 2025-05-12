using DatabaseHelper.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseHelper.Interfaces
{
    /// <summary>
    /// Repository interface for Chassis entities with specialized query methods
    /// </summary>
    public interface IChassisRepository : IRepository<ChassisEntity>
    {
        /// <summary>
        /// Gets all chassis belonging to a specific switch
        /// </summary>
        /// <param name="switchId">The ID of the switch</param>
        /// <returns>A collection of chassis entities</returns>
        Task<IEnumerable<ChassisEntity>> GetBySwitchIdAsync(Guid switchId);
    }
}
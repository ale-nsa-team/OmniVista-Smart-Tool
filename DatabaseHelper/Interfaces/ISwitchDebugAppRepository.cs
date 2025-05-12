using DatabaseHelper.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseHelper.Interfaces
{
    /// <summary>
    /// Repository interface for SwitchDebugApp entities with specialized query methods
    /// </summary>
    public interface ISwitchDebugAppRepository : IRepository<SwitchDebugAppEntity>
    {
        /// <summary>
        /// Gets all debug apps for a specific switch
        /// </summary>
        /// <param name="switchId">The ID of the switch</param>
        /// <returns>A collection of switch debug app entities</returns>
        Task<IEnumerable<SwitchDebugAppEntity>> GetBySwitchIdAsync(Guid switchId);
    }
}
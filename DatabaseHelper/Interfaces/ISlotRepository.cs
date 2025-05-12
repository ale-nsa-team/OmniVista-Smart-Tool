using DatabaseHelper.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseHelper.Interfaces
{
    /// <summary>
    /// Repository interface for Slot entities with specialized query methods
    /// </summary>
    public interface ISlotRepository : IRepository<SlotEntity>
    {
        /// <summary>
        /// Gets all slots belonging to a specific chassis
        /// </summary>
        /// <param name="chassisId">The ID of the chassis</param>
        /// <returns>A collection of slot entities</returns>
        Task<IEnumerable<SlotEntity>> GetByChassisIdAsync(Guid chassisId);
    }
}
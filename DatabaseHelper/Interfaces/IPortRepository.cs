using DatabaseHelper.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseHelper.Interfaces
{
    /// <summary>
    /// Repository interface for Port entities with specialized query methods
    /// </summary>
    public interface IPortRepository : IRepository<PortEntity>
    {
        /// <summary>
        /// Gets all ports belonging to a specific slot
        /// </summary>
        /// <param name="slotId">The ID of the slot</param>
        /// <returns>A collection of port entities</returns>
        Task<IEnumerable<PortEntity>> GetBySlotIdAsync(Guid slotId);
    }
}
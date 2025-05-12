using DatabaseHelper.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseHelper.Interfaces
{
    /// <summary>
    /// Repository interface for EndPointDevice entities with specialized query methods
    /// </summary>
    public interface IEndpointDeviceRepository : IRepository<EndpointDeviceEntity>
    {
        /// <summary>
        /// Gets all end point devices connected to a specific port
        /// </summary>
        /// <param name="portId">The ID of the port</param>
        /// <returns>A collection of end point device entities</returns>
        Task<IEnumerable<EndpointDeviceEntity>> GetByPortIdAsync(Guid portId);
    }
}
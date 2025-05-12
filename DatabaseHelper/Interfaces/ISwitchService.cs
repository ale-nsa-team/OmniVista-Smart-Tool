using DatabaseHelper.Entities;
using System;
using System.Threading.Tasks;

namespace DatabaseHelper.Interfaces
{
    public interface ISwitchService : IDisposable
    {
        /// <summary>
        /// Creates a new switch with its entire hierarchy of components
        /// </summary>
        /// <param name="switchEntity">The switch entity with all child components</param>
        /// <returns>The created switch entity</returns>
        Task<SwitchEntity> CreateSwitchWithHierarchyAsync(SwitchEntity switchEntity);

        /// <summary>
        /// Retrieves a switch with its complete hierarchy of components
        /// </summary>
        /// <param name="switchId">The ID of the switch to retrieve</param>
        /// <returns>The switch entity with all child components</returns>
        Task<SwitchEntity> GetSwitchWithHierarchyAsync(Guid switchId);

        /// <summary>
        /// Updates a switch and its entire hierarchy of components
        /// </summary>
        /// <param name="switchEntity">The switch entity with updated components</param>
        Task UpdateSwitchWithHierarchyAsync(SwitchEntity switchEntity);

        /// <summary>
        /// Deletes a switch and its entire hierarchy of components
        /// </summary>
        /// <param name="switchId">The ID of the switch to delete</param>
        Task DeleteSwitchWithHierarchyAsync(Guid switchId);
    }
}
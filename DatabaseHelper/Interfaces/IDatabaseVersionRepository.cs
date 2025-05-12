using DatabaseHelper.Entities;
using System.Threading.Tasks;

namespace DatabaseHelper.Interfaces
{
    /// <summary>
    /// Repository interface for DatabaseVersion entities with specialized query methods
    /// </summary>
    public interface IDatabaseVersionRepository : IRepository<DatabaseVersionEntity>
    {
        /// <summary>
        /// Gets the latest database version
        /// </summary>
        /// <returns>The latest database version entity or null if not found</returns>
        Task<DatabaseVersionEntity> GetLatestAsync();
    }
}
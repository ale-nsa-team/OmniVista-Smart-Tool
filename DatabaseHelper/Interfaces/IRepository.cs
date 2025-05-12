using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseHelper.Interfaces
{
    public interface IRepository<TEntity> where TEntity : class
    {
        Task<TEntity> GetByIdAsync(object id);

        Task<IEnumerable<TEntity>> GetAllAsync();

        Task<TEntity> AddAsync(TEntity entity);

        Task UpdateAsync(TEntity entity);

        Task DeleteAsync(object id);

        Task<bool> ExistsAsync(object id);
    }
}
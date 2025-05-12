using DatabaseHelper.Exceptions;
using DatabaseHelper.Interfaces;
using DatabaseHelper.Logging;
using DatabaseHelper.Validation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace DatabaseHelper.Repositories
{
    public abstract class BaseRepository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly SQLiteConnection _connection;
        protected readonly ILogger _logger;
        protected readonly string _entityName;

        protected BaseRepository(SQLiteConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _entityName = typeof(TEntity).Name;
            _logger = LoggerFactory.CreateLogger(GetType());
            _logger.Debug($"Repository for {_entityName} initialized");
        }

        public abstract Task<TEntity> GetByIdAsync(object id);

        public abstract Task<IEnumerable<TEntity>> GetAllAsync();

        public abstract Task<TEntity> AddAsync(TEntity entity);

        public abstract Task UpdateAsync(TEntity entity);

        public abstract Task DeleteAsync(object id);

        public abstract Task<bool> ExistsAsync(object id);

        /// <summary>
        /// Ensures the database connection is open
        /// </summary>
        protected void EnsureConnectionOpen()
        {
            if (_connection.State != ConnectionState.Open)
            {
                _logger.Debug($"Opening closed database connection for {_entityName} repository");
                _connection.Open();
            }
        }

        /// <summary>
        /// Validates the entity using the appropriate validator
        /// </summary>
        /// <param name="entity">The entity to validate</param>
        protected virtual void ValidateEntity(TEntity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), $"{_entityName} entity cannot be null");
            }

            if (ValidatorFactory.HasValidator<TEntity>())
            {
                try
                {
                    var validator = ValidatorFactory.GetValidator<TEntity>();
                    validator.ValidateAndThrow(entity);
                    _logger.Debug($"Entity of type {typeof(TEntity).Name} passed validation");
                }
                catch (ValidationException ex)
                {
                    _logger.Error($"Validation failed for {typeof(TEntity).Name}: {ex.Message}");
                    throw;
                }
            }
            else
            {
                _logger.Warn($"No validator registered for entity type {typeof(TEntity).Name}");
            }
        }

        protected async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, string operationName, string entityId = null)
        {
            try
            {
                _logger.Debug($"Executing {operationName} operation on {_entityName}" + (entityId != null ? $" with ID {entityId}" : ""));
                EnsureConnectionOpen();
                return await operation();
            }
            catch (SQLiteException ex)
            {
                _logger.Error($"SQLite error during {operationName} operation on {_entityName}" + (entityId != null ? $" with ID {entityId}" : ""), ex);

                // Handle specific SQLite error codes
                if (operationName == "Add" && (ex.Message.Contains("UNIQUE constraint failed") || ex.Message.Contains("unique constraint")))
                {
                    throw new DuplicateEntityException($"Entity with duplicate key already exists for {_entityName}", _entityName, ex);
                }
                else if (ex.Message.Contains("FOREIGN KEY constraint failed") || ex.Message.Contains("foreign key constraint"))
                {
                    throw new RepositoryException($"Foreign key constraint failed for {_entityName}" + (entityId != null ? $" with ID {entityId}" : ""), _entityName, operationName, ex);
                }
                else if ((operationName == "Delete" || operationName == "Update") && (ex.Message.Contains("no such row") || ex.Message.Contains("not found")))
                {
                    throw new RepositoryException($"Entity not found for {_entityName}" + (entityId != null ? $" with ID {entityId}" : ""), _entityName, operationName, ex);
                }

                throw new RepositoryException($"Database error during {operationName} operation on {_entityName}" + (entityId != null ? $" with ID {entityId}" : ""), _entityName, operationName, ex);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error during {operationName} operation on {_entityName}" + (entityId != null ? $" with ID {entityId}" : ""), ex);
                throw new RepositoryException($"Error during {operationName} operation on {_entityName}" + (entityId != null ? $" with ID {entityId}" : ""), _entityName, operationName, ex);
            }
        }

        protected async Task ExecuteWithErrorHandlingAsync(Func<Task> operation, string operationName, string entityId = null)
        {
            try
            {
                _logger.Debug($"Executing {operationName} operation on {_entityName}" + (entityId != null ? $" with ID {entityId}" : ""));
                EnsureConnectionOpen();
                await operation();
            }
            catch (SQLiteException ex)
            {
                _logger.Error($"SQLite error during {operationName} operation on {_entityName}" + (entityId != null ? $" with ID {entityId}" : ""), ex);

                // Handle specific SQLite error codes
                if (operationName == "Add" && (ex.Message.Contains("UNIQUE constraint failed") || ex.Message.Contains("unique constraint")))
                {
                    throw new DuplicateEntityException($"Entity with duplicate key already exists for {_entityName}", _entityName, ex);
                }
                else if (ex.Message.Contains("FOREIGN KEY constraint failed") || ex.Message.Contains("foreign key constraint"))
                {
                    throw new RepositoryException($"Foreign key constraint failed for {_entityName}" + (entityId != null ? $" with ID {entityId}" : ""), _entityName, operationName, ex);
                }
                else if ((operationName == "Delete" || operationName == "Update") && (ex.Message.Contains("no such row") || ex.Message.Contains("not found")))
                {
                    throw new RepositoryException($"Entity not found for {_entityName}" + (entityId != null ? $" with ID {entityId}" : ""), _entityName, operationName, ex);
                }

                throw new RepositoryException($"Database error during {operationName} operation on {_entityName}" + (entityId != null ? $" with ID {entityId}" : ""), _entityName, operationName, ex);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error during {operationName} operation on {_entityName}" + (entityId != null ? $" with ID {entityId}" : ""), ex);
                throw new RepositoryException($"Error during {operationName} operation on {_entityName}" + (entityId != null ? $" with ID {entityId}" : ""), _entityName, operationName, ex);
            }
        }

        /// <summary>
        /// Converts a nullable value to a proper SQLite parameter value
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="value">The value to convert</param>
        /// <returns>The converted value or DBNull.Value if null</returns>
        protected object ToDbValue<T>(T value)
        {
            return value == null ? DBNull.Value : (object)value;
        }

        /// <summary>
        /// Converts a boolean value to an integer for SQLite
        /// </summary>
        /// <param name="value">The boolean value</param>
        /// <returns>1 for true, 0 for false</returns>
        protected int BoolToInt(bool value)
        {
            return value ? 1 : 0;
        }
    }
}
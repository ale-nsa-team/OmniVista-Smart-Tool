using DatabaseHelper.Constants.Queries;
using DatabaseHelper.Entities;
using DatabaseHelper.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace DatabaseHelper.Repositories
{
    public class DatabaseVersionRepository : BaseRepository<DatabaseVersionEntity>, IDatabaseVersionRepository
    {
        public DatabaseVersionRepository(SQLiteConnection connection) : base(connection)
        {
        }

        public override async Task<DatabaseVersionEntity> GetByIdAsync(object id)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var versionId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = DatabaseVersionQueries.GetById;
                    command.Parameters.AddWithValue("@Id", versionId.ToString());

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return ParseFromReader(reader);
                        }
                    }
                }
                return null;
            }, "GetById", id.ToString());
        }

        public override async Task<IEnumerable<DatabaseVersionEntity>> GetAllAsync()
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var versions = new List<DatabaseVersionEntity>();
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = DatabaseVersionQueries.GetAll;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            versions.Add(ParseFromReader(reader));
                        }
                    }
                }
                return versions;
            }, "GetAll");
        }

        public async Task<DatabaseVersionEntity> GetLatestAsync()
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = DatabaseVersionQueries.GetLatest;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return ParseFromReader(reader);
                        }
                    }
                }
                return null;
            }, "GetLatest");
        }

        public override async Task<DatabaseVersionEntity> AddAsync(DatabaseVersionEntity entity)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                ValidateEntity(entity);

                if (entity.Id == Guid.Empty)
                {
                    entity.Id = Guid.NewGuid();
                }

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = DatabaseVersionQueries.Insert;
                    AddParameters(command, entity);
                    await command.ExecuteNonQueryAsync();
                    return entity;
                }
            }, "Add");
        }

        public override async Task UpdateAsync(DatabaseVersionEntity entity)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                ValidateEntity(entity);

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = DatabaseVersionQueries.Update;
                    AddParameters(command, entity);
                    await command.ExecuteNonQueryAsync();
                }
            }, "Update", entity.Id.ToString());
        }

        public override async Task DeleteAsync(object id)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var versionId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = DatabaseVersionQueries.Delete;
                    command.Parameters.AddWithValue("@Id", versionId.ToString());
                    await command.ExecuteNonQueryAsync();
                }
            }, "Delete", id.ToString());
        }

        public override async Task<bool> ExistsAsync(object id)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var versionId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = DatabaseVersionQueries.Exists;
                    command.Parameters.AddWithValue("@Id", versionId.ToString());
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
            }, "Exists", id.ToString());
        }

        private void AddParameters(SQLiteCommand command, DatabaseVersionEntity entity)
        {
            command.Parameters.AddWithValue("@Id", entity.Id.ToString());
            command.Parameters.AddWithValue("@Version", entity.Version);
            command.Parameters.AddWithValue("@ReleaseDate", entity.ReleaseDate);
            command.Parameters.AddWithValue("@Description", entity.Description);
        }

        private DatabaseVersionEntity ParseFromReader(DbDataReader reader)
        {
            return new DatabaseVersionEntity
            {
                Id = Guid.Parse(reader["Id"].ToString()),
                Version = reader["Version"].ToString(),
                ReleaseDate = Convert.ToDateTime(reader["ReleaseDate"]),
                Description = reader["Description"].ToString()
            };
        }
    }
}
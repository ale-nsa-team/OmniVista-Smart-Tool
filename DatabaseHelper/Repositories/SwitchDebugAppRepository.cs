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
    public class SwitchDebugAppRepository : BaseRepository<SwitchDebugAppEntity>, ISwitchDebugAppRepository
    {
        public SwitchDebugAppRepository(SQLiteConnection connection) : base(connection)
        {
        }

        public override async Task<SwitchDebugAppEntity> GetByIdAsync(object id)
        {
            var debugAppId = (Guid)id;
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = SwitchDebugAppQueries.GetById;
                command.Parameters.AddWithValue("@Id", debugAppId.ToString());

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return ParseFromReader(reader);
                    }
                }
            }
            return null;
        }

        public override async Task<IEnumerable<SwitchDebugAppEntity>> GetAllAsync()
        {
            var debugApps = new List<SwitchDebugAppEntity>();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = SwitchDebugAppQueries.GetAll;
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        debugApps.Add(ParseFromReader(reader));
                    }
                }
            }
            return debugApps;
        }

        public override async Task<SwitchDebugAppEntity> AddAsync(SwitchDebugAppEntity entity)
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
                    command.CommandText = SwitchDebugAppQueries.Insert;
                    AddParameters(command, entity);
                    await command.ExecuteNonQueryAsync();
                    return entity;
                }
            }, "Add");
        }

        public override async Task UpdateAsync(SwitchDebugAppEntity entity)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                ValidateEntity(entity);

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = SwitchDebugAppQueries.Update;
                    AddParameters(command, entity);
                    await command.ExecuteNonQueryAsync();
                }
            }, "Update", entity.Id.ToString());
        }

        public override async Task DeleteAsync(object id)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var debugAppId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = SwitchDebugAppQueries.Delete;
                    command.Parameters.AddWithValue("@Id", debugAppId.ToString());
                    await command.ExecuteNonQueryAsync();
                }
            }, "Delete", id.ToString());
        }

        public override async Task<bool> ExistsAsync(object id)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var debugAppId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = SwitchDebugAppQueries.Exists;
                    command.Parameters.AddWithValue("@Id", debugAppId.ToString());
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
            }, "Exists", id.ToString());
        }

        public async Task<IEnumerable<SwitchDebugAppEntity>> GetBySwitchIdAsync(Guid switchId)
        {
            var debugApps = new List<SwitchDebugAppEntity>();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = SwitchDebugAppQueries.GetBySwitchId;
                command.Parameters.AddWithValue("@SwitchId", switchId.ToString());

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        debugApps.Add(ParseFromReader(reader));
                    }
                }
            }
            return debugApps;
        }

        private void AddParameters(SQLiteCommand command, SwitchDebugAppEntity entity)
        {
            command.Parameters.AddWithValue("@Id", entity.Id.ToString());
            command.Parameters.AddWithValue("@Name", entity.Name);
            command.Parameters.AddWithValue("@AppId", entity.AppId);
            command.Parameters.AddWithValue("@AppIndex", entity.AppIndex);
            command.Parameters.AddWithValue("@NbSubApp", entity.NbSubApp);
            command.Parameters.AddWithValue("@DebugLevel", entity.DebugLevel);
            command.Parameters.AddWithValue("@SwitchId", entity.SwitchId.ToString());
        }

        private SwitchDebugAppEntity ParseFromReader(DbDataReader reader)
        {
            return new SwitchDebugAppEntity
            {
                Id = Guid.Parse(reader["Id"].ToString()),
                Name = reader["Name"].ToString(),
                AppId = reader["AppId"].ToString(),
                AppIndex = reader["AppIndex"].ToString(),
                NbSubApp = reader["NbSubApp"].ToString(),
                DebugLevel = Convert.ToInt32(reader["DebugLevel"]),
                SwitchId = Guid.Parse(reader["SwitchId"].ToString())
            };
        }
    }
}
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
    public class CapabilityRepository : BaseRepository<CapabilityEntity>, ICapabilityRepository
    {
        public CapabilityRepository(SQLiteConnection connection) : base(connection)
        {
        }

        public override async Task<CapabilityEntity> GetByIdAsync(object id)
        {
            var capabilityId = (Guid)id;
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = CapabilityQueries.GetById;
                command.Parameters.AddWithValue("@Id", capabilityId.ToString());

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

        public override async Task<IEnumerable<CapabilityEntity>> GetAllAsync()
        {
            var capabilities = new List<CapabilityEntity>();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = CapabilityQueries.GetAll;
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        capabilities.Add(ParseFromReader(reader));
                    }
                }
            }
            return capabilities;
        }

        public override async Task<CapabilityEntity> AddAsync(CapabilityEntity entity)
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
                    command.CommandText = CapabilityQueries.Insert;
                    AddParameters(command, entity);
                    await command.ExecuteNonQueryAsync();
                    return entity;
                }
            }, "Add");
        }

        public override async Task UpdateAsync(CapabilityEntity entity)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                ValidateEntity(entity);

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = CapabilityQueries.Update;
                    AddParameters(command, entity);
                    await command.ExecuteNonQueryAsync();
                }
            }, "Update", entity.Id.ToString());
        }

        public override async Task DeleteAsync(object id)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var capabilityId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = CapabilityQueries.Delete;
                    command.Parameters.AddWithValue("@Id", capabilityId.ToString());
                    await command.ExecuteNonQueryAsync();
                }
            }, "Delete", id.ToString());
        }

        public override async Task<bool> ExistsAsync(object id)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var capabilityId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = CapabilityQueries.Exists;
                    command.Parameters.AddWithValue("@Id", capabilityId.ToString());
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
            }, "Exists", id.ToString());
        }

        public async Task<IEnumerable<CapabilityEntity>> GetByDeviceIdAsync(Guid deviceId)
        {
            var capabilities = new List<CapabilityEntity>();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = CapabilityQueries.GetByDeviceId;
                command.Parameters.AddWithValue("@DeviceId", deviceId.ToString());

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        capabilities.Add(ParseFromReader(reader));
                    }
                }
            }
            return capabilities;
        }

        private void AddParameters(SQLiteCommand command, CapabilityEntity entity)
        {
            command.Parameters.AddWithValue("@Id", entity.Id.ToString());
            command.Parameters.AddWithValue("@Value", entity.Value);
            command.Parameters.AddWithValue("@EndPointDeviceId", entity.EndPointDeviceId.ToString());
        }

        private CapabilityEntity ParseFromReader(DbDataReader reader)
        {
            return new CapabilityEntity
            {
                Id = Guid.Parse(reader["Id"].ToString()),
                Value = reader["Value"].ToString(),
                EndPointDeviceId = Guid.Parse(reader["EndPointDeviceId"].ToString())
            };
        }
    }
}
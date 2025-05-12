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
    public class TemperatureRepository : BaseRepository<TemperatureEntity>, ITemperatureRepository
    {
        public TemperatureRepository(SQLiteConnection connection) : base(connection)
        {
        }

        public override async Task<TemperatureEntity> GetByIdAsync(object id)
        {
            var temperatureId = (Guid)id;
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = TemperatureQueries.GetById;
                command.Parameters.AddWithValue("@Id", temperatureId.ToString());

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

        public override async Task<IEnumerable<TemperatureEntity>> GetAllAsync()
        {
            var temperatures = new List<TemperatureEntity>();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = TemperatureQueries.GetAll;
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        temperatures.Add(ParseFromReader(reader));
                    }
                }
            }
            return temperatures;
        }

        public override async Task<TemperatureEntity> AddAsync(TemperatureEntity entity)
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
                    command.CommandText = TemperatureQueries.Insert;
                    AddParameters(command, entity);
                    await command.ExecuteNonQueryAsync();
                    return entity;
                }
            }, "Add");
        }

        public override async Task UpdateAsync(TemperatureEntity entity)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                ValidateEntity(entity);

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = TemperatureQueries.Update;
                    AddParameters(command, entity);
                    await command.ExecuteNonQueryAsync();
                }
            }, "Update", entity.Id.ToString());
        }

        public override async Task DeleteAsync(object id)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var temperatureId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = TemperatureQueries.Delete;
                    command.Parameters.AddWithValue("@Id", temperatureId.ToString());
                    await command.ExecuteNonQueryAsync();
                }
            }, "Delete", id.ToString());
        }

        public override async Task<bool> ExistsAsync(object id)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var temperatureId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = TemperatureQueries.Exists;
                    command.Parameters.AddWithValue("@Id", temperatureId.ToString());
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
            }, "Exists", id.ToString());
        }

        public async Task<TemperatureEntity> GetByChassisIdAsync(Guid chassisId)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = TemperatureQueries.GetByChassisId;
                command.Parameters.AddWithValue("@ChassisId", chassisId.ToString());

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

        private void AddParameters(SQLiteCommand command, TemperatureEntity entity)
        {
            command.Parameters.AddWithValue("@Id", entity.Id.ToString());
            command.Parameters.AddWithValue("@Device", entity.Device);
            command.Parameters.AddWithValue("@Current", entity.Current);
            command.Parameters.AddWithValue("@Range", entity.Range);
            command.Parameters.AddWithValue("@Threshold", entity.Threshold);
            command.Parameters.AddWithValue("@Danger", entity.Danger);
            command.Parameters.AddWithValue("@Status", entity.Status);
            command.Parameters.AddWithValue("@ChassisId", entity.ChassisId.ToString());
        }

        private TemperatureEntity ParseFromReader(DbDataReader reader)
        {
            return new TemperatureEntity
            {
                Id = Guid.Parse(reader["Id"].ToString()),
                Device = reader["Device"].ToString(),
                Current = Convert.ToInt32(reader["Current"]),
                Range = reader["Range"].ToString(),
                Threshold = Convert.ToInt32(reader["Threshold"]),
                Danger = Convert.ToInt32(reader["Danger"]),
                Status = reader["Status"].ToString(),
                ChassisId = Guid.Parse(reader["ChassisId"].ToString())
            };
        }
    }
}
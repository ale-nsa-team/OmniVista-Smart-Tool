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
    public class PowerSupplyRepository : BaseRepository<PowerSupplyEntity>, IPowerSupplyRepository
    {
        public PowerSupplyRepository(SQLiteConnection connection) : base(connection)
        {
        }

        public override async Task<PowerSupplyEntity> GetByIdAsync(object id)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var powerSupplyId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = PowerSupplyQueries.GetById;
                    command.Parameters.AddWithValue("@Id", powerSupplyId.ToString());

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

        public override async Task<IEnumerable<PowerSupplyEntity>> GetAllAsync()
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var powerSupplies = new List<PowerSupplyEntity>();
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = PowerSupplyQueries.GetAll;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            powerSupplies.Add(ParseFromReader(reader));
                        }
                    }
                }
                return powerSupplies;
            }, "GetAll");
        }

        public override async Task<PowerSupplyEntity> AddAsync(PowerSupplyEntity entity)
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
                    command.CommandText = PowerSupplyQueries.Insert;
                    AddParameters(command, entity);
                    await command.ExecuteNonQueryAsync();
                    return entity;
                }
            }, "Add");
        }

        public override async Task UpdateAsync(PowerSupplyEntity entity)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                ValidateEntity(entity);

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = PowerSupplyQueries.Update;
                    AddParameters(command, entity);
                    await command.ExecuteNonQueryAsync();
                }
            }, "Update", entity.Id.ToString());
        }

        public override async Task DeleteAsync(object id)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var powerSupplyId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = PowerSupplyQueries.Delete;
                    command.Parameters.AddWithValue("@Id", powerSupplyId.ToString());
                    await command.ExecuteNonQueryAsync();
                }
            }, "Delete", id.ToString());
        }

        public override async Task<bool> ExistsAsync(object id)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var powerSupplyId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = PowerSupplyQueries.Exists;
                    command.Parameters.AddWithValue("@Id", powerSupplyId.ToString());
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
            }, "Exists", id.ToString());
        }

        public async Task<IEnumerable<PowerSupplyEntity>> GetByChassisIdAsync(Guid chassisId)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var powerSupplies = new List<PowerSupplyEntity>();
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = PowerSupplyQueries.GetByChassisId;
                    command.Parameters.AddWithValue("@ChassisId", chassisId.ToString());

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            powerSupplies.Add(ParseFromReader(reader));
                        }
                    }
                }
                return powerSupplies;
            }, "GetByChassisId", chassisId.ToString());
        }

        private void AddParameters(SQLiteCommand command, PowerSupplyEntity entity)
        {
            command.Parameters.AddWithValue("@Id", entity.Id.ToString());
            command.Parameters.AddWithValue("@Name", entity.Name);
            command.Parameters.AddWithValue("@Model", entity.Model);
            command.Parameters.AddWithValue("@Type", entity.Type);
            command.Parameters.AddWithValue("@Location", entity.Location);
            command.Parameters.AddWithValue("@Description", entity.Description);
            command.Parameters.AddWithValue("@PowerProvision", entity.PowerProvision);
            command.Parameters.AddWithValue("@Status", entity.Status);
            command.Parameters.AddWithValue("@PartNumber", entity.PartNumber);
            command.Parameters.AddWithValue("@HardwareRevision", entity.HardwareRevision);
            command.Parameters.AddWithValue("@SerialNumber", entity.SerialNumber);
            command.Parameters.AddWithValue("@ChassisId", entity.ChassisId.ToString());
        }

        private PowerSupplyEntity ParseFromReader(DbDataReader reader)
        {
            return new PowerSupplyEntity
            {
                Id = Guid.Parse(reader["Id"].ToString()),
                Name = reader["Name"].ToString(),
                Model = reader["Model"].ToString(),
                Type = reader["Type"].ToString(),
                Location = reader["Location"].ToString(),
                Description = reader["Description"].ToString(),
                PowerProvision = reader["PowerProvision"].ToString(),
                Status = reader["Status"].ToString(),
                PartNumber = reader["PartNumber"].ToString(),
                HardwareRevision = reader["HardwareRevision"].ToString(),
                SerialNumber = reader["SerialNumber"].ToString(),
                ChassisId = Guid.Parse(reader["ChassisId"].ToString())
            };
        }
    }
}
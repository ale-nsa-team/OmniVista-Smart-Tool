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
    public class ChassisRepository : BaseRepository<ChassisEntity>, IChassisRepository
    {
        public ChassisRepository(SQLiteConnection connection) : base(connection)
        {
        }

        public override async Task<ChassisEntity> GetByIdAsync(object id)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var chassisId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = ChassisQueries.GetById;
                    command.Parameters.AddWithValue("@Id", chassisId.ToString());

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

        public override async Task<IEnumerable<ChassisEntity>> GetAllAsync()
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var chassisList = new List<ChassisEntity>();
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = ChassisQueries.GetAll;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            chassisList.Add(ParseFromReader(reader));
                        }
                    }
                }
                return chassisList;
            }, "GetAll");
        }

        public override async Task<ChassisEntity> AddAsync(ChassisEntity entity)
        {
            ValidateEntity(entity);
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (entity.Id == Guid.Empty)
                {
                    entity.Id = Guid.NewGuid();
                }

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = ChassisQueries.Insert;
                    AddParameters(command, entity);
                    await command.ExecuteNonQueryAsync();
                    return entity;
                }
            }, "Add", entity.Id.ToString());
        }

        public override async Task UpdateAsync(ChassisEntity entity)
        {
            ValidateEntity(entity);
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = ChassisQueries.Update;
                    AddParameters(command, entity);
                    await command.ExecuteNonQueryAsync();
                }
            }, "Update", entity.Id.ToString());
        }

        public override async Task DeleteAsync(object id)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var chassisId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = ChassisQueries.Delete;
                    command.Parameters.AddWithValue("@Id", chassisId.ToString());
                    await command.ExecuteNonQueryAsync();
                }
            }, "Delete", id.ToString());
        }

        public override async Task<bool> ExistsAsync(object id)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var chassisId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = ChassisQueries.Exists;
                    command.Parameters.AddWithValue("@Id", chassisId.ToString());
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
            }, "Exists", id.ToString());
        }

        public async Task<IEnumerable<ChassisEntity>> GetBySwitchIdAsync(Guid switchId)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var chassis = new List<ChassisEntity>();
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = ChassisQueries.GetBySwitchId;
                    command.Parameters.AddWithValue("@SwitchId", switchId.ToString());

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            chassis.Add(ParseFromReader(reader));
                        }
                    }
                }
                return chassis;
            }, "GetBySwitchId", switchId.ToString());
        }

        private void AddParameters(SQLiteCommand command, ChassisEntity entity)
        {
            command.Parameters.AddWithValue("@Id", entity.Id.ToString());
            command.Parameters.AddWithValue("@Number", entity.Number);
            command.Parameters.AddWithValue("@Model", entity.Model);
            command.Parameters.AddWithValue("@Type", entity.Type);
            command.Parameters.AddWithValue("@IsMaster", entity.IsMaster);
            command.Parameters.AddWithValue("@AdminStatus", entity.AdminStatus);
            command.Parameters.AddWithValue("@OperationalStatus", entity.OperationalStatus);
            command.Parameters.AddWithValue("@Status", entity.Status);
            command.Parameters.AddWithValue("@PowerBudget", entity.PowerBudget);
            command.Parameters.AddWithValue("@PowerConsumed", entity.PowerConsumed);
            command.Parameters.AddWithValue("@PowerRemaining", entity.PowerRemaining);
            command.Parameters.AddWithValue("@SerialNumber", entity.SerialNumber);
            command.Parameters.AddWithValue("@PartNumber", entity.PartNumber);
            command.Parameters.AddWithValue("@HardwareRevision", entity.HardwareRevision);
            command.Parameters.AddWithValue("@MacAddress", entity.MacAddress);
            command.Parameters.AddWithValue("@SwitchTemperature", entity.SwitchTemperature);
            command.Parameters.AddWithValue("@SupportsPoE", entity.SupportsPoE);
            command.Parameters.AddWithValue("@Fpga", entity.Fpga);
            command.Parameters.AddWithValue("@Cpld", entity.Cpld);
            command.Parameters.AddWithValue("@Uboot", entity.Uboot);
            command.Parameters.AddWithValue("@Onie", entity.Onie);
            command.Parameters.AddWithValue("@Cpu", entity.Cpu);
            command.Parameters.AddWithValue("@FlashSize", entity.FlashSize);
            command.Parameters.AddWithValue("@FlashUsage", entity.FlashUsage);
            command.Parameters.AddWithValue("@FlashSizeUsed", entity.FlashSizeUsed);
            command.Parameters.AddWithValue("@FlashSizeFree", entity.FlashSizeFree);
            command.Parameters.AddWithValue("@FreeFlash", entity.FreeFlash);
            command.Parameters.AddWithValue("@SwitchId", entity.SwitchId.ToString());
        }

        private ChassisEntity ParseFromReader(DbDataReader reader)
        {
            return new ChassisEntity
            {
                Id = Guid.Parse(reader["Id"].ToString()),
                Number = Convert.ToInt32(reader["Number"]),
                Model = reader["Model"].ToString(),
                Type = reader["Type"].ToString(),
                IsMaster = Convert.ToBoolean(reader["IsMaster"]),
                AdminStatus = reader["AdminStatus"].ToString(),
                OperationalStatus = reader["OperationalStatus"].ToString(),
                Status = reader["Status"].ToString(),
                PowerBudget = Convert.ToDouble(reader["PowerBudget"]),
                PowerConsumed = Convert.ToDouble(reader["PowerConsumed"]),
                PowerRemaining = Convert.ToDouble(reader["PowerRemaining"]),
                SerialNumber = reader["SerialNumber"].ToString(),
                PartNumber = reader["PartNumber"].ToString(),
                HardwareRevision = reader["HardwareRevision"].ToString(),
                MacAddress = reader["MacAddress"].ToString(),
                SwitchTemperature = reader["SwitchTemperature"].ToString(),
                SupportsPoE = Convert.ToBoolean(reader["SupportsPoE"]),
                Fpga = reader["Fpga"].ToString(),
                Cpld = reader["Cpld"].ToString(),
                Uboot = reader["Uboot"].ToString(),
                Onie = reader["Onie"].ToString(),
                Cpu = Convert.ToInt32(reader["Cpu"]),
                FlashSize = reader["FlashSize"].ToString(),
                FlashUsage = reader["FlashUsage"].ToString(),
                FlashSizeUsed = reader["FlashSizeUsed"].ToString(),
                FlashSizeFree = reader["FlashSizeFree"].ToString(),
                FreeFlash = reader["FreeFlash"].ToString(),
                SwitchId = Guid.Parse(reader["SwitchId"].ToString())
            };
        }
    }
}
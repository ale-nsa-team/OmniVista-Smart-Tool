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
    public class SlotRepository : BaseRepository<SlotEntity>, ISlotRepository
    {
        public SlotRepository(SQLiteConnection connection) : base(connection)
        {
        }

        public override async Task<SlotEntity> GetByIdAsync(object id)
        {
            var slotId = (Guid)id;
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = SlotQueries.GetById;
                command.Parameters.AddWithValue("@Id", slotId.ToString());

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

        public override async Task<IEnumerable<SlotEntity>> GetAllAsync()
        {
            var slots = new List<SlotEntity>();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = SlotQueries.GetAll;
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        slots.Add(ParseFromReader(reader));
                    }
                }
            }
            return slots;
        }

        public override async Task<SlotEntity> AddAsync(SlotEntity entity)
        {
            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = SlotQueries.Insert;
                AddParameters(command, entity);
                await command.ExecuteNonQueryAsync();
                return entity;
            }
        }

        public override async Task UpdateAsync(SlotEntity entity)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = SlotQueries.Update;
                AddParameters(command, entity);
                await command.ExecuteNonQueryAsync();
            }
        }

        public override async Task DeleteAsync(object id)
        {
            var slotId = (Guid)id;
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = SlotQueries.Delete;
                command.Parameters.AddWithValue("@Id", slotId.ToString());
                await command.ExecuteNonQueryAsync();
            }
        }

        public override async Task<bool> ExistsAsync(object id)
        {
            var slotId = (Guid)id;
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = SlotQueries.Exists;
                command.Parameters.AddWithValue("@Id", slotId.ToString());
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
        }

        public async Task<IEnumerable<SlotEntity>> GetByChassisIdAsync(Guid chassisId)
        {
            var slots = new List<SlotEntity>();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = SlotQueries.GetByChassisId;
                command.Parameters.AddWithValue("@ChassisId", chassisId.ToString());

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        slots.Add(ParseFromReader(reader));
                    }
                }
            }
            return slots;
        }

        private void AddParameters(SQLiteCommand command, SlotEntity entity)
        {
            command.Parameters.AddWithValue("@Id", entity.Id.ToString());
            command.Parameters.AddWithValue("@Number", entity.Number);
            command.Parameters.AddWithValue("@Name", entity.Name);
            command.Parameters.AddWithValue("@Model", entity.Model);
            command.Parameters.AddWithValue("@NbPorts", entity.NbPorts);
            command.Parameters.AddWithValue("@NbPoePorts", entity.NbPoePorts);
            command.Parameters.AddWithValue("@PoeStatus", entity.PoeStatus);
            command.Parameters.AddWithValue("@Power", entity.Power);
            command.Parameters.AddWithValue("@Budget", entity.Budget);
            command.Parameters.AddWithValue("@Threshold", entity.Threshold);
            command.Parameters.AddWithValue("@Is8023btSupport", entity.Is8023btSupport);
            command.Parameters.AddWithValue("@IsPoeModeEnable", entity.IsPoeModeEnable);
            command.Parameters.AddWithValue("@IsPriorityDisconnect", entity.IsPriorityDisconnect);
            command.Parameters.AddWithValue("@FPoE", entity.FPoE);
            command.Parameters.AddWithValue("@PPoE", entity.PPoE);
            command.Parameters.AddWithValue("@PowerClassDetection", entity.PowerClassDetection);
            command.Parameters.AddWithValue("@IsHiResDetection", entity.IsHiResDetection);
            command.Parameters.AddWithValue("@IsInitialized", entity.IsInitialized);
            command.Parameters.AddWithValue("@SupportsPoE", entity.SupportsPoE);
            command.Parameters.AddWithValue("@IsMaster", entity.IsMaster);
            command.Parameters.AddWithValue("@ChassisId", entity.ChassisId.ToString());
        }

        private SlotEntity ParseFromReader(DbDataReader reader)
        {
            return new SlotEntity
            {
                Id = Guid.Parse(reader["Id"].ToString()),
                Number = Convert.ToInt32(reader["Number"]),
                Name = reader["Name"].ToString(),
                Model = reader["Model"].ToString(),
                NbPorts = Convert.ToInt32(reader["NbPorts"]),
                NbPoePorts = Convert.ToInt32(reader["NbPoePorts"]),
                PoeStatus = reader["PoeStatus"].ToString(),
                Power = Convert.ToDouble(reader["Power"]),
                Budget = Convert.ToDouble(reader["Budget"]),
                Threshold = Convert.ToDouble(reader["Threshold"]),
                Is8023btSupport = Convert.ToBoolean(reader["Is8023btSupport"]),
                IsPoeModeEnable = Convert.ToBoolean(reader["IsPoeModeEnable"]),
                IsPriorityDisconnect = Convert.ToBoolean(reader["IsPriorityDisconnect"]),
                FPoE = reader["FPoE"].ToString(),
                PPoE = reader["PPoE"].ToString(),
                PowerClassDetection = reader["PowerClassDetection"].ToString(),
                IsHiResDetection = Convert.ToBoolean(reader["IsHiResDetection"]),
                IsInitialized = Convert.ToBoolean(reader["IsInitialized"]),
                SupportsPoE = Convert.ToBoolean(reader["SupportsPoE"]),
                IsMaster = Convert.ToBoolean(reader["IsMaster"]),
                ChassisId = Guid.Parse(reader["ChassisId"].ToString())
            };
        }
    }
}
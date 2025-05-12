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
    public class PortRepository : BaseRepository<PortEntity>, IPortRepository
    {
        public PortRepository(SQLiteConnection connection) : base(connection)
        {
        }

        public override async Task<PortEntity> GetByIdAsync(object id)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var portId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = PortQueries.GetById;
                    command.Parameters.AddWithValue("@Id", portId.ToString());

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

        public override async Task<IEnumerable<PortEntity>> GetAllAsync()
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var ports = new List<PortEntity>();
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = PortQueries.GetAll;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            ports.Add(ParseFromReader(reader));
                        }
                    }
                }
                return ports;
            }, "GetAll");
        }

        public override async Task<PortEntity> AddAsync(PortEntity entity)
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
                    command.CommandText = PortQueries.Insert;
                    AddParameters(command, entity);
                    await command.ExecuteNonQueryAsync();
                    return entity;
                }
            }, "Add", entity.Id.ToString());
        }

        public override async Task UpdateAsync(PortEntity entity)
        {
            ValidateEntity(entity);
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = PortQueries.Update;
                    AddParameters(command, entity);
                    await command.ExecuteNonQueryAsync();
                }
            }, "Update", entity.Id.ToString());
        }

        public override async Task DeleteAsync(object id)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var portId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = PortQueries.Delete;
                    command.Parameters.AddWithValue("@Id", portId.ToString());
                    await command.ExecuteNonQueryAsync();
                }
            }, "Delete", id.ToString());
        }

        public override async Task<bool> ExistsAsync(object id)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var portId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = PortQueries.Exists;
                    command.Parameters.AddWithValue("@Id", portId.ToString());
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
            }, "Exists", id.ToString());
        }

        public async Task<IEnumerable<PortEntity>> GetBySlotIdAsync(Guid slotId)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var ports = new List<PortEntity>();
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = PortQueries.GetBySlotId;
                    command.Parameters.AddWithValue("@SlotId", slotId.ToString());

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            ports.Add(ParseFromReader(reader));
                        }
                    }
                }
                return ports;
            }, "GetBySlotId", slotId.ToString());
        }

        private void AddParameters(SQLiteCommand command, PortEntity entity)
        {
            command.Parameters.AddWithValue("@Id", entity.Id.ToString());
            command.Parameters.AddWithValue("@Number", entity.Number);
            command.Parameters.AddWithValue("@Name", entity.Name);
            command.Parameters.AddWithValue("@PortIndex", entity.PortIndex);
            command.Parameters.AddWithValue("@Poe", entity.Poe);
            command.Parameters.AddWithValue("@Power", entity.Power);
            command.Parameters.AddWithValue("@MaxPower", entity.MaxPower);
            command.Parameters.AddWithValue("@Status", entity.Status);
            command.Parameters.AddWithValue("@IsPoeON", entity.IsPoeON);
            command.Parameters.AddWithValue("@PriorityLevel", entity.PriorityLevel);
            command.Parameters.AddWithValue("@IsUplink", entity.IsUplink);
            command.Parameters.AddWithValue("@IsLldpMdi", entity.IsLldpMdi);
            command.Parameters.AddWithValue("@IsLldpExtMdi", entity.IsLldpExtMdi);
            command.Parameters.AddWithValue("@IsVfLink", entity.IsVfLink);
            command.Parameters.AddWithValue("@Is4Pair", entity.Is4Pair);
            command.Parameters.AddWithValue("@IsPowerOverHdmi", entity.IsPowerOverHdmi);
            command.Parameters.AddWithValue("@IsCapacitorDetection", entity.IsCapacitorDetection);
            command.Parameters.AddWithValue("@Protocol8023bt", entity.Protocol8023bt);
            command.Parameters.AddWithValue("@IsEnabled", entity.IsEnabled);
            command.Parameters.AddWithValue("@Class", entity.Class);
            command.Parameters.AddWithValue("@IpAddress", entity.IpAddress);
            command.Parameters.AddWithValue("@Alias", entity.Alias);
            command.Parameters.AddWithValue("@Violation", entity.Violation);
            command.Parameters.AddWithValue("@Type", entity.Type);
            command.Parameters.AddWithValue("@InterfaceType", entity.InterfaceType);
            command.Parameters.AddWithValue("@Bandwidth", entity.Bandwidth);
            command.Parameters.AddWithValue("@Duplex", entity.Duplex);
            command.Parameters.AddWithValue("@AutoNegotiation", entity.AutoNegotiation);
            command.Parameters.AddWithValue("@Transceiver", entity.Transceiver);
            command.Parameters.AddWithValue("@EPP", entity.EPP);
            command.Parameters.AddWithValue("@LinkQuality", entity.LinkQuality);
            command.Parameters.AddWithValue("@SlotId", entity.SlotId.ToString());
        }

        private PortEntity ParseFromReader(DbDataReader reader)
        {
            return new PortEntity
            {
                Id = Guid.Parse(reader["Id"].ToString()),
                Number = Convert.ToInt32(reader["Number"]),
                Name = reader["Name"].ToString(),
                PortIndex = reader["PortIndex"].ToString(),
                Poe = Convert.ToDouble(reader["Poe"]),
                Power = Convert.ToDouble(reader["Power"]),
                MaxPower = Convert.ToDouble(reader["MaxPower"]),
                Status = reader["Status"].ToString(),
                IsPoeON = Convert.ToBoolean(reader["IsPoeON"]),
                PriorityLevel = reader["PriorityLevel"].ToString(),
                IsUplink = Convert.ToBoolean(reader["IsUplink"]),
                IsLldpMdi = Convert.ToBoolean(reader["IsLldpMdi"]),
                IsLldpExtMdi = Convert.ToBoolean(reader["IsLldpExtMdi"]),
                IsVfLink = Convert.ToBoolean(reader["IsVfLink"]),
                Is4Pair = Convert.ToBoolean(reader["Is4Pair"]),
                IsPowerOverHdmi = Convert.ToBoolean(reader["IsPowerOverHdmi"]),
                IsCapacitorDetection = Convert.ToBoolean(reader["IsCapacitorDetection"]),
                Protocol8023bt = reader["Protocol8023bt"].ToString(),
                IsEnabled = Convert.ToBoolean(reader["IsEnabled"]),
                Class = reader["Class"].ToString(),
                IpAddress = reader["IpAddress"].ToString(),
                Alias = reader["Alias"].ToString(),
                Violation = reader["Violation"].ToString(),
                Type = reader["Type"].ToString(),
                InterfaceType = reader["InterfaceType"].ToString(),
                Bandwidth = reader["Bandwidth"].ToString(),
                Duplex = reader["Duplex"].ToString(),
                AutoNegotiation = reader["AutoNegotiation"].ToString(),
                Transceiver = reader["Transceiver"].ToString(),
                EPP = reader["EPP"].ToString(),
                LinkQuality = reader["LinkQuality"].ToString(),
                SlotId = Guid.Parse(reader["SlotId"].ToString())
            };
        }
    }
}
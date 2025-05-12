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
    public class EndpointDeviceRepository : BaseRepository<EndpointDeviceEntity>, IEndpointDeviceRepository
    {
        public EndpointDeviceRepository(SQLiteConnection connection) : base(connection)
        {
        }

        public override async Task<EndpointDeviceEntity> GetByIdAsync(object id)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var deviceId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = EndpointDeviceQueries.GetById;
                    command.Parameters.AddWithValue("@Id", deviceId.ToString());

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

        public override async Task<IEnumerable<EndpointDeviceEntity>> GetAllAsync()
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var devices = new List<EndpointDeviceEntity>();
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = EndpointDeviceQueries.GetAll;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            devices.Add(ParseFromReader(reader));
                        }
                    }
                }
                return devices;
            }, "GetAll");
        }

        public override async Task<EndpointDeviceEntity> AddAsync(EndpointDeviceEntity entity)
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
                    command.CommandText = EndpointDeviceQueries.Insert;
                    AddParameters(command, entity);
                    await command.ExecuteNonQueryAsync();
                    return entity;
                }
            }, "Add", entity.Id.ToString());
        }

        public override async Task UpdateAsync(EndpointDeviceEntity entity)
        {
            ValidateEntity(entity);
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = EndpointDeviceQueries.Update;
                    AddParameters(command, entity);
                    await command.ExecuteNonQueryAsync();
                }
            }, "Update", entity.Id.ToString());
        }

        public override async Task DeleteAsync(object id)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var deviceId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = EndpointDeviceQueries.Delete;
                    command.Parameters.AddWithValue("@Id", deviceId.ToString());
                    await command.ExecuteNonQueryAsync();
                }
            }, "Delete", id.ToString());
        }

        public override async Task<bool> ExistsAsync(object id)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var deviceId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = EndpointDeviceQueries.Exists;
                    command.Parameters.AddWithValue("@Id", deviceId.ToString());
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
            }, "Exists", id.ToString());
        }

        public async Task<IEnumerable<EndpointDeviceEntity>> GetByPortIdAsync(Guid portId)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var devices = new List<EndpointDeviceEntity>();
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = EndpointDeviceQueries.GetByPortId;
                    command.Parameters.AddWithValue("@PortId", portId.ToString());

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            devices.Add(ParseFromReader(reader));
                        }
                    }
                }
                return devices;
            }, "GetByPortId", portId.ToString());
        }

        private void AddParameters(SQLiteCommand command, EndpointDeviceEntity entity)
        {
            command.Parameters.AddWithValue("@Id", entity.Id.ToString());
            command.Parameters.AddWithValue("@RemoteId", entity.RemoteId);
            command.Parameters.AddWithValue("@Vendor", entity.Vendor);
            command.Parameters.AddWithValue("@Model", entity.Model);
            command.Parameters.AddWithValue("@SoftwareVersion", entity.SoftwareVersion);
            command.Parameters.AddWithValue("@HardwareVersion", entity.HardwareVersion);
            command.Parameters.AddWithValue("@SerialNumber", entity.SerialNumber);
            command.Parameters.AddWithValue("@PowerClass", entity.PowerClass);
            command.Parameters.AddWithValue("@LocalPort", entity.LocalPort);
            command.Parameters.AddWithValue("@PortSubType", entity.PortSubType);
            command.Parameters.AddWithValue("@MacAddress", entity.MacAddress);
            command.Parameters.AddWithValue("@Type", entity.Type);
            command.Parameters.AddWithValue("@IpAddress", entity.IpAddress);
            command.Parameters.AddWithValue("@EthernetType", entity.EthernetType);
            command.Parameters.AddWithValue("@RemotePort", entity.RemotePort);
            command.Parameters.AddWithValue("@Name", entity.Name);
            command.Parameters.AddWithValue("@Description", entity.Description);
            command.Parameters.AddWithValue("@PortDescription", entity.PortDescription);
            command.Parameters.AddWithValue("@MEDPowerType", entity.MEDPowerType);
            command.Parameters.AddWithValue("@MEDPowerSource", entity.MEDPowerSource);
            command.Parameters.AddWithValue("@MEDPowerPriority", entity.MEDPowerPriority);
            command.Parameters.AddWithValue("@MEDPowerValue", entity.MEDPowerValue);
            command.Parameters.AddWithValue("@IsMacName", entity.IsMacName);
            command.Parameters.AddWithValue("@Label", entity.Label);
            command.Parameters.AddWithValue("@PortId", entity.PortId.ToString());
        }

        private EndpointDeviceEntity ParseFromReader(DbDataReader reader)
        {
            return new EndpointDeviceEntity
            {
                Id = Guid.Parse(reader["Id"].ToString()),
                RemoteId = reader["RemoteId"].ToString(),
                Vendor = reader["Vendor"].ToString(),
                Model = reader["Model"].ToString(),
                SoftwareVersion = reader["SoftwareVersion"].ToString(),
                HardwareVersion = reader["HardwareVersion"].ToString(),
                SerialNumber = reader["SerialNumber"].ToString(),
                PowerClass = reader["PowerClass"].ToString(),
                LocalPort = reader["LocalPort"].ToString(),
                PortSubType = reader["PortSubType"].ToString(),
                MacAddress = reader["MacAddress"].ToString(),
                Type = reader["Type"].ToString(),
                IpAddress = reader["IpAddress"].ToString(),
                EthernetType = reader["EthernetType"].ToString(),
                RemotePort = reader["RemotePort"].ToString(),
                Name = reader["Name"].ToString(),
                Description = reader["Description"].ToString(),
                PortDescription = reader["PortDescription"].ToString(),
                MEDPowerType = reader["MEDPowerType"].ToString(),
                MEDPowerSource = reader["MEDPowerSource"].ToString(),
                MEDPowerPriority = reader["MEDPowerPriority"].ToString(),
                MEDPowerValue = reader["MEDPowerValue"].ToString(),
                IsMacName = Convert.ToBoolean(reader["IsMacName"]),
                Label = reader["Label"].ToString(),
                PortId = Guid.Parse(reader["PortId"].ToString())
            };
        }
    }
}
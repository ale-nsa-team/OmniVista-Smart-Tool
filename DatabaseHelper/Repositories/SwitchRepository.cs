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
    public class SwitchRepository : BaseRepository<SwitchEntity>, ISwitchRepository
    {
        public SwitchRepository(SQLiteConnection connection) : base(connection)
        {
        }

        public override async Task<SwitchEntity> GetByIdAsync(object id)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var switchId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = SwitchQueries.GetById;
                    command.Parameters.AddWithValue("@Id", switchId.ToString());

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

        public override async Task<IEnumerable<SwitchEntity>> GetAllAsync()
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var switches = new List<SwitchEntity>();
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = SwitchQueries.GetAll;
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            switches.Add(ParseFromReader(reader));
                        }
                    }
                }
                return switches;
            }, "GetAll");
        }

        public override async Task<SwitchEntity> AddAsync(SwitchEntity entity)
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
                    command.CommandText = SwitchQueries.Insert;
                    AddParameters(command, entity);
                    await command.ExecuteNonQueryAsync();
                    return entity;
                }
            }, "Add", entity.Id.ToString());
        }

        public override async Task UpdateAsync(SwitchEntity entity)
        {
            ValidateEntity(entity);

            await ExecuteWithErrorHandlingAsync(async () =>
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = SwitchQueries.Update;
                    AddParameters(command, entity);
                    await command.ExecuteNonQueryAsync();
                }
            }, "Update", entity.Id.ToString());
        }

        public override async Task DeleteAsync(object id)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var switchId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = SwitchQueries.Delete;
                    command.Parameters.AddWithValue("@Id", switchId.ToString());
                    await command.ExecuteNonQueryAsync();
                }
            }, "Delete", id.ToString());
        }

        public override async Task<bool> ExistsAsync(object id)
        {
            return await ExecuteWithErrorHandlingAsync(async () =>
            {
                var switchId = (Guid)id;
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = SwitchQueries.Exists;
                    command.Parameters.AddWithValue("@Id", switchId.ToString());
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result) > 0;
                }
            }, "Exists", id.ToString());
        }

        private void AddParameters(SQLiteCommand command, SwitchEntity entity)
        {
            command.Parameters.AddWithValue("@Id", entity.Id.ToString());
            command.Parameters.AddWithValue("@IpAddress", entity.IpAddress);
            command.Parameters.AddWithValue("@Name", entity.Name);
            command.Parameters.AddWithValue("@NetMask", entity.NetMask);
            command.Parameters.AddWithValue("@DefaultGateway", entity.DefaultGateway);
            command.Parameters.AddWithValue("@MacAddress", entity.MacAddress);
            command.Parameters.AddWithValue("@Login", entity.Login);
            command.Parameters.AddWithValue("@Password", entity.Password);
            command.Parameters.AddWithValue("@CnxTimeout", entity.CnxTimeout);
            command.Parameters.AddWithValue("@Status", entity.Status);
            command.Parameters.AddWithValue("@Version", entity.Version);
            command.Parameters.AddWithValue("@SerialNumber", entity.SerialNumber);
            command.Parameters.AddWithValue("@Model", entity.Model);
            command.Parameters.AddWithValue("@Location", entity.Location);
            command.Parameters.AddWithValue("@Description", entity.Description);
            command.Parameters.AddWithValue("@Contact", entity.Contact);
            command.Parameters.AddWithValue("@UpTime", entity.UpTime);
            command.Parameters.AddWithValue("@RunningDirectory", entity.RunningDirectory);
            command.Parameters.AddWithValue("@ConfigSnapshot", entity.ConfigSnapshot);
            command.Parameters.AddWithValue("@Power", entity.Power);
            command.Parameters.AddWithValue("@Budget", entity.Budget);
            command.Parameters.AddWithValue("@SyncStatus", entity.SyncStatus);
            command.Parameters.AddWithValue("@SupportsPoE", entity.SupportsPoE);
        }

        private SwitchEntity ParseFromReader(DbDataReader reader)
        {
            return new SwitchEntity
            {
                Id = Guid.Parse(reader["Id"].ToString()),
                IpAddress = reader["IpAddress"].ToString(),
                Name = reader["Name"].ToString(),
                NetMask = reader["NetMask"].ToString(),
                DefaultGateway = reader["DefaultGateway"].ToString(),
                MacAddress = reader["MacAddress"].ToString(),
                Login = reader["Login"].ToString(),
                Password = reader["Password"].ToString(),
                CnxTimeout = Convert.ToInt32(reader["CnxTimeout"]),
                Status = reader["Status"].ToString(),
                Version = reader["Version"].ToString(),
                SerialNumber = reader["SerialNumber"].ToString(),
                Model = reader["Model"].ToString(),
                Location = reader["Location"].ToString(),
                Description = reader["Description"].ToString(),
                Contact = reader["Contact"].ToString(),
                UpTime = reader["UpTime"].ToString(),
                RunningDirectory = reader["RunningDirectory"].ToString(),
                ConfigSnapshot = reader["ConfigSnapshot"].ToString(),
                Power = Convert.ToDouble(reader["Power"]),
                Budget = Convert.ToDouble(reader["Budget"]),
                SyncStatus = reader["SyncStatus"].ToString(),
                SupportsPoE = Convert.ToBoolean(reader["SupportsPoE"])
            };
        }
    }
}
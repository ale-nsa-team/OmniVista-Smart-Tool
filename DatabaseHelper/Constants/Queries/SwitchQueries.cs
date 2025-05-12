namespace DatabaseHelper.Constants.Queries
{
    /// <summary>
    /// Contains SQL query constants for the Switch repository
    /// </summary>
    public static class SwitchQueries
    {
        /// <summary>
        /// Query to get a switch by ID
        /// </summary>
        public const string GetById = @"
            SELECT * FROM Switch
            WHERE Id = @Id";

        /// <summary>
        /// Query to get all switches
        /// </summary>
        public const string GetAll = @"SELECT * FROM Switch";

        /// <summary>
        /// Query to insert a new switch
        /// </summary>
        public const string Insert = @"
            INSERT INTO Switch (
                Id, Name, IpAddress, NetMask, DefaultGateway, MacAddress,
                Login, Password, CnxTimeout, Status, Version, SerialNumber,
                Model, Location, Description, Contact, UpTime, RunningDirectory,
                ConfigSnapshot, Power, Budget, SyncStatus, SupportsPoE
            ) VALUES (
                @Id, @Name, @IpAddress, @NetMask, @DefaultGateway, @MacAddress,
                @Login, @Password, @CnxTimeout, @Status, @Version, @SerialNumber,
                @Model, @Location, @Description, @Contact, @UpTime, @RunningDirectory,
                @ConfigSnapshot, @Power, @Budget, @SyncStatus, @SupportsPoE
            )";

        /// <summary>
        /// Query to update an existing switch
        /// </summary>
        public const string Update = @"
            UPDATE Switch SET
                Name = @Name,
                IpAddress = @IpAddress,
                NetMask = @NetMask,
                DefaultGateway = @DefaultGateway,
                MacAddress = @MacAddress,
                Login = @Login,
                Password = @Password,
                CnxTimeout = @CnxTimeout,
                Status = @Status,
                Version = @Version,
                SerialNumber = @SerialNumber,
                Model = @Model,
                Location = @Location,
                Description = @Description,
                Contact = @Contact,
                UpTime = @UpTime,
                RunningDirectory = @RunningDirectory,
                ConfigSnapshot = @ConfigSnapshot,
                Power = @Power,
                Budget = @Budget,
                SyncStatus = @SyncStatus,
                SupportsPoE = @SupportsPoE
            WHERE Id = @Id";

        /// <summary>
        /// Query to delete a switch by ID
        /// </summary>
        public const string Delete = @"DELETE FROM Switch WHERE Id = @Id";

        /// <summary>
        /// Query to check if a switch exists by ID
        /// </summary>
        public const string Exists = @"SELECT COUNT(1) FROM Switch WHERE Id = @Id";
    }
}
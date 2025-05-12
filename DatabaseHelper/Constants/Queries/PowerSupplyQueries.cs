namespace DatabaseHelper.Constants.Queries
{
    /// <summary>
    /// Contains SQL query constants for the PowerSupply repository
    /// </summary>
    public static class PowerSupplyQueries
    {
        /// <summary>
        /// Query to get a power supply by ID
        /// </summary>
        public const string GetById = @"
            SELECT * FROM PowerSupply
            WHERE Id = @Id";

        /// <summary>
        /// Query to get all power supplies
        /// </summary>
        public const string GetAll = @"SELECT * FROM PowerSupply";

        /// <summary>
        /// Query to get power supplies by chassis ID
        /// </summary>
        public const string GetByChassisId = @"SELECT * FROM PowerSupply WHERE ChassisId = @ChassisId";

        /// <summary>
        /// Query to insert a new power supply
        /// </summary>
        public const string Insert = @"
            INSERT INTO PowerSupply (
                Id, Name, Model, Type, Location, Description, PowerProvision, Status,
                PartNumber, HardwareRevision, SerialNumber, ChassisId
            ) VALUES (
                @Id, @Name, @Model, @Type, @Location, @Description, @PowerProvision, @Status,
                @PartNumber, @HardwareRevision, @SerialNumber, @ChassisId
            )";

        /// <summary>
        /// Query to update an existing power supply
        /// </summary>
        public const string Update = @"
            UPDATE PowerSupply SET
                Name = @Name,
                Model = @Model,
                Type = @Type,
                Location = @Location,
                Description = @Description,
                PowerProvision = @PowerProvision,
                Status = @Status,
                PartNumber = @PartNumber,
                HardwareRevision = @HardwareRevision,
                SerialNumber = @SerialNumber,
                ChassisId = @ChassisId
            WHERE Id = @Id";

        /// <summary>
        /// Query to delete a power supply by ID
        /// </summary>
        public const string Delete = @"DELETE FROM PowerSupply WHERE Id = @Id";

        /// <summary>
        /// Query to check if a power supply exists by ID
        /// </summary>
        public const string Exists = @"SELECT COUNT(1) FROM PowerSupply WHERE Id = @Id";
    }
}
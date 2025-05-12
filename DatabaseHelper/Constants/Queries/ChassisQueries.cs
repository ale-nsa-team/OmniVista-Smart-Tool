namespace DatabaseHelper.Constants.Queries
{
    /// <summary>
    /// Contains SQL query constants for the Chassis repository
    /// </summary>
    public static class ChassisQueries
    {
        /// <summary>
        /// Query to get a chassis by ID
        /// </summary>
        public const string GetById = @"
            SELECT * FROM Chassis
            WHERE Id = @Id";

        /// <summary>
        /// Query to get all chassis
        /// </summary>
        public const string GetAll = @"SELECT * FROM Chassis";

        /// <summary>
        /// Query to get chassis by switch ID
        /// </summary>
        public const string GetBySwitchId = @"SELECT * FROM Chassis WHERE SwitchId = @SwitchId";

        /// <summary>
        /// Query to insert a new chassis
        /// </summary>
        public const string Insert = @"
            INSERT INTO Chassis (
                Id, Number, Model, Type, IsMaster, AdminStatus, OperationalStatus, Status,
                PowerBudget, PowerConsumed, PowerRemaining, SerialNumber, PartNumber,
                HardwareRevision, MacAddress, SwitchTemperature, SupportsPoE, Fpga,
                Cpld, Uboot, Onie, Cpu, FlashSize, FlashUsage, FlashSizeUsed,
                FlashSizeFree, FreeFlash, SwitchId
            ) VALUES (
                @Id, @Number, @Model, @Type, @IsMaster, @AdminStatus, @OperationalStatus, @Status,
                @PowerBudget, @PowerConsumed, @PowerRemaining, @SerialNumber, @PartNumber,
                @HardwareRevision, @MacAddress, @SwitchTemperature, @SupportsPoE, @Fpga,
                @Cpld, @Uboot, @Onie, @Cpu, @FlashSize, @FlashUsage, @FlashSizeUsed,
                @FlashSizeFree, @FreeFlash, @SwitchId
            )";

        /// <summary>
        /// Query to update an existing chassis
        /// </summary>
        public const string Update = @"
            UPDATE Chassis SET
                Number = @Number,
                Model = @Model,
                Type = @Type,
                IsMaster = @IsMaster,
                AdminStatus = @AdminStatus,
                OperationalStatus = @OperationalStatus,
                Status = @Status,
                PowerBudget = @PowerBudget,
                PowerConsumed = @PowerConsumed,
                PowerRemaining = @PowerRemaining,
                SerialNumber = @SerialNumber,
                PartNumber = @PartNumber,
                HardwareRevision = @HardwareRevision,
                MacAddress = @MacAddress,
                SwitchTemperature = @SwitchTemperature,
                SupportsPoE = @SupportsPoE,
                Fpga = @Fpga,
                Cpld = @Cpld,
                Uboot = @Uboot,
                Onie = @Onie,
                Cpu = @Cpu,
                FlashSize = @FlashSize,
                FlashUsage = @FlashUsage,
                FlashSizeUsed = @FlashSizeUsed,
                FlashSizeFree = @FlashSizeFree,
                FreeFlash = @FreeFlash,
                SwitchId = @SwitchId
            WHERE Id = @Id";

        /// <summary>
        /// Query to delete a chassis by ID
        /// </summary>
        public const string Delete = @"DELETE FROM Chassis WHERE Id = @Id";

        /// <summary>
        /// Query to check if a chassis exists by ID
        /// </summary>
        public const string Exists = @"SELECT COUNT(1) FROM Chassis WHERE Id = @Id";
    }
}
namespace DatabaseHelper.Constants.Queries
{
    /// <summary>
    /// Contains SQL query constants for the Slot repository
    /// </summary>
    public static class SlotQueries
    {
        /// <summary>
        /// Query to get a slot by ID
        /// </summary>
        public const string GetById = @"
            SELECT * FROM Slot
            WHERE Id = @Id";

        /// <summary>
        /// Query to get all slots
        /// </summary>
        public const string GetAll = @"SELECT * FROM Slot";

        /// <summary>
        /// Query to get slots by chassis ID
        /// </summary>
        public const string GetByChassisId = @"SELECT * FROM Slot WHERE ChassisId = @ChassisId";

        /// <summary>
        /// Query to insert a new slot
        /// </summary>
        public const string Insert = @"
            INSERT INTO Slot (
                Id, Number, Name, Model, NbPorts, NbPoePorts,
                PoeStatus, Power, Budget, Threshold, Is8023btSupport,
                IsPoeModeEnable, IsPriorityDisconnect, FPoE, PPoE,
                PowerClassDetection, IsHiResDetection, IsInitialized,
                SupportsPoE, IsMaster, ChassisId
            ) VALUES (
                @Id, @Number, @Name, @Model, @NbPorts, @NbPoePorts,
                @PoeStatus, @Power, @Budget, @Threshold, @Is8023btSupport,
                @IsPoeModeEnable, @IsPriorityDisconnect, @FPoE, @PPoE,
                @PowerClassDetection, @IsHiResDetection, @IsInitialized,
                @SupportsPoE, @IsMaster, @ChassisId
            )";

        /// <summary>
        /// Query to update an existing slot
        /// </summary>
        public const string Update = @"
            UPDATE Slot SET
                Number = @Number,
                Name = @Name,
                Model = @Model,
                NbPorts = @NbPorts,
                NbPoePorts = @NbPoePorts,
                PoeStatus = @PoeStatus,
                Power = @Power,
                Budget = @Budget,
                Threshold = @Threshold,
                Is8023btSupport = @Is8023btSupport,
                IsPoeModeEnable = @IsPoeModeEnable,
                IsPriorityDisconnect = @IsPriorityDisconnect,
                FPoE = @FPoE,
                PPoE = @PPoE,
                PowerClassDetection = @PowerClassDetection,
                IsHiResDetection = @IsHiResDetection,
                IsInitialized = @IsInitialized,
                SupportsPoE = @SupportsPoE,
                IsMaster = @IsMaster,
                ChassisId = @ChassisId
            WHERE Id = @Id";

        /// <summary>
        /// Query to delete a slot by ID
        /// </summary>
        public const string Delete = @"DELETE FROM Slot WHERE Id = @Id";

        /// <summary>
        /// Query to check if a slot exists by ID
        /// </summary>
        public const string Exists = @"SELECT COUNT(1) FROM Slot WHERE Id = @Id";
    }
}
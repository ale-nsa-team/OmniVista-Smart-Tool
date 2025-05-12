namespace DatabaseHelper.Constants.Queries
{
    /// <summary>
    /// Contains SQL query constants for the Port repository
    /// </summary>
    public static class PortQueries
    {
        /// <summary>
        /// Query to get a port by ID
        /// </summary>
        public const string GetById = @"
            SELECT * FROM Port
            WHERE Id = @Id";

        /// <summary>
        /// Query to get all ports
        /// </summary>
        public const string GetAll = @"SELECT * FROM Port";

        /// <summary>
        /// Query to get ports by slot ID
        /// </summary>
        public const string GetBySlotId = @"SELECT * FROM Port WHERE SlotId = @SlotId";

        /// <summary>
        /// Query to insert a new port
        /// </summary>
        public const string Insert = @"
            INSERT INTO Port (
                Id, Number, Name, PortIndex, Poe,
                Power, MaxPower, Status, IsPoeON,
                PriorityLevel, IsUplink, IsLldpMdi, IsLldpExtMdi,
                IsVfLink, Is4Pair, IsPowerOverHdmi, IsCapacitorDetection,
                Protocol8023bt, IsEnabled, Class, IpAddress,
                Alias, Violation, Type, InterfaceType,
                Bandwidth, Duplex, AutoNegotiation,
                Transceiver, EPP, LinkQuality,
                SlotId
            ) VALUES (
                @Id, @Number, @Name, @PortIndex, @Poe,
                @Power, @MaxPower, @Status, @IsPoeON,
                @PriorityLevel, @IsUplink, @IsLldpMdi, @IsLldpExtMdi,
                @IsVfLink, @Is4Pair, @IsPowerOverHdmi, @IsCapacitorDetection,
                @Protocol8023bt, @IsEnabled, @Class, @IpAddress,
                @Alias, @Violation, @Type, @InterfaceType,
                @Bandwidth, @Duplex, @AutoNegotiation,
                @Transceiver, @EPP, @LinkQuality,
                @SlotId
            )";

        /// <summary>
        /// Query to update an existing port
        /// </summary>
        public const string Update = @"
            UPDATE Port SET
                Number = @Number,
                Name = @Name,
                PortIndex = @PortIndex,
                Poe = @Poe,
                Power = @Power,
                MaxPower = @MaxPower,
                Status = @Status,
                IsPoeON = @IsPoeON,
                PriorityLevel = @PriorityLevel,
                IsUplink = @IsUplink,
                IsLldpMdi = @IsLldpMdi,
                IsLldpExtMdi = @IsLldpExtMdi,
                IsVfLink = @IsVfLink,
                Is4Pair = @Is4Pair,
                IsPowerOverHdmi = @IsPowerOverHdmi,
                IsCapacitorDetection = @IsCapacitorDetection,
                Protocol8023bt = @Protocol8023bt,
                IsEnabled = @IsEnabled,
                Class = @Class,
                IpAddress = @IpAddress,
                Alias = @Alias,
                Violation = @Violation,
                Type = @Type,
                InterfaceType = @InterfaceType,
                Bandwidth = @Bandwidth,
                Duplex = @Duplex,
                AutoNegotiation = @AutoNegotiation,
                Transceiver = @Transceiver,
                EPP = @EPP,
                LinkQuality = @LinkQuality,
                SlotId = @SlotId
            WHERE Id = @Id";

        /// <summary>
        /// Query to delete a port by ID
        /// </summary>
        public const string Delete = @"DELETE FROM Port WHERE Id = @Id";

        /// <summary>
        /// Query to check if a port exists by ID
        /// </summary>
        public const string Exists = @"SELECT COUNT(1) FROM Port WHERE Id = @Id";
    }
}
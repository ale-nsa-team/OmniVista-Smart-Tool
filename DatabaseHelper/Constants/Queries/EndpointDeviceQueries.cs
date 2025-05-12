namespace DatabaseHelper.Constants.Queries
{
    /// <summary>
    /// Contains SQL query constants for the EndpointDevice repository
    /// </summary>
    public static class EndpointDeviceQueries
    {
        /// <summary>
        /// Query to get an endpoint device by ID
        /// </summary>
        public const string GetById = @"
            SELECT * FROM EndPointDevice
            WHERE Id = @Id";

        /// <summary>
        /// Query to get all endpoint devices
        /// </summary>
        public const string GetAll = @"SELECT * FROM EndPointDevice";

        /// <summary>
        /// Query to get endpoint devices by port ID
        /// </summary>
        public const string GetByPortId = @"SELECT * FROM EndPointDevice WHERE PortId = @PortId";

        /// <summary>
        /// Query to insert a new endpoint device
        /// </summary>
        public const string Insert = @"
            INSERT INTO EndPointDevice (
                Id, RemoteId, Vendor, Model, SoftwareVersion, HardwareVersion,
                SerialNumber, PowerClass, LocalPort, PortSubType, MacAddress,
                Type, IpAddress, EthernetType, RemotePort, Name, Description,
                PortDescription, MEDPowerType, MEDPowerSource, MEDPowerPriority,
                MEDPowerValue, IsMacName, Label, PortId
            ) VALUES (
                @Id, @RemoteId, @Vendor, @Model, @SoftwareVersion, @HardwareVersion,
                @SerialNumber, @PowerClass, @LocalPort, @PortSubType, @MacAddress,
                @Type, @IpAddress, @EthernetType, @RemotePort, @Name, @Description,
                @PortDescription, @MEDPowerType, @MEDPowerSource, @MEDPowerPriority,
                @MEDPowerValue, @IsMacName, @Label, @PortId
            )";

        /// <summary>
        /// Query to update an existing endpoint device
        /// </summary>
        public const string Update = @"
            UPDATE EndPointDevice SET
                RemoteId = @RemoteId,
                Vendor = @Vendor,
                Model = @Model,
                SoftwareVersion = @SoftwareVersion,
                HardwareVersion = @HardwareVersion,
                SerialNumber = @SerialNumber,
                PowerClass = @PowerClass,
                LocalPort = @LocalPort,
                PortSubType = @PortSubType,
                MacAddress = @MacAddress,
                Type = @Type,
                IpAddress = @IpAddress,
                EthernetType = @EthernetType,
                RemotePort = @RemotePort,
                Name = @Name,
                Description = @Description,
                PortDescription = @PortDescription,
                MEDPowerType = @MEDPowerType,
                MEDPowerSource = @MEDPowerSource,
                MEDPowerPriority = @MEDPowerPriority,
                MEDPowerValue = @MEDPowerValue,
                IsMacName = @IsMacName,
                Label = @Label,
                PortId = @PortId
            WHERE Id = @Id";

        /// <summary>
        /// Query to delete an endpoint device by ID
        /// </summary>
        public const string Delete = @"DELETE FROM EndPointDevice WHERE Id = @Id";

        /// <summary>
        /// Query to check if an endpoint device exists by ID
        /// </summary>
        public const string Exists = @"SELECT COUNT(1) FROM EndPointDevice WHERE Id = @Id";
    }
}
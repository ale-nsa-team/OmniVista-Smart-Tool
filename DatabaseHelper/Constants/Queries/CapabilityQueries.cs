namespace DatabaseHelper.Constants.Queries
{
    /// <summary>
    /// Contains SQL query constants for the Capability repository
    /// </summary>
    public static class CapabilityQueries
    {
        /// <summary>
        /// Query to get a capability by ID
        /// </summary>
        public const string GetById = @"
            SELECT * FROM Capability
            WHERE Id = @Id";

        /// <summary>
        /// Query to get all capabilities
        /// </summary>
        public const string GetAll = @"SELECT * FROM Capability";

        /// <summary>
        /// Query to get capabilities by endpoint device ID
        /// </summary>
        public const string GetByDeviceId = @"SELECT * FROM Capability WHERE EndPointDeviceId = @DeviceId";

        /// <summary>
        /// Query to insert a new capability
        /// </summary>
        public const string Insert = @"
            INSERT INTO Capability (
                Id, Value, EndPointDeviceId
            ) VALUES (
                @Id, @Value, @EndPointDeviceId
            )";

        /// <summary>
        /// Query to update an existing capability
        /// </summary>
        public const string Update = @"
            UPDATE Capability SET
                Value = @Value,
                EndPointDeviceId = @EndPointDeviceId
            WHERE Id = @Id";

        /// <summary>
        /// Query to delete a capability by ID
        /// </summary>
        public const string Delete = @"DELETE FROM Capability WHERE Id = @Id";

        /// <summary>
        /// Query to check if a capability exists by ID
        /// </summary>
        public const string Exists = @"SELECT COUNT(1) FROM Capability WHERE Id = @Id";
    }
}
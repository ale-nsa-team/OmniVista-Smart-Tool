namespace DatabaseHelper.Constants.Queries
{
    /// <summary>
    /// Contains SQL query constants for the Temperature repository
    /// </summary>
    public static class TemperatureQueries
    {
        /// <summary>
        /// Query to get a temperature by ID
        /// </summary>
        public const string GetById = @"
            SELECT * FROM Temperature
            WHERE Id = @Id";

        /// <summary>
        /// Query to get all temperatures
        /// </summary>
        public const string GetAll = @"SELECT * FROM Temperature";

        /// <summary>
        /// Query to get temperature by chassis ID
        /// </summary>
        public const string GetByChassisId = @"SELECT * FROM Temperature WHERE ChassisId = @ChassisId";

        /// <summary>
        /// Query to insert a new temperature
        /// </summary>
        public const string Insert = @"
            INSERT INTO Temperature (
                Id, Device, Current, Range, Threshold, Danger, Status, ChassisId
            ) VALUES (
                @Id, @Device, @Current, @Range, @Threshold, @Danger, @Status, @ChassisId
            )";

        /// <summary>
        /// Query to update an existing temperature
        /// </summary>
        public const string Update = @"
            UPDATE Temperature SET
                Device = @Device,
                Current = @Current,
                Range = @Range,
                Threshold = @Threshold,
                Danger = @Danger,
                Status = @Status,
                ChassisId = @ChassisId
            WHERE Id = @Id";

        /// <summary>
        /// Query to delete a temperature by ID
        /// </summary>
        public const string Delete = @"DELETE FROM Temperature WHERE Id = @Id";

        /// <summary>
        /// Query to check if a temperature exists by ID
        /// </summary>
        public const string Exists = @"SELECT COUNT(1) FROM Temperature WHERE Id = @Id";
    }
}
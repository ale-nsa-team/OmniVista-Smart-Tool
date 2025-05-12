namespace DatabaseHelper.Constants.Queries
{
    /// <summary>
    /// Contains SQL query constants for the SwitchDebugApp repository
    /// </summary>
    public static class SwitchDebugAppQueries
    {
        /// <summary>
        /// Query to get a switch debug app by ID
        /// </summary>
        public const string GetById = @"
            SELECT * FROM SwitchDebugApp
            WHERE Id = @Id";

        /// <summary>
        /// Query to get all switch debug apps
        /// </summary>
        public const string GetAll = @"SELECT * FROM SwitchDebugApp";

        /// <summary>
        /// Query to get switch debug apps by switch ID
        /// </summary>
        public const string GetBySwitchId = @"SELECT * FROM SwitchDebugApp WHERE SwitchId = @SwitchId";

        /// <summary>
        /// Query to insert a new switch debug app
        /// </summary>
        public const string Insert = @"
            INSERT INTO SwitchDebugApp (
                Id, Name, AppId, AppIndex, NbSubApp, DebugLevel, SwitchId
            ) VALUES (
                @Id, @Name, @AppId, @AppIndex, @NbSubApp, @DebugLevel, @SwitchId
            )";

        /// <summary>
        /// Query to update an existing switch debug app
        /// </summary>
        public const string Update = @"
            UPDATE SwitchDebugApp SET
                Name = @Name,
                AppId = @AppId,
                AppIndex = @AppIndex,
                NbSubApp = @NbSubApp,
                DebugLevel = @DebugLevel,
                SwitchId = @SwitchId
            WHERE Id = @Id";

        /// <summary>
        /// Query to delete a switch debug app by ID
        /// </summary>
        public const string Delete = @"DELETE FROM SwitchDebugApp WHERE Id = @Id";

        /// <summary>
        /// Query to check if a switch debug app exists by ID
        /// </summary>
        public const string Exists = @"SELECT COUNT(1) FROM SwitchDebugApp WHERE Id = @Id";
    }
}
namespace DatabaseHelper.Constants.Queries
{
    /// <summary>
    /// Contains SQL query constants for the DatabaseVersion repository
    /// </summary>
    public static class DatabaseVersionQueries
    {
        /// <summary>
        /// Query to get a database version by ID
        /// </summary>
        public const string GetById = @"
            SELECT * FROM DatabaseVersion
            WHERE Id = @Id";

        /// <summary>
        /// Query to get all database versions
        /// </summary>
        public const string GetAll = @"SELECT * FROM DatabaseVersion";

        /// <summary>
        /// Query to get the latest database version
        /// </summary>
        public const string GetLatest = @"SELECT * FROM DatabaseVersion ORDER BY Version DESC LIMIT 1";

        /// <summary>
        /// Query to insert a new database version
        /// </summary>
        public const string Insert = @"
            INSERT INTO DatabaseVersion (
                Id, Version, ReleaseDate, Description
            ) VALUES (
                @Id, @Version, @ReleaseDate, @Description
            )";

        /// <summary>
        /// Query to update an existing database version
        /// </summary>
        public const string Update = @"
            UPDATE DatabaseVersion SET
                Version = @Version,
                ReleaseDate = @ReleaseDate,
                Description = @Description
            WHERE Id = @Id";

        /// <summary>
        /// Query to delete a database version by ID
        /// </summary>
        public const string Delete = @"DELETE FROM DatabaseVersion WHERE Id = @Id";

        /// <summary>
        /// Query to check if a database version exists by ID
        /// </summary>
        public const string Exists = @"SELECT COUNT(1) FROM DatabaseVersion WHERE Id = @Id";
    }
}
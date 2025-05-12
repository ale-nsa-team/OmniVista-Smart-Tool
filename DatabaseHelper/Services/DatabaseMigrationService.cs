using DatabaseHelper.Entities;
using DatabaseHelper.Exceptions;
using DatabaseHelper.Logging;
using DatabaseHelper.Repositories;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace DatabaseHelper.Services
{
    public class DatabaseMigrationService
    {
        private readonly SQLiteConnection _connection;
        private readonly DatabaseVersionRepository _versionRepository;
        private readonly ILogger _logger;
        private const string CURRENT_DATABASE_VERSION = "1.0.0";

        public DatabaseMigrationService(SQLiteConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _versionRepository = new DatabaseVersionRepository(connection);
            _logger = LoggerFactory.CreateLogger<DatabaseMigrationService>();
        }

        /// <summary>
        /// Checks if the database needs to be upgraded and performs the upgrade if necessary
        /// </summary>
        /// <returns>True if an upgrade was performed, false otherwise</returns>
        public async Task<bool> CheckAndUpgradeDatabaseAsync()
        {
            try
            {
                _logger.Info("Checking database version");

                // First check if the DatabaseVersion table exists, create it if not
                bool tableExists = await TableExistsAsync("DatabaseVersion");
                if (!tableExists)
                {
                    _logger.Info("DatabaseVersion table does not exist, creating it");
                    await InitializeDatabaseWithVersionTableAsync();
                    return false;
                }

                var currentVersion = await GetCurrentDatabaseVersionAsync();

                if (currentVersion == null)
                {
                    _logger.Info("No database version found, initializing with current version");
                    await InitializeDatabaseWithVersionTableAsync();
                    return false;
                }

                if (NeedsUpgrade(currentVersion.Version))
                {
                    _logger.Info($"Database upgrade needed from version {currentVersion.Version} to {CURRENT_DATABASE_VERSION}");
                    await UpgradeDatabaseAsync(currentVersion.Version);
                    return true;
                }

                _logger.Info($"Database is up to date at version {currentVersion.Version}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error("Error checking or upgrading database version", ex);
                throw new DatabaseException("Error checking or upgrading database version", ex);
            }
        }

        /// <summary>
        /// Checks if a table exists in the database
        /// </summary>
        private async Task<bool> TableExistsAsync(string tableName)
        {
            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'";
                    var result = await command.ExecuteScalarAsync();
                    return result != null && result.ToString() == tableName;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error checking if table {tableName} exists", ex);
                throw new DatabaseException($"Error checking if table {tableName} exists", ex);
            }
        }

        /// <summary>
        /// Gets the current database version from the DatabaseVersion table
        /// </summary>
        /// <returns>The current database version entity or null if not found</returns>
        public async Task<DatabaseVersionEntity> GetCurrentDatabaseVersionAsync()
        {
            try
            {
                return await _versionRepository.GetLatestAsync();
            }
            catch (SQLiteException ex) when (ex.Message.Contains("no such table"))
            {
                // Table doesn't exist yet, so return null to indicate no version exists
                _logger.Info("DatabaseVersion table does not exist yet");
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error("Error getting current database version", ex);
                throw new DatabaseException("Error getting current database version", ex);
            }
        }

        /// <summary>
        /// Initializes the database version with the current version
        /// </summary>
        private async Task InitializeDatabaseWithVersionTableAsync()
        {
            // Create the DatabaseVersion table if it doesn't exist
            await CreateDatabaseVersionTableAsync();

            // Add the initial version record
            var versionEntity = new DatabaseVersionEntity
            {
                Id = Guid.NewGuid(),
                Version = CURRENT_DATABASE_VERSION,
                ReleaseDate = DateTime.UtcNow,
                Description = "Initial database version"
            };

            await _versionRepository.AddAsync(versionEntity);
            _logger.Info($"Database version initialized to {CURRENT_DATABASE_VERSION}");
        }

        /// <summary>
        /// Creates the DatabaseVersion table if it doesn't exist
        /// </summary>
        private async Task CreateDatabaseVersionTableAsync()
        {
            try
            {
                _logger.Info("Creating DatabaseVersion table");
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS DatabaseVersion (
                            Id TEXT PRIMARY KEY,
                            Version TEXT NOT NULL,
                            ReleaseDate TEXT NOT NULL,
                            Description TEXT
                        );";
                    await command.ExecuteNonQueryAsync();
                }
                _logger.Info("DatabaseVersion table created successfully");
            }
            catch (Exception ex)
            {
                _logger.Error("Error creating DatabaseVersion table", ex);
                throw new DatabaseException("Error creating DatabaseVersion table", ex);
            }
        }

        /// <summary>
        /// Determines if the database needs to be upgraded
        /// </summary>
        /// <param name="currentVersion">The current database version</param>
        /// <returns>True if an upgrade is needed, false otherwise</returns>
        private bool NeedsUpgrade(string currentVersion)
        {
            return CompareVersions(currentVersion, CURRENT_DATABASE_VERSION) < 0;
        }

        /// <summary>
        /// Compares two version strings
        /// </summary>
        /// <param name="version1">First version</param>
        /// <param name="version2">Second version</param>
        /// <returns>-1 if version1 is less than version2, 0 if equal, 1 if greater</returns>
        private int CompareVersions(string version1, string version2)
        {
            var v1Parts = version1.Split('.');
            var v2Parts = version2.Split('.');

            for (int i = 0; i < Math.Max(v1Parts.Length, v2Parts.Length); i++)
            {
                int v1Part = i < v1Parts.Length ? int.Parse(v1Parts[i]) : 0;
                int v2Part = i < v2Parts.Length ? int.Parse(v2Parts[i]) : 0;

                if (v1Part < v2Part)
                    return -1;
                if (v1Part > v2Part)
                    return 1;
            }

            return 0;
        }

        /// <summary>
        /// Upgrades the database from the current version to the target version
        /// </summary>
        /// <param name="currentVersion">The current database version</param>
        /// <returns>A task representing the asynchronous operation</returns>
        private async Task UpgradeDatabaseAsync(string currentVersion)
        {
            _logger.Info($"Starting database upgrade from {currentVersion} to {CURRENT_DATABASE_VERSION}");

            // Get all migrations that need to be applied
            var migrations = GetMigrationsToApply(currentVersion);

            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    foreach (var migration in migrations)
                    {
                        _logger.Info($"Applying migration to version {migration.TargetVersion}: {migration.Description}");
                        await migration.ApplyAsync(_connection);

                        // Record the migration
                        var versionEntity = new DatabaseVersionEntity
                        {
                            Id = Guid.NewGuid(),
                            Version = migration.TargetVersion,
                            ReleaseDate = DateTime.UtcNow,
                            Description = migration.Description
                        };

                        await _versionRepository.AddAsync(versionEntity);
                        _logger.Info($"Migration to version {migration.TargetVersion} applied successfully");
                    }

                    transaction.Commit();
                    _logger.Info($"Database successfully upgraded to version {CURRENT_DATABASE_VERSION}");
                }
                catch (Exception ex)
                {
                    _logger.Error("Error during database upgrade, rolling back", ex);
                    transaction.Rollback();
                    throw new DatabaseException("Error during database upgrade", ex);
                }
            }
        }

        /// <summary>
        /// Gets the list of migrations that need to be applied to upgrade from the current version
        /// </summary>
        /// <param name="currentVersion">The current database version</param>
        /// <returns>A list of migrations to apply</returns>
        private List<IMigration> GetMigrationsToApply(string currentVersion)
        {
            var migrations = new List<IMigration>();

            // Add migrations based on version comparison
            if (CompareVersions(currentVersion, "1.0.0") < 0)
            {
                migrations.Add(new Migration_1_0_0());
            }

            // Add future migrations here
            // if (CompareVersions(currentVersion, "1.1.0") < 0)
            // {
            //     migrations.Add(new Migration_1_1_0());
            // }

            return migrations;
        }

        /// <summary>
        /// Adds a new column to an existing table
        /// </summary>
        /// <param name="tableName">The name of the table</param>
        /// <param name="columnName">The name of the column to add</param>
        /// <param name="columnType">The SQLite data type of the column</param>
        /// <param name="defaultValue">Optional default value for the column</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task AddColumnToTableAsync(string tableName, string columnName, string columnType, string defaultValue = null)
        {
            try
            {
                _logger.Info($"Adding column {columnName} to table {tableName}");

                // Check if the column already exists
                if (await ColumnExistsAsync(tableName, columnName))
                {
                    _logger.Info($"Column {columnName} already exists in table {tableName}, skipping");
                    return;
                }

                using (var command = _connection.CreateCommand())
                {
                    string sql = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnType}";

                    if (!string.IsNullOrEmpty(defaultValue))
                    {
                        sql += $" DEFAULT {defaultValue}";
                    }

                    command.CommandText = sql;
                    await command.ExecuteNonQueryAsync();
                    _logger.Info($"Column {columnName} added to table {tableName} successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error adding column {columnName} to table {tableName}", ex);
                throw new DatabaseException($"Error adding column {columnName} to table {tableName}", ex);
            }
        }

        /// <summary>
        /// Checks if a column exists in a table
        /// </summary>
        /// <param name="tableName">The name of the table</param>
        /// <param name="columnName">The name of the column</param>
        /// <returns>True if the column exists, false otherwise</returns>
        private async Task<bool> ColumnExistsAsync(string tableName, string columnName)
        {
            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = $"PRAGMA table_info({tableName})";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string name = reader["name"].ToString();
                            if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error checking if column {columnName} exists in table {tableName}", ex);
                throw new DatabaseException($"Error checking if column {columnName} exists in table {tableName}", ex);
            }
        }

        /// <summary>
        /// Registers a new database version after applying schema changes
        /// </summary>
        /// <param name="version">The new version number</param>
        /// <param name="description">Description of the changes</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public async Task RegisterNewVersionAsync(string version, string description)
        {
            try
            {
                _logger.Info($"Registering new database version: {version}");

                var versionEntity = new DatabaseVersionEntity
                {
                    Id = Guid.NewGuid(),
                    Version = version,
                    ReleaseDate = DateTime.UtcNow,
                    Description = description
                };

                await _versionRepository.AddAsync(versionEntity);
                _logger.Info($"Database version {version} registered successfully");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error registering database version {version}", ex);
                throw new DatabaseException($"Error registering database version {version}", ex);
            }
        }
    }

    /// <summary>
    /// Interface for database migrations
    /// </summary>
    public interface IMigration
    {
        string TargetVersion { get; }
        string Description { get; }

        Task ApplyAsync(SQLiteConnection connection);
    }

    /// <summary>
    /// Migration to version 1.0.0
    /// </summary>
    public class Migration_1_0_0 : IMigration
    {
        public string TargetVersion => "1.0.0";
        public string Description => "Initial database schema";

        public async Task ApplyAsync(SQLiteConnection connection)
        {
            // This is the initial version, so no migration is needed
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Example migration to version 1.1.0 that adds new columns
    /// </summary>
    public class Migration_1_1_0 : IMigration
    {
        public string TargetVersion => "1.1.0";
        public string Description => "Add new columns to Switch and Port tables";

        public async Task ApplyAsync(SQLiteConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                // Add new columns to Switch table
                command.CommandText = @"
                    ALTER TABLE Switch ADD COLUMN LastSeen TEXT;
                    ALTER TABLE Switch ADD COLUMN FirmwareVersion TEXT;
                    ALTER TABLE Switch ADD COLUMN IsManaged INTEGER DEFAULT 0;";
                await command.ExecuteNonQueryAsync();

                // Add new columns to Port table
                command.CommandText = @"
                    ALTER TABLE Port ADD COLUMN LastActivity TEXT;
                    ALTER TABLE Port ADD COLUMN ErrorCount INTEGER DEFAULT 0;
                    ALTER TABLE Port ADD COLUMN IsMonitored INTEGER DEFAULT 0;";
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
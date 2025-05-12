using DatabaseHelper.Exceptions;
using DatabaseHelper.Logging;
using DatabaseHelper.Services;
using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

namespace DatabaseHelper
{
    public class DbContext : IDisposable
    {
        private bool _disposed;
        private static DbContext _instance;
        private static readonly object _lock = new object();
        private static readonly ILogger _logger = LoggerFactory.CreateLogger<DbContext>();
        private const string POOLING_CONNECTION_STRING = ";Pooling=True;Max Pool Size=100;";
        internal readonly SQLiteConnection _connection;
        private readonly DatabaseMigrationService _migrationService;

        public static DbContext Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            throw new InvalidOperationException("Database must be initialized with Initialize() before accessing Instance");
                        }
                    }
                }
                return _instance;
            }
        }

        public static void Initialize(string dataPath)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _logger.Info($"Initializing database with path: {dataPath}");
                    _instance = new DbContext(dataPath);
                    _logger.Info("Database initialized successfully");
                }
                else
                {
                    _logger.Warn("Attempted to initialize database more than once - ignored");
                }
            }
        }

        private DbContext(string dataPath)
        {
            try
            {
                var dbDirectory = Path.Combine(dataPath, "Database");
                var dbPath = Path.Combine(dbDirectory, "poe_wizard.db");
                var connectionString = $"Data Source={dbPath};Version=3;{POOLING_CONNECTION_STRING}";

                _logger.Debug($"Creating database directory if it doesn't exist: {dbDirectory}");
                if (!Directory.Exists(dbDirectory))
                {
                    Directory.CreateDirectory(dbDirectory);
                    _logger.Debug("Created database directory");
                }

                _logger.Debug($"Checking if database file exists: {dbPath}");
                bool isNewDatabase = !File.Exists(dbPath);
                if (isNewDatabase)
                {
                    _logger.Info($"Creating new database file: {dbPath}");
                    SQLiteConnection.CreateFile(dbPath);
                    _logger.Info("Database file created successfully");
                }

                _logger.Debug($"Opening database connection with connection string: {connectionString}");
                _connection = new SQLiteConnection(connectionString);
                _migrationService = new DatabaseMigrationService(_connection);

                InitializeDatabase();
                _logger.Info("Database connection opened successfully with connection pooling enabled");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to initialize database", ex);
                throw new ConnectionException("Failed to initialize database", ex);
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                _logger.Debug("Opening database connection");
                _connection.Open();
                _logger.Debug("Connection opened, initializing database schema");

                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS DatabaseVersion (
                            Id TEXT PRIMARY KEY,
                            Version TEXT NOT NULL,
                            ReleaseDate TEXT NOT NULL,
                            Description TEXT
                        );

                        CREATE TABLE IF NOT EXISTS Switch (
                            Id TEXT PRIMARY KEY,
                            IpAddress TEXT UNIQUE NOT NULL,
                            Name TEXT,
                            NetMask TEXT,
                            DefaultGateway TEXT,
                            MacAddress TEXT,
                            Login TEXT,
                            Password TEXT,
                            CnxTimeout INTEGER,
                            Status TEXT,
                            Version TEXT,
                            SerialNumber TEXT,
                            Model TEXT,
                            Location TEXT,
                            Description TEXT,
                            Contact TEXT,
                            UpTime TEXT,
                            RunningDirectory TEXT,
                            ConfigSnapshot TEXT,
                            Power REAL,
                            Budget REAL,
                            SyncStatus TEXT,
                            SupportsPoE INTEGER
                        );

                        CREATE TABLE IF NOT EXISTS SwitchDebugApp (
                            Id TEXT PRIMARY KEY,
                            Name TEXT,
                            AppId TEXT,
                            AppIndex TEXT,
                            NbSubApp TEXT,
                            DebugLevel INTEGER,
                            SwitchId TEXT,
                            FOREIGN KEY(SwitchId) REFERENCES Switch(Id)
                        );

                        CREATE TABLE IF NOT EXISTS Chassis (
                            Id TEXT PRIMARY KEY,
                            Number INTEGER,
                            Model TEXT,
                            Type TEXT,
                            IsMaster INTEGER,
                            AdminStatus TEXT,
                            OperationalStatus TEXT,
                            Status TEXT,
                            PowerBudget REAL,
                            PowerConsumed REAL,
                            PowerRemaining REAL,
                            SerialNumber TEXT,
                            PartNumber TEXT,
                            HardwareRevision TEXT,
                            MacAddress TEXT,
                            SwitchTemperature TEXT,
                            SupportsPoE INTEGER,
                            Fpga TEXT,
                            Cpld TEXT,
                            Uboot TEXT,
                            Onie TEXT,
                            Cpu INTEGER,
                            FlashSize TEXT,
                            FlashUsage TEXT,
                            FlashSizeUsed TEXT,
                            FlashSizeFree TEXT,
                            FreeFlash TEXT,
                            SwitchId TEXT,
                            FOREIGN KEY(SwitchId) REFERENCES Switch(Id)
                        );

                        CREATE TABLE IF NOT EXISTS Temperature (
                            Id TEXT PRIMARY KEY,
                            Device TEXT,
                            Current INTEGER,
                            Range TEXT,
                            Threshold INTEGER,
                            Danger INTEGER,
                            Status TEXT,
                            ChassisId TEXT,
                            FOREIGN KEY(ChassisId) REFERENCES Chassis(Id)
                        );

                        CREATE TABLE IF NOT EXISTS PowerSupply (
                            Id TEXT PRIMARY KEY,
                            Name TEXT,
                            Model TEXT,
                            Type TEXT,
                            Location TEXT,
                            Description TEXT,
                            PowerProvision TEXT,
                            Status TEXT,
                            PartNumber TEXT,
                            HardwareRevision TEXT,
                            SerialNumber TEXT,
                            ChassisId TEXT,
                            FOREIGN KEY(ChassisId) REFERENCES Chassis(Id)
                        );

                        CREATE TABLE IF NOT EXISTS Slot (
                            Id TEXT PRIMARY KEY,
                            Number INTEGER,
                            Name TEXT,
                            Model TEXT,
                            NbPorts INTEGER,
                            NbPoePorts INTEGER,
                            PoeStatus TEXT,
                            Power REAL,
                            Budget REAL,
                            Threshold REAL,
                            Is8023btSupport INTEGER,
                            IsPoeModeEnable INTEGER,
                            IsPriorityDisconnect INTEGER,
                            FPoE TEXT,
                            PPoE TEXT,
                            PowerClassDetection TEXT,
                            IsHiResDetection INTEGER,
                            IsInitialized INTEGER,
                            SupportsPoE INTEGER,
                            IsMaster INTEGER,
                            ChassisId TEXT,
                            FOREIGN KEY(ChassisId) REFERENCES Chassis(Id)
                        );

                        CREATE TABLE IF NOT EXISTS Port (
                            Id TEXT PRIMARY KEY,
                            Number INTEGER,
                            Name TEXT,
                            PortIndex TEXT,
                            Poe REAL,
                            Power REAL,
                            MaxPower REAL,
                            Status TEXT,
                            IsPoeON INTEGER,
                            PriorityLevel TEXT,
                            IsUplink INTEGER,
                            IsLldpMdi INTEGER,
                            IsLldpExtMdi INTEGER,
                            IsVfLink INTEGER,
                            Is4Pair INTEGER,
                            IsPowerOverHdmi INTEGER,
                            IsCapacitorDetection INTEGER,
                            Protocol8023bt TEXT,
                            IsEnabled INTEGER,
                            Class TEXT,
                            IpAddress TEXT,
                            Alias TEXT,
                            Violation TEXT,
                            Type TEXT,
                            InterfaceType TEXT,
                            Bandwidth TEXT,
                            Duplex TEXT,
                            AutoNegotiation TEXT,
                            Transceiver TEXT,
                            EPP TEXT,
                            LinkQuality TEXT,
                            SlotId TEXT,
                            FOREIGN KEY(SlotId) REFERENCES Slot(Id)
                        );

                        CREATE TABLE IF NOT EXISTS EndPointDevice (
                            Id TEXT PRIMARY KEY,
                            RemoteId TEXT,
                            Vendor TEXT,
                            Model TEXT,
                            SoftwareVersion TEXT,
                            HardwareVersion TEXT,
                            SerialNumber TEXT,
                            PowerClass TEXT,
                            LocalPort TEXT,
                            PortSubType TEXT,
                            MacAddress TEXT,
                            Type TEXT,
                            IpAddress TEXT,
                            EthernetType TEXT,
                            RemotePort TEXT,
                            Name TEXT,
                            Description TEXT,
                            PortDescription TEXT,
                            MEDPowerType TEXT,
                            MEDPowerSource TEXT,
                            MEDPowerPriority TEXT,
                            MEDPowerValue TEXT,
                            IsMacName INTEGER,
                            Label TEXT,
                            PortId TEXT,
                            FOREIGN KEY(PortId) REFERENCES Port(Id)
                        );

                        CREATE TABLE IF NOT EXISTS Capability (
                            Id TEXT PRIMARY KEY,
                            Value TEXT,
                            EndPointDeviceId TEXT,
                            FOREIGN KEY(EndPointDeviceId) REFERENCES EndPointDevice(Id)
                        );";

                    _logger.Debug("Executing database schema creation");
                    command.ExecuteNonQuery();
                    _logger.Info("Database schema created/updated successfully");
                }

                // Check and upgrade database if needed
                _logger.Debug("Checking if database needs to be upgraded");
                var wasUpgraded = _migrationService.CheckAndUpgradeDatabaseAsync().GetAwaiter().GetResult();
                if (wasUpgraded)
                {
                    _logger.Info("Database was upgraded successfully");
                }
                else
                {
                    _logger.Info("No database upgrade was needed");
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to initialize database schema", ex);
                throw new DatabaseException("Failed to initialize database schema", ex);
            }
        }

        /// <summary>
        /// Gets the current database version
        /// </summary>
        /// <returns>The current database version entity or null if not found</returns>
        public async Task<string> GetDatabaseVersionAsync()
        {
            var versionEntity = await _migrationService.GetCurrentDatabaseVersionAsync();
            return versionEntity?.Version ?? "Unknown";
        }

        /// <summary>
        /// Upgrades the database schema by adding new columns to existing tables
        /// </summary>
        /// <param name="newVersion">The new version number after the upgrade</param>
        /// <param name="description">Description of the changes</param>
        /// <param name="columnDefinitions">Array of column definitions in the format: [tableName, columnName, columnType, defaultValue]</param>
        /// <returns>True if the upgrade was successful, false otherwise</returns>
        public async Task<bool> UpgradeDatabaseSchemaAsync(string newVersion, string description, params (string TableName, string ColumnName, string ColumnType, string DefaultValue)[] columnDefinitions)
        {
            try
            {
                _logger.Info($"Starting database schema upgrade to version {newVersion}");

                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        foreach (var (TableName, ColumnName, ColumnType, DefaultValue) in columnDefinitions)
                        {
                            await _migrationService.AddColumnToTableAsync(
                                TableName,
                                ColumnName,
                                ColumnType,
                                DefaultValue);
                        }

                        // Register the new version
                        await _migrationService.RegisterNewVersionAsync(newVersion, description);

                        transaction.Commit();
                        _logger.Info($"Database schema upgraded successfully to version {newVersion}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error("Error during database schema upgrade, rolling back", ex);
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to upgrade database schema to version {newVersion}", ex);
                throw new DatabaseException($"Failed to upgrade database schema to version {newVersion}", ex);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _logger.Debug("Disposing database connection");
                _connection?.Close();
                _connection?.Dispose();
                _logger.Info("Database connection disposed successfully");
            }

            _disposed = true;
        }

        ~DbContext()
        {
            Dispose(false);
        }
    }
}
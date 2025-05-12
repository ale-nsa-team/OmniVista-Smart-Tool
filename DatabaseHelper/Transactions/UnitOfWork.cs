using DatabaseHelper.Entities;
using DatabaseHelper.Exceptions;
using DatabaseHelper.Interfaces;
using DatabaseHelper.Logging;
using DatabaseHelper.Repositories;
using System;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace DatabaseHelper.Transactions
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SQLiteConnection _connection;
        private SQLiteTransaction _transaction;
        private bool _disposed;
        private readonly ILogger _logger = LoggerFactory.CreateLogger<UnitOfWork>();

        private IRepository<SwitchEntity> _switches;
        private IChassisRepository _chassis;
        private ISlotRepository _slots;
        private IPortRepository _ports;
        private IEndpointDeviceRepository _endPointDevices;
        private IPowerSupplyRepository _powerSupplies;
        private ITemperatureRepository _temperatures;
        private ICapabilityRepository _capabilities;
        private ISwitchDebugAppRepository _switchDebugApps;
        private IDatabaseVersionRepository _databaseVersions;

        public UnitOfWork(SQLiteConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _logger.Debug("Unit of work initialized");
        }

        public IRepository<SwitchEntity> Switches
        {
            get
            {
                if (_switches == null)
                {
                    _logger.Debug("Creating SwitchRepository");
                    _switches = new SwitchRepository(_connection);
                }
                return _switches;
            }
        }

        public IChassisRepository Chassis
        {
            get
            {
                if (_chassis == null)
                {
                    _logger.Debug("Creating ChassisRepository");
                    _chassis = new ChassisRepository(_connection);
                }
                return _chassis;
            }
        }

        public ISlotRepository Slots
        {
            get
            {
                if (_slots == null)
                {
                    _logger.Debug("Creating SlotRepository");
                    _slots = new SlotRepository(_connection);
                }
                return _slots;
            }
        }

        public IPortRepository Ports
        {
            get
            {
                if (_ports == null)
                {
                    _logger.Debug("Creating PortRepository");
                    _ports = new PortRepository(_connection);
                }
                return _ports;
            }
        }

        public IEndpointDeviceRepository EndPointDevices
        {
            get
            {
                if (_endPointDevices == null)
                {
                    _logger.Debug("Creating EndPointDeviceRepository");
                    _endPointDevices = new EndpointDeviceRepository(_connection);
                }
                return _endPointDevices;
            }
        }

        public IPowerSupplyRepository PowerSupplies
        {
            get
            {
                if (_powerSupplies == null)
                {
                    _logger.Debug("Creating PowerSupplyRepository");
                    _powerSupplies = new PowerSupplyRepository(_connection);
                }
                return _powerSupplies;
            }
        }

        public ITemperatureRepository Temperatures
        {
            get
            {
                if (_temperatures == null)
                {
                    _logger.Debug("Creating TemperatureRepository");
                    _temperatures = new TemperatureRepository(_connection);
                }
                return _temperatures;
            }
        }

        public ICapabilityRepository Capabilities
        {
            get
            {
                if (_capabilities == null)
                {
                    _logger.Debug("Creating CapabilityRepository");
                    _capabilities = new CapabilityRepository(_connection);
                }
                return _capabilities;
            }
        }

        public ISwitchDebugAppRepository SwitchDebugApps
        {
            get
            {
                if (_switchDebugApps == null)
                {
                    _logger.Debug("Creating SwitchDebugAppRepository");
                    _switchDebugApps = new SwitchDebugAppRepository(_connection);
                }
                return _switchDebugApps;
            }
        }

        public IDatabaseVersionRepository DatabaseVersions
        {
            get
            {
                if (_databaseVersions == null)
                {
                    _logger.Debug("Creating DatabaseVersionRepository");
                    _databaseVersions = new DatabaseVersionRepository(_connection);
                }
                return _databaseVersions;
            }
        }

        public async Task BeginTransactionAsync()
        {
            try
            {
                _logger.Debug("Beginning transaction");

                // Check if a transaction is already in progress
                if (_transaction != null)
                {
                    _logger.Error("Cannot begin a new transaction while another is in progress");
                    throw new TransactionException("Cannot begin a new transaction while another is in progress");
                }

                if (_connection.State != System.Data.ConnectionState.Open)
                {
                    _logger.Debug("Connection not open, opening connection");
                    await _connection.OpenAsync();
                }

                _transaction = _connection.BeginTransaction();
                _logger.Info("Transaction started successfully");
            }
            catch (TransactionException)
            {
                // Rethrow transaction exceptions
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to begin transaction", ex);
                throw new TransactionException("Failed to begin transaction", ex);
            }
        }

        public async Task CommitAsync()
        {
            try
            {
                _logger.Debug("Committing transaction");
                if (_transaction == null)
                {
                    _logger.Warn("No active transaction to commit");
                    return;
                }

                _transaction.Commit();
                _transaction = null;
                _logger.Info("Transaction committed successfully");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to commit transaction", ex);
                throw new TransactionException("Failed to commit transaction", ex);
            }
        }

        public async Task RollbackAsync()
        {
            try
            {
                _logger.Debug("Rolling back transaction");
                if (_transaction == null)
                {
                    _logger.Warn("No active transaction to roll back");
                    return;
                }

                _transaction.Rollback();
                _transaction = null;
                _logger.Info("Transaction rolled back successfully");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to roll back transaction", ex);
                throw new TransactionException("Failed to roll back transaction", ex);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _logger.Debug("Disposing transaction and connection");
                    _transaction?.Dispose();
                    _logger.Debug("Transaction disposed");
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
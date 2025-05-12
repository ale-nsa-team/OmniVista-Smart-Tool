using MVVM.Services.Interfaces;
using PoEWizard.Comm;
using PoEWizard.Data;
using PoEWizard.Device;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static PoEWizard.Data.Constants;

namespace MVVM.Services.Implementations
{
    public class NetworkService : INetworkService
    {
        private readonly RestApiClient _apiClient;
        private readonly AosSshService _sshService;
        private readonly SftpService _sftpService;
        private SwitchModel _switchModel;
        private bool _disposed = false;
        private CancellationTokenSource _operationCts;

        public bool IsConnected => _apiClient?.IsConnected() ?? false;
        public bool IsReady { get; set; }
        public int Timeout { get; set; }

        public SwitchModel SwitchModel
        {
            get => _switchModel;
            set => _switchModel = value;
        }

        public event EventHandler<NetworkStatusEventArgs> ConnectionStatusChanged;

        public event EventHandler<NetworkErrorEventArgs> ErrorOccurred;

        public NetworkService(SwitchModel switchModel)
        {
            _switchModel = switchModel ?? throw new ArgumentNullException(nameof(switchModel));
            _apiClient = new RestApiClient(switchModel);
            _sshService = new AosSshService(switchModel);
            _sftpService = new SftpService("localhost", 2214, switchModel.Login, switchModel.Password);
            Timeout = switchModel.CnxTimeout;
        }

        public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _operationCts?.Cancel();
                _operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _apiClient.Login();

                string sshPrompt = await Task.Run(() => _sshService.ConnectSshClient(), _operationCts.Token);
                string sftpError = await Task.Run(() => _sftpService.Connect(), _operationCts.Token);
                bool isConnected = IsConnected && _sshService.IsSwitchConnected() && _sftpService.IsConnected;

                IsReady = isConnected;
                OnConnectionStatusChanged(isConnected, isConnected ? "Connected successfully" : "Connection failed");
                return isConnected;
            }
            catch (OperationCanceledException)
            {
                OnConnectionStatusChanged(false, "Connection operation was canceled");
                throw;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex, "ConnectAsync", true);
                OnConnectionStatusChanged(false, $"Connection failed: {ex.Message}");
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                _operationCts?.Cancel();

                IsReady = false;
                _sshService.DisconnectSshClient();
                _sftpService.Disconnect();
                _apiClient.Close();

                OnConnectionStatusChanged(false, "Disconnected");
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex, "Disconnect");
            }
        }

        public Task<object> ExecuteCommandAsync(Command command, ParseType parseType = ParseType.Htable, string[] parameters = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> ExecuteRawCommandAsync(string command, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RebootSwitchAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> WaitForSwitchRebootAsync(int timeoutSeconds = 120, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> BackupConfigurationAsync(string destinationPath, IProgress<ProgressReport> progress = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RestoreConfigurationAsync(string sourcePath, IProgress<ProgressReport> progress = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> DownloadFileAsync(string remotePath, string localPath, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UploadFileAsync(string localPath, string remotePath, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> ListFilesAsync(string remotePath, string pattern = "*", CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnConnectionStatusChanged(bool isConnected, string message = null)
        {
            ConnectionStatusChanged?.Invoke(this, new NetworkStatusEventArgs(isConnected, message));
        }

        protected virtual void OnErrorOccurred(Exception exception, string operation, bool isFatal = false)
        {
            ErrorOccurred?.Invoke(this, new NetworkErrorEventArgs(exception, operation, isFatal));
            Logger.Error($"Network service error during {operation}: {exception.Message}", exception);
        }

        private CancellationToken GetLinkedCancellationToken(CancellationToken externalToken)
        {
            // Cancel any ongoing operations
            _operationCts?.Cancel();
            _operationCts?.Dispose();

            // Create a new CTS linked to the external token
            _operationCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            return _operationCts.Token;
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
                _operationCts?.Cancel();
                _operationCts?.Dispose();
                _operationCts = null;

                Disconnect();

                (_apiClient as IDisposable)?.Dispose();
                (_sshService as IDisposable)?.Dispose();
                (_sftpService as IDisposable)?.Dispose();
            }

            _disposed = true;
        }

        ~NetworkService()
        {
            Dispose(false);
        }
    }
}
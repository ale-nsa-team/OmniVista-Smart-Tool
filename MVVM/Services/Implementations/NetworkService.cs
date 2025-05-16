using Common.Data;
using Common.Enums;
using Common.Services;
using Common.Services.Implementations;
using MVVM.Models;
using MVVM.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MVVM.Services.Implementations
{
    public class NetworkService : INetworkService
    {
        private readonly IRestService _restService;
        private readonly ISshService _sshService;
        private readonly ISftpService _sftpService;
        private SwitchModel _switchModel;
        private bool _disposed = false;
        private CancellationTokenSource _operationCts;

        public bool IsConnected
        {
            get
            {
                if (_restService is null || _sshService is null || _sftpService is null)
                    return false;
                return _restService.IsConnected && _sshService.IsConnected && _sftpService.IsConnected;
            }
        }

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
            _restService = new RestService(switchModel);
            _sshService = new SshService(switchModel);
            _sftpService = new SftpService(switchModel);
            Timeout = switchModel.Timeout;
        }

        public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _operationCts?.Cancel();
                _operationCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                await Task.Run(() => _restService.Connect(), _operationCts.Token);
                await Task.Run(() => _sshService.Connect(), _operationCts.Token);
                await Task.Run(() => _sftpService.Connect(), _operationCts.Token);

                OnConnectionStatusChanged(IsConnected, IsConnected ? "Connected successfully" : "Connection failed");
                return IsConnected;
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

                _restService?.Disconnect();
                _sshService?.Disconnect();
                _sftpService?.Disconnect();

                OnConnectionStatusChanged(false, "Disconnected");
            }
            catch (Exception ex)
            {
                OnErrorOccurred(ex, "Disconnect");
            }
        }

        public Task<object> ExecuteCommandAsync(CommandLine command, ParseType parseType = ParseType.Htable, string[] parameters = null, CancellationToken cancellationToken = default)
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
            //Logger.Error($"Network service error during {operation}: {exception.Message}", exception);
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

                (_restService as IDisposable)?.Dispose();
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
using Common.Data;
using Common.Enums;
using MVVM.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MVVM.Services.Interfaces
{
    public interface INetworkService : IDisposable
    {
        bool IsConnected { get; }
        bool IsReady { get; set; }
        int Timeout { get; set; }
        SwitchModel SwitchModel { get; set; }

        Task<bool> ConnectAsync(CancellationToken cancellationToken = default);

        void Disconnect();

        Task<object> ExecuteCommandAsync(CommandLine command, ParseType parseType = ParseType.Htable, string[] parameters = null, CancellationToken cancellationToken = default);

        Task<string> ExecuteRawCommandAsync(string command, CancellationToken cancellationToken = default);

        Task<bool> RebootSwitchAsync(CancellationToken cancellationToken = default);

        Task<bool> WaitForSwitchRebootAsync(int timeoutSeconds = 120, CancellationToken cancellationToken = default);

        Task<bool> BackupConfigurationAsync(string destinationPath, IProgress<ProgressReport> progress = null, CancellationToken cancellationToken = default);

        Task<bool> RestoreConfigurationAsync(string sourcePath, IProgress<ProgressReport> progress = null, CancellationToken cancellationToken = default);

        Task<string> DownloadFileAsync(string remotePath, string localPath, CancellationToken cancellationToken = default);

        Task<bool> UploadFileAsync(string localPath, string remotePath, CancellationToken cancellationToken = default);

        Task<List<string>> ListFilesAsync(string remotePath, string pattern = "*", CancellationToken cancellationToken = default);

        event EventHandler<NetworkStatusEventArgs> ConnectionStatusChanged;

        event EventHandler<NetworkErrorEventArgs> ErrorOccurred;
    }

    public class NetworkStatusEventArgs : EventArgs
    {
        public bool IsConnected { get; }
        public string Message { get; }

        public NetworkStatusEventArgs(bool isConnected, string message = null)
        {
            IsConnected = isConnected;
            Message = message;
        }
    }

    public class NetworkErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string Operation { get; }
        public bool IsFatal { get; }

        public NetworkErrorEventArgs(Exception exception, string operation, bool isFatal = false)
        {
            Exception = exception;
            Operation = operation;
            IsFatal = isFatal;
        }
    }
}
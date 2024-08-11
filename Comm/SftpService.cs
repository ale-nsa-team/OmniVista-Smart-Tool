using System;
using System.IO;
using System.Threading;
using PoEWizard.Data;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace PoEWizard.Comm
{
    public class SftpService
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsConnected => _sftpClient?.IsConnected ?? false;

        private SftpClient _sftpClient;

        public SftpService(string host, string user, string password) : this(host, 22, user, password) { }

        public SftpService(string host, int port, string user, string password)
        {
            Host = host;
            Port = port;
            Username = user;
            Password = password;
            _sftpClient = new SftpClient(host, port, user, password);
        }

        public void Connect()
        {
            try
            {
                if (!_sftpClient.IsConnected) _sftpClient.Connect();
            }
            catch (Exception ex) 
            {
                Logger.Error("Error connecting to switch.", ex);
            }
        }

        public void ResetConnection()
        {
            try
            {
                _sftpClient?.Disconnect();
                Thread.Sleep(1000);
                _sftpClient = new SftpClient(Host, Port, Username, Password);
                _sftpClient.Connect();
            }
            catch (Exception ex)
            {
                Logger.Error("Error reset connecting to switch.", ex);
            }
        }

        public void UploadFile(string localPath, string remotePath)
        {
            try
            {
                using (var fs = new FileStream(localPath, FileMode.Open))
                {
                    _sftpClient.UploadFile(fs, remotePath);
                }
            }
            catch (Exception ex) 
            {
                Logger.Error("Error uploading file.", ex);
            }
        }

        public string DownloadFile(string remotePath)
        {
            try
            {
                DateTime startTime = DateTime.Now;
                string localPath = Path.Combine(MainWindow.dataPath, Path.GetFileName(remotePath));
                using (var fs = new FileStream(localPath, FileMode.Create))
                {
                    _sftpClient.DownloadFile(remotePath, fs);
                }
                Logger.Info($"End download file\n{localPath}\nDuration : {Utils.CalcStringDuration(startTime)}");
                return localPath;
            }
            catch (Exception ex)
            {
                Logger.Error("Error downloading file.", ex);
            }
            return null;
        }

        public string DownloadToMemory(string remotePath)
        {
            try
            {
                return _sftpClient.ReadAllText(remotePath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return string.Empty;
        }

        public void DeleteFile(string remotePath)
        {
            try
            {
                _sftpClient.DeleteFile(remotePath);
            }
            catch { }
        }

        public long GetFileSize(string remotePath)
        {
            try
            {
                ISftpFile file = _sftpClient.Get(remotePath);
                return file.Attributes.Size;
            }
            catch (Exception ex)
            {
                if (!ex.Message.ToLower().Contains("no such file")) Logger.Error(ex);
            }
            return 0;
        }

        public void Disconnect()
        {
            _sftpClient?.Disconnect();
        }
    }
}

using System;
using System.IO;
using PoEWizard.Data;
using PoEWizard.Device;
using Renci.SshNet;

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

        public SftpService() : this(DeviceModel.IpAddress, 22, DeviceModel.Username, DeviceModel.Password) { }

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
                _sftpClient.Connect();
            }
            catch (Exception ex) 
            {
                Logger.Error("Error connecting to switch.", ex);
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
            catch (IOException ex) 
            {
                Logger.Error("Error uploading file.", ex);
            }
        }

        public void DownloadFile(string remotePath)
        {
            try
            {
                using (var fs = new FileStream(Path.GetFileName(remotePath), FileMode.OpenOrCreate))
                {
                    _sftpClient.DownloadFile(remotePath, fs);
                }
            }
            catch (IOException ex)
            {
                Logger.Error("Error downloading file.", ex);
            }
        }

        public void Disconntect()
        {
            _sftpClient?.Disconnect();
        }
    }
}

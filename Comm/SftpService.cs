using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        public string Connect()
        {
            string sftpError;
            try
            {
                if (!_sftpClient.IsConnected) _sftpClient.Connect();
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error connecting to switch {Host} (Port: {Port}, User: {Username})", ex);
                sftpError = ex.Message;
            }
            return sftpError;
        }

        public void ResetConnection()
        {
            try
            {
                _sftpClient?.Disconnect();
                Thread.Sleep(1000);
                _sftpClient = new SftpClient(Host, Port, Username, Password);
                _sftpClient.Connect();
                Logger.Warn($"Resetting SFTP connection with {Host}");
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
                if (remotePath.Contains("*")) DeleteFileWC(remotePath);
                else _sftpClient.DeleteFile(remotePath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
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

        private IEnumerable<ISftpFile> ListDirectoryWC(string pattern)
        {
            string directoryName = (pattern[0] == '/' ? "" : "/") + pattern.Substring(0, pattern.LastIndexOf('/'));
            string regexPattern = pattern.Substring(pattern.LastIndexOf('/') + 1)
                    .Replace(".", "\\.")
                    .Replace("*", ".*")
                    .Replace("?", ".");
            Regex reg = new Regex('^' + regexPattern + '$');

            var results = _sftpClient.ListDirectory(string.IsNullOrEmpty(directoryName) ? "/" : directoryName);
            
            return results.Where(e => reg.IsMatch(e.Name));

        }

        private void DeleteFileWC(string pattern)
        {
            foreach (var file in ListDirectoryWC(pattern))
            {
                file.Delete();
            }
        }
    }
}

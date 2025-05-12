using PoEWizard.Data;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using static PoEWizard.Data.Utils;

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

        public bool UploadFile(string localPath, string remotePath, bool overWrite = false)
        {
            try
            {
                using (var fs = new FileStream(localPath, FileMode.Open))
                {
                    _sftpClient.UploadFile(fs, remotePath, overWrite);
                }
                UpdateLastWriteTime(localPath, remotePath);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error uploading file {remotePath}", ex);
            }
            return false;
        }

        public bool UploadStream(Stream ms, string remotePath, bool overWrite = false)
        {
            try
            {
                _sftpClient.UploadFile(ms, remotePath, overWrite);
                return true;
            }

            catch (Exception ex)
            {
                Logger.Error($"Error uploading file {remotePath}", ex);
            }
            return false;
        }

        private void UpdateLastWriteTime(string localPath, string remotePath)
        {
            try
            {
                _sftpClient.SetLastWriteTime(remotePath, File.GetLastWriteTime(localPath));
            }
            catch { }
        }

        public string DownloadFile(string remotePath, string destPath = null)
        {
            try
            {
                DateTime startTime = DateTime.Now;
                string localPath;
                if (string.IsNullOrEmpty(destPath))
                {
                    localPath = Path.Combine(MainWindow.DataPath, Path.GetFileName(remotePath));
                }
                else
                {
                    localPath = Path.Combine(MainWindow.DataPath, destPath);
                    CreateLocalDirectory(destPath);
                }
                using (var fs = new FileStream(localPath, FileMode.Create))
                {
                    _sftpClient.DownloadFile(remotePath, fs);
                }
                Logger.Info($"End download file\n{localPath}\nDuration : {CalcStringDuration(startTime)}");
                return localPath;
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("No such file")) Logger.Error("Error downloading file.", ex);
            }
            return null;
        }

        public void UnzipBackupSwitchFiles(string selFilePath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(selFilePath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    CreateLocalDirectory(entry.FullName);
                    try
                    {
                        string fileName = Path.GetFileName(entry.FullName);
                        if (!string.IsNullOrEmpty(fileName)) entry.ExtractToFile(Path.Combine(MainWindow.DataPath, entry.FullName), true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                }
            }
        }

        private void CreateLocalDirectory(string destPath)
        {
            string dir = Path.GetDirectoryName(destPath);
            string[] split = dir.Split('\\');
            string destDir = MainWindow.DataPath;
            foreach (string fld in split)
            {
                if (string.IsNullOrEmpty(fld)) continue;
                destDir = Path.Combine(destDir, fld.Trim());
                if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);
            }
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
                if (!ex.Message.Contains("No such file")) Logger.Error(ex);
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

        public List<string> GetFilesInRemoteDir(string remoteDir, string suffix = null)
        {
            List<string> filesList = new List<string>();
            if (_sftpClient.IsConnected)
            {
                var files = _sftpClient.ListDirectory(remoteDir);
                foreach (var file in files)
                {
                    if (file.IsRegularFile)
                    {
                        if (string.IsNullOrEmpty(suffix)) filesList.Add(file.Name);
                        else if (file.Name.EndsWith(suffix.Replace("*", string.Empty))) filesList.Add(file.Name);
                    }
                }
            }
            return filesList;
        }

        public void Disconnect()
        {
            _sftpClient?.Disconnect();
        }

        private IEnumerable<ISftpFile> ListDirectoryWC(string pattern)
        {
            string directoryName = (pattern[0] == '/' ? "" : "/") + pattern.Substring(0, pattern.LastIndexOf('/'));
            string regexPattern = pattern.Substring(pattern.LastIndexOf('/') + 1).Replace(".", "\\.").Replace("*", ".*").Replace("?", ".");
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

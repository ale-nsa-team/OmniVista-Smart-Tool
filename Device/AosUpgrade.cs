using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using PoEWizard.Comm;
using PoEWizard.Data;
using static PoEWizard.Data.Utils;
using static PoEWizard.Data.Constants;
using PoEWizard.Exceptions;
using System.Text.RegularExpressions;

namespace PoEWizard.Device
{
    public static class AosUpgrade
    {
        private const string SHA_SUM = "imgsha256sum";
        private const string LSM = "software.lsm";

        private static List<string> imgFiles;
        private static List<string> reqFiles;
        private static SwitchModel swModel;
        private static SftpService sftpSrv;

        public static IProgress<ProgressReport> Progress { get; set; }

        public static bool UpgradeAos(SwitchModel model, string archive)
        {
            try
            {
                string err = OpenScp(model);
                if (err != null) throw new UpgradeException(Translate("i18n_aosUpg"), err);

                imgFiles = sftpSrv.GetFilesInRemoteDir(FLASH_WORKING_DIR, "img");
                if (imgFiles.Count == 0) throw new UpgradeException(Translate("i18n_aosUpg"), $"{Translate("i18n_noImg")} {FLASH_WORKING_DIR}");

                reqFiles = imgFiles.Concat(new List<string> { SHA_SUM, LSM }).ToList();
                List<string> files = CheckMissingFilesInArchive(archive) ??
                    throw new UpgradeException(Translate("i18n_aosUpg"), $"{Translate("i18n_zipErr")} {archive}");
                if (files.Count > 0) throw new UpgradeException(Translate("i18n_aosUpg"), $"{Translate("i18n_upgErr")}: {string.Join(", ", files)}");

                ExtractArchive(archive);
                foreach (string file in reqFiles)
                {
                    string localFile = Path.Combine(Path.GetTempPath(), file);
                    string remoteFile = $"{FLASH_WORKING_DIR}/{file}";
                    if (!sftpSrv.UploadFile(localFile, remoteFile, true)) 
                        throw new UpgradeException(Translate("i18n_aosUpg"), $"{Translate("i18n_uplErr")} {file}");
                }
                DeleteTempFiles();
                return true;
            }
            catch (UpgradeException ex)
            {
                Progress.Report(new ProgressReport(ReportType.Error, ex.Source, ex.Message));
                return false;
            }
            finally
            {
                if (sftpSrv.IsConnected) sftpSrv.Disconnect();
            }
        }

        public static bool UpgradeUboot(SwitchModel model, string archive)
        {
            try
            {
                if (model.Uboot == "N/A") throw new UpgradeException(Translate("i18n_ubootUpg"), Translate("i18n_noUboot"));
                string err = OpenScp(model);
                if (err != null) throw new UpgradeException(Translate("i18n_ubootUpg"), err);
                string ubootf = ExtractUbootFromArchive(archive) ?? 
                    throw new UpgradeException(Translate("i18n_ubootUpg"), Translate("i18n_noUbootf"));
                string localFile = Path.Combine(Path.GetTempPath(), ubootf);
                string remoteFile = $"/{FLASH_DIR}/{ubootf}";
                if (!sftpSrv.UploadFile(localFile, remoteFile, true)) 
                    throw new UpgradeException(Translate("i18n_ubootUpg"), $"{Translate("i18n_uplErr")} {ubootf} to switch");
                if (sftpSrv.IsConnected) sftpSrv.Disconnect();
                try { File.Delete(localFile); } catch { }
                SendUpgradeCommand(remoteFile);
                return true;
            }
            catch (UpgradeException ex)
            {
                Progress.Report(new ProgressReport(ReportType.Error, ex.Source, ex.Message));
                if (sftpSrv.IsConnected) sftpSrv.Disconnect();
                return false;
            }
        }

        public static void UpgradeOnie(SwitchModel model)
        {
            Progress.Report(new ProgressReport(ReportType.Warning, Translate("i18n_onieUpg"), Translate("i18n_noOnie")));
        }

        private static string OpenScp(SwitchModel model)
        {
            swModel = model;
            if (sftpSrv == null)
            {
                sftpSrv = new SftpService(model.IpAddress, model.Login, model.Password);
            }
            return sftpSrv.Connect();
        }

        private static List<string> CheckMissingFilesInArchive(string path)
        {
            List<string> files = new List<string>(reqFiles);

            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(path))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (files.Contains(entry.Name)) files.Remove(entry.Name);
                    }
                }
                return files;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error opening archive {path}", ex);
            }
            return null;
        }

        private static string ExtractUbootFromArchive(string path)
        {
            try
            {
                var regex = new Regex(@"u-boot.+\.tar\.gz");
                using (ZipArchive archive = ZipFile.OpenRead(path))
                {
                    var uboot = archive.Entries.FirstOrDefault(f => regex.IsMatch(f.Name));
                    if (uboot == null) return null;
                    string tempFolder = Path.GetTempPath();
                    uboot.ExtractToFile(Path.Combine(tempFolder, uboot.Name), true);
                    return uboot.Name;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error opening archive {path}", ex);
            }
            return null;
        }

        private static void ExtractArchive(string path)
        {
            string tempFolder = Path.GetTempPath();
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(path))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (reqFiles.Contains(entry.Name)) entry.ExtractToFile(Path.Combine(tempFolder, entry.Name), true);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error extracting archive {path}", ex);
            }
        }

        private static void DeleteTempFiles()
        {
            foreach (string file in reqFiles)
            {
                try
                {
                    File.Delete(Path.Combine(Path.GetTempPath(), file));
                }
                catch { }
            }
        }

        private static void SendUpgradeCommand(string filepath)
        {
            AosSshService ssh = new AosSshService(swModel);
            ssh.ConnectSshClient();
            Dictionary<string, string> response = ssh.SendCommand(new RestUrlEntry(Command.UPDATE_UBOOT), new string[] { filepath });
            if (response != null && response.ContainsKey("output") && !string.IsNullOrEmpty(response["output"]))
            {
                if (response["output"].Contains("ERROR"))
                {
                    string err = Regex.Match(response["output"], "ERROR: (.+)$").Groups[1].Value;
                    throw new UpgradeException(Translate("i18n_ubootUpg"), err);
                }
            }
        }
    }
}

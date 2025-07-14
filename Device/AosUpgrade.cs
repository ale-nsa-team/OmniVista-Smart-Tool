using PoEWizard.Comm;
using PoEWizard.Components;
using PoEWizard.Data;
using PoEWizard.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.Utils;

namespace PoEWizard.Device
{
    public static class AosUpgrade
    {
        // Product Family to Image Name mapping
        private static readonly Dictionary<string, string> FamilyImageMap = new Dictionary<string, string>
        {
            { "OS6360", "Nosa.img" },
            { "OS6465", "Nos.img" },
            { "OS6560", "Nos.img" },
            { "OS6570M", "Wos.img" },
            { "OS6870", "Kaosn.img" },
            { "OS6860", "Uos.img" },
            { "OS6860N", "Uosn.img" },
            { "OS6860-N", "Uosn.img" },
            { "OS6865", "Uos.img" },
            { "OS6869-N", "Uosn.img" },
            { "OS6900-Yukon", "Yos.img" },
            { "OS9900", "Mos.img" },
            // Deprecated platforms
            { "OS10K", "Ros.img" },
            { "OS6900", "Tos.img" }
        };

        private const string SHA_SUM = "imgsha256sum";
        private const string LSM = "software.lsm";

        private static List<string> reqFiles;
        private static SwitchModel swModel;
        private static SftpService sftpSrv;

        public static IProgress<ProgressReport> Progress { get; set; }

        public static bool UpgradeAos(SwitchModel model, string archive)
        {
            try
            {
                string source = Translate("i18n_aosUpg");
                Progress.Report(new ProgressReport(ReportType.Status, source, Translate("i18n_upgCheck")));
                // Get the expected image name for this switch model
                string expectedImageName;
                try
                {
                    expectedImageName = GetExpectedImageName(model.Model);
                }
                catch (ArgumentException ex)
                {
                    throw new UpgradeException(source, ex.Message);
                }

                // Set required files list with expected image
                reqFiles = new List<string> { expectedImageName, SHA_SUM, LSM };

                string err = OpenScp(model);
                if (err != null) throw new UpgradeException(source, err);
                
                List<string> files = CheckMissingFilesInArchive(archive) ??
                    throw new UpgradeException(source, $"{Translate("i18n_zipErr")} {archive}");
                if (files.Count > 0)
                {
                    // Check if expected image file is missing - provide specific guidance
                    if (files.Contains(expectedImageName))
                    {
                        string errorMsg = $"{Translate("i18n_archiveMissingImage", expectedImageName, model.Model)}." +
                                         $"\n{Translate("i18n_expectedImageGuide", model.Model, expectedImageName)}. " +
                                         $"{Translate("i18n_checkFamilyMapping")}.";
                        throw new UpgradeException(source, errorMsg);
                    }
                    else if (files.Count == 1 && files[0] == SHA_SUM)
                    {
                        bool res = true;
                        MainWindow.Instance.Dispatcher.Invoke(new Action(() =>
                        {
                            CustomMsgBox cmb = new CustomMsgBox(MainWindow.Instance, MsgBoxButtons.YesNo)
                            {
                                Header = source,
                                Message = Translate("i18n_noFile", SHA_SUM)
                            };
                            cmb.ShowDialog();
                            if (cmb.Result == MsgBoxResult.No)
                            {
                                DeleteTempFiles();
                                res = false;
                            }
                            else
                            {
                                reqFiles.Remove(SHA_SUM);
                            }
                        }));

                        if (!res) return false;
                    }
                    else
                    {
                        throw new UpgradeException(source, $"{Translate("i18n_upgErr")}: {string.Join(", ", files)}");
                    }
                }
                Progress.Report(new ProgressReport(ReportType.Status, source, Translate("i18n_upgUpl")));
                ExtractArchive(archive);
                foreach (string file in reqFiles)
                {
                    string localFile = Path.Combine(Path.GetTempPath(), file);
                    string remoteFile = $"{FLASH_WORKING_DIR}/{file}";
                    if (!sftpSrv.UploadFile(localFile, remoteFile, true))
                        throw new UpgradeException(source, $"{Translate("i18n_uplErr")} {file}");
                }
                DeleteTempFiles();
                Logger.Activity($"Switch {model.Name}, Model {model.Model}: AOS upgraded to {Path.GetFileNameWithoutExtension(archive)}");
                return true;
            }
            catch (UpgradeException ex)
            {
                Progress.Report(new ProgressReport(ReportType.Error, ex.Source, ex.Message));
                Logger.Error($"Switch {model.Name}, Model {model.Model}: AOS upgrade failed - {ex.Message}");
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
                string source = Translate("i18n_ubootUpg");
                if (model.Uboot == "N/A") throw new UpgradeException(source, Translate("i18n_noUboot"));
                Progress.Report(new ProgressReport(ReportType.Status, source, Translate("i18n_upgCheck")));
                string err = OpenScp(model);
                if (err != null) throw new UpgradeException(source, err);
                string ubootf = ExtractUbootFromArchive(archive) ?? throw new UpgradeException(source, Translate("i18n_noUbootf"));
                string localFile = Path.Combine(Path.GetTempPath(), ubootf);
                string remoteFile = $"/{FLASH_DIR}/{ubootf}";
                Progress.Report(new ProgressReport(ReportType.Status, source, Translate("i18n_upgUpl")));
                if (!sftpSrv.UploadFile(localFile, remoteFile, true)) 
                    throw new UpgradeException(source, $"{Translate("i18n_uplErr")} {ubootf} to switch");
                if (sftpSrv.IsConnected) sftpSrv.Disconnect();
                try { File.Delete(localFile); } catch { }
                Progress.Report(new ProgressReport(ReportType.Status, source, Translate("i18n_ubootCmd")));
                SendUpgradeCommand(remoteFile);
                Logger.Activity($"Switch {model.Name}, Model {model.Model}: U-Boot upgraded to {Path.GetFileNameWithoutExtension(archive)}");
                return true;
            }
            catch (UpgradeException ex)
            {
                Progress.Report(new ProgressReport(ReportType.Error, ex.Source, ex.Message));
                Logger.Error($"Switch {model.Name}, Model {model.Model}: U-Boot upgrade failed - {ex.Message}");
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

        private static string GetExpectedImageName(string productFamily)
        {
            // Try exact match first
            if (FamilyImageMap.TryGetValue(productFamily, out var imageName))
            {
                return imageName;
            }

            // If exact match fails, try to find a base family that matches as a prefix
            foreach (var family in FamilyImageMap.Keys)
            {
                if (productFamily.StartsWith(family))
                {
                    return FamilyImageMap[family];
                }
            }

            var supportedFamilies = string.Join(", ", FamilyImageMap.Keys.OrderBy(k => k));
            string errorMsg = $"{Translate("i18n_unsupportedFamily", productFamily)}. " +
                             $"{Translate("i18n_supportedFamilies", supportedFamilies)}. " +
                             $"{Translate("i18n_ensureSupported")}.";
            throw new ArgumentException(errorMsg);
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
            try
            {
                AosSshService ssh = new AosSshService(swModel);
                ssh.ConnectSshClient();
                Dictionary<string, string> response = ssh.SendCommand(new RestUrlEntry(Command.UPDATE_UBOOT), new string[] { filepath });
                ssh.DisconnectSshClient();
                ssh.Dispose();

                if (response != null && response.ContainsKey("output") && !string.IsNullOrEmpty(response["output"]))
                {
                    if (response["output"].Contains("ERROR"))
                    {
                        string err = Regex.Match(response["output"], "ERROR: (.+)$").Groups[1].Value;
                        throw new Exception(err);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new UpgradeException(Translate("i18n_ubootUpg"), ex.Message);
            }

        }
    }
}

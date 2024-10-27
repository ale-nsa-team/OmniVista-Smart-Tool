using PoEWizard.Comm;
using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    public static class FactoryDefault
    {
        private const string TEMPLATE = "vcboot_template.txt";
        private static RestApiService restSvc;
        private static SwitchModel swModel;
        public static IProgress<ProgressReport> Progress { get; set; }

        private static readonly List<string> files = new List<string>()
        {
            $"/{FLASH_WORKING_DIR}/*.cfg",
            $"/{FLASH_CERTIFIED_DIR}/*.cfg",
            $"/{FLASH_WORKING_DIR}/*.conf",
            $"/{FLASH_CERTIFIED_DIR}/*.conf",
            $"/{FLASH_WORKING_DIR}/*.sav",
            $"/{FLASH_CERTIFIED_DIR}/*.sav",
            $"/{FLASH_WORKING_DIR}/*.md5",
            $"/{FLASH_CERTIFIED_DIR}/*.md5",
            $"/{FLASH_WORKING_DIR}/*.txt",
            $"/{FLASH_CERTIFIED_DIR}/*.txt",
            $"/{FLASH_WORKING_DIR}/*.cfg-ft",
            $"/{FLASH_CERTIFIED_DIR}/*.cfg-ft",
            $"/{FLASH_WORKING_DIR}/Udiag.img",
            $"/{FLASH_WORKING_DIR}/Urescue.img",
            $"/{FLASH_CERTIFIED_DIR}/Udiag.img",
            $"/{FLASH_CERTIFIED_DIR}/Urescue.img",
            $"/{FLASH_DIR}/*.err",
            $"/{FLASH_DIR}/*.cfg",
            $"/{FLASH_DIR}/tech*",
            $"/{FLASH_DIR}/swlog_archive/*",
            $"/{FLASH_DIR}/system/user*",
            $"/{FLASH_DIR}/switch/cloud/*",
            $"/{FLASH_DIR}/switch/dhcpd*",
            $"/{FLASH_DIR}/switch/*.txt",
            $"/{FLASH_DIR}/libcurl*",
            $"/{FLASH_DIR}/agcmm*",
            $"/{FLASH_WORKING_DIR}/pkg/*",
            $"/{FLASH_CERTIFIED_DIR}/pkg/*",
            $"/{FLASH_DIR}/pmd",
            $"/{FLASH_DIR}/serial.txt",
            $"/{FLASH_DIR}/rcl.log"
        };

        public static void Reset(SwitchModel device)
        {
            Progress.Report(new ProgressReport("Removing config files..."));
            swModel = device;
            SftpService sftp = new SftpService(device.IpAddress, "admin", device.Password);
            sftp.Connect();
            //foreach (string file in files)
            //{
            //    sftp.DeleteFile(file);
            //}
            restSvc = MainWindow.restApiService;
            restSvc.RunSwitchCommand(new CmdRequest(Command.CLEAR_SWLOG));
            sftp.DeleteFile($"/{FLASH_DIR}/.bash_history");
            LoadTemplate(TEMPLATE);
            sftp.UploadFile(Path.Combine(MainWindow.DataPath, TEMPLATE), VCBOOT_PATH);
            sftp.Disconnect();
        }

        private static void LoadTemplate(string filename)
        {
            try
            {
                string content = ReadFromDisk(filename);
                string res = content.Replace("{Name}", swModel.Name)
                    .Replace("{Location}", swModel.Location)
                    .Replace("{IpAddress}", swModel.IpAddress)
                    .Replace("{SubnetMask}", swModel.NetMask);
                if (!string.IsNullOrEmpty(swModel.DefaultGwy))
                {
                    res += $"ip static-route 0.0.0.0/0 gateway {swModel.DefaultGwy}";
                }
                string path = Path.Combine(MainWindow.DataPath, filename);
                if (File.Exists(path)) File.Delete(path);
                using (TextWriter writer = new StreamWriter(path))
                {
                    writer.NewLine = "\n";
                    foreach (string line in res.Split('\n'))
                    {
                        writer.WriteLine(line);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception while writing config file {filename}: {ex.Message}");
            }

        }

        public static string ReadFromDisk(string filename)
        {
            {
                try
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    using (StreamReader reader = new StreamReader(assembly.GetManifestResourceStream($"PoEWizard.Resources.{filename}")))
                    {
                        return reader.ReadToEnd();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Exception while reading config file {filename}: {ex.Message}");
                    return null;
                }
            }
        }
    }
}

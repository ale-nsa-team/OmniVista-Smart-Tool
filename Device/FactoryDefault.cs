using PoEWizard.Comm;
using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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
            "/flash/working/*.cfg",
            "/flash/certified/*.cfg",
            "/flash/working/*.conf",
            "/flash/certified/*.conf",
            "/flash/working/*.sav",
            "/flash/certified/*.sav",
            "/flash/working/*.md5",
            "/flash/certified/*.md5",
            "/flash/working/*.txt",
            "/flash/certified/*.txt",
            "/flash/working/*.cfg-ft",
            "/flash/certified/*.cfg-ft",
            "/flash/working/Udiag.img",
            "/flash/working/Urescue.img",
            "/flash/certified/Udiag.img",
            "/flash/certified/Urescue.img",
            "/flash/*.err",
            "/flash/*.cfg",
            "/flash/tech*",
            "/flash/swlog_archive/*",
            "/flash/system/user*",
            "/flash/switch/cloud/*",
            "/flash/switch/dhcpd*",
            "/flash/switch/*.txt",
            "/flash/libcurl*",
            "/flash/agcmm*",
            "/flash/working/pkg/*",
            "/flash/certified/pkg/*",
            "/flash/pmd",
            "/flash/serial.txt",
            "/flash/rcl.log"
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
            sftp.DeleteFile("/flash/.bash_history");
            LoadTemplate(TEMPLATE);
            sftp.UploadFile(Path.Combine(MainWindow.dataPath, TEMPLATE), "/flash/working/vcboot.cfg");
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
                string path = Path.Combine(MainWindow.dataPath, filename);
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

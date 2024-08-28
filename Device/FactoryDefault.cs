using PoEWizard.Comm;
using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PoEWizard.Device
{
    public static class FactoryDefault
    {
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

        private static void Reset(SwitchModel device)
        {
            Progress.Report(new ProgressReport("Removing config files..."));
            swModel = device;
            SftpService sftp = new SftpService(device.IpAddress, "admin", device.Password);
            sftp.Connect();
            foreach (string file in files)
            {
                sftp.DeleteFile(file);
            }
            restSvc = MainWindow.restApiService;
            restSvc.RunSwitchCommand(new CmdRequest(Command.CLEAR_SWLOG));
            sftp.DeleteFile("/flash/.bash_history");
            
            sftp.Disconnect();
            restSvc.RebootSwitch(600);
        }

        private static string LoadTemplate(string filename)
        {
            try
            {
                string path = Path.Combine(MainWindow.dataPath, filename);
                IEnumerable<string> lines = ReadFromDisk(Path.Combine("Resources", filename));
                var res = lines.Select(l => l.Replace("{Name}", swModel.Name));
                res = res.Select(l => l.Replace("{Location}", swModel.Location));
                res = res.Select(l => l.Replace("{IpAddress}", swModel.IpAddress));
                res = res.Select(l => l.Replace("{SubnetMask}", swModel.NetMask));
                File.WriteAllLines(path, res);
                return path;
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception while writing config file {filename}: {ex.Message}");
                return null;
            }

        }

        public static IEnumerable<string> ReadFromDisk(string filepath)
        {
            string filename = Path.GetFileName(filepath);
            IEnumerable<string> lines = new List<string>();

            if (File.Exists(filepath))
            {
                try
                {
                    lines = File.ReadLines(filepath, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Exception while reading config file {filename}: {ex.Message}");
                    return null;
                }
            }
            return lines;
        }
    }
}

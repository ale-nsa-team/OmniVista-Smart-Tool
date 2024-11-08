using PoEWizard.Comm;
using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.Utils;

namespace PoEWizard.Device
{
    public static class FactoryDefault
    {
        private const string TEMPLATE = "vcboot_template.txt";
        private static RestApiService restSvc;
        private static SwitchModel swModel;
        private static readonly string templateFilePath = Path.Combine(MainWindow.DataPath, TEMPLATE);
        public static IProgress<ProgressReport> Progress { get; set; }

        private static readonly List<string> files = new List<string>()
        {
            $"{FLASH_WORKING_DIR}/*.cfg",
            $"{FLASH_CERTIFIED_DIR}/*.cfg",
            $"{FLASH_WORKING_DIR}/*.conf",
            $"{FLASH_CERTIFIED_DIR}/*.conf",
            $"{FLASH_WORKING_DIR}/*.sav",
            $"{FLASH_CERTIFIED_DIR}/*.sav",
            $"{FLASH_WORKING_DIR}/*.md5",
            $"{FLASH_CERTIFIED_DIR}/*.md5",
            $"{FLASH_WORKING_DIR}/*.txt",
            $"{FLASH_CERTIFIED_DIR}/*.txt",
            $"{FLASH_WORKING_DIR}/*.cfg-ft",
            $"{FLASH_CERTIFIED_DIR}/*.cfg-ft",
            $"{FLASH_WORKING_DIR}/Udiag.img",
            $"{FLASH_WORKING_DIR}/Urescue.img",
            $"{FLASH_CERTIFIED_DIR}/Udiag.img",
            $"{FLASH_CERTIFIED_DIR}/Urescue.img",
            $"{FLASH_DIR}/*.err",
            $"{FLASH_DIR}/*.cfg",
            $"{FLASH_DIR}/tech*",
            $"{FLASH_DIR}/swlog_archive/*",
            $"{FLASH_DIR}/system/user*",
            $"{FLASH_DIR}/switch/cloud/*",
            $"{FLASH_DIR}/switch/dhcpd*",
            $"{FLASH_DIR}/switch/*.txt",
            $"{FLASH_DIR}/libcurl*",
            $"{FLASH_DIR}/agcmm*",
            $"{FLASH_WORKING_DIR}/pkg/*",
            $"{FLASH_CERTIFIED_DIR}/pkg/*",
            $"{FLASH_DIR}/pmd",
            $"{FLASH_DIR}/serial.txt",
            $"{FLASH_DIR}/rcl.log",
            $"{FLASH_DIR}/.bash_history"
        };

        public static void Reset(SwitchModel device)
        {
            restSvc = MainWindow.restApiService;
            Progress.Report(new ProgressReport(Translate("i18n_frPrep")));
            swModel = device;
            LinuxCommandSeq cmdSeq = new LinuxCommandSeq();
            List<LinuxCommand> cmds = new List<LinuxCommand>();
            foreach (string file in files)
            {
                cmds.Add(new LinuxCommand($"rm -f {file}"));
            }
            cmdSeq.AddCommandSeq(cmds);
            LinuxCommandSeq res = restSvc.SendSshLinuxCommandSeq(cmdSeq, Translate("i18n_frDelete"));
            List<string> errs = new List<string>();
            foreach (LinuxCommand cmd in cmds)
            {
                var resp = res.GetResponse(cmd.Command);
                if (resp.ContainsKey(ERROR)) errs.Add(resp[ERROR]);
            }
            if (errs.Count > 0)
            {
                Progress.Report(new ProgressReport(ReportType.Error, string.Join("\n", errs), Translate("i18n_fctRst")));
            }
            restSvc.RunSwitchCommand(new CmdRequest(Command.CLEAR_SWLOG));
            Progress.Report(new ProgressReport(Translate("i18n_frTmplt")));
            LoadTemplate();
            SftpService sftp = new SftpService(device.IpAddress, device.Login, device.Password);
            sftp.Connect();
            sftp.UploadFile(templateFilePath, VCBOOT_WORK);
            sftp.UploadFile(templateFilePath, VCBOOT_CERT);
            sftp.Disconnect();
            File.Delete(templateFilePath);
        }

        private static void LoadTemplate()
        {
            try
            {
                string content = ReadFromDisk();
                if (!string.IsNullOrEmpty(swModel.DefaultGwy))
                {
                    content += $"ip static-route 0.0.0.0/0 gateway {swModel.DefaultGwy}\n";
                }
                // get management vlan
                VlanModel vlan = restSvc.VlanSettings.FirstOrDefault(v => v.IpAddress == swModel.IpAddress);
                if (vlan != null)
                {
                    if (vlan.Device.Equals(EMP, StringComparison.OrdinalIgnoreCase))
                    {
                        content += $"ip interface master emp address {vlan.IpAddress} mask {vlan.SubnetMask}\n";
                    }
                    else if (vlan.Device.ToLower().Contains("vlan"))
                    {
                        List<string> tagged = GetTaggedPorts(vlan.Device);
                        if (tagged.Count == 0) vlan.Device = "vlan 1";
                        content += $"{vlan.Device} admin-state enable\n";
                        content += $"spantree {vlan.Device} admin-state enable\n";
                        content += $"ip interface {MGT_IF_NAME} address {vlan.IpAddress} mask {vlan.SubnetMask} {vlan.Device}\n";
                        for (int i = 0; i < tagged.Count; i++)
                        {
                            if (tagged[i].StartsWith(LINKAGG_PFX)) //linkagg
                            {
                                tagged[i] = GetLinkAggPrimary(tagged[i]);
                            }
                            if (tagged[i] != null) content += $"{vlan.Device} members port {tagged[i]} tagged\n";
                        }
                    }
                }
                if (File.Exists(templateFilePath)) File.Delete(templateFilePath);
                using (TextWriter writer = new StreamWriter(templateFilePath))
                {
                    writer.Write(content);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception while writing config file {TEMPLATE}: {ex.Message}");
            }
        }

        public static string ReadFromDisk()
        {
            {
                try
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    using (StreamReader reader = new StreamReader(assembly.GetManifestResourceStream($"PoEWizard.Resources.{TEMPLATE}")))
                    {
                        return reader.ReadToEnd();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Exception while reading config file {TEMPLATE}: {ex.Message}");
                    return null;
                }
            }
        }

        private static List<string> GetTaggedPorts(string device)
        {
            string vlan = Regex.Split(device, @"\s+")[1];
            List<Dictionary<string, string>> members = restSvc.RunSwitchCommand(new CmdRequest(Command.SHOW_VLAN_MEMBERS, ParseType.Htable, vlan)) as List<Dictionary<string, string>>;
            return members.Where(m => m[MEMBER_TYPE] == TAGGED).Select(m => m[MEMBER]).ToList();
        }

        private static string GetLinkAggPrimary(string agg)
        {
            string id = agg.Split('/')[1];
            List<Dictionary<string, string>> members = restSvc.RunSwitchCommand(new CmdRequest(Command.SHOW_LINKAGG_PORTS, ParseType.Htable, id)) as List<Dictionary<string, string>>;
            Dictionary<string, string> prim = members.FirstOrDefault(m => m[PRIMARY] == YES);
            if (prim != null) return prim.ElementAt(0).Value;
            return null;
        }
    }
}
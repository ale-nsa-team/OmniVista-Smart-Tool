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
            SftpService sftp = new SftpService(device.IpAddress, "admin", device.Password);
            sftp.Connect();
            List<string> cmdList = GetCmdListFromVcboot(sftp);
            LoadTemplate(cmdList);
            LinuxCommandSeq cmdSeq = new LinuxCommandSeq();
            List<LinuxCommand> cmds = new List<LinuxCommand>();
            foreach (string file in files)
            {
                cmds.Add(new LinuxCommand($"rm -f {file}"));
            }
            cmdSeq.AddCommandSeq(cmds);
            LinuxCommandSeq res = restSvc.SendSshLinuxCommandSeq(cmdSeq, Translate("i18n_frDelete"));
            List<string> errs = new List<string>();
            foreach(LinuxCommand cmd in cmds)
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
            sftp.UploadFile(templateFilePath, VCBOOT_WORK);
            sftp.UploadFile(templateFilePath, VCBOOT_CERT);
            sftp.Disconnect();
            File.Delete(templateFilePath);
        }

        private static void LoadTemplate(List<string> cmdList)
        {
            try
            {
                string content = ReadFromDisk();
                string res = content.Replace("{IpAddress}", swModel.IpAddress).Replace("{SubnetMask}", swModel.NetMask);
                if (!string.IsNullOrEmpty(swModel.DefaultGwy))
                {
                    res += $"ip static-route 0.0.0.0/0 gateway {swModel.DefaultGwy}";
                }
                if (File.Exists(templateFilePath)) File.Delete(templateFilePath);
                using (TextWriter writer = new StreamWriter(templateFilePath))
                {
                    writer.NewLine = "\n";
                    foreach (string line in res.Split('\n'))
                    {
                        if (line.StartsWith("ip interface") && line.Contains($"address {swModel.IpAddress} mask") && cmdList.Count > 0)
                        {
                            foreach (string cmd in cmdList)
                            {
                                writer.WriteLine(cmd);
                            }
                        }
                        else
                        {
                            writer.WriteLine(line);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception while writing config file {TEMPLATE}: {ex.Message}");
            }
        }

        private static List<string> GetCmdListFromVcboot(SftpService sftp)
        {
            RestApiService restSvc = MainWindow.restApiService;
            List<string> cmdList = new List<string>();
            VlanModel vlan = restSvc.VlanSettings.FirstOrDefault(v => v.IpAddress == restSvc.SwitchModel.IpAddress);
            if (vlan != null)
            {
                string[] search = vlan.Device.Split(' ');
                if (search.Length > 0)
                {
                    if (sftp.IsConnected)
                    {
                        string vcbootVfg = sftp.DownloadToMemory(VCBOOT_WORK);
                        if (!string.IsNullOrEmpty(vcbootVfg))
                        {
                            List<string> linkAggId = new List<string>();
                            using (StringReader reader = new StringReader(vcbootVfg))
                            {
                                string line;
                                while ((line = reader.ReadLine()) != null)
                                {
                                    string sLine = line.Trim();
                                    if (sLine.Length == 0 || line.Contains("=====")) continue;
                                    if (line.StartsWith("ip interface") && line.Contains($"address {restSvc.SwitchModel.IpAddress} mask"))
                                    {
                                        cmdList.Add(line);
                                        continue;
                                    }
                                    if (!line.StartsWith(search[0].Trim())) continue;
                                    if (search.Length > 1 && !string.IsNullOrEmpty(search[1].Trim()) && !line.Contains(search[1].Trim())) continue;
                                    cmdList.Add(line);
                                    if (line.Contains("linkagg"))
                                    {
                                        string[] split = Regex.Split(line, "linkagg");
                                        if (split.Length > 1)
                                        {
                                            split = split[1].Trim().Split(' ');
                                            if (split[0].Contains("-"))
                                            {
                                                split = split[0].Split('-');
                                                if (split.Length > 1)
                                                {
                                                    int startId = StringToInt(split[0]);
                                                    int endId = StringToInt(split[1]);
                                                    for (int idx = startId; idx <= endId; idx++)
                                                    {
                                                        linkAggId.Add(idx.ToString());
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                linkAggId.Add(split[0]);
                                            }
                                        }
                                    }
                                }
                            }
                            List<string> cmdLinkAggList = new List<string>();
                            if (linkAggId.Count > 0)
                            {
                                using (StringReader reader = new StringReader(vcbootVfg))
                                {
                                    string line;
                                    while ((line = reader.ReadLine()) != null)
                                    {
                                        string sLine = line.Trim();
                                        if (sLine.Length == 0 || line.Contains("=====") || !line.StartsWith("linkagg")) continue;
                                        string aggId = linkAggId.FirstOrDefault(agg => line.Contains(agg));
                                        if (string.IsNullOrEmpty(aggId)) continue;
                                        cmdLinkAggList.Add(line);
                                    }
                                }
                            }
                            if (cmdLinkAggList.Count > 0)
                            {
                                cmdLinkAggList.AddRange(cmdList);
                                cmdList = cmdLinkAggList;
                            }
                        }
                    }
                }
            }
            return cmdList;
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
    }
}

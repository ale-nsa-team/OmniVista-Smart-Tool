using PoEWizard.Data;
using PoEWizard.Device;
using PoEWizard.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.RestUrl;

namespace PoEWizard.Comm
{
    public class RestApiService
    {
        private List<Dictionary<string, string>> _dictList = new List<Dictionary<string, string>>();
        private Dictionary<string, string> _dict = new Dictionary<string, string>();
        private readonly IProgress<ProgressReport> _progress;
        private PortModel _wizardSwitchPort;
        private SlotModel _wizardSwitchSlot;
        private ProgressReport _wizardProgressReport;
        private Command _wizardCommand = Command.SHOW_SYSTEM;
        private WizardReport _wizardReportResult;
        private SwitchDebugModel _debugSwitchLog;
        private SwitchTrafficModel _switchTraffic;
        private static AbortType stopTrafficAnalysis;
        private static string stopTrafficAnalysisReason = "completed";
        private double totalProgressBar;
        private double progressBarCnt;
        private DateTime progressStartTime;

        public bool IsReady { get; set; } = false;
        public int Timeout { get; set; }
        public ResultCallback Callback { get; set; }
        public SwitchModel SwitchModel { get; set; }
        public RestApiClient RestApiClient { get; set; }
        private AosSshService SshService { get; set; }

        public RestApiService(SwitchModel device, IProgress<ProgressReport> progress)
        {
            this.SwitchModel = device;
            this._progress = progress;
            this.RestApiClient = new RestApiClient(SwitchModel);
            this.IsReady = false;
            _progress = progress;
        }

        public void Connect(WizardReport reportResult)
        {
            try
            {
                DateTime startTime = DateTime.Now;
                this.IsReady = true;
                Logger.Info($"Connecting Rest API");
                string progrMsg = $"Connecting to switch {SwitchModel.IpAddress} ...";
                StartProgressBar(progrMsg, 29);
                _progress.Report(new ProgressReport(progrMsg));
                RestApiClient.Login();
                UpdateProgressBar(++progressBarCnt); //  1
                if (!RestApiClient.IsConnected()) throw new SwitchConnectionFailure($"Could not connect to switch {SwitchModel.IpAddress}!");
                SwitchModel.IsConnected = true;
                _progress.Report(new ProgressReport($"Reading system information on switch {SwitchModel.IpAddress}"));
                _dictList = SendCommand(new CmdRequest(Command.SHOW_MICROCODE, ParseType.Htable)) as List<Dictionary<string, string>>;
                SwitchModel.LoadFromDictionary(_dictList[0], DictionaryType.MicroCode);
                UpdateProgressBar(++progressBarCnt); //  2
                _dictList = SendCommand(new CmdRequest(Command.DEBUG_SHOW_APP_LIST, ParseType.MibTable, DictionaryType.SwitchDebugAppList)) as List<Dictionary<string, string>>;
                SwitchModel.LoadFromList(_dictList, DictionaryType.SwitchDebugAppList);
                UpdateProgressBar(++progressBarCnt); //  3
                _dictList = SendCommand(new CmdRequest(Command.SHOW_CHASSIS, ParseType.MVTable, DictionaryType.Chassis)) as List<Dictionary<string, string>>;
                SwitchModel.LoadFromList(_dictList, DictionaryType.Chassis);
                UpdateProgressBar(++progressBarCnt); // 4
                ScanSwitch(progrMsg, reportResult);
                UpdateFlashInfo(progrMsg);
                LogActivity($"Switch connected", $", duration: {Utils.CalcStringDuration(startTime)}");
            }
            catch (Exception ex)
            {
                SendSwitchError("Connect", ex);
            }
            CloseProgressBar();
        }

        public void ScanSwitch(string source, WizardReport reportResult = null)
        {
            bool closeProgressBar = false;
            try
            {
                if (totalProgressBar == 0)
                {
                    StartProgressBar($"Scanning switch {SwitchModel.Name} ...", 22);
                    closeProgressBar = true;
                }
                GetCurrentSwitchDebugLevel();
                progressBarCnt += 2;
                UpdateProgressBar(progressBarCnt); //  5 , 6
                GetSnapshot();
                progressBarCnt += 2;
                UpdateProgressBar(progressBarCnt); //  7, 8
                if (reportResult != null) this._wizardReportResult = reportResult;
                else this._wizardReportResult = new WizardReport();
                GetSystemInfo();
                UpdateProgressBar(++progressBarCnt); //  9
                SendProgressReport("Reading chassis and port information");
                _dictList = SendCommand(new CmdRequest(Command.SHOW_CMM, ParseType.MVTable, DictionaryType.Cmm)) as List<Dictionary<string, string>>;
                SwitchModel.LoadFromList(_dictList, DictionaryType.Cmm);
                UpdateProgressBar(++progressBarCnt); //  10
                _dictList = SendCommand(new CmdRequest(Command.SHOW_TEMPERATURE, ParseType.Htable)) as List<Dictionary<string, string>>;
                SwitchModel.LoadFromList(_dictList, DictionaryType.TemperatureList);
                UpdateProgressBar(++progressBarCnt); // 11
                _dict = SendCommand(new CmdRequest(Command.SHOW_HEALTH_CONFIG, ParseType.Etable)) as Dictionary<string, string>;
                SwitchModel.UpdateCpuThreshold(_dict);
                UpdateProgressBar(++progressBarCnt); // 12
                _dictList = SendCommand(new CmdRequest(Command.SHOW_PORTS_LIST, ParseType.Htable3)) as List<Dictionary<string, string>>;
                SwitchModel.LoadFromList(_dictList, DictionaryType.PortsList);
                UpdateProgressBar(++progressBarCnt); // 13
                SendProgressReport("Reading power supply information");
                _dictList = SendCommand(new CmdRequest(Command.SHOW_POWER_SUPPLIES, ParseType.Htable2)) as List<Dictionary<string, string>>;
                SwitchModel.LoadFromList(_dictList, DictionaryType.PowerSupply);
                UpdateProgressBar(++progressBarCnt); // 14
                _dictList = SendCommand(new CmdRequest(Command.SHOW_HEALTH, ParseType.Htable2)) as List<Dictionary<string, string>>;
                SwitchModel.LoadFromList(_dictList, DictionaryType.CpuTrafficList);
                UpdateProgressBar(++progressBarCnt); // 15
                GetLanPower();
                progressBarCnt += 3;
                UpdateProgressBar(progressBarCnt); // 16, 17, 18
                GetMacAndLldpInfo();
                progressBarCnt += 3;
                UpdateProgressBar(progressBarCnt); // 19, 20, 21
                if (!File.Exists(Path.Combine(Path.Combine(MainWindow.dataPath, SNAPSHOT_FOLDER), $"{SwitchModel.IpAddress}{SNAPSHOT_SUFFIX}")))
                {
                    SaveConfigSnapshot();
                }
                else
                {
                    PurgeConfigSnapshotFiles();
                }
                UpdateProgressBar(++progressBarCnt); // 22
                string title = string.IsNullOrEmpty(source) ? $"Refresh switch {SwitchModel.Name}" : source;
            }
            catch (Exception ex)
            {
                SendSwitchError(source, ex);
            }
            if (closeProgressBar) CloseProgressBar();
        }

        private void UpdateFlashInfo(string source)
        {
            try
            {
                if (SwitchModel?.ChassisList?.Count > 0)
                {
                    try
                    {
                        foreach (ChassisModel chassis in SwitchModel.ChassisList)
                        {
                            LinuxCommandSeq cmdSeq;
                            if (chassis.IsMaster) cmdSeq = new LinuxCommandSeq();
                            else cmdSeq = new LinuxCommandSeq(new LinuxCommand($"ssh-chassis {SwitchModel.Login}@{chassis.Number}", "Password|Are you sure", 20));
                            cmdSeq.AddCommandSeq(new List<LinuxCommand> { new LinuxCommand("su", "->"), new LinuxCommand("df -h", "->"), new LinuxCommand("exit", "->") });
                            cmdSeq = SendSshLinuxCommandSeq(cmdSeq, $"Reading Flash data of chassis {chassis.Number}");
                            _dict = cmdSeq?.GetResponse("df -h");
                            if (_dict != null && _dict.ContainsKey(OUTPUT)) SwitchModel.LoadFlashSizeFromList(_dict[OUTPUT], chassis);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                        string response = SendCommand(new CmdRequest(Command.SHOW_FREE_SPACE, ParseType.NoParsing)).ToString();
                        if (!string.IsNullOrEmpty(response)) SwitchModel.LoadFreeFlashFromList(response);
                    }
                }
            }
            catch (Exception ex)
            {
                SendSwitchError(source, ex);
            }
        }

        public void GetSystemInfo()
        {
            SendProgressReport("Reading system information");
            GetSyncStatus();
            _dictList = SendCommand(new CmdRequest(Command.SHOW_IP_INTERFACE, ParseType.Htable)) as List<Dictionary<string, string>>;
            _dict = _dictList.FirstOrDefault(d => d[IP_ADDR] == SwitchModel.IpAddress);
            if (_dict != null) SwitchModel.NetMask = _dict[SUBNET_MASK];
            _dictList = SendCommand(new CmdRequest(Command.SHOW_IP_ROUTES, ParseType.Htable)) as List<Dictionary<string, string>>;
            _dict = _dictList.FirstOrDefault(d => d[DNS_DEST] == "0.0.0.0/0");
            if (_dict != null) SwitchModel.DefaultGwy = _dict[GATEWAY];
        }

        public string GetSyncStatus()
        {
            _dict = SendCommand(new CmdRequest(Command.SHOW_SYSTEM_RUNNING_DIR, ParseType.MibTable, DictionaryType.SystemRunningDir)) as Dictionary<string, string>;
            SwitchModel.LoadFromDictionary(_dict, DictionaryType.SystemRunningDir);
            try
            {
                SwitchModel.ConfigSnapshot = SendCommand(new CmdRequest(Command.SHOW_CONFIGURATION, ParseType.NoParsing)) as string;
                string filePath = Path.Combine(Path.Combine(MainWindow.dataPath, SNAPSHOT_FOLDER), $"{SwitchModel.IpAddress}{SNAPSHOT_SUFFIX}");
                if (File.Exists(filePath))
                {
                    string prevCfgSnapshot = File.ReadAllText(filePath);
                    if (!string.IsNullOrEmpty(prevCfgSnapshot)) return ConfigChanges.GetChanges(SwitchModel, prevCfgSnapshot);
                }
            }
            catch (Exception ex)
            {
                SendSwitchError("Get Snapshot", ex);
            }
            return null;
        }

        public void GetSnapshot()
        {
            try
            {
                SendProgressReport("Reading configuration snapshot");
                SwitchModel.ConfigSnapshot = SendCommand(new CmdRequest(Command.SHOW_CONFIGURATION, ParseType.NoParsing)) as string;
                if (!SwitchModel.ConfigSnapshot.Contains(CMD_TBL[Command.LLDP_SYSTEM_DESCRIPTION_ENABLE]))
                {
                    SendProgressReport("Enabling LLDP description");
                    SendCommand(new CmdRequest(Command.LLDP_SYSTEM_DESCRIPTION_ENABLE));
                }
            }
            catch (Exception ex)
            {
                SendSwitchError("Get Snapshot", ex);
            }
        }

        public object RunSwitchCommand(CmdRequest cmdReq)
        {
            try
            {
                return SendCommand(cmdReq);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return null;
        }

        private object SendCommand(CmdRequest cmdReq)
        {
            Dictionary<string, object> resp = SendRequest(GetRestUrlEntry(cmdReq));
            if (cmdReq.ParseType == ParseType.MibTable)
            {
                if (!resp.ContainsKey(DATA) || resp[DATA] == null) return resp;
                Dictionary<string, string>  xmlDict = resp[DATA] as Dictionary<string, string>;
                switch (cmdReq.DictionaryType)
                {
                    case DictionaryType.MibList:
                        if (MIB_REQ_TBL.ContainsKey(cmdReq.Command)) return CliParseUtils.ParseListFromDictionary(xmlDict, MIB_REQ_TBL[cmdReq.Command]);
                        else return resp;

                    case DictionaryType.SwitchDebugAppList:
                        return CliParseUtils.ParseSwitchDebugAppTable(xmlDict, new string[2] { LPNI, LPCMM });

                    default:
                        return xmlDict;
                }
            }
            else if (resp.ContainsKey(STRING) && resp[STRING] != null)
            {
                switch (cmdReq.ParseType)
                {
                    case ParseType.Htable:
                        return CliParseUtils.ParseHTable(resp[STRING].ToString(), 1);
                    case ParseType.Htable2:
                        return CliParseUtils.ParseHTable(resp[STRING].ToString(), 2);
                    case ParseType.Htable3:
                        return CliParseUtils.ParseHTable(resp[STRING].ToString(), 3);
                    case ParseType.Vtable:
                        return CliParseUtils.ParseVTable(resp[STRING].ToString());
                    case ParseType.MVTable:
                        return CliParseUtils.ParseMultipleTables(resp[STRING].ToString(), cmdReq.DictionaryType);
                    case ParseType.Etable:
                        return CliParseUtils.ParseETable(resp[STRING].ToString());
                    case ParseType.LldpRemoteTable:
                        return CliParseUtils.ParseLldpRemoteTable(resp[STRING].ToString());
                    case ParseType.TrafficTable:
                        return CliParseUtils.ParseTrafficTable(resp[STRING].ToString());
                    case ParseType.NoParsing:
                        return resp[STRING].ToString();
                    default:
                        return resp;
                }
            }
            return null;
        }

        public void RunGetSwitchLog(SwitchDebugModel debugLog, bool restartPoE, string port = null)
        {
            try
            {
                _debugSwitchLog = debugLog;
                if (port != null)
                {
                    GetSwitchSlotPort(port);
                    if (_wizardSwitchPort == null)
                    {
                        SendProgressError("Get Switch Log", $"Couldn't get data for port {port}");
                        return;
                    }
                }
                progressStartTime = DateTime.Now;
                StartProgressBar($"Collecting logs on switch {SwitchModel.Name} ...", Utils.GetEstimateCollectLogDuration(restartPoE, port));
                ConnectAosSsh();
                UpdateSwitchLogBar();
                int debugSelected = _debugSwitchLog.IntDebugLevelSelected;
                // Getting current lan power status
                if (_wizardSwitchSlot != null)
                {
                    SendProgressReport($"Getting lan power information of slot {_wizardSwitchSlot.Name}");
                    _debugSwitchLog.LanPowerStatus = SendCommand(new CmdRequest(Command.DEBUG_SHOW_LAN_POWER_STATUS, ParseType.NoParsing, new string[1] { _wizardSwitchSlot.Name })) as string;
                }
                UpdateSwitchLogBar();
                // Getting current switch debug level
                GetCurrentSwitchDebugLevel();
                int prevLpNiDebug = SwitchModel.LpNiDebugLevel;
                int prevLpCmmDebug = SwitchModel.LpCmmDebugLevel;
                // Setting switch debug level
                SetAppDebugLevel($"Setting PoE debug log level to {Utils.IntToSwitchDebugLevel(debugSelected)}", Command.DEBUG_UPDATE_LPNI_LEVEL, debugSelected);
                SetAppDebugLevel($"Setting CMM debug log level to {Utils.IntToSwitchDebugLevel(debugSelected)}", Command.DEBUG_UPDATE_LPCMM_LEVEL, debugSelected);
                if (restartPoE)
                {
                    if (_wizardSwitchPort != null)
                    {
                        // Recycling power on switch port
                        RestartDeviceOnPort($"Resetting port {_wizardSwitchPort.Name} to capture log", 5);
                    }
                    else
                    {
                        // Recycling power on all chassis of the switch
                        RestartChassisPoE();
                    }
                }
                else
                {
                    WaitSec($"Collecting logs on switch {SwitchModel.Name} ...", 5);
                }
                UpdateSwitchLogBar();
                // Setting switch debug level back to the previous values
                SetAppDebugLevel($"Resetting PoE debug level back to {Utils.IntToSwitchDebugLevel(prevLpNiDebug)}", Command.DEBUG_UPDATE_LPNI_LEVEL, prevLpNiDebug);
                SetAppDebugLevel($"Resetting CMM debug level back to {Utils.IntToSwitchDebugLevel(prevLpCmmDebug)}", Command.DEBUG_UPDATE_LPCMM_LEVEL, prevLpCmmDebug);
                // Generating tar file
                string msg = "Generating tar file";
                SendProgressReport(msg);
                WaitSec(msg, 5);
                SendCommand(new CmdRequest(Command.DEBUG_CREATE_LOG));
                Logger.Info($"Generated log file in {SwitchDebugLogLevel.Debug3} level on switch {SwitchModel.Name}, duration: {Utils.CalcStringDuration(progressStartTime)}");
                UpdateSwitchLogBar();
            }
            catch (Exception ex)
            {
                SendSwitchError("Get Switch Log", ex);
            }
            finally
            {
                DisconnectAosSsh();
            }
        }

        private void RestartChassisPoE()
        {
            foreach (var chassis in this.SwitchModel.ChassisList)
            {
                string msg = $"Turning power OFF on all slots of chassis {chassis.Number} to capture logs ...";
                _progress.Report(new ProgressReport(msg));
                SendCommand(new CmdRequest(Command.STOP_CHASSIS_POE, new string[1] { chassis.Number.ToString() }));
                UpdateSwitchLogBar();
                WaitSec(msg, 5);
                _progress.Report(new ProgressReport($"Turning power ON on all slots of chassis {chassis.Number} to capture logs ..."));
                SendCommand(new CmdRequest(Command.START_CHASSIS_POE, new string[1] { chassis.Number.ToString() }));
                foreach (var slot in chassis.Slots)
                {
                    UpdateSwitchLogBar();
                    _wizardSwitchSlot = slot;
                    WaitSlotPower(true);
                }
            }
        }

        private void ConnectAosSsh()
        {
            SshService = new AosSshService(SwitchModel);
            SshService.ConnectSshClient();
        }

        private void DisconnectAosSsh()
        {
            try
            {
                SshService?.DisconnectSshClient();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private bool IsAosSshConnected()
        {
            try
            {
                return SshService != null && SshService.IsSwitchConnected();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return false;
        }

        private void SetAppDebugLevel(string progressMsg, Command cmd, int dbgLevel)
        {
            Command showDbgCmd = cmd == Command.DEBUG_UPDATE_LPCMM_LEVEL ? Command.DEBUG_SHOW_LPCMM_LEVEL : Command.DEBUG_SHOW_LPNI_LEVEL;
            _progress.Report(new ProgressReport($"{progressMsg} ..."));
            DateTime startCmdTime = DateTime.Now;
            SendSshUpdateLogCommand(cmd, new string[1] { dbgLevel.ToString() });
            UpdateSwitchLogBar();
            bool done = false;
            int loopCnt = 1;
            while (!done)
            {
                Thread.Sleep(1000);
                _progress.Report(new ProgressReport($"{progressMsg} ({loopCnt} sec) ..."));
                UpdateSwitchLogBar();
                if (loopCnt % 5 == 0) done = GetAppDebugLevel(showDbgCmd) == dbgLevel;
                if (loopCnt >= 30)
                {
                    Logger.Error($"Took too long ({Utils.CalcStringDuration(startCmdTime)}) to complete\"{cmd}\" to \"{dbgLevel}\"!");
                    return;
                }
                loopCnt++;
            }
            Logger.Info($"{(cmd == Command.DEBUG_UPDATE_LPCMM_LEVEL ? "\"lpCmm\"" : "\"lpNi\"")} debug level set to \"{dbgLevel}\", Duration: {Utils.CalcStringDuration(startCmdTime)}");
            UpdateSwitchLogBar();
        }

        private void GetCurrentSwitchDebugLevel()
        {
            SendProgressReport($"Getting current log levels");
            if (_debugSwitchLog == null) _debugSwitchLog = new SwitchDebugModel();
            GetAppDebugLevel(Command.DEBUG_SHOW_LPNI_LEVEL);
            UpdateSwitchLogBar();
            SwitchModel.SetAppLogLevel(LPNI, _debugSwitchLog.LpNiLogLevel);
            GetAppDebugLevel(Command.DEBUG_SHOW_LPCMM_LEVEL);
            SwitchModel.SetAppLogLevel(LPCMM, _debugSwitchLog.LpCmmLogLevel);
            UpdateSwitchLogBar();
        }

        private void UpdateSwitchLogBar()
        {
            UpdateProgressBar(Utils.GetTimeDuration(progressStartTime));
        }

        private int GetAppDebugLevel(Command cmd)
        {
            try
            {
                string appName = cmd == Command.DEBUG_SHOW_LPNI_LEVEL ? LPNI : LPCMM;
                if (SwitchModel.DebugApp.ContainsKey(appName))
                {
                    _dictList = SendCommand(new CmdRequest(Command.DEBUG_SHOW_LEVEL, ParseType.MibTable, DictionaryType.MibList,
                                                new string[2] { SwitchModel.DebugApp[appName].Index, SwitchModel.DebugApp[appName].NbSubApp })) as List<Dictionary<string, string>>;
                    _debugSwitchLog.LoadFromDictionary(_dictList);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Dictionary<string, string> response = SendSshUpdateLogCommand(cmd);
                if (response != null && response.ContainsKey(OUTPUT) && !string.IsNullOrEmpty(response[OUTPUT]))
                {
                    _debugSwitchLog.LoadFromDictionary(CliParseUtils.ParseCliSwitchDebugLevel(response[OUTPUT]));
                }
            }
            return cmd == Command.DEBUG_SHOW_LPCMM_LEVEL ? _debugSwitchLog.LpCmmLogLevel : _debugSwitchLog.LpNiLogLevel;
        }

        private Dictionary<string, string> SendSshUpdateLogCommand(Command cmd, string[] data = null)
        {
            try
            {
                if (!IsAosSshConnected()) ConnectAosSsh();
                Dictionary<Command, Command> cmdTranslation = new Dictionary<Command, Command>
                {
                    [Command.DEBUG_UPDATE_LPNI_LEVEL] = Command.DEBUG_CLI_UPDATE_LPNI_LEVEL,
                    [Command.DEBUG_UPDATE_LPCMM_LEVEL] = Command.DEBUG_CLI_UPDATE_LPCMM_LEVEL,
                    [Command.DEBUG_SHOW_LPNI_LEVEL] = Command.DEBUG_CLI_SHOW_LPNI_LEVEL,
                    [Command.DEBUG_SHOW_LPCMM_LEVEL] = Command.DEBUG_CLI_SHOW_LPCMM_LEVEL
                };
                if (cmdTranslation.ContainsKey(cmd))
                {
                    return SshService?.SendCommand(new RestUrlEntry(cmdTranslation[cmd]), data);
                }
            }
            catch (Exception ex)
            {
                SendSwitchError("Connect", ex);
            }
            return null;
        }

        public LinuxCommandSeq SendSshLinuxCommandSeq(LinuxCommandSeq cmdEntry, string progressMsg)
        {
            try
            {
                _progress.Report(new ProgressReport(progressMsg));
                UpdateProgressBar(++progressBarCnt); //  1
                DateTime startTime = DateTime.Now;
                if (!IsAosSshConnected()) ConnectAosSsh();
                string msg = $"{progressMsg} on switch {SwitchModel.Name}";
                Dictionary<string, string> response = new Dictionary<string, string>();
                cmdEntry.StartTime = DateTime.Now;
                foreach (LinuxCommand cmdLinux in cmdEntry.CommandSeq)
                {
                    cmdLinux.Response = SshService?.SendLinuxCommand(cmdLinux);
                    if (cmdLinux.DelaySec > 0) WaitSec(msg, cmdLinux.DelaySec);
                    SendWaitProgressReport(msg, startTime);
                    UpdateProgressBar(++progressBarCnt); //  1
                }
                cmdEntry.Duration = Utils.CalcStringDuration(cmdEntry.StartTime);
                return cmdEntry;
            }
            catch (Exception ex)
            {
               throw ex;
            }
            finally
            {
                DisconnectAosSsh();
            }
        }

        public void WriteMemory(int waitSec = 40)
        {
            try
            {
                if (SwitchModel.SyncStatus == SyncStatusType.Synchronized) return;
                string msg = $"Writing memory on switch {SwitchModel.Name}";
                StartProgressBar($"{msg} ...", 30);
                SendCommand(new CmdRequest(Command.WRITE_MEMORY));
                progressStartTime = DateTime.Now;
                double dur = 0;
                while (dur < waitSec)
                {
                    Thread.Sleep(1000);
                    dur = Utils.GetTimeDuration(progressStartTime);
                    try
                    {
                        int period = (int)dur;
                        if (period > 20 && period % 5 == 0) GetSyncStatus();
                    }
                    catch { }
                    if (SwitchModel.SyncStatus != SyncStatusType.NotSynchronized || dur >= waitSec) break;
                    UpdateProgressBarMessage($"{msg} ({(int)dur} sec) ...", dur);
                }
                LogActivity("Write memory completed", $", duration: {Utils.CalcStringDuration(progressStartTime)}");
                SaveConfigSnapshot();
            }
            catch (Exception ex)
            {
                Logger.Warn(ex.Message);
            }
            CloseProgressBar();
        }

        private void SaveConfigSnapshot()
        {
            try
            {
                string folder = Path.Combine(MainWindow.dataPath, SNAPSHOT_FOLDER);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, $"{SwitchModel.IpAddress}{SNAPSHOT_SUFFIX}"), SwitchModel.ConfigSnapshot);
                PurgeConfigSnapshotFiles();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void PurgeConfigSnapshotFiles()
        {
            string folder = Path.Combine(MainWindow.dataPath, SNAPSHOT_FOLDER);
            if (Directory.Exists(folder))
            {
                string txt = Utils.PurgeFiles(folder, MAX_NB_SNAPSHOT_SAVED);
                if (!string.IsNullOrEmpty(txt)) Logger.Warn($"Purging snapshot configuration files{txt}");
                if (SwitchModel.SyncStatus == SyncStatusType.Synchronized)
                {
                    string filePath = Path.Combine(folder, Path.Combine(folder, $"{SwitchModel.IpAddress}{SNAPSHOT_SUFFIX}"));
                    if (File.Exists(filePath))
                    {
                        string currSnapshot = File.ReadAllText(filePath);
                        string cfgChanges = ConfigChanges.GetChanges(SwitchModel, currSnapshot);
                        if (!string.IsNullOrEmpty(cfgChanges))
                        {
                            Logger.Activity($"\n\tUpdating snapshot config file {SwitchModel.IpAddress}{SNAPSHOT_SUFFIX}.\n\tSwitch {SwitchModel.Name} was synchronized but the snapshot config file was different.");
                            File.WriteAllText(filePath, SwitchModel.ConfigSnapshot);
                        }
                    }
                }
            }
            else Directory.CreateDirectory(folder);
        }

        public string RebootSwitch(int waitSec)
        {
            progressStartTime = DateTime.Now;
            try
            {
                string msg = $"Rebooting switch {SwitchModel.Name}";
                Logger.Info(msg);
                StartProgressBar($"{msg} ...", 320);
                SendRebootSwitchRequest();
                if (waitSec <= 0) return string.Empty;
                msg = $"Waiting switch {SwitchModel.Name} reboot ";
                WaitSec(msg, 5);
                _progress.Report(new ProgressReport($"{msg}..."));
                double dur = 0;
                while (dur <= 60)
                {
                    if (dur >= waitSec)
                    {
                        throw new Exception($"Switch {SwitchModel.Name} didn't come back within {Utils.CalcStringDuration(progressStartTime, true)}!");
                    }
                    Thread.Sleep(1000);
                    dur = Utils.GetTimeDuration(progressStartTime);
                    UpdateProgressBarMessage($"{msg}({Utils.CalcStringDuration(progressStartTime, true)}) ...", dur);
                }
                while (dur < waitSec + 1)
                {
                    if (dur >= waitSec)
                    {
                        throw new Exception($"Switch {SwitchModel.Name} didn't come back within {Utils.CalcStringDuration(progressStartTime, true)}!");
                    }
                    Thread.Sleep(1000);
                    dur = (int)Utils.GetTimeDuration(progressStartTime);
                    UpdateProgressBarMessage($"{msg}({Utils.CalcStringDuration(progressStartTime, true)}) ...", dur);
                    if (!Utils.IsReachable(SwitchModel.IpAddress)) continue;
                    try
                    {
                        if (dur % 5 == 0)
                        {
                            RestApiClient.Login();
                            if (RestApiClient.IsConnected()) break;
                        }
                    }
                    catch { }
                }
                LogActivity("Switch rebooted", $", duration: {Utils.CalcStringDuration(progressStartTime, true)}");
            }
            catch (Exception ex)
            {
                SendSwitchError($"Reboot switch {SwitchModel.Name}", ex);
                return null;
            }
            CloseProgressBar();
            return Utils.CalcStringDuration(progressStartTime, true);
        }

        private void SendRebootSwitchRequest()
        {
            const double MAX_WAIT_RETRY = 30;
            DateTime startTime = DateTime.Now;
            double dur = 0;
            while (dur < MAX_WAIT_RETRY)
            {
                try
                {
                    SendCommand(new CmdRequest(Command.REBOOT_SWITCH));
                    return;
                }
                catch (Exception ex)
                {
                    dur = Utils.GetTimeDuration(startTime);
                    if (dur >= MAX_WAIT_RETRY) throw ex;
                }
            }
        }

        private void StartProgressBar(string barText, double initValue)
        {
            try
            {
                totalProgressBar = initValue;
                progressBarCnt = 0;
                Utils.StartProgressBar(_progress, barText);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void UpdateProgressBarMessage(string txt, double currVal)
        {
            try
            {
                _progress.Report(new ProgressReport(txt));
                UpdateProgressBar(currVal);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void UpdateProgressBar(double currVal)
        {
            try
            {
                Utils.UpdateProgressBar(_progress, currVal, totalProgressBar);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void CloseProgressBar()
        {
            try
            {
                Utils.CloseProgressBar(_progress);
                progressBarCnt = 0;
                totalProgressBar = 0;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void StopTrafficAnalysis(AbortType abortType, string stopReason)
        {
            stopTrafficAnalysis = abortType;
            stopTrafficAnalysisReason = stopReason;
        }

        public TrafficReport RunTrafficAnalysis(int duration)
        {
            TrafficReport report;
            try
            {
                stopTrafficAnalysis = AbortType.Running;
                stopTrafficAnalysisReason = "completed";
                _switchTraffic = null;
                GetPortsTrafficInformation();
                DateTime startTime = DateTime.Now;
                DateTime sampleTime = DateTime.Now;
                LogActivity($"Started traffic analysis", $" for {duration} sec");
                while (Utils.GetTimeDuration(startTime) <= duration)
                {
                    if (stopTrafficAnalysis != AbortType.Running) break;
                    Thread.Sleep(250);
                }
                if (stopTrafficAnalysis == AbortType.Close)
                {
                    Logger.Warn($"Traffic analysis on switch {SwitchModel.IpAddress} was canceled because the switch is disconnected!");
                    Activity.Log(SwitchModel, "Traffic analysis interrupted.");
                    return null;
                }
                GetMacAndLldpInfo();
                GetPortsTrafficInformation();
                report = new TrafficReport(_switchTraffic, stopTrafficAnalysisReason, duration);
                if (stopTrafficAnalysis == AbortType.CanceledByUser)
                {
                    Logger.Warn($"Traffic analysis on switch {SwitchModel.IpAddress} was {stopTrafficAnalysisReason}, selected duration: {duration / 60} minutes!");
                }
                LogActivity($"Traffic analysis {stopTrafficAnalysisReason}.", $"\n{report.Summary}");
            }
            catch (Exception ex)
            {
                SendSwitchError($"Traffic analysis on switch {SwitchModel.Name}", ex);
                return null;
            }
            return report;
        }

        private void GetMacAndLldpInfo()
        {
            SendProgressReport("Reading lldp remote information");
            object lldpList = SendCommand(new CmdRequest(Command.SHOW_LLDP_REMOTE, ParseType.LldpRemoteTable));
            SwitchModel.LoadLldpFromList(lldpList as Dictionary<string, List<Dictionary<string, string>>>, DictionaryType.LldpRemoteList);
            lldpList = SendCommand(new CmdRequest(Command.SHOW_LLDP_INVENTORY, ParseType.LldpRemoteTable));
            SwitchModel.LoadLldpFromList(lldpList as Dictionary<string, List<Dictionary<string, string>>>, DictionaryType.LldpInventoryList);
            SendProgressReport("Reading MAC address information");
            _dictList = SendCommand(new CmdRequest(Command.SHOW_MAC_LEARNING, ParseType.Htable)) as List<Dictionary<string, string>>;
            SwitchModel.LoadFromList(_dictList, DictionaryType.MacAddressList);
        }

        private void GetPortsTrafficInformation()
        {
            try
            {
                _dictList = SendCommand(new CmdRequest(Command.SHOW_INTERFACES, ParseType.TrafficTable)) as List<Dictionary<string, string>>;
                if (_dictList?.Count > 0)
                {
                    if (_switchTraffic == null) _switchTraffic = new SwitchTrafficModel(SwitchModel, _dictList);
                    else _switchTraffic.UpdateTraffic(_dictList);
                }
            }
            catch (Exception ex)
            {
                SendSwitchError($"Traffic analysis on switch {SwitchModel.Name}", ex);
            }
        }

        public bool SetPerpetualOrFastPoe(SlotModel slot, Command cmd)
        {
            bool enable = cmd == Command.POE_PERPETUAL_ENABLE || cmd == Command.POE_FAST_ENABLE;
            string poeType = (cmd == Command.POE_PERPETUAL_ENABLE || cmd == Command.POE_PERPETUAL_DISABLE) ? "Perpetual" : "Fast";
            string action = $"{(enable ? "Enable" : "Disable")} {poeType}";
            ProgressReport progressReport = new ProgressReport($"{action} PoE Report:")
            {
                Type = ReportType.Info
            };
            try
            {
                _wizardSwitchSlot = slot;
                if (_wizardSwitchSlot == null) return false;
                DateTime startTime = DateTime.Now;
                RefreshPoEData();
                string result = ChangePerpetualOrFastPoe(cmd);
                RefreshPortsInformation();
                progressReport.Message += result;
                progressReport.Message += $"\n - Duration: {Utils.PrintTimeDurationSec(startTime)}";
                _progress.Report(progressReport);
                LogActivity($"{action} PoE on slot {_wizardSwitchSlot.Name} completed", $"\n{progressReport.Message}");
                return true;
            }
            catch (Exception ex)
            {
                SendSwitchError("Set Perpetual/Fast PoE", ex);
            }
            return false;
        }

        public bool ChangePowerPriority(string port, PriorityLevelType priority)
        {
            ProgressReport progressReport = new ProgressReport($"Change priority Report:")
            {
                Type = ReportType.Info
            };
            try
            {
                GetSwitchSlotPort(port);
                if (_wizardSwitchSlot == null || _wizardSwitchPort == null) return false;
                RefreshPoEData();
                UpdatePortData();
                DateTime startTime = DateTime.Now;
                if (_wizardSwitchPort.PriorityLevel == priority) return false;
                _wizardSwitchPort.PriorityLevel = priority;
                SendCommand(new CmdRequest(Command.POWER_PRIORITY_PORT, new string[2] { port, _wizardSwitchPort.PriorityLevel.ToString() }));
                RefreshPortsInformation();
                progressReport.Message += $"\n - Priority on port {port} set to {priority}";
                progressReport.Message += $"\n - Duration: {Utils.PrintTimeDurationSec(startTime)}";
                _progress.Report(progressReport);
                LogActivity($"Changed power priority to {priority} on port {port}", $"\n{progressReport.Message}");
                return true;
            }
            catch (Exception ex)
            {
                SendSwitchError("Change power priority", ex);
            }
            return false;
        }

        public void ResetPort(string port, int waitSec)
        {
            try
            {
                GetSwitchSlotPort(port);
                if (_wizardSwitchSlot == null || _wizardSwitchPort == null) return;
                RefreshPoEData();
                UpdatePortData();
                DateTime startTime = DateTime.Now;
                string progressMessage = _wizardSwitchPort.Poe == PoeStatus.NoPoe ? $"Restarting port {port}" : $"Restarting power on port {port}";
                if (_wizardSwitchPort.Poe == PoeStatus.NoPoe)
                {
                    RestartEthernetOnPort(progressMessage, 10);
                }
                else
                {
                    RestartDeviceOnPort(progressMessage, 10);
                    WaitPortUp(waitSec, !string.IsNullOrEmpty(progressMessage) ? progressMessage : string.Empty);
                }
                RefreshPortsInformation();
                ProgressReport progressReport = new ProgressReport("");
                if (_wizardSwitchPort.Status == PortStatus.Up)
                {
                    progressReport.Message = $"Port {port} restarted.";
                    progressReport.Type = ReportType.Info;
                }
                else
                {
                    progressReport.Message = $"Port {port} failed to restart!";
                    progressReport.Type = ReportType.Warning;
                }
                progressReport.Message += $"\nStatus: {_wizardSwitchPort.Status}, PoE Status: {_wizardSwitchPort.Poe}, Duration: {Utils.CalcStringDuration(startTime, true)}";
                _progress.Report(progressReport);
                LogActivity($"Port {port} restarted by the user", $"\n{progressReport.Message}");
            }
            catch (Exception ex)
            {
                SendSwitchError("Change power priority", ex);
            }
        }

        private void RestartEthernetOnPort(string progressMessage, int waitTimeSec = 5)
        {
            string action = !string.IsNullOrEmpty(progressMessage) ? progressMessage : string.Empty;
            SendCommand(new CmdRequest(Command.ETHERNET_DISABLE, new string[1] { _wizardSwitchPort.Name }));
            string msg = $"{action} ...\nShutting DOWN port";
            _progress.Report(new ProgressReport($"{msg}{PrintPortStatus()}"));
            WaitSec(msg, 5);
            WaitEthernetStatus(waitTimeSec, PortStatus.Down, msg);
            SendCommand(new CmdRequest(Command.ETHERNET_ENABLE, new string[1] { _wizardSwitchPort.Name }));
            msg = $"{action} ...\nStarting UP port";
            _progress.Report(new ProgressReport($"{msg}{PrintPortStatus()}"));
            WaitSec(msg, 5);
            WaitEthernetStatus(waitTimeSec, PortStatus.Up, msg);
        }

        private DateTime WaitEthernetStatus(int waitSec, PortStatus waitStatus, string progressMessage = null)
        {
            string msg = !string.IsNullOrEmpty(progressMessage) ? $"{progressMessage}\n" : string.Empty;
            msg += $"Waiting port {_wizardSwitchPort.Name} to come UP";
            _progress.Report(new ProgressReport($"{msg} ...{PrintPortStatus()}"));
            DateTime startTime = DateTime.Now;
            PortStatus ethStatus = UpdateEthStatus();
            int dur = 0;
            while (dur < waitSec)
            {
                Thread.Sleep(1000);
                dur = (int)Utils.GetTimeDuration(startTime);
                _progress.Report(new ProgressReport($"{msg} ({Utils.CalcStringDuration(startTime, true)}) ...{PrintPortStatus()}"));
                if (ethStatus == waitStatus) break;
                if (dur % 5 == 0) ethStatus = UpdateEthStatus();
            }
            return startTime;
        }

        private PortStatus UpdateEthStatus()
        {
            _dictList = SendCommand(new CmdRequest(Command.SHOW_INTERFACE_PORT, ParseType.TrafficTable, new string[1] { _wizardSwitchPort.Name })) as List<Dictionary<string, string>>;
            if (_dictList?.Count > 0)
            {
                foreach (Dictionary<string, string> dict in _dictList)
                {
                    string port = Utils.GetDictValue(dict, PORT);
                    if (!string.IsNullOrEmpty(port))
                    {
                        if (port == _wizardSwitchPort.Name)
                        {
                            string sValStatus = Utils.FirstChToUpper(Utils.GetDictValue(dict, OPERATIONAL_STATUS));
                            if (!string.IsNullOrEmpty(sValStatus) && Enum.TryParse(sValStatus, out PortStatus portStatus))return portStatus; else return PortStatus.Unknown;
                        }
                    }
                }
            }
            return PortStatus.Unknown;
        }

        public void RefreshSwitchPorts()
        {
            GetSystemInfo();
            GetLanPower();
            RefreshPortsInformation();
            GetMacAndLldpInfo();
        }

        public void RefreshMacAndLldpInfo()
        {
            GetMacAndLldpInfo();
        }

        private void RefreshPortsInformation()
        {
            _progress.Report(new ProgressReport($"Refreshing ports information on switch {SwitchModel.Name}"));
            _dictList = SendCommand(new CmdRequest(Command.SHOW_PORTS_LIST, ParseType.Htable3)) as List<Dictionary<string, string>>;
            SwitchModel.LoadFromList(_dictList, DictionaryType.PortsList);
        }

        public void PowerSlotUpOrDown(Command cmd, string slotNr)
        {
            string msg = $"Turning slot {slotNr} PoE {(cmd == Command.POWER_UP_SLOT ? "ON" : "OFF")}";
            _wizardProgressReport = new ProgressReport($"{msg} ...");
            try
            {
                _wizardSwitchSlot = SwitchModel.GetSlot(slotNr);
                if (_wizardSwitchSlot == null)
                {
                    SendProgressError(msg, $"Couldn't get data for slot {slotNr}");
                    return;
                }
                if (cmd == Command.POWER_UP_SLOT) PowerSlotUp(); else PowerSlotDown();
            }
            catch (Exception ex)
            {
                SendSwitchError(msg, ex);
            }
        }

        public void RunWizardCommands(string port, WizardReport reportResult, List<Command> commands, int waitSec)
        {
            _wizardProgressReport = new ProgressReport("PoE Wizard Report:");
            try
            {
                GetSwitchSlotPort(port);
                if (_wizardSwitchPort == null || _wizardSwitchSlot == null)
                {
                    SendProgressError("PoE Wizard", $"Couldn't get data for port {port}");
                    return;
                }
                if (!_wizardSwitchSlot.IsInitialized) PowerSlotUp();
                _wizardReportResult = reportResult;
                if (_wizardSwitchPort.Poe == PoeStatus.NoPoe)
                {
                    CreateReportPortNoPoe();
                    return;
                }
                ExecuteWizardCommands(commands, waitSec);
            }
            catch (Exception ex)
            {
                SendSwitchError("PoE Wizard", ex);
            }
        }

        public void RunPoeWizard(string port, WizardReport reportResult, List<Command> commands, int waitSec)
        {
            if (reportResult.IsWizardStopped(port)) return;
            _wizardProgressReport = new ProgressReport("PoE Wizard Report:");
            try
            {
                GetSwitchSlotPort(port);
                if (_wizardSwitchPort == null || _wizardSwitchSlot == null)
                {
                    SendProgressError("PoE Wizard", $"Couldn't get data for port {port}");
                    return;
                }
                string msg = "Running PoE Wizard";
                _progress.Report(new ProgressReport($"{msg} ..."));
                if (!_wizardSwitchSlot.IsInitialized) PowerSlotUp();
                _wizardReportResult = reportResult;
                if (_wizardSwitchPort.Poe == PoeStatus.NoPoe)
                {
                    CreateReportPortNoPoe();
                    return;
                }
                if (_wizardSwitchPort.Poe == PoeStatus.Conflict)
                {
                    DisableConflictPower();
                    return;
                }
                else if (_wizardSwitchPort.Poe != PoeStatus.Fault && _wizardSwitchPort.Poe != PoeStatus.Deny && _wizardSwitchPort.Poe != PoeStatus.Searching)
                {
                    WaitSec(msg, 5);
                    GetSlotLanPower(_wizardSwitchSlot);
                    if (_wizardSwitchPort.Poe != PoeStatus.Fault && _wizardSwitchPort.Poe != PoeStatus.Deny && _wizardSwitchPort.Poe != PoeStatus.Searching)
                    {
                        NothingToDo();
                        return;
                    }
                }
                ExecuteWizardCommands(commands, waitSec);
            }
            catch (Exception ex)
            {
                SendSwitchError("PoE Wizard", ex);
            }
        }

        private void DisableConflictPower()
        {
            string wizardAction = $"Turning off the power on port {_wizardSwitchPort.Name}";
            _progress.Report(new ProgressReport(wizardAction));
            _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
            WaitSec(wizardAction, 5);
            GetSlotLanPower(_wizardSwitchSlot);
            if (_wizardSwitchPort.Poe == PoeStatus.Conflict)
            {
                PowerDevice(Command.POWER_DOWN_PORT);
                WaitPortUp(30, wizardAction);
                _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Ok, "solved the problem");
                StringBuilder portStatus = new StringBuilder(PrintPortStatus());
                portStatus.Append(_wizardSwitchPort.Status);
                if (_wizardSwitchPort.MacList?.Count > 0) portStatus.Append(", Device MAC address: ").Append(_wizardSwitchPort.MacList[0]);
                _wizardReportResult.UpdatePortStatus(_wizardSwitchPort.Name, portStatus.ToString());
                Logger.Info($"{wizardAction} solve the problem on port {_wizardSwitchPort.Name}");
            }
            else
            {
                _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed);
                return;
            }
        }

        private void NothingToDo()
        {
            _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.NothingToDo);
        }

        private void CreateReportPortNoPoe()
        {
            string wizardAction = $"Nothing to do\n    Port {_wizardSwitchPort.Name} doesn't have PoE";
            _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.NothingToDo, wizardAction);
        }

        private void ExecuteWizardCommands(List<Command> commands, int waitSec)
        {
            foreach (Command command in commands)
            {
                _wizardCommand = command;
                switch (_wizardCommand)
                {
                    case Command.POWER_823BT_ENABLE:
                        Enable823BT(waitSec);
                        break;

                    case Command.POWER_2PAIR_PORT:
                        TryEnable2PairPower(waitSec);
                        break;

                    case Command.POWER_HDMI_ENABLE:
                        if (_wizardSwitchPort.IsPowerOverHdmi)
                        {
                            _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed);
                            continue;
                        }
                        ExecuteActionOnPort($"Enabling power HDMI on port {_wizardSwitchPort.Name}", waitSec, Command.POWER_HDMI_DISABLE);
                        break;

                    case Command.LLDP_POWER_MDI_ENABLE:
                        if (_wizardSwitchPort.IsLldpMdi)
                        {
                            _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed);
                            continue;
                        }
                        ExecuteActionOnPort($"Enabling LLDP power via MDI on port {_wizardSwitchPort.Name}", waitSec, Command.LLDP_POWER_MDI_DISABLE);
                        break;

                    case Command.LLDP_EXT_POWER_MDI_ENABLE:
                        if (_wizardSwitchPort.IsLldpExtMdi)
                        {
                            _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed);
                            continue;
                        }
                        ExecuteActionOnPort($"Enabling LLDP extended power via MDI on port {_wizardSwitchPort.Name}", waitSec, Command.LLDP_EXT_POWER_MDI_DISABLE);
                        break;

                    case Command.CHECK_POWER_PRIORITY:
                        CheckPriority();
                        return;

                    case Command.CHECK_823BT:
                        Check823BT();
                        break;

                    case Command.POWER_PRIORITY_PORT:
                        TryChangePriority(waitSec);
                        break;

                    case Command.CHECK_CAPACITOR_DETECTION:
                        CheckCapacitorDetection(waitSec);
                        break;

                    case Command.RESET_POWER_PORT:
                        ResetPortPower(waitSec);
                        break;

                    case Command.CHECK_MAX_POWER:
                        CheckMaxPower();
                        break;

                    case Command.CHANGE_MAX_POWER:
                        ChangePortMaxPower();
                        break;
                }
                if (_wizardReportResult.GetReportResult(_wizardSwitchPort.Name) == WizardResult.Ok)
                {
                    break;
                }
            }
        }

        private void CheckCapacitorDetection(int waitSec)
        {
            try
            {
                string msg = $"Checking capacitor detection on port {_wizardSwitchPort.Name}";
                _progress.Report(new ProgressReport(msg));
                GetSlotLanPower(_wizardSwitchSlot);
                WaitSec(msg, 5);
                GetSlotLanPower(_wizardSwitchSlot);
                if (_wizardSwitchPort.Poe == PoeStatus.Searching)
                    _wizardCommand = Command.CAPACITOR_DETECTION_ENABLE;
                else if (_wizardSwitchPort.Poe == PoeStatus.Fault || _wizardSwitchPort.Poe == PoeStatus.Deny || _wizardSwitchPort.Poe == PoeStatus.PoweredOff)
                    _wizardCommand = Command.CAPACITOR_DETECTION_DISABLE;
                else
                {
                    _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed);
                    return;
                }
                string wizardAction = _wizardCommand == Command.CAPACITOR_DETECTION_ENABLE ? "Enabling" : "Disabling";
                wizardAction += $" capacitor detection on port {_wizardSwitchPort.Name}";
                _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
                if (_wizardCommand == Command.CAPACITOR_DETECTION_ENABLE)
                {
                    if (_wizardSwitchPort.IsCapacitorDetection)
                    {
                        _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed);
                        return;
                    }
                    PortSubType portSubType = _wizardSwitchPort.EndPointDevice != null ? _wizardSwitchPort.EndPointDevice.PortSubType : PortSubType.Unknown;
                    switch (portSubType)
                    {
                        case PortSubType.MacAddress:
                        case PortSubType.NetworkAddress:
                        case PortSubType.LocallyAssigned:
                            StringBuilder actionResult = new StringBuilder("\n    Cannot enable capacitor detection for device type ").Append(_wizardSwitchPort.EndPointDevice.Type);
                            if (!string.IsNullOrEmpty(_wizardSwitchPort.EndPointDevice.Name)) actionResult.Append(" (").Append(_wizardSwitchPort.EndPointDevice.Name).Append(")");
                            _wizardReportResult.UpdateWizardReport(_wizardSwitchPort.Name, WizardResult.Proceed, actionResult.ToString());
                            Logger.Info($"{wizardAction}\n{_wizardProgressReport.Message}");
                            return;

                        default:
                            break;
                    }
                }
                else if (!_wizardSwitchPort.IsCapacitorDetection)
                {
                    _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed);
                    return;
                }
                _progress.Report(new ProgressReport(wizardAction));
                SendCommand(new CmdRequest(_wizardCommand, new string[1] { _wizardSwitchPort.Name }));
                WaitSec(wizardAction, 5);
                RestartDeviceOnPort(wizardAction);
                CheckPortUp(waitSec, wizardAction);
                string txt = string.Empty;
                if (_wizardReportResult.GetReportResult(_wizardSwitchPort.Name) == WizardResult.Ok)
                    txt = $"{wizardAction} solved the problem";
                else
                {
                    txt = $"{wizardAction} didn't solve the problem\nDisabling capacitor detection on port {_wizardSwitchPort.Name} to restore the previous config";
                    _wizardCommand = Command.CAPACITOR_DETECTION_DISABLE;
                    SendCommand(new CmdRequest(_wizardCommand, new string[1] { _wizardSwitchPort.Name }));
                    wizardAction = $"Disabling capacitor detection on port {_wizardSwitchPort.Name}";
                    RestartDeviceOnPort(wizardAction, 5);
                    WaitSec(wizardAction, 10);
                    WaitPortUp(30, wizardAction);
                }
                Logger.Info(txt);
            }
            catch (Exception ex)
            {
                ParseException(_wizardProgressReport, ex);
            }
        }

        private void CheckMaxPower()
        {
            string wizardAction = $"Checking max. power on port {_wizardSwitchPort.Name}";
            _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
            _progress.Report(new ProgressReport(wizardAction));
            double prevMaxPower = _wizardSwitchPort.MaxPower;
            double maxDefaultPower = GetMaxDefaultPower();
            double maxPowerAllowed = SetMaxPowerToDefault(maxDefaultPower);
            if (maxPowerAllowed == 0) SetMaxPowerToDefault(prevMaxPower); else maxDefaultPower = maxPowerAllowed;
            string info;
            if (_wizardSwitchPort.MaxPower < maxDefaultPower)
            {
                _wizardReportResult.SetReturnParameter(_wizardSwitchPort.Name, maxDefaultPower);
                info = $"Max. power on port {_wizardSwitchPort.Name} is {_wizardSwitchPort.MaxPower} Watts, it should be {maxDefaultPower} Watts";
                _wizardReportResult.UpdateAlert(_wizardSwitchPort.Name, WizardResult.Warning, info);
            }
            else
            {
                info = $"\n    Max. power on port {_wizardSwitchPort.Name} is already the maximum allowed ({_wizardSwitchPort.MaxPower} Watts)";
                _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed, info);
            }
            Logger.Info($"{wizardAction}\n{_wizardProgressReport.Message}");
        }

        private void ChangePortMaxPower()
        {
            object obj = _wizardReportResult.GetReturnParameter(_wizardSwitchPort.Name);
            if (obj == null)
            {
                _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed);
                return;
            }
            double maxDefaultPower = (double)obj;
            string wizardAction = $"Restoring max. power on port {_wizardSwitchPort.Name} from {_wizardSwitchPort.MaxPower} Watts to maximum allowed {maxDefaultPower} Watts";
            _progress.Report(new ProgressReport(wizardAction));
            double prevMaxPower = _wizardSwitchPort.MaxPower;
            SetMaxPowerToDefault(maxDefaultPower);
            if (prevMaxPower != _wizardSwitchPort.MaxPower)
            {
                _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
                Logger.Info($"{wizardAction}\n{_wizardProgressReport.Message}");
            }
        }

        private double SetMaxPowerToDefault(double maxDefaultPower)
        {
            try
            {
                SendCommand(new CmdRequest(Command.SET_MAX_POWER_PORT, new string[2] { _wizardSwitchPort.Name, $"{maxDefaultPower * 1000}" }));
                _wizardSwitchPort.MaxPower = maxDefaultPower;
                return 0;
            }
            catch (Exception ex)
            {
                return Utils.StringToDouble(Utils.ExtractSubString(ex.Message, "power not exceed ", " when").Trim()) / 1000;
            }
        }

        private double GetMaxDefaultPower()
        {
            string error = null;
            try
            {
                SendCommand(new CmdRequest(Command.SET_MAX_POWER_PORT, new string[2] { _wizardSwitchPort.Name, "0" }));
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            return !string.IsNullOrEmpty(error) ? Utils.StringToDouble(Utils.ExtractSubString(error, "to ", "mW").Trim()) / 1000 : _wizardSwitchPort.MaxPower;
        }

        private void RefreshPoEData()
        {
            _progress.Report(new ProgressReport($"Refreshing PoE information on switch {SwitchModel.Name}"));
            GetSlotPowerStatus();
            GetSlotPowerAndConfig(_wizardSwitchSlot);
        }

        private void GetSwitchSlotPort(string port)
        {
            ChassisSlotPort chassisSlotPort = new ChassisSlotPort(port);
            ChassisModel chassis = SwitchModel.GetChassis(chassisSlotPort.ChassisNr);
            if (chassis == null) return;
            _wizardSwitchSlot = chassis.GetSlot(chassisSlotPort.SlotNr);
            if (_wizardSwitchSlot == null) return;
            _wizardSwitchPort = _wizardSwitchSlot.GetPort(chassisSlotPort.PortNr);
        }

        private void TryEnable2PairPower(int waitSec)
        {
            DateTime startTime = DateTime.Now;
            bool fastPoe = _wizardSwitchSlot.FPoE == ConfigType.Enable;
            if (fastPoe) SendCommand(new CmdRequest(Command.POE_FAST_DISABLE, new string[1] { _wizardSwitchSlot.Name }));
            double prevMaxPower = _wizardSwitchPort.MaxPower;
            if (!_wizardSwitchPort.Is4Pair)
            {
                string wizardAction = $"Re-enabling 2-Pair power on port {_wizardSwitchPort.Name}";
                try
                {
                    SendCommand(new CmdRequest(Command.POWER_4PAIR_PORT, new string[1] { _wizardSwitchPort.Name }));
                    WaitSec(wizardAction, 3);
                    ExecuteActionOnPort(wizardAction, waitSec, Command.POWER_2PAIR_PORT);
                }
                catch (Exception ex)
                {
                    _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
                    _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.PrintTimeDurationSec(startTime));
                    string resultDescription = $"{wizardAction} didn't solve the problem\nCommand not supported on slot {_wizardSwitchSlot.Name}";
                    _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Fail, resultDescription);
                    Logger.Info($"{ex.Message}\n{resultDescription}");
                }
            }
            else
            {
                Command init4Pair = _wizardSwitchPort.Is4Pair ? Command.POWER_4PAIR_PORT : Command.POWER_2PAIR_PORT;
                _wizardCommand = _wizardSwitchPort.Is4Pair ? Command.POWER_2PAIR_PORT : Command.POWER_4PAIR_PORT;
                ExecuteActionOnPort($"Enabling {(_wizardSwitchPort.Is4Pair ? "2-Pair" : "4-Pair")} power on port {_wizardSwitchPort.Name}", waitSec, init4Pair);
            }
            if (prevMaxPower != _wizardSwitchPort.MaxPower) SetMaxPowerToDefault(prevMaxPower);
            if (fastPoe) SendCommand(new CmdRequest(Command.POE_FAST_ENABLE, new string[1] { _wizardSwitchSlot.Name }));
            _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.PrintTimeDurationSec(startTime));
        }

        private void SendSwitchError(string title, Exception ex)
        {
            string error = ex.Message;
            if (ex is SwitchConnectionFailure || ex is SwitchConnectionDropped || ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure)
            {
                if (ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure)
                {
                    error = $"Switch {(string.IsNullOrEmpty(SwitchModel.Name) ? SwitchModel.IpAddress : SwitchModel.Name)} login failed (username: {SwitchModel.Login})";
                    this.SwitchModel.Status = SwitchStatus.LoginFail;
                }
                else
                {
                    error = $"Switch {(string.IsNullOrEmpty(SwitchModel.Name) ? SwitchModel.IpAddress : SwitchModel.Name)} unreachable\n{error}";
                    this.SwitchModel.Status = SwitchStatus.Unreachable;
                }
            }
            else if (ex is SwitchCommandNotSupported)
            {
                Logger.Warn(error);
            }
            else
            {
                Logger.Error(ex);
            }
            _progress?.Report(new ProgressReport(ReportType.Error, title, error));
        }

        private void ExecuteActionOnPort(string wizardAction, int waitSec, Command restoreCmd)
        {
            try
            {
                DateTime startTime = DateTime.Now;
                _progress.Report(new ProgressReport(wizardAction));
                _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
                SendCommand(new CmdRequest(_wizardCommand, new string[1] { _wizardSwitchPort.Name }));
                WaitSec(wizardAction, 3);
                CheckPortUp(waitSec, wizardAction);
                _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.PrintTimeDurationSec(startTime));
                if (_wizardReportResult.GetReportResult(_wizardSwitchPort.Name) == WizardResult.Ok) return;
                if (restoreCmd != _wizardCommand) SendCommand(new CmdRequest(restoreCmd, new string[1] { _wizardSwitchPort.Name }));
                Logger.Info($"{wizardAction} didn't solve the problem\nExecuting command {restoreCmd} on port {_wizardSwitchPort.Name} to restore the previous config");
            }
            catch (Exception ex)
            {
                ParseException(_wizardProgressReport, ex);
            }
        }

        private void Check823BT()
        {
            string wizardAction = $"Checking 802.3.bt on port {_wizardSwitchPort.Name}";
            _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
            DateTime startTime = DateTime.Now;
            StringBuilder txt = new StringBuilder();
            switch (_wizardSwitchPort.Protocol8023bt)
            {
                case ConfigType.Disable:
                    string alert = _wizardSwitchSlot.FPoE == ConfigType.Enable ? $"Fast PoE is enabled on slot {_wizardSwitchSlot.Name}" : null;
                    _wizardReportResult.UpdateAlert(_wizardSwitchPort.Name, WizardResult.Warning, alert);
                    break;
                case ConfigType.Unavailable:
                    txt.Append($"\n    Switch {SwitchModel.Name} doesn't support 802.3.bt");
                    _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Skip, txt.ToString());
                    break;
                case ConfigType.Enable:
                    txt.Append($"\n    802.3.bt already enabled on port {_wizardSwitchPort.Name}");
                    _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Skip, txt.ToString());
                    break;
            }
            _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.PrintTimeDurationSec(startTime));
            Logger.Info($"{wizardAction}{txt}");
        }

        private void Enable823BT(int waitSec)
        {
            try
            {
                string wizardAction = $"Enabling 802.3.bt on slot {_wizardSwitchSlot.Name}";
                _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
                DateTime startTime = DateTime.Now;
                _progress.Report(new ProgressReport(wizardAction));
                if (!_wizardSwitchSlot.Is8023btSupport)
                {
                    _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed, $"\n    Slot {_wizardSwitchSlot.Name} doesn't support 802.3.bt");
                }
                CheckFPOEand823BT(Command.POWER_823BT_ENABLE);
                Change823BT(Command.POWER_823BT_ENABLE);
                CheckPortUp(waitSec, wizardAction);
                _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.PrintTimeDurationSec(startTime));
                if (_wizardReportResult.GetReportResult(_wizardSwitchPort.Name) == WizardResult.Ok) return;
                Change823BT(Command.POWER_823BT_DISABLE);
                Logger.Info($"{wizardAction} didn't solve the problem\nDisabling 802.3.bt on port {_wizardSwitchPort.Name} to restore the previous config");
            }
            catch (Exception ex)
            {
                SendCommand(new CmdRequest(Command.POWER_UP_SLOT, new string[1] { _wizardSwitchSlot.Name }));
                ParseException(_wizardProgressReport, ex);
            }
        }

        private void CheckPriority()
        {
            string wizardAction = $"Checking power priority on port {_wizardSwitchPort.Name}";
            _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
            DateTime startTime = DateTime.Now;
            double powerRemaining = _wizardSwitchSlot.Budget - _wizardSwitchSlot.Power;
            double maxPower = _wizardSwitchPort.MaxPower;
            StringBuilder txt = new StringBuilder();
            WizardResult changePriority;
            string remainingPower = $"Remaining power = {powerRemaining} Watts, max. power = {maxPower} Watts";
            string text;
            if (_wizardSwitchPort.PriorityLevel < PriorityLevelType.High && powerRemaining < maxPower)
            {
                changePriority = WizardResult.Warning;
                string alert = $"Changing power priority on port {_wizardSwitchPort.Name} may solve the problem";
                text = $"\n    {remainingPower}";
                _wizardReportResult.UpdateAlert(_wizardSwitchPort.Name, WizardResult.Warning, alert);
            }
            else
            {
                changePriority = WizardResult.Skip;
                text = $"\n    No need to change power priority on port {_wizardSwitchPort.Name} (";
                if (_wizardSwitchPort.PriorityLevel >= PriorityLevelType.High)
                {
                    text += $"priority is already {_wizardSwitchPort.PriorityLevel}";
                }
                else
                {
                    text += $"{remainingPower}";
                }
                text += ")";
            }
            _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, changePriority, text);
            _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.PrintTimeDurationSec(startTime));
            Logger.Info(txt.ToString());
        }

        private void TryChangePriority(int waitSec)
        {
            try
            {
                PriorityLevelType priority = PriorityLevelType.High;
                string wizardAction = $"Changing power priority to {priority} on port {_wizardSwitchPort.Name}";
                _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
                PriorityLevelType prevPriority = _wizardSwitchPort.PriorityLevel;
                DateTime startTime = DateTime.Now;
                StringBuilder txt = new StringBuilder(wizardAction);
                _progress.Report(new ProgressReport(txt.ToString()));
                SendCommand(new CmdRequest(Command.POWER_PRIORITY_PORT, new string[2] { _wizardSwitchPort.Name, priority.ToString() }));
                CheckPortUp(waitSec, txt.ToString());
                _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.CalcStringDuration(startTime, true));
                if (_wizardReportResult.GetReportResult(_wizardSwitchPort.Name) == WizardResult.Ok) return;
                SendCommand(new CmdRequest(Command.POWER_PRIORITY_PORT, new string[2] { _wizardSwitchPort.Name, prevPriority.ToString() }));
                Logger.Info($"{wizardAction} didn't solve the problem\nChanging priority back to {prevPriority} on port {_wizardSwitchPort.Name} to restore the previous config");
            }
            catch (Exception ex)
            {
                ParseException(_wizardProgressReport, ex);
            }
        }

        private void ResetPortPower(int waitSec)
        {
            try
            {
                string wizardAction = $"Recycling the power on port {_wizardSwitchPort.Name}";
                _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
                DateTime startTime = DateTime.Now;
                _progress.Report(new ProgressReport(wizardAction));
                RestartDeviceOnPort(wizardAction);
                CheckPortUp(waitSec, wizardAction);
                _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.PrintTimeDurationSec(startTime));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void RestartDeviceOnPort(string progressMessage, int waitTimeSec = 5)
        {
            string action = !string.IsNullOrEmpty(progressMessage) ? progressMessage : string.Empty;
            SendCommand(new CmdRequest(Command.POWER_DOWN_PORT, new string[1] { _wizardSwitchPort.Name }));
            string msg = $"{action} ...\nTurning power OFF";
            _progress.Report(new ProgressReport($"{msg}{PrintPortStatus()}"));
            WaitSec(msg, waitTimeSec);
            SendCommand(new CmdRequest(Command.POWER_UP_PORT, new string[1] { _wizardSwitchPort.Name }));
            msg = $"{action} ...\nTurning power ON";
            _progress.Report(new ProgressReport($"{msg}{PrintPortStatus()}"));
            WaitSec(msg, 5);
        }

        private void CheckPortUp(int waitSec, string progressMessage)
        {
            DateTime startTime = WaitPortUp(waitSec, !string.IsNullOrEmpty(progressMessage) ? progressMessage : string.Empty);
            UpdateProgressReport();
            StringBuilder text = new StringBuilder("Port ").Append(_wizardSwitchPort.Name).Append(" Status: ").Append(_wizardSwitchPort.Status).Append(", PoE Status: ");
            text.Append(_wizardSwitchPort.Poe).Append(", Power: ").Append(_wizardSwitchPort.Power).Append(" (Duration: ").Append(Utils.CalcStringDuration(startTime));
            text.Append(", MAC List: ").Append(String.Join(",", _wizardSwitchPort.MacList)).Append(")");
            Logger.Info(text.ToString());
        }

        private DateTime WaitPortUp(int waitSec, string progressMessage = null)
        {
            string msg = !string.IsNullOrEmpty(progressMessage) ? $"{progressMessage}\n" : string.Empty;
            msg += $"Waiting port {_wizardSwitchPort.Name} to come UP";
            _progress.Report(new ProgressReport($"{msg} ...{PrintPortStatus()}"));
            DateTime startTime = DateTime.Now;
            UpdatePortData();
            int dur = 0;
            while (dur < waitSec)
            {
                Thread.Sleep(1000);
                dur = (int)Utils.GetTimeDuration(startTime);
                _progress.Report(new ProgressReport($"{msg} ({Utils.CalcStringDuration(startTime, true)}) ...{PrintPortStatus()}"));
                if (IsPortUp()) break;
                if (dur % 5 == 0) UpdatePortData();
            }
            return startTime;
        }

        private bool IsPortUp()
        {
            if (_wizardSwitchPort.Status != PortStatus.Up) return false;
            else if (_wizardSwitchPort.Poe == PoeStatus.On && _wizardSwitchPort.Power > 0) return true;
            else if (_wizardSwitchPort.Poe == PoeStatus.Off) return true;
            else if (_wizardSwitchPort.Poe == PoeStatus.Searching && _wizardCommand == Command.CAPACITOR_DETECTION_DISABLE) return true;
            return false;
        }

        private void WaitSec(string msg, int waitSec)
        {
            if (waitSec < 1) return;
            DateTime startTime = DateTime.Now;
            double dur = 0;
            while (dur <= waitSec)
            {
                if (dur >= waitSec) return;
                SendWaitProgressReport(msg, startTime);
                Thread.Sleep(1000);
                dur = Utils.GetTimeDuration(startTime);
            }
        }

        private void SendWaitProgressReport(string msg, DateTime startTime)
        {
            string strDur = Utils.CalcStringDuration(startTime, true);
            string txt = $"{msg}";
            if (!string.IsNullOrEmpty(strDur)) txt += $" ({strDur})";
            txt += $" ...{PrintPortStatus()}";
            _progress.Report(new ProgressReport(txt));
        }

        private void UpdateProgressReport()
        {
            WizardResult result;
            switch (_wizardSwitchPort.Poe)
            {
                case PoeStatus.On:
                    if (_wizardSwitchPort.Status == PortStatus.Up) result = WizardResult.Ok; else result = WizardResult.Fail;
                    break;

                case PoeStatus.Searching:
                    if (_wizardCommand == Command.CAPACITOR_DETECTION_DISABLE) result = WizardResult.Ok; else result = WizardResult.Fail;
                    break;

                case PoeStatus.Conflict:
                case PoeStatus.Fault:
                case PoeStatus.Deny:
                    result = WizardResult.Fail;
                    break;

                default:
                    if (_wizardSwitchPort.MacList?.Count > 0) result = WizardResult.Ok; else result = WizardResult.Proceed;
                    break;
            }
            string resultDescription;
            if (result == WizardResult.Ok) resultDescription = "solved the problem";
            else if (result == WizardResult.Fail) resultDescription = "didn't solve the problem";
            else resultDescription = "";
            _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, result, resultDescription);
            StringBuilder portStatus = new StringBuilder(PrintPortStatus());
            portStatus.Append(_wizardSwitchPort.Status);
            if (_wizardSwitchPort.MacList?.Count > 0) portStatus.Append(", Device MAC address: ").Append(_wizardSwitchPort.MacList[0]);
            _wizardReportResult.UpdatePortStatus(_wizardSwitchPort.Name, portStatus.ToString());
        }

        private string PrintPortStatus()
        {
            if (_wizardSwitchPort == null) return string.Empty;
            return $"\nPoE status: {_wizardSwitchPort.Poe}, port status: {_wizardSwitchPort.Status}, power: {_wizardSwitchPort.Power} Watts";
        }

        private void UpdatePortData()
        {
            if (_wizardSwitchPort == null) return;
            GetSlotPowerAndConfig(_wizardSwitchSlot);
            _dictList = SendCommand(new CmdRequest(Command.SHOW_PORT_STATUS, ParseType.Htable3, new string[1] { _wizardSwitchPort.Name })) as List<Dictionary<string, string>>;
            if (_dictList?.Count > 0) _wizardSwitchPort.UpdatePortStatus(_dictList[0]);
            _dictList = SendCommand(new CmdRequest(Command.SHOW_PORT_MAC_ADDRESS, ParseType.Htable, new string[1] { _wizardSwitchPort.Name })) as List<Dictionary<string, string>>;
            _wizardSwitchPort.UpdateMacList(_dictList);
            Dictionary<string, List<Dictionary<string, string>>> lldpList = SendCommand(new CmdRequest(Command.SHOW_LLDP_REMOTE, ParseType.LldpRemoteTable,
                new string[] { _wizardSwitchPort.Name })) as Dictionary<string, List<Dictionary<string, string>>>;
            if (lldpList.ContainsKey(_wizardSwitchPort.Name)) _wizardSwitchPort.LoadLldpRemoteTable(lldpList[_wizardSwitchPort.Name]);
        }

        private void GetLanPower()
        {
            SendProgressReport("Reading PoE information");
            int nbChassisPoE = SwitchModel.ChassisList.Count;
            foreach (var chassis in SwitchModel.ChassisList)
            {
                GetLanPowerStatus(chassis);
                if (!chassis.SupportsPoE) nbChassisPoE--;
                foreach (var slot in chassis.Slots)
                {
                    if (slot.Ports.Count == 0) continue;
                    if (!chassis.SupportsPoE)
                    {
                        slot.IsPoeModeEnable = false;
                        slot.SupportsPoE = false;
                        continue;
                    }
                    GetSlotPowerAndConfig(slot);
                    if (!slot.IsInitialized)
                    {
                        slot.IsPoeModeEnable = false;
                        if (slot.SupportsPoE)
                        {
                            slot.PoeStatus = SlotPoeStatus.Off;
                            _wizardReportResult.CreateReportResult(slot.Name, WizardResult.Warning, $"\nPoE on slot {slot.Name} is turned OFF!");
                        }
                        else
                        {
                            slot.FPoE = ConfigType.Unavailable;
                            slot.PPoE = ConfigType.Unavailable;
                            slot.PoeStatus = SlotPoeStatus.NotSupported;
                        }
                    }
                    chassis.PowerBudget += slot.Budget;
                    chassis.PowerConsumed += slot.Power;
                    CheckPowerClassDetection(slot);
                }
                chassis.PowerRemaining = chassis.PowerBudget - chassis.PowerConsumed;
                foreach (var ps in chassis.PowerSupplies)
                {
                    string psId = chassis.Number > 0 ? $"{chassis.Number} {ps.Id}" : $"{ps.Id}";
                    _dict = SendCommand(new CmdRequest(Command.SHOW_POWER_SUPPLY, ParseType.Vtable, new string[1] { psId })) as Dictionary<string, string>;
                    ps.LoadFromDictionary(_dict);
                }
            }
            SwitchModel.SupportsPoE = (nbChassisPoE > 0);
            if (!SwitchModel.SupportsPoE) _wizardReportResult.CreateReportResult(SWITCH, WizardResult.Fail, $"Switch {SwitchModel.Name} doesn't support PoE!");
        }

        private void CheckPowerClassDetection(SlotModel slot)
        {
            try
            {
                if (slot.PowerClassDetection == ConfigType.Disable) SendCommand(new CmdRequest(Command.POWER_CLASS_DETECTION_ENABLE, new string[1] { $"{slot.Name}" }));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void GetLanPowerStatus(ChassisModel chassis)
        {
            try
            {
                _dictList = SendCommand(new CmdRequest(Command.SHOW_CHASSIS_LAN_POWER_STATUS, ParseType.Htable2, new string[1] { chassis.Number.ToString() })) as List<Dictionary<string, string>>;
                chassis.LoadFromList(_dictList);
                chassis.PowerBudget = 0;
                chassis.PowerConsumed = 0;
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("lanpower not supported")) chassis.SupportsPoE = false;
                Logger.Error(ex);
            }
        }

        private void GetSlotPowerAndConfig(SlotModel slot)
        {
            GetSlotLanPower(slot);
            if (slot.SupportsPoE)
            {
                _dictList = SendCommand(new CmdRequest(Command.SHOW_LAN_POWER_CONFIG, ParseType.Htable2, new string[1] { $"{slot.Name}" })) as List<Dictionary<string, string>>;
                slot.LoadFromList(_dictList, DictionaryType.LanPowerCfg);
            }
        }

        private void GetSlotLanPower(SlotModel slot)
        {
            try
            {
                _dictList = SendCommand(new CmdRequest(Command.SHOW_LAN_POWER, ParseType.Htable, new string[1] { $"{slot.Name}" })) as List<Dictionary<string, string>>;
                slot.LoadFromList(_dictList, DictionaryType.LanPower);
            }
            catch (Exception ex)
            {
                string error = ex.Message.ToLower();
                if (error.Contains("lanpower not supported") || error.Contains("invalid entry: \"lanpower\"") || error.Contains("incorrect index"))
                {
                    slot.SupportsPoE = false;
                    Logger.Warn(ex.Message);
                }
                else
                {
                    Logger.Error(ex);
                }
            }
        }

        private void ParseException(ProgressReport progressReport, Exception ex)
        {
            if (ex.Message.ToLower().Contains("command not supported"))
            {
                _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed, $"\n    Command not supported by the switch {SwitchModel.Name}");
                return;
            }
            Logger.Error(ex);
            progressReport.Type = ReportType.Error;
            progressReport.Message += $"didn't solve the problem{WebUtility.UrlDecode($"\n{ex.Message}")}";
            PowerDevice(Command.POWER_UP_PORT);
        }

        private string ChangePerpetualOrFastPoe(Command cmd)
        {
            if (_wizardSwitchSlot == null) return "";
            bool enable = cmd == Command.POE_PERPETUAL_ENABLE || cmd == Command.POE_FAST_ENABLE;
            string poeType = (cmd == Command.POE_PERPETUAL_ENABLE || cmd == Command.POE_PERPETUAL_DISABLE) ? "Perpetual" : "Fast";
            string txt = $"{poeType} PoE on Slot {_wizardSwitchSlot.Name}";
            bool ppoe = _wizardSwitchSlot.PPoE == ConfigType.Enable;
            bool fpoe = _wizardSwitchSlot.FPoE == ConfigType.Enable;
            string wizardAction = $"{(enable ? "Enabling" : "Disabling")} {txt}";
            if (cmd == Command.POE_PERPETUAL_ENABLE && ppoe || cmd == Command.POE_FAST_ENABLE && fpoe ||
                cmd == Command.POE_PERPETUAL_DISABLE && !ppoe || cmd == Command.POE_FAST_DISABLE && !fpoe)
            {
                _progress.Report(new ProgressReport(wizardAction));
                txt = $"{txt} is already {(enable ? "enabled" : "disabled")}";
                Logger.Info(txt);
                return $"\n - {txt} ";
            }
            _progress.Report(new ProgressReport(wizardAction));
            string result = $"\n - {wizardAction} ";
            Logger.Info(wizardAction);
            CheckFPOEand823BT(cmd);
            SendCommand(new CmdRequest(cmd, new string[1] { _wizardSwitchSlot.Name }));
            WaitSec(wizardAction, 3);
            GetSlotPowerStatus();
            if (cmd == Command.POE_PERPETUAL_ENABLE && _wizardSwitchSlot.PPoE == ConfigType.Enable ||
                cmd == Command.POE_FAST_ENABLE && _wizardSwitchSlot.FPoE == ConfigType.Enable ||
                cmd == Command.POE_PERPETUAL_DISABLE && _wizardSwitchSlot.PPoE == ConfigType.Disable ||
                cmd == Command.POE_FAST_DISABLE && _wizardSwitchSlot.FPoE == ConfigType.Disable)
            {
                result += "executed";
            }
            else
            {
                result += "failed to execute";
            }
            return result;
        }

        private void CheckFPOEand823BT(Command cmd)
        {
            if (!_wizardSwitchSlot.IsInitialized) PowerSlotUp();
            if (cmd == Command.POE_FAST_ENABLE)
            {
                if (_wizardSwitchSlot.Is8023btSupport && _wizardSwitchSlot.Ports?.FirstOrDefault(p => p.Protocol8023bt == ConfigType.Enable) != null)
                {
                    Change823BT(Command.POWER_823BT_DISABLE);
                }
            }
            else if (cmd == Command.POWER_823BT_ENABLE)
            {
                if (_wizardSwitchSlot.FPoE == ConfigType.Enable) SendCommand(new CmdRequest(Command.POE_FAST_DISABLE, new string[1] { _wizardSwitchSlot.Name }));
            }
        }

        private void Change823BT(Command cmd)
        {
            StringBuilder txt = new StringBuilder();
            if (cmd == Command.POWER_823BT_ENABLE) txt.Append("Enabling");
            else if (cmd == Command.POWER_823BT_DISABLE) txt.Append("Disabling");
            txt.Append(" 802.3bt on slot ").Append(_wizardSwitchSlot.Name).Append(" of switch ").Append(SwitchModel.Name);
            _progress.Report(new ProgressReport($"{txt} ..."));
            PowerSlotDown();
            WaitSlotPower(false);
            SendCommand(new CmdRequest(cmd, new string[1] { _wizardSwitchSlot.Name }));
            PowerSlotUp();
        }

        private void PowerSlotDown()
        {
            SendCommand(new CmdRequest(Command.POWER_DOWN_SLOT, _wizardSwitchSlot.Name));
            WaitSlotPower(false);
        }

        private void PowerSlotUp()
        {
            SendCommand(new CmdRequest(Command.POWER_UP_SLOT, _wizardSwitchSlot.Name));
            WaitSlotPower(true);
        }

        private void WaitSlotPower(bool powerUp)
        {
            DateTime startTime = DateTime.Now;
            StringBuilder txt = new StringBuilder("Turning slot ").Append(_wizardSwitchSlot.Name).Append(" PoE ");
            if (powerUp) txt.Append("ON"); else txt.Append("OFF");
            txt.Append(" on switch ").Append(SwitchModel.Name);
            _progress.Report(new ProgressReport($"{txt} ..."));
            int dur = 0;
            while (dur < 50)
            {
                Thread.Sleep(1000);
                dur = (int)Utils.GetTimeDuration(startTime);
                _progress.Report(new ProgressReport($"{txt} ({dur} sec) ..."));
                if (dur % 5 == 0)
                {
                    GetSlotPowerStatus();
                    if (powerUp && _wizardSwitchSlot.IsInitialized || !powerUp && !_wizardSwitchSlot.IsInitialized) break;
                }
            }
        }

        private void GetSlotPowerStatus()
        {
            _dictList = SendCommand(new CmdRequest(Command.SHOW_SLOT_LAN_POWER_STATUS, ParseType.Htable2, new string[1] { _wizardSwitchSlot.Name })) as List<Dictionary<string, string>>;
            if (_dictList?.Count > 0) _wizardSwitchSlot.LoadFromDictionary(_dictList[0]);
        }

        private void PowerDevice(Command cmd)
        {
            try
            {
                SendCommand(new CmdRequest(cmd, new string[1] { _wizardSwitchPort.Name }));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                if (ex is SwitchConnectionFailure || ex is SwitchConnectionDropped || ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure)
                {
                    if (ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure) this.SwitchModel.Status = SwitchStatus.LoginFail;
                    else this.SwitchModel.Status = SwitchStatus.Unreachable;
                }
                else
                {
                    Logger.Error(ex);
                }
            }
        }

        private void SendProgressReport(string progrMsg)
        {
            string msg = $"{progrMsg} on switch {(!string.IsNullOrEmpty(SwitchModel.Name) ? SwitchModel.Name : SwitchModel.IpAddress)}";
            _progress.Report(new ProgressReport(msg));
            Logger.Info(msg);
        }

        private void SendProgressError(string title, string error)
        {
            string errorMessage = $"{error} on switch {SwitchModel.Name}";
            _progress.Report(new ProgressReport(ReportType.Error, title, $"{errorMessage} on switch {SwitchModel.Name}"));
            Logger.Error(errorMessage);
        }

        public void Close()
        {
            RestApiClient?.Close();
            LogActivity("Switch disconnected");
        }

        private void LogActivity(string action, string data = null)
        {
            string txt = $"Switch {SwitchModel.Name} ({SwitchModel.IpAddress}), S/N {SwitchModel.SerialNumber}, model {SwitchModel.Model}: {action}";
            if (!string.IsNullOrEmpty(data)) txt += data;
            Logger.Activity(txt);
            Activity.Log(SwitchModel, action.Contains(".") ? action : $"{action}.");
        }

        private RestUrlEntry GetRestUrlEntry(CmdRequest req)
        {
            Dictionary<string, string> body = GetContent(req.Command, req.Data);
            return new RestUrlEntry(req.Command, req.Data)
            {
                Method = body == null ? HttpMethod.Get : HttpMethod.Post,
                Content = body
            };
        }

        private Dictionary<string, object> SendRequest(RestUrlEntry entry)
        {
            Dictionary<string, object> response = new Dictionary<string, object> { [STRING] = null, [DATA] = null };
            Dictionary<string, string> respReq = this.RestApiClient.SendRequest(entry);
            if (respReq == null) return null;
            if (respReq.ContainsKey(ERROR) && !string.IsNullOrEmpty(respReq[ERROR]))
            {
                if (respReq[ERROR].ToLower().Contains("not supported")) throw new SwitchCommandNotSupported(respReq[ERROR]);
                else throw new SwitchCommandError(respReq[ERROR]);
            }
            LogSendRequest(entry, respReq);
            Dictionary<string, string> result = null;
            if (respReq.ContainsKey(RESULT) && !string.IsNullOrEmpty(respReq[RESULT])) result = CliParseUtils.ParseXmlToDictionary(respReq[RESULT]);
            if (result != null)
            {
                if (entry.Method == HttpMethod.Post)
                {
                    response[DATA] = result;
                }
                else if (string.IsNullOrEmpty(result[OUTPUT]))
                {
                    response[DATA] = result;
                }
                else
                {
                    response[STRING] = result[OUTPUT];
                }
            }
            return response;
        }

        private void LogSendRequest(RestUrlEntry entry, Dictionary<string, string> response)
        {
            StringBuilder txt = new StringBuilder("API Request sent by ").Append(Utils.PrintMethodClass(3)).Append(":\n").Append(entry.ToString());
            txt.Append("\nRequest API URL: ").Append(response[REST_URL]);
            if (Logger.LogLevel == LogLevel.Info) Logger.Info(txt.ToString()); txt.Append(entry.Response[ERROR]);
            if (entry.Content != null && entry.Content?.Count > 0)
            {
                txt.Append("\nHTTP key-value pair Body:");
                foreach (string key in entry.Content.Keys.ToList())
                {
                    txt.Append("\n\t").Append(key).Append(": ").Append(entry.Content[key]);
                }
            }
            if (entry.Response.ContainsKey(ERROR) && !string.IsNullOrEmpty(entry.Response[ERROR]))
            {
                txt.Append("\nSwitch Error: ").Append(entry.Response[ERROR]);
            }
            if (entry.Response.ContainsKey(RESULT))
            {
                txt.Append("\nSwitch Response:\n").Append(new string('=', 132)).Append("\n").Append(Utils.PrintXMLDoc(response[RESULT]));
                txt.Append("\n").Append(new string('=', 132));
            }
            Logger.Debug(txt.ToString());
        }
    }

}

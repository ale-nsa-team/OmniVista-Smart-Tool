using PoEWizard.Data;
using PoEWizard.Device;
using PoEWizard.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.RestUrl;
using static PoEWizard.Data.Utils;

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
        private static TrafficStatus trafficAnalysisStatus = TrafficStatus.Idle;
        private static string stopTrafficAnalysisReason = "completed";
        private double totalProgressBar;
        private double progressBarCnt;
        private DateTime progressStartTime;
        private SftpService _sftpService = null;
        private DateTime _backupStartTime;
        private List<VlanModel> _vlanSettings = new List<VlanModel>();

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
                string progrMsg = $"{Translate("i18n_rsCnx")} {SwitchModel.IpAddress}{WAITING}";
                StartProgressBar(progrMsg, 31);
                _progress.Report(new ProgressReport(progrMsg));
                UpdateProgressBar(progressBarCnt);
                RestApiClient.Login();
                UpdateProgressBar(++progressBarCnt); //  1
                if (!RestApiClient.IsConnected()) throw new SwitchConnectionFailure($"{Translate("i18n_rsNocnx")} {SwitchModel.IpAddress}!");
                SwitchModel.IsConnected = true;
                _progress.Report(new ProgressReport($"{Translate("i18n_vinfo")} {Translate("i18n_onsw")} {SwitchModel.IpAddress}"));
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
                UpdateProgressBar(++progressBarCnt); // 30
                ShowInterfacesList();
                UpdateProgressBar(++progressBarCnt); // 31
                LogActivity($"Switch connected", $", duration: {CalcStringDuration(startTime)}");
            }
            catch (Exception ex)
            {
                SendSwitchError(Translate("i18n_Conn"), ex);
            }
            CloseProgressBar();
            DisconnectAosSsh();
        }

        public void RefreshSwitch(string source, WizardReport reportResult = null)
        {
            StartProgressBar($"S{Translate("i18n_scan")} {SwitchModel.Name}{WAITING}", 24);
            ScanSwitch(source, reportResult);
            ShowInterfacesList();
            UpdateProgressBar(++progressBarCnt); // 24
        }

        public void ScanSwitch(string source, WizardReport reportResult = null)
        {
            bool closeProgressBar = false;
            try
            {
                if (totalProgressBar == 0)
                {
                    StartProgressBar($"{Translate("i18n_scan")} {SwitchModel.Name}{WAITING}", 23);
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
                SendProgressReport(Translate("i18n_chas"));
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
                SendProgressReport(Translate("i18n_psi"));
                _dictList = SendCommand(new CmdRequest(Command.SHOW_POWER_SUPPLIES, ParseType.Htable2)) as List<Dictionary<string, string>>;
                SwitchModel.LoadFromList(_dictList, DictionaryType.PowerSupply);
                UpdateProgressBar(++progressBarCnt); // 14
                _dictList = SendCommand(new CmdRequest(Command.SHOW_HEALTH, ParseType.Htable2)) as List<Dictionary<string, string>>;
                SwitchModel.LoadFromList(_dictList, DictionaryType.CpuTrafficList);
                UpdateProgressBar(++progressBarCnt); // 15
                GetLanPower();
                progressBarCnt += 3;
                UpdateProgressBar(progressBarCnt); // 16, 17, 18
                GetMacAndLldpInfo(MAX_SCAN_NB_MAC_PER_PORT);
                progressBarCnt += 3;
                UpdateProgressBar(progressBarCnt); // 19, 20, 21
                if (!File.Exists(Path.Combine(Path.Combine(MainWindow.DataPath, SNAPSHOT_FOLDER), $"{SwitchModel.IpAddress}{SNAPSHOT_SUFFIX}")))
                {
                    SaveConfigSnapshot();
                }
                else
                {
                    PurgeConfigSnapshotFiles();
                }
                UpdateProgressBar(++progressBarCnt); // 22
                string title = string.IsNullOrEmpty(source) ? $"{Translate("i18n_refrsw")} {SwitchModel.Name}" : source;
            }
            catch (Exception ex)
            {
                SendSwitchError(source, ex);
            }
            DisconnectAosSsh();
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
                        ConnectAosSsh();
                        string sessionPrompt = SshService.SessionPrompt;
                        foreach (ChassisModel chassis in SwitchModel.ChassisList)
                        {
                            LinuxCommandSeq cmdSeq;
                            if (chassis.IsMaster) cmdSeq = new LinuxCommandSeq();
                            else cmdSeq = new LinuxCommandSeq(new LinuxCommand($"ssh-chassis {SwitchModel.Login}@{chassis.Number}", "Password|Are you sure", 20));
                            Thread.Sleep(500);
                            cmdSeq.AddCommandSeq(new List<LinuxCommand> { new LinuxCommand("su", "->"), new LinuxCommand("df -h", "->"), new LinuxCommand("exit", sessionPrompt) });
                            cmdSeq = SendSshLinuxCommandSeq(cmdSeq, $"{Translate("i18n_flashd")} {chassis.Number}");
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

        private void EnableRestApi()
        {
            string progrMsg = $"{Translate("i18n_rsCnx")} {SwitchModel.IpAddress}{WAITING}";
            try
            {
                if (SwitchModel?.ChassisList?.Count > 0)
                {
                    try
                    {
                        ConnectAosSsh();
                        string sessionPrompt = SshService.SessionPrompt;
                        LinuxCommandSeq cmdSeq = new LinuxCommandSeq(
                            new List<LinuxCommand> {
                                new LinuxCommand("ip service http admin-state enable", sessionPrompt),
                                new LinuxCommand("aaa authentication default local", sessionPrompt),
                                new LinuxCommand("aaa  authentication http local", sessionPrompt),
                                new LinuxCommand("write memory", sessionPrompt)
                        });
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
                SendSwitchError(progrMsg, ex);
            }
            DisconnectAosSsh();
        }
        public void GetSystemInfo()
        {
            SendProgressReport(Translate("i18n_sys"));
            GetSyncStatus();
            GetVlanSettings();
            _dictList = SendCommand(new CmdRequest(Command.SHOW_IP_ROUTES, ParseType.Htable)) as List<Dictionary<string, string>>;
            _dict = _dictList.FirstOrDefault(d => d[DNS_DEST] == "0.0.0.0/0");
            if (_dict != null) SwitchModel.DefaultGwy = _dict[GATEWAY];
        }

        public List<Dictionary<string, string>> GetVlanSettings()
        {
            _dictList = SendCommand(new CmdRequest(Command.SHOW_IP_INTERFACE, ParseType.Htable)) as List<Dictionary<string, string>>;
            _vlanSettings = new List<VlanModel>();
            foreach (Dictionary<string, string> dict in _dictList) { _vlanSettings.Add(new VlanModel(dict)); }
            _dict = _dictList.FirstOrDefault(d => d[IP_ADDR] == SwitchModel.IpAddress);
            if (_dict != null) SwitchModel.NetMask = _dict[SUBNET_MASK];
            return _dictList;
        }

        public string GetSyncStatus()
        {
            _dict = SendCommand(new CmdRequest(Command.SHOW_SYSTEM_RUNNING_DIR, ParseType.MibTable, DictionaryType.SystemRunningDir)) as Dictionary<string, string>;
            SwitchModel.LoadFromDictionary(_dict, DictionaryType.SystemRunningDir);
            try
            {
                SwitchModel.ConfigSnapshot = SendCommand(new CmdRequest(Command.SHOW_CONFIGURATION, ParseType.NoParsing)) as string;
                string filePath = Path.Combine(Path.Combine(MainWindow.DataPath, SNAPSHOT_FOLDER), $"{SwitchModel.IpAddress}{SNAPSHOT_SUFFIX}");
                if (File.Exists(filePath))
                {
                    string prevCfgSnapshot = File.ReadAllText(filePath);
                    if (!string.IsNullOrEmpty(prevCfgSnapshot)) return ConfigChanges.GetChanges(SwitchModel, prevCfgSnapshot);
                }
            }
            catch (Exception ex)
            {
                SendSwitchError(Translate("i18n_rsGs"), ex);
            }
            return null;
        }

        public void GetSnapshot()
        {
            try
            {
                SendProgressReport(Translate("i18n_lsnap"));
                SwitchModel.ConfigSnapshot = SendCommand(new CmdRequest(Command.SHOW_CONFIGURATION, ParseType.NoParsing)) as string;
                if (!SwitchModel.ConfigSnapshot.Contains(CMD_TBL[Command.LLDP_SYSTEM_DESCRIPTION_ENABLE]))
                {
                    SendProgressReport(Translate("i18n_rsLldp"));
                    SendCommand(new CmdRequest(Command.LLDP_SYSTEM_DESCRIPTION_ENABLE));
                }
            }
            catch (Exception ex)
            {
                SendSwitchError(Translate("i18n_rsGs"), ex);
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

        public LinuxCommandSeq SendSshLinuxCommandSeq(LinuxCommandSeq cmdEntry, string progressMsg)
        {
            try
            {
                _progress.Report(new ProgressReport(progressMsg));
                UpdateProgressBar(++progressBarCnt); //  1
                DateTime startTime = DateTime.Now;
                ConnectAosSsh();
                string msg = $"{progressMsg} {Translate("i18n_onsw")} {SwitchModel.Name}";
                Dictionary<string, string> response = new Dictionary<string, string>();
                cmdEntry.StartTime = DateTime.Now;
                foreach (LinuxCommand cmdLinux in cmdEntry.CommandSeq)
                {
                    cmdLinux.Response = SshService?.SendLinuxCommand(cmdLinux);
                    if (cmdLinux.DelaySec > 0) WaitSec(msg, cmdLinux.DelaySec);
                    SendWaitProgressReport(msg, startTime);
                    UpdateProgressBar(++progressBarCnt); //  1
                }
                cmdEntry.Duration = CalcStringDuration(cmdEntry.StartTime);
                return cmdEntry;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Dictionary<string, string> RunSwitchCommandSsh(Command cmd, string[] data)
        {
            try
            {
                ConnectAosSsh();
                Dictionary<string, string> result = SshService?.SendCommand(new RestUrlEntry(cmd), data);
                DisconnectAosSsh();
                return result;
            }
            catch (Exception ex)
            {
                DisconnectAosSsh();
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

        public void RunGetSwitchLog(SwitchDebugModel debugLog, bool restartPoE, double maxLogDur, string slotPortNr)
        {
            try
            {
                _wizardSwitchSlot = null;
                _debugSwitchLog = debugLog;
                if (!string.IsNullOrEmpty(slotPortNr))
                {
                    GetSwitchSlotPort(slotPortNr);
                    if (_wizardSwitchPort == null)
                    {
                        SendProgressError(Translate("i18n_getLog"), $"{Translate("i18n_nodp")} {slotPortNr}");
                        return;
                    }
                }
                progressStartTime = DateTime.Now;
                StartProgressBar($"{Translate("i18n_clog")} {SwitchModel.Name}{WAITING}", maxLogDur);
                ConnectAosSsh();
                UpdateSwitchLogBar();
                int debugSelected = _debugSwitchLog.IntDebugLevelSelected;
                // Getting current lan power status
                GetCurrentLanPowerStatus();
                // Getting current switch debug level
                GetCurrentSwitchDebugLevel();
                int prevLpNiDebug = SwitchModel.LpNiDebugLevel;
                int prevLpCmmDebug = SwitchModel.LpCmmDebugLevel;
                // Setting switch debug level
                SetAppDebugLevel($"{Translate("i18n_pdbg")} {IntToSwitchDebugLevel(debugSelected)}", Command.DEBUG_UPDATE_LPNI_LEVEL, debugSelected);
                SetAppDebugLevel($"{Translate("i18n_pdbg")} {IntToSwitchDebugLevel(debugSelected)}", Command.DEBUG_UPDATE_LPCMM_LEVEL, debugSelected);
                if (restartPoE)
                {
                    if (_wizardSwitchPort != null) RestartDeviceOnPort($"{Translate("i18n_prst")} {_wizardSwitchPort.Name} {Translate("i18n_caplog")}", 5);
                    else RestartChassisPoE();
                }
                else
                {
                    WaitSec($"{Translate("i18n_clog")} {SwitchModel.Name}", 5);
                }
                UpdateSwitchLogBar();
                // Setting switch debug level back to the previous values
                SetAppDebugLevel($"{Translate("i18n_rpdbg")} {IntToSwitchDebugLevel(prevLpNiDebug)}", Command.DEBUG_UPDATE_LPNI_LEVEL, prevLpNiDebug);
                SetAppDebugLevel($"{Translate("i18n_rcdbg")} {IntToSwitchDebugLevel(prevLpCmmDebug)}", Command.DEBUG_UPDATE_LPCMM_LEVEL, prevLpCmmDebug);
                // Generating tar file
                string msg = Translate("i18n_targ");
                SendProgressReport(msg);
                WaitSec(msg, 5);
                SendCommand(new CmdRequest(Command.DEBUG_CREATE_LOG));
                Logger.Info($"Generated log file in {SwitchDebugLogLevel.Debug3} level on switch {SwitchModel.Name}, duration: {CalcStringDuration(progressStartTime)}");
                UpdateSwitchLogBar();
            }
            catch (Exception ex)
            {
                SendSwitchError(Translate("i18n_getLog"), ex);
            }
            finally
            {
                DisconnectAosSsh();
            }
        }

        private void GetCurrentLanPowerStatus()
        {
            if (_wizardSwitchSlot != null)
            {
                if (!_wizardSwitchSlot.SupportsPoE)
                {
                    Logger.Warn($"Cannot get the lanpower status on slot {_wizardSwitchSlot.Name} because it doesn't support PoE!");
                    return;
                }
                GetShowDebugSlotPower(_wizardSwitchSlot.Name);
            }
            else
            {
                foreach (ChassisModel chassis in this.SwitchModel.ChassisList)
                {
                    if (!chassis.SupportsPoE)
                    {
                        Logger.Warn($"Cannot get the lanpower status on chassis {chassis.Number} because it doesn't support PoE!");
                        continue;
                    }
                    foreach (var slot in chassis.Slots)
                    {
                        GetShowDebugSlotPower(slot.Name);
                    }
                }
            }
        }

        private void GetShowDebugSlotPower(string slotNr)
        {
            SendProgressReport($"{Translate("i18n_lanpw")} {slotNr}");
            string resp = SendCommand(new CmdRequest(Command.DEBUG_SHOW_LAN_POWER_STATUS, ParseType.NoParsing, slotNr)) as string;
            if (!string.IsNullOrEmpty(resp)) _debugSwitchLog.UpdateLanPowerStatus($"debug show lanpower slot {slotNr} status ni", resp);
            UpdateSwitchLogBar();
        }

        private void RestartChassisPoE()
        {
            foreach (var chassis in this.SwitchModel.ChassisList)
            {
                if (!chassis.SupportsPoE)
                {
                    Logger.Warn($"Cannot turn the power OFF on chassis {chassis.Number} of the switch {SwitchModel.IpAddress} because it doesn't support PoE!");
                    continue;
                }
                string msg = $"{Translate("i18n_chasoff")} {chassis.Number} {Translate("i18n_caplog")}";
                _progress.Report(new ProgressReport($"{msg}{WAITING}"));
                foreach (SlotModel slot in chassis.Slots)
                {
                    SendCommand(new CmdRequest(Command.POWER_DOWN_SLOT, slot.Name.ToString()));
                }
                UpdateSwitchLogBar();
                WaitSec(msg, 5);
                _progress.Report(new ProgressReport($"{Translate("i18n_chason")} {chassis.Number} {Translate("i18n_caplog")}{WAITING}"));
                foreach (SlotModel slot in chassis.Slots)
                {
                    SendCommand(new CmdRequest(Command.POWER_UP_SLOT, slot.Name.ToString()));
                }
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
            if (SshService != null && SshService.IsSwitchConnected()) return;
            if (SshService != null) DisconnectAosSsh();
            SshService = new AosSshService(SwitchModel);
            SshService.ConnectSshClient();
        }

        private void DisconnectAosSsh()
        {
            if (SshService == null) return;
            try
            {
                SshService.DisconnectSshClient();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            SshService = null;
        }

        private void SetAppDebugLevel(string progressMsg, Command cmd, int dbgLevel)
        {
            string appName = cmd == Command.DEBUG_SHOW_LPNI_LEVEL ? LPNI : LPCMM;
            try
            {
                if (dbgLevel == (int)SwitchDebugLogLevel.Invalid || dbgLevel == (int)SwitchDebugLogLevel.Unknown)
                {
                    Logger.Warn(GetSwitchDebugLevelError(appName, $"Invalid switch debug level {dbgLevel}!"));
                    return;
                }
                Command showDbgCmd = cmd == Command.DEBUG_UPDATE_LPCMM_LEVEL ? Command.DEBUG_SHOW_LPCMM_LEVEL : Command.DEBUG_SHOW_LPNI_LEVEL;
                _progress.Report(new ProgressReport($"{progressMsg}{WAITING}"));
                DateTime startCmdTime = DateTime.Now;
                SendSshUpdateLogCommand(cmd, new string[1] { dbgLevel.ToString() });
                UpdateSwitchLogBar();
                bool done = false;
                int loopCnt = 1;
                while (!done)
                {
                    Thread.Sleep(1000);
                    _progress.Report(new ProgressReport($"{progressMsg} ({loopCnt} {Translate("i18n_sec")}){WAITING}"));
                    UpdateSwitchLogBar();
                    if (loopCnt % 5 == 0) done = GetAppDebugLevel(showDbgCmd) == dbgLevel;
                    if (loopCnt >= 30)
                    {
                        Logger.Error($"Took too long ({CalcStringDuration(startCmdTime)}) to complete\"{cmd}\" to \"{dbgLevel}\"!");
                        return;
                    }
                    loopCnt++;
                }
                Logger.Info($"\"{appName}\" debug level set to \"{dbgLevel}\", Duration: {CalcStringDuration(startCmdTime)}");
                UpdateSwitchLogBar();
            }
            catch (Exception ex)
            {
                Logger.Warn(GetSwitchDebugLevelError(appName, ex.Message));
            }
        }

        private void GetCurrentSwitchDebugLevel()
        {
            SendProgressReport(Translate("i18n_clogl"));
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
            UpdateProgressBar(GetTimeDuration(progressStartTime));
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
                    if (_dictList?.Count > 0 && _dictList[0]?.Count > 0)
                    {
                        _debugSwitchLog.LoadFromDictionary(_dictList);
                        return cmd == Command.DEBUG_SHOW_LPCMM_LEVEL ? _debugSwitchLog.LpCmmLogLevel : _debugSwitchLog.LpNiLogLevel;
                    }
                }
            }
            catch { }
            GetSshDebugLevel(cmd);
            return cmd == Command.DEBUG_SHOW_LPCMM_LEVEL ? _debugSwitchLog.LpCmmLogLevel : _debugSwitchLog.LpNiLogLevel;
        }

        private void GetSshDebugLevel(Command cmd)
        {
            string appName = cmd == Command.DEBUG_SHOW_LPCMM_LEVEL ? LPCMM : LPNI;
            try
            {
                Dictionary<string, string> response = SendSshUpdateLogCommand(cmd);
                if (response != null && response.ContainsKey(OUTPUT) && !string.IsNullOrEmpty(response[OUTPUT]))
                {
                    _debugSwitchLog.LoadFromDictionary(CliParseUtils.ParseCliSwitchDebugLevel(response[OUTPUT]));
                }
            }
            catch (Exception ex)
            {
                if (appName == LPNI) _debugSwitchLog.LpNiApp.SetDebugLevel(SwitchDebugLogLevel.Invalid);
                else _debugSwitchLog.LpCmmApp.SetDebugLevel(SwitchDebugLogLevel.Invalid);
                Logger.Warn(GetSwitchDebugLevelError(appName, ex.Message));
            }
        }

        private string GetSwitchDebugLevelError(string appName, string error)
        {
            return $"Switch {SwitchModel.Name} ({SwitchModel.IpAddress}) doesn't support \"{appName}\" debug level!\n{error}";
        }

        private Dictionary<string, string> SendSshUpdateLogCommand(Command cmd, string[] data = null)
        {
            ConnectAosSsh();
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
            return null;
        }

        public void WriteMemory(int waitSec = 40)
        {
            try
            {
                if (SwitchModel.SyncStatus == SyncStatusType.Synchronized) return;
                string msg = $"{Translate("i18n_rsMem")} {SwitchModel.Name}";
                StartProgressBar($"{msg}{WAITING}", 30);
                SendCommand(new CmdRequest(Command.WRITE_MEMORY));
                progressStartTime = DateTime.Now;
                double dur = 0;
                while (dur < waitSec)
                {
                    Thread.Sleep(1000);
                    dur = GetTimeDuration(progressStartTime);
                    try
                    {
                        int period = (int)dur;
                        if (period > 20 && period % 5 == 0) GetSyncStatus();
                    }
                    catch { }
                    if (SwitchModel.SyncStatus != SyncStatusType.NotSynchronized || dur >= waitSec) break;
                    UpdateProgressBarMessage($"{msg} ({(int)dur} {Translate("i18n_sec")}){WAITING}", dur);
                }
                LogActivity("Write memory completed", $", duration: {CalcStringDuration(progressStartTime)}");
                SaveConfigSnapshot();
            }
            catch (Exception ex)
            {
                Logger.Warn(ex.Message);
            }
            CloseProgressBar();
        }

        public string BackupConfiguration(double maxDur, bool backupImage)
        {
            try
            {
                _sftpService = new SftpService(SwitchModel.IpAddress, SwitchModel.Login, SwitchModel.Password);
                string sftpError = _sftpService.Connect();
                if (string.IsNullOrEmpty(sftpError))
                {
                    _backupStartTime = DateTime.Now;
                    string msg = $"{Translate("i18n_bckRunning")} {SwitchModel.Name}";
                    Logger.Info(msg);
                    StartProgressBar($"{msg}{WAITING}", maxDur);
                    PurgeBackupRestoreFolder();
                    DowloadSwitchFiles(FLASH_CERTIFIED_DIR, FLASH_CERTIFIED_FILES);
                    DowloadSwitchFiles(FLASH_NETWORK_DIR, FLASH_NETWORK_FILES);
                    DowloadSwitchFiles(FLASH_SWITCH_DIR, FLASH_SWITCH_FILES);
                    DowloadSwitchFiles(FLASH_SYSTEM_DIR, FLASH_SYSTEM_FILES);
                    DowloadSwitchFiles(FLASH_WORKING_DIR, FLASH_WORKING_FILES, backupImage);
                    DowloadSwitchFiles(FLASH_PYTHON_DIR, FLASH_PYTHON_FILES);
                    CreateAdditionalFiles();
                    string backupFile = CompressBackupFiles();
                    StringBuilder sb = new StringBuilder("Backup configuration of switch ");
                    sb.Append(SwitchModel.Name).Append(" (").Append(SwitchModel.IpAddress).Append(") completed.");
                    FileInfo info = new FileInfo(backupFile);
                    if (info?.Length > 0) sb.Append("\r\nFile created: \"").Append(info.Name).Append("\" (").Append(PrintNumberBytes(info.Length)).Append(")");
                    else sb.Append("\r\nBackup file not created!");
                    sb.Append("\r\nBackup duration: ").Append(CalcStringDuration(_backupStartTime));
                    Logger.Activity(sb.ToString());
                    return backupFile;
                }
                else
                {
                    throw new Exception($"Fail to establish the SFTP connection to switch {SwitchModel.Name} ({SwitchModel.IpAddress})!\r\nReason: {sftpError}");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex.Message);
                return null;
            }
            finally
            {
                _sftpService?.Disconnect();
                _sftpService = null;
                CloseProgressBar();
            }
        }

        public void UnzipBackupSwitchFiles(double maxDur, string selFilePath)
        {
            Thread th = null;
            try
            {
                _backupStartTime = DateTime.Now;
                string msg = $"{Translate("i18n_restRunning")} {SwitchModel.Name}";
                StartProgressBar($"{msg}{WAITING}", maxDur);
                th = new Thread(() => SendProgressMessage(msg, _backupStartTime, Translate("i18n_restUnzip")));
                th.Start();
                string restoreFolder = PurgeBackupRestoreFolder();
                _sftpService = new SftpService(SwitchModel.IpAddress, SwitchModel.Login, SwitchModel.Password);
                DateTime startTime = DateTime.Now;
                _sftpService.UnzipBackupSwitchFiles(selFilePath);
                string swInfoFilePath = Path.Combine(restoreFolder, BACKUP_SWITCH_INFO_FILE);
                string vlanFilePath = Path.Combine(restoreFolder, BACKUP_VLAN_CSV_FILE);
                string vcBootFilePath = Path.Combine(restoreFolder, FLASH_WORKING_DIR, VCBOOT_FILE);
                StringBuilder txt = new StringBuilder($"Unzipping backup configuration file of switch ");
                txt.Append(SwitchModel.Name).Append(" (").Append(SwitchModel.IpAddress).Append(").");
                txt.Append("\r\nSelected file: \"").Append(selFilePath).Append("\", size: ").Append(PrintNumberBytes(new FileInfo(selFilePath).Length));
                txt.Append("\r\nDuration: ").Append(Utils.CalcStringDuration(startTime));
                Logger.Activity(txt.ToString());
                th.Abort();
            }
            catch (Exception ex)
            {
                th?.Abort();
                Logger.Error(ex);
            }
        }

        private string PurgeBackupRestoreFolder()
        {
            string restoreFolder = Path.Combine(MainWindow.DataPath, BACKUP_DIR);
            if (Directory.Exists(restoreFolder)) PurgeFilesInFolder(restoreFolder);
            return restoreFolder;
        }

        public void UploadConfigurationFiles(double maxDur, bool restoreImage)
        {
            try
            {
                _sftpService = new SftpService(SwitchModel.IpAddress, SwitchModel.Login, SwitchModel.Password);
                string sftpError = _sftpService.Connect();
                StringBuilder filesUploaded = new StringBuilder();
                int cnt = 0;
                if (string.IsNullOrEmpty(sftpError))
                {
                    _backupStartTime = DateTime.Now;
                    string msg = $"{Translate("i18n_restRunning")} {SwitchModel.Name}";
                    Logger.Info(msg);
                    StartProgressBar($"{msg}{WAITING}", maxDur);
                    string restoreFolder = Path.Combine(MainWindow.DataPath, BACKUP_DIR);
                    string[] filesList = GetFilesInFolder(Path.Combine(restoreFolder, FLASH_DIR));
                    foreach (string localFilePath in filesList)
                    {
                        try
                        {
                            if (localFilePath.EndsWith(".img") && !restoreImage) continue;
                            string fileInfo = UploadRemoteFile(localFilePath);
                            if (!string.IsNullOrEmpty(fileInfo))
                            {
                                if (cnt % 5 == 0) filesUploaded.Append("\r\n\t");
                                else if (filesUploaded.Length > 0) filesUploaded.Append(", ");
                                filesUploaded.Append(fileInfo);
                                cnt++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                        }
                    }
                    StringBuilder sb = new StringBuilder("Upload configuration files of switch ");
                    sb.Append(SwitchModel.Name).Append(" (").Append(SwitchModel.IpAddress).Append(") completed.");
                    if (filesUploaded?.Length > 0) sb.Append("\r\nFiles uploaded:").Append(filesUploaded);
                    else sb.Append("\r\nConfiguration files not uploaded!");
                    sb.Append("\r\nUpload duration: ").Append(CalcStringDuration(_backupStartTime));
                    Logger.Activity(sb.ToString());
                }
                else
                {
                    throw new Exception($"Fail to establish the SFTP connection to switch {SwitchModel.Name} ({SwitchModel.IpAddress})!\r\nReason: {sftpError}");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex.Message);
            }
            finally
            {
                _sftpService?.Disconnect();
                _sftpService = null;
                CloseProgressBar();
            }
        }

        private string UploadRemoteFile(string localFilePath)
        {
            string fileInfo = string.Empty;
            Thread th = null;
            try
            {
                string fileName = Path.GetFileName(localFilePath);
                th = new Thread(() => SendProgressMessage($"{Translate("i18n_restRunning")} {SwitchModel.Name}", _backupStartTime, $"{Translate("i18n_restUploadFile")} {fileName}"));
                th.Start();
                string restoreFolder = Path.Combine(MainWindow.DataPath, BACKUP_DIR);
                FileInfo info = new FileInfo(localFilePath);
                if (info.Exists && info.Length > 0)
                {
                    DateTime startTime = DateTime.Now;
                    string remotepath = $"{Path.GetDirectoryName(localFilePath).Replace(restoreFolder, string.Empty).Replace("\\", "/")}/{fileName}";
                    _sftpService.UploadFile(localFilePath, remotepath, true);
                    fileInfo = $"{remotepath} ({PrintNumberBytes(info.Length)}, {CalcStringDuration(startTime)})";
                    Logger.Debug($"Uploading file \"{remotepath}\"");
                }
                th.Abort();
            }
            catch (Exception ex)
            {
                th?.Abort();
                Logger.Error(ex);
            }
            return fileInfo;
        }

        private void CreateAdditionalFiles()
        {
            Thread th = null;
            try
            {
                th = new Thread(() => SendProgressMessage($"{Translate("i18n_bckRunning")} {SwitchModel.Name}", _backupStartTime, Translate("i18n_bckAddFiles")));
                th.Start();
                string users = SendCommand(new CmdRequest(Command.SHOW_USER, ParseType.NoParsing)) as string;
                string filePath = Path.Combine(MainWindow.DataPath, BACKUP_DIR, BACKUP_USERS_FILE);
                File.WriteAllText(filePath, users);
                filePath = Path.Combine(MainWindow.DataPath, BACKUP_DIR, BACKUP_SWITCH_INFO_FILE);
                StringBuilder sb = new StringBuilder();
                string swInfo = $"{BACKUP_SWITCH_NAME}: {SwitchModel.Name}\r\n{BACKUP_SWITCH_IP}: {SwitchModel.IpAddress}";
                if (SwitchModel?.ChassisList?.Count > 0)
                {
                    foreach (ChassisModel chassis in SwitchModel?.ChassisList)
                    {
                        swInfo += $"\r\n{BACKUP_CHASSIS} {chassis.Number} {BACKUP_SERIAL_NUMBER}: {chassis.SerialNumber}";
                    }
                }
                File.WriteAllText(filePath, swInfo);
                filePath = Path.Combine(MainWindow.DataPath, BACKUP_DIR, BACKUP_DATE_FILE);
                File.WriteAllText(filePath, DateTime.Now.ToString("MM/dd/yyyy h:mm:ss tt"));
                if (_vlanSettings?.Count > 0)
                {
                    filePath = Path.Combine(MainWindow.DataPath, BACKUP_DIR, BACKUP_VLAN_CSV_FILE);
                    StringBuilder txt = new StringBuilder();
                    txt.Append(VLAN_NAME).Append(",").Append(VLAN_IP).Append(",").Append(VLAN_MASK).Append(",").Append(VLAN_DEVICE);
                    foreach (VlanModel vlan in _vlanSettings)
                    {
                        txt.Append("\r\n\"").Append(vlan.Name).Append("\",\"").Append(vlan.IpAddress).Append("\",\"");
                        txt.Append(vlan.SubnetMask).Append("\",\"").Append(vlan.Device).Append("\"");
                    }
                    File.WriteAllText(filePath, txt.ToString());
                }
                th.Abort();
            }
            catch (Exception ex)
            {
                th?.Abort();
                Logger.Error(ex);
            }
        }

        private void DowloadSwitchFiles(string remoteDir, List<string> filesToDownload, bool backImage = true)
        {
            List<string> filesList = _sftpService.GetFilesInRemoteDir(remoteDir);
            foreach (string fileName in filesToDownload)
            {
                try
                {
                    if (fileName.StartsWith("*.")) DownloadFilteredRemoteFiles(remoteDir, fileName, backImage);
                    else if (filesList.Contains(fileName)) DownloadRemoteFile(remoteDir, fileName);
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex.Message);
                }
            }
        }

        private void DownloadFilteredRemoteFiles(string remoteDir, string fileSuffix, bool backImage = true)
        {
            List<string> files = _sftpService.GetFilesInRemoteDir(remoteDir, fileSuffix.Replace("*", string.Empty));
            if (files.Count < 1) return;
            foreach (string fileName in files)
            {
                if (!fileName.Contains(".img") || (fileName.Contains(".img") && backImage)) DownloadRemoteFile(remoteDir, fileName);
            }
        }

        private void DownloadRemoteFile(string srcFileDir, string fileName)
        {
            Thread th = null;
            try
            {
                th = new Thread(() => SendProgressMessage($"{Translate("i18n_bckRunning")} {SwitchModel.Name}", _backupStartTime, $"{Translate("i18n_bckDowloadFile")} {fileName}"));
                th.Start();
                string srcFilePath = $"{srcFileDir}/{fileName}";
                _sftpService.DownloadFile(srcFilePath, $"{BACKUP_DIR}{srcFileDir.Replace("/", "\\")}\\{fileName}");
                th.Abort();
            }
            catch (Exception ex)
            {
                th?.Abort();
                Logger.Error(ex);
            }
        }

        private string CompressBackupFiles()
        {
            Thread th = null;
            DateTime startTime = DateTime.Now;
            string zipPath = string.Empty;
            try
            {
                string backupPath = Path.Combine(MainWindow.DataPath, BACKUP_DIR);
                zipPath = Path.Combine(MainWindow.DataPath, $"{SwitchModel.Name}_{DateTime.Now:MM-dd-yyyy_hh_mm_ss}.zip");
                th = new Thread(() => SendProgressMessage($"{Translate("i18n_bckRunning")} {SwitchModel.Name}", _backupStartTime, Translate("i18n_bckZipping")));
                th.Start();
                if (File.Exists(zipPath)) File.Delete(zipPath);
                ZipFile.CreateFromDirectory(backupPath, zipPath, CompressionLevel.Fastest, true);
                PurgeFilesInFolder(backupPath);
                th.Abort();
            }
            catch (Exception ex)
            {
                th?.Abort();
                Logger.Error(ex);
            }
            Logger.Activity($"Compressing backup files completed (duration: {CalcStringDuration(startTime)})");
            return zipPath;
        }

        private void SendProgressMessage(string title, DateTime startTime, string progrMsg)
        {
            int dur;
            while (Thread.CurrentThread.IsAlive)
            {
                string msg = $"({CalcStringDurationTranslate(startTime, true)}){WAITING}";
                dur = (int)GetTimeDuration(startTime);
                UpdateProgressBarMessage($"{title} {msg}\n{progrMsg}", dur);
                Thread.Sleep(1000);
                if (dur >= 300) break;
            }
        }

        private void SaveConfigSnapshot()
        {
            try
            {
                string folder = Path.Combine(MainWindow.DataPath, SNAPSHOT_FOLDER);
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
            string folder = Path.Combine(MainWindow.DataPath, SNAPSHOT_FOLDER);
            if (Directory.Exists(folder))
            {
                string txt = PurgeFiles(folder, MAX_NB_SNAPSHOT_SAVED);
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
                string msg = $"{Translate("i18n_swrst")} {SwitchModel.Name}";
                Logger.Info(msg);
                StartProgressBar($"{msg}{WAITING}", 320);
                SendRebootSwitchRequest();
                if (waitSec <= 0) return string.Empty;
                msg = $"{Translate("i18n_rsReboot")} {SwitchModel.Name} {Translate("i18n_reboot")} ";
                WaitSec(msg, 5);
                _progress.Report(new ProgressReport($"{msg}{WAITING}"));
                double dur = 0;
                while (dur <= 60)
                {
                    if (dur >= waitSec)
                    {
                        throw new Exception($"{Translate("i18n_switch")} {SwitchModel.Name} {Translate("i18n_rsTout")} {CalcStringDurationTranslate(progressStartTime, true)}!");
                    }
                    Thread.Sleep(1000);
                    dur = GetTimeDuration(progressStartTime);
                    UpdateProgressBarMessage($"{msg}({CalcStringDurationTranslate(progressStartTime, true)}){WAITING}", dur);
                }
                while (dur < waitSec + 1)
                {
                    if (dur >= waitSec)
                    {
                        throw new Exception($"{Translate("i18n_switch") }{SwitchModel.Name} {Translate("i18n_rsTout")} {CalcStringDurationTranslate(progressStartTime, true)}!");
                    }
                    Thread.Sleep(1000);
                    dur = (int)GetTimeDuration(progressStartTime);
                    UpdateProgressBarMessage($"{msg}({CalcStringDurationTranslate(progressStartTime, true)}){WAITING}", dur);
                    if (!IsReachable(SwitchModel.IpAddress)) continue;
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
                LogActivity("Switch rebooted", $", duration: {CalcStringDuration(progressStartTime, true)}");
            }
            catch (Exception ex)
            {
                SendSwitchError($"{Translate("i18n_rebsw")} {SwitchModel.Name}", ex);
                return null;
            }
            CloseProgressBar();
            return CalcStringDurationTranslate(progressStartTime, true);
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
                    dur = GetTimeDuration(startTime);
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

        public void StopTrafficAnalysis(TrafficStatus abortType, string stopReason)
        {
            trafficAnalysisStatus = abortType;
            stopTrafficAnalysisReason = stopReason;
        }

        public bool IsTrafficAnalysisRunning()
        {
            return trafficAnalysisStatus == TrafficStatus.Running;
        }

        public TrafficReport RunTrafficAnalysis(int selectedDuration)
        {
            TrafficReport report;
            try
            {
                trafficAnalysisStatus = TrafficStatus.Running;
                _switchTraffic = null;
                GetPortsTrafficInformation();
                report = new TrafficReport(_switchTraffic, selectedDuration);
                DateTime startTime = DateTime.Now;
                LogActivity($"Started traffic analysis", $" for {selectedDuration} sec");
                double dur = 0;
                while (dur < selectedDuration)
                {
                    if (trafficAnalysisStatus != TrafficStatus.Running) break;
                    dur = GetTimeDuration(startTime);
                    if (dur >= selectedDuration)
                    {
                        trafficAnalysisStatus = TrafficStatus.Completed;
                        stopTrafficAnalysisReason = "completed";
                        break;
                    }
                    Thread.Sleep(250);
                }
                if (trafficAnalysisStatus == TrafficStatus.Abort)
                {
                    Logger.Warn($"Traffic analysis on switch {SwitchModel.IpAddress} was {stopTrafficAnalysisReason}!");
                    Activity.Log(SwitchModel, "Traffic analysis interrupted.");
                    return null;
                }
                GetMacAndLldpInfo(MAX_SCAN_NB_MAC_PER_PORT);
                GetPortsTrafficInformation();
                report.Complete(stopTrafficAnalysisReason, GetDdmReport());
                if (trafficAnalysisStatus == TrafficStatus.CanceledByUser)
                {
                    Logger.Warn($"Traffic analysis on switch {SwitchModel.IpAddress} was {stopTrafficAnalysisReason}, selected duration: {report.SelectedDuration}!");
                }
                LogActivity($"Traffic analysis {stopTrafficAnalysisReason}.", $"\n{report.Summary}");
            }
            catch (Exception ex)
            {
                SendSwitchError($"{Translate("i18n_taerr")} {SwitchModel.Name}", ex);
                return null;
            }
            finally
            {
                trafficAnalysisStatus = TrafficStatus.Idle;
            }
            return report;
        }

        private void GetMacAndLldpInfo(int maxNbMacPerPort)
        {
            SendProgressReport(Translate("i18n_rlldp"));
            object lldpList = SendCommand(new CmdRequest(Command.SHOW_LLDP_REMOTE, ParseType.LldpRemoteTable));
            SwitchModel.LoadLldpFromList(lldpList as Dictionary<string, List<Dictionary<string, string>>>, DictionaryType.LldpRemoteList);
            lldpList = SendCommand(new CmdRequest(Command.SHOW_LLDP_INVENTORY, ParseType.LldpRemoteTable));
            SwitchModel.LoadLldpFromList(lldpList as Dictionary<string, List<Dictionary<string, string>>>, DictionaryType.LldpInventoryList);
            SendProgressReport(Translate("i18n_rmac"));
            _dictList = SendCommand(new CmdRequest(Command.SHOW_MAC_LEARNING, ParseType.Htable)) as List<Dictionary<string, string>>;
            SwitchModel.LoadMacAddressFromList(_dictList, maxNbMacPerPort);
        }

        private void GetPortsTrafficInformation()
        {
            try
            {
                _dictList = SendCommand(new CmdRequest(Command.SHOW_INTERFACES, ParseType.TrafficTable)) as List<Dictionary<string, string>>;
                if (_dictList?.Count > 0)
                {
                    SwitchModel.LoadFromList(_dictList, DictionaryType.ShowInterfacesList);
                    if (_switchTraffic == null) _switchTraffic = new SwitchTrafficModel(SwitchModel, _dictList);
                    else _switchTraffic.UpdateTraffic(_dictList);
                }
            }
            catch (Exception ex)
            {
                SendSwitchError($"{Translate("i18n_taerr")} {SwitchModel.Name}", ex);
            }
        }

        private void ShowInterfacesList()
        {
            try
            {
                SendProgressReport(Translate("i18n_rpdet"));
                _dictList = SendCommand(new CmdRequest(Command.SHOW_INTERFACES, ParseType.TrafficTable)) as List<Dictionary<string, string>>;
                if (_dictList?.Count > 0)
                {
                    SwitchModel.LoadFromList(_dictList, DictionaryType.ShowInterfacesList);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private string GetDdmReport()
        {
            try
            {
                string resp = SendCommand(new CmdRequest(Command.SHOW_DDM_INTERFACES, ParseType.NoParsing)) as string;
                if (!string.IsNullOrEmpty(resp))
                {
                    using (StringReader reader = new StringReader(resp))
                    {
                        bool found = false;
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.Contains("----"))
                            {
                                found = true;
                                continue;
                            }
                            if (found)
                            {
                                string[] split = line.Split('/');
                                if (split.Length > 1 && line.Length > 10) return resp;
                            }
                        }
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        public bool SetPerpetualOrFastPoe(SlotModel slot, Command cmd)
        {
            bool enable = cmd == Command.POE_PERPETUAL_ENABLE || cmd == Command.POE_FAST_ENABLE;
            string poeType = (cmd == Command.POE_PERPETUAL_ENABLE || cmd == Command.POE_PERPETUAL_DISABLE) 
                ? Translate("i18n_ppoe") : Translate("i18n_fpoe");
            string action = $"{(enable ? Translate("i18n_en") : Translate("i18n_dis"))} {poeType}";
            ProgressReport progressReport = new ProgressReport($"{action} {Translate("i18n_pRep")}")
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
                progressReport.Message += $"\n - {Translate("i18n_dur")}: {PrintTimeDurationSec(startTime)}";
                _progress.Report(progressReport);
                LogActivity($"{action} on slot {_wizardSwitchSlot.Name} completed", $"\n{progressReport.Message}");
                return true;
            }
            catch (Exception ex)
            {
                SendSwitchError(action, ex);
            }
            return false;
        }

        public bool ChangePowerPriority(string port, PriorityLevelType priority)
        {
            ProgressReport progressReport = new ProgressReport(Translate("i18n_cpRep"))
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
                progressReport.Message += $"\n - {Translate("i18n_sprio")} {port} {Translate("i18n_set")} {priority}";
                progressReport.Message += $"\n - {Translate("i18n_dur")} {PrintTimeDurationSec(startTime)}";
                _progress.Report(progressReport);
                LogActivity($"Changed power priority to {priority} on port {port}", $"\n{progressReport.Message}");
                return true;
            }
            catch (Exception ex)
            {
                SendSwitchError(Translate("i18n_chprio"), ex);
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
                string progressMessage = _wizardSwitchPort.Poe == PoeStatus.NoPoe ? $"{Translate("i18n_rstp")} {port}" : $"{Translate("i18n_rstpp")} {port}";
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
                    progressReport.Message = $"{Translate("i18n_port")} {port} {Translate("i18n_rst")}.";
                    progressReport.Type = ReportType.Info;
                }
                else
                {
                    progressReport.Message = $"{Translate("i18n_port")} {port} {Translate("i18n_pfrst")}!";
                    progressReport.Type = ReportType.Warning;
                }
                progressReport.Message += $"\n{Translate("i18n_ptSt")}: {_wizardSwitchPort.Status}, {Translate("i18n_poeSt")}: {_wizardSwitchPort.Poe}, {Translate("i18n_dur")} {CalcStringDurationTranslate(startTime, true)}";
                _progress.Report(progressReport);
                LogActivity($"Port {port} restarted by the user", $"\n{progressReport.Message}");
            }
            catch (Exception ex)
            {
                SendSwitchError(Translate("i18n_chprio"), ex);
            }
        }

        private void RestartEthernetOnPort(string progressMessage, int waitTimeSec = 5)
        {
            string action = !string.IsNullOrEmpty(progressMessage) ? progressMessage : string.Empty;
            SendCommand(new CmdRequest(Command.ETHERNET_DISABLE, _wizardSwitchPort.Name));
            string msg = $"{action}{WAITING}\n{Translate("i18n_pstdown")}";
            _progress.Report(new ProgressReport($"{msg}{PrintPortStatus()}"));
            WaitSec(msg, 5);
            WaitEthernetStatus(waitTimeSec, PortStatus.Down, msg);
            SendCommand(new CmdRequest(Command.ETHERNET_ENABLE, _wizardSwitchPort.Name));
            msg = $"{action}{WAITING}\n{Translate("i18n_pstup")}";
            _progress.Report(new ProgressReport($"{msg}{PrintPortStatus()}"));
            WaitSec(msg, 5);
            WaitEthernetStatus(waitTimeSec, PortStatus.Up, msg);
        }

        private DateTime WaitEthernetStatus(int waitSec, PortStatus waitStatus, string progressMessage = null)
        {
            string msg = !string.IsNullOrEmpty(progressMessage) ? $"{progressMessage}\n" : string.Empty;
            msg += $"{Translate("i18n_waitp")} {_wizardSwitchPort.Name} {Translate("i18n_waitup")}";
            _progress.Report(new ProgressReport($"{msg}{WAITING}{PrintPortStatus()}"));
            DateTime startTime = DateTime.Now;
            PortStatus ethStatus = UpdateEthStatus();
            int dur = 0;
            while (dur < waitSec)
            {
                Thread.Sleep(1000);
                dur = (int)GetTimeDuration(startTime);
                _progress.Report(new ProgressReport($"{msg} ({CalcStringDurationTranslate(startTime, true)}){WAITING}{PrintPortStatus()}"));
                if (ethStatus == waitStatus) break;
                if (dur % 5 == 0) ethStatus = UpdateEthStatus();
            }
            return startTime;
        }

        private PortStatus UpdateEthStatus()
        {
            _dictList = SendCommand(new CmdRequest(Command.SHOW_INTERFACE_PORT, ParseType.TrafficTable, _wizardSwitchPort.Name)) as List<Dictionary<string, string>>;
            if (_dictList?.Count > 0)
            {
                foreach (Dictionary<string, string> dict in _dictList)
                {
                    string port = GetDictValue(dict, PORT);
                    if (!string.IsNullOrEmpty(port))
                    {
                        if (port == _wizardSwitchPort.Name)
                        {
                            string sValStatus = FirstChToUpper(GetDictValue(dict, OPERATIONAL_STATUS));
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
            GetMacAndLldpInfo(MAX_SCAN_NB_MAC_PER_PORT);
        }

        public void RefreshMacAndLldpInfo()
        {
            GetMacAndLldpInfo(MAX_SEARCH_NB_MAC_PER_PORT);
        }

        private void RefreshPortsInformation()
        {
            _progress.Report(new ProgressReport($"{Translate("i18n_rsPrfsh")} {SwitchModel.Name}"));
            _dictList = SendCommand(new CmdRequest(Command.SHOW_PORTS_LIST, ParseType.Htable3)) as List<Dictionary<string, string>>;
            SwitchModel.LoadFromList(_dictList, DictionaryType.PortsList);
        }

        public void PowerSlotUpOrDown(Command cmd, string slotNr)
        {
            string msg = $"{(cmd == Command.POWER_UP_SLOT ? Translate("i18n_poeon") : Translate("i18n_poeoff"))} {Translate("i18n_onsl")} {slotNr}";
            _wizardProgressReport = new ProgressReport($"{msg}{WAITING}");
            try
            {
                _wizardSwitchSlot = SwitchModel.GetSlot(slotNr);
                if (_wizardSwitchSlot == null)
                {
                    SendProgressError(msg, $"{Translate("i18n_noslt")} {slotNr}");
                    return;
                }
                if (cmd == Command.POWER_UP_SLOT) PowerSlotUp(); else PowerSlotDown();
            }
            catch (Exception ex)
            {
                SendSwitchError(msg, ex);
            }
        }

        public void RunPoeWizard(string port, WizardReport reportResult, List<Command> commands, int waitSec)
        {
            if (reportResult.IsWizardStopped(port)) return;
            _wizardProgressReport = new ProgressReport(Translate("i18n_pwRep"));
            try
            {
                GetSwitchSlotPort(port);
                if (_wizardSwitchPort == null || _wizardSwitchSlot == null)
                {
                    SendProgressError(Translate("i18n_pwiz"), $"{Translate("i18n_nodp")} {port}");
                    return;
                }
                string msg = Translate("i18n_pwRun");
                _progress.Report(new ProgressReport($"{msg}{WAITING}"));
                if (!_wizardSwitchSlot.IsInitialized) PowerSlotUp();
                _wizardReportResult = reportResult;
                if (!IsPoeWizardAborted(msg)) ExecuteWizardCommands(commands, waitSec);
            }
            catch (Exception ex)
            {
                SendSwitchError(Translate("i18n_pwiz"), ex);
            }
        }

        private bool IsPoeWizardAborted(string msg)
        {
            if (_wizardSwitchPort.Poe == PoeStatus.Conflict)
            {
                DisableConflictPower();
            }
            else if (_wizardSwitchPort.Poe == PoeStatus.NoPoe)
            {
                CreateReportPortNothingToDo($"{Translate("i18n_port")} {_wizardSwitchPort.Name} {Translate("")}");
            }
            else if (_wizardSwitchPort.IsSwitchUplink())
            {
                CreateReportPortNothingToDo($"{Translate("i18n_port")} {_wizardSwitchPort.Name} {Translate("i18n_isupl")}");
            }
            else
            {
                if (IsPoeOk())
                {
                    WaitSec(msg, 5);
                    GetSlotLanPower(_wizardSwitchSlot);
                }
                if (IsPoeOk()) NothingToDo(); else return false;
            }
            return true;
        }

        private bool IsPoeOk()
        {
            return _wizardSwitchPort.Poe != PoeStatus.Fault && _wizardSwitchPort.Poe != PoeStatus.Deny && _wizardSwitchPort.Poe != PoeStatus.Searching;
        }

        private void DisableConflictPower()
        {
            string wizardAction = $"{Translate("i18n_poeoff")} {Translate("i18n_onport")} {_wizardSwitchPort.Name}";
            _progress.Report(new ProgressReport(wizardAction));
            _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
            WaitSec(wizardAction, 5);
            GetSlotLanPower(_wizardSwitchSlot);
            if (_wizardSwitchPort.Poe == PoeStatus.Conflict)
            {
                PowerDevice(Command.POWER_DOWN_PORT);
                WaitPortUp(30, wizardAction);
                _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Ok, Translate("i18n_wizOk"));
                StringBuilder portStatus = new StringBuilder(PrintPortStatus());
                portStatus.Append(_wizardSwitchPort.Status);
                if (_wizardSwitchPort.MacList?.Count > 0) portStatus.Append($", {Translate("pwDevMac")}").Append(_wizardSwitchPort.MacList[0]);
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

        private void CreateReportPortNothingToDo(string reason)
        {
            string wizardAction = $"{Translate("i18n_noact")}\n    {reason}";
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
                        ExecuteActionOnPort($"{Translate("i18n_hdmi")} {_wizardSwitchPort.Name}", waitSec, Command.POWER_HDMI_DISABLE);
                        break;

                    case Command.LLDP_POWER_MDI_ENABLE:
                        if (_wizardSwitchPort.IsLldpMdi)
                        {
                            _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed);
                            continue;
                        }
                        ExecuteActionOnPort($"{Translate("i18n_pmdi")} {_wizardSwitchPort.Name}", waitSec, Command.LLDP_POWER_MDI_DISABLE);
                        break;

                    case Command.LLDP_EXT_POWER_MDI_ENABLE:
                        if (_wizardSwitchPort.IsLldpExtMdi)
                        {
                            _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed);
                            continue;
                        }
                        ExecuteActionOnPort($"{Translate("i18n_extmdi")} {_wizardSwitchPort.Name}", waitSec, Command.LLDP_EXT_POWER_MDI_DISABLE);
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

                    case Command.CAPACITOR_DETECTION_DISABLE:
                        ExecuteDisableCapacitorDetection(waitSec);
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
                string wizardAction = $"Checking capacitor detection on port {_wizardSwitchPort.Name}";
                _progress.Report(new ProgressReport(wizardAction));
                WaitSec(wizardAction, 5);
                GetSlotLanPower(_wizardSwitchSlot);
                wizardAction = $"{Translate("i18n_capdet")} {_wizardSwitchPort.Name}";
                _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
                if (_wizardSwitchPort.IsCapacitorDetection)
                {
                    _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed, $"\n    {Translate("i18n_cden")} {_wizardSwitchPort.Name}");
                    return;
                }
                SendCommand(new CmdRequest(Command.CAPACITOR_DETECTION_ENABLE, _wizardSwitchPort.Name));
                WaitSec(wizardAction, 5);
                RestartDeviceOnPort(wizardAction);
                CheckPortUp(waitSec, wizardAction);
                string txt;
                if (_wizardReportResult.GetReportResult(_wizardSwitchPort.Name) == WizardResult.Ok) txt = $"{wizardAction} {Translate("i18n_wizOk")}";
                else
                {
                    txt = $"{wizardAction} didn't solve the problem\nDisabling capacitor detection on port {_wizardSwitchPort.Name} to restore the previous config";
                    DisableCapacitorDetection();
                }
                Logger.Info(txt);
            }
            catch (Exception ex)
            {
                ParseException(_wizardProgressReport, ex);
            }
        }

        private void ExecuteDisableCapacitorDetection(int waitSec)
        {
            try
            {
                string wizardAction = $"{Translate("i18n_cdetdis")} {_wizardSwitchPort.Name}";
                DateTime startTime = DateTime.Now;
                _progress.Report(new ProgressReport(wizardAction));
                _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
                if (_wizardSwitchPort.IsCapacitorDetection)
                {
                    _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed, $"\n    {Translate("i18n_cdnoten")} {_wizardSwitchPort.Name}");
                }
                else
                {
                    DisableCapacitorDetection();
                    CheckPortUp(waitSec, wizardAction);
                    _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, PrintTimeDurationSec(startTime));
                    if (_wizardReportResult.GetReportResult(_wizardSwitchPort.Name) == WizardResult.Ok) return;
                    Logger.Info($"{wizardAction} didn't solve the problem");
                }
            }
            catch (Exception ex)
            {
                ParseException(_wizardProgressReport, ex);
            }
        }

        private void DisableCapacitorDetection()
        {
            SendCommand(new CmdRequest(Command.CAPACITOR_DETECTION_DISABLE, _wizardSwitchPort.Name));
            string wizardAction = $"{Translate("i18n_cdetdis")} {_wizardSwitchPort.Name}";
            RestartDeviceOnPort(wizardAction, 5);
            WaitSec(wizardAction, 10);
        }

        private void CheckMaxPower()
        {
            string wizardAction = $"{Translate("i18n_ckmxpw")} {_wizardSwitchPort.Name}";
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
                info = Translate("i18n_bmxpw").Replace("$1", _wizardSwitchPort.Name).Replace("$2", $"{_wizardSwitchPort.MaxPower}").Replace("$3", $"{maxDefaultPower}");
                _wizardReportResult.UpdateAlert(_wizardSwitchPort.Name, WizardResult.Warning, info);
            }
            else
            {
                 info = "\n    " + Translate("i18n_gmxpw").Replace("$1", _wizardSwitchPort.Name).Replace("$2", $"{_wizardSwitchPort.MaxPower}");
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
            string wizardAction = Translate("i18n_rstmxpw").Replace("$1", _wizardSwitchPort.Name).Replace("$2", $"{_wizardSwitchPort.MaxPower}").Replace("$3", $"{maxDefaultPower}");
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
                return StringToDouble(ExtractSubString(ex.Message, "power not exceed ", " when").Trim()) / 1000;
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
            return !string.IsNullOrEmpty(error) ? StringToDouble(ExtractSubString(error, "to ", "mW").Trim()) / 1000 : _wizardSwitchPort.MaxPower;
        }

        private void RefreshPoEData()
        {
            _progress.Report(new ProgressReport($"{Translate("i18n_rfsw")} {SwitchModel.Name}"));
            GetSlotPowerStatus(_wizardSwitchSlot);
            GetSlotPowerAndConfig(_wizardSwitchSlot);
        }

        private void GetSwitchSlotPort(string port)
        {
            ChassisSlotPort chassisSlotPort = new ChassisSlotPort(port);
            ChassisModel chassis = SwitchModel.GetChassis(chassisSlotPort.ChassisNr);
            if (chassis == null) return;
            _wizardSwitchSlot = chassis.GetSlot(chassisSlotPort.SlotNr);
            if (_wizardSwitchSlot == null) return;
            _wizardSwitchPort = _wizardSwitchSlot.GetPort(port);
        }

        private void TryEnable2PairPower(int waitSec)
        {
            DateTime startTime = DateTime.Now;
            bool fastPoe = _wizardSwitchSlot.FPoE == ConfigType.Enable;
            if (fastPoe) SendCommand(new CmdRequest(Command.POE_FAST_DISABLE, _wizardSwitchSlot.Name));
            double prevMaxPower = _wizardSwitchPort.MaxPower;
            if (!_wizardSwitchPort.Is4Pair)
            {
                string wizardAction = $"{Translate("i18n_r2pair")} {_wizardSwitchPort.Name}";
                try
                {
                    SendCommand(new CmdRequest(Command.POWER_4PAIR_PORT, _wizardSwitchPort.Name));
                    WaitSec(wizardAction, 3);
                    ExecuteActionOnPort(wizardAction, waitSec, Command.POWER_2PAIR_PORT);
                }
                catch (Exception ex)
                {
                    _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
                    _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, PrintTimeDurationSec(startTime));
                    string resultDescription = $"{wizardAction} {Translate("i18n_nspb")}\n   {Translate("i18n_nosup")} {_wizardSwitchSlot.Name}";
                    _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Fail, resultDescription);
                    Logger.Info($"{ex.Message}\n{resultDescription}");
                }
            }
            else
            {
                Command init4Pair = _wizardSwitchPort.Is4Pair ? Command.POWER_4PAIR_PORT : Command.POWER_2PAIR_PORT;
                _wizardCommand = _wizardSwitchPort.Is4Pair ? Command.POWER_2PAIR_PORT : Command.POWER_4PAIR_PORT;
                string i18n = _wizardSwitchPort.Is4Pair ? "i18n_e4pair" : "i18n_e2pair";
                ExecuteActionOnPort($"{Translate(i18n)} {_wizardSwitchPort.Name}", waitSec, init4Pair);
            }
            if (prevMaxPower != _wizardSwitchPort.MaxPower) SetMaxPowerToDefault(prevMaxPower);
            if (fastPoe) SendCommand(new CmdRequest(Command.POE_FAST_ENABLE, _wizardSwitchSlot.Name));
            _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, PrintTimeDurationSec(startTime));
        }

        private void SendSwitchError(string title, Exception ex)
        {
            string error = ex.Message;
            if (ex is SwitchConnectionFailure || ex is SwitchConnectionDropped || ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure)
            {
                if (ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure)
                {
                    error = $"{Translate("i18n_lifail")} {(string.IsNullOrEmpty(SwitchModel.Name) ? SwitchModel.IpAddress : SwitchModel.Name)} ({Translate("i18n_user")}:  {SwitchModel.Login})";
                    this.SwitchModel.Status = SwitchStatus.LoginFail;
                }
                else
                {
                    error = $"{Translate("i18n_dev")} {SwitchModel.IpAddress} {Translate("i18n_unrch")}\n{error}";
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
                SendCommand(new CmdRequest(_wizardCommand, _wizardSwitchPort.Name));
                WaitSec(wizardAction, 3);
                CheckPortUp(waitSec, wizardAction);
                _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, PrintTimeDurationSec(startTime));
                if (_wizardReportResult.GetReportResult(_wizardSwitchPort.Name) == WizardResult.Ok) return;
                if (restoreCmd != _wizardCommand) SendCommand(new CmdRequest(restoreCmd, _wizardSwitchPort.Name));
                Logger.Info($"{wizardAction} didn't solve the problem\nExecuting command {restoreCmd} on port {_wizardSwitchPort.Name} to restore the previous config");
            }
            catch (Exception ex)
            {
                ParseException(_wizardProgressReport, ex);
            }
        }

        private void Check823BT()
        {
            string wizardAction = $"{Translate("i18n_chkbt")} {_wizardSwitchPort.Name}";
            _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
            DateTime startTime = DateTime.Now;
            StringBuilder txt = new StringBuilder();
            switch (_wizardSwitchPort.Protocol8023bt)
            {
                case ConfigType.Disable:
                    string alert = _wizardSwitchSlot.FPoE == ConfigType.Enable ? $"{Translate("i18n_fpen")} {_wizardSwitchSlot.Name}" : null;
                    _wizardReportResult.UpdateAlert(_wizardSwitchPort.Name, WizardResult.Warning, alert);
                    break;
                case ConfigType.Unavailable:
                    txt.Append($"\n    {SwitchModel.Name} {Translate("i18n_nobt")}");
                    _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Skip, txt.ToString());
                    break;
                case ConfigType.Enable:
                    txt.Append($"\n    {Translate("i18n_bten")} {_wizardSwitchPort.Name}");
                    _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Skip, txt.ToString());
                    break;
            }
            _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, PrintTimeDurationSec(startTime));
            Logger.Info($"{wizardAction}{txt}");
        }

        private void Enable823BT(int waitSec)
        {
            try
            {
                string wizardAction = $"{Translate("i18n_sbten")} {_wizardSwitchSlot.Name}";
                _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
                DateTime startTime = DateTime.Now;
                _progress.Report(new ProgressReport(wizardAction));
                if (!_wizardSwitchSlot.Is8023btSupport)
                {
                    _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed, $"\n    {Translate("i18n_slot")} {_wizardSwitchSlot.Name} {Translate("i18n_nobt")}");
                    return;
                }
                CheckFPOEand823BT(Command.POWER_823BT_ENABLE);
                Change823BT(Command.POWER_823BT_ENABLE);
                CheckPortUp(waitSec, wizardAction);
                _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, PrintTimeDurationSec(startTime));
                if (_wizardReportResult.GetReportResult(_wizardSwitchPort.Name) == WizardResult.Ok) return;
                Change823BT(Command.POWER_823BT_DISABLE);
                Logger.Info($"{wizardAction} didn't solve the problem\nDisabling 802.3.bt on port {_wizardSwitchPort.Name} to restore the previous config");
            }
            catch (Exception ex)
            {
                SendCommand(new CmdRequest(Command.POWER_UP_SLOT, _wizardSwitchSlot.Name));
                ParseException(_wizardProgressReport, ex);
            }
        }

        private void CheckPriority()
        {
            string wizardAction = $"{Translate("i18n_ckprio")} {_wizardSwitchPort.Name}";
            _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
            DateTime startTime = DateTime.Now;
            double powerRemaining = _wizardSwitchSlot.Budget - _wizardSwitchSlot.Power;
            double maxPower = _wizardSwitchPort.MaxPower;
            StringBuilder txt = new StringBuilder();
            WizardResult changePriority;
            string remainingPower = $"{Translate("i18n_rempw")} {powerRemaining} Watts, {Translate("i18n_maxpw")} {maxPower} Watts";
            string text;
            if (_wizardSwitchPort.PriorityLevel < PriorityLevelType.High && powerRemaining < maxPower)
            {
                changePriority = WizardResult.Warning;
                string alert = $"{Translate("i18n_chgprio")} {_wizardSwitchPort.Name} {Translate("i18n_pmaysolve")}";
                text = $"\n    {remainingPower}";
                _wizardReportResult.UpdateAlert(_wizardSwitchPort.Name, WizardResult.Warning, alert);
            }
            else
            {
                changePriority = WizardResult.Skip;
                text = $"\n    {Translate("ckprio")} {_wizardSwitchPort.Name} (";
                if (_wizardSwitchPort.PriorityLevel >= PriorityLevelType.High)
                {
                    text += $"{Translate("i18n_palready")} {_wizardSwitchPort.PriorityLevel}";
                }
                else
                {
                    text += $"{remainingPower}";
                }
                text += ")";
            }
            _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, changePriority, text);
            _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, PrintTimeDurationSec(startTime));
            Logger.Info(txt.ToString());
        }

        private void TryChangePriority(int waitSec)
        {
            try
            {
                PriorityLevelType priority = PriorityLevelType.High;
                string wizardAction = $"{Translate("i18n_cprio")} {priority} {Translate("i18n_onport")} {_wizardSwitchPort.Name}";
                _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
                PriorityLevelType prevPriority = _wizardSwitchPort.PriorityLevel;
                DateTime startTime = DateTime.Now;
                StringBuilder txt = new StringBuilder(wizardAction);
                _progress.Report(new ProgressReport(txt.ToString()));
                SendCommand(new CmdRequest(Command.POWER_PRIORITY_PORT, new string[2] { _wizardSwitchPort.Name, priority.ToString() }));
                CheckPortUp(waitSec, txt.ToString());
                _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, CalcStringDurationTranslate(startTime, true));
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
                string wizardAction = $"{Translate("i18n_rstpp")} {_wizardSwitchPort.Name}";
                _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
                DateTime startTime = DateTime.Now;
                _progress.Report(new ProgressReport(wizardAction));
                RestartDeviceOnPort(wizardAction);
                CheckPortUp(waitSec, wizardAction);
                _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, PrintTimeDurationSec(startTime));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void RestartDeviceOnPort(string progressMessage, int waitTimeSec = 5)
        {
            string action = !string.IsNullOrEmpty(progressMessage) ? progressMessage : string.Empty;
            SendCommand(new CmdRequest(Command.POWER_DOWN_PORT, _wizardSwitchPort.Name));
            string msg = $"{action}{WAITING}\n{Translate("i18n_poeoff")}";
            _progress.Report(new ProgressReport($"{msg}{PrintPortStatus()}"));
            WaitSec(msg, waitTimeSec);
            SendCommand(new CmdRequest(Command.POWER_UP_PORT, _wizardSwitchPort.Name));
            msg = $"{action}{WAITING}\n{Translate("i18n_poeon")}";
            _progress.Report(new ProgressReport($"{msg}{PrintPortStatus()}"));
            WaitSec(msg, 5);
        }

        private void CheckPortUp(int waitSec, string progressMessage)
        {
            DateTime startTime = WaitPortUp(waitSec, !string.IsNullOrEmpty(progressMessage) ? progressMessage : string.Empty);
            UpdateProgressReport();
            StringBuilder text = new StringBuilder("Port ").Append(_wizardSwitchPort.Name).Append(" Status: ").Append(_wizardSwitchPort.Status).Append(", PoE Status: ");
            text.Append(_wizardSwitchPort.Poe).Append(", Power: ").Append(_wizardSwitchPort.Power).Append(" (Duration: ").Append(CalcStringDuration(startTime));
            text.Append(", MAC List: ").Append(string.Join(",", _wizardSwitchPort.MacList)).Append(")");
            Logger.Info(text.ToString());
        }

        private DateTime WaitPortUp(int waitSec, string progressMessage = null)
        {
            string msg = !string.IsNullOrEmpty(progressMessage) ? $"{progressMessage}\n" : string.Empty;
            msg += $"{Translate("i18n_waitp")} {_wizardSwitchPort.Name} {Translate("i18n_waitup")}";
            _progress.Report(new ProgressReport($"{msg}{WAITING}{PrintPortStatus()}"));
            DateTime startTime = DateTime.Now;
            UpdatePortData();
            int dur = 0;
            int cntUp = 1;
            while (dur < waitSec)
            {
                Thread.Sleep(1000);
                dur = (int)GetTimeDuration(startTime);
                _progress.Report(new ProgressReport($"{msg} ({CalcStringDurationTranslate(startTime, true)}){WAITING}{PrintPortStatus()}"));
                if (dur % 5 == 0)
                {
                    UpdatePortData();
                    if (IsPortUp())
                    {
                        if (cntUp > 2) break;
                        cntUp++;
                    }
                }
            }
            return startTime;
        }

        private bool IsPortUp()
        {
            if (_wizardSwitchPort.Status != PortStatus.Up) return false;
            else if (_wizardSwitchPort.Poe == PoeStatus.On && _wizardSwitchPort.Power * 1000 > MIN_POWER_CONSUMPTION_MW) return true;
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
                dur = GetTimeDuration(startTime);
            }
        }

        private void SendWaitProgressReport(string msg, DateTime startTime)
        {
            string strDur = CalcStringDurationTranslate(startTime, true);
            string txt = $"{msg}";
            if (!string.IsNullOrEmpty(strDur)) txt += $" ({strDur})";
            txt += $"{WAITING}{PrintPortStatus()}";
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
                    result = WizardResult.Proceed;
                    break;
            }
            string resultDescription;
            if (result == WizardResult.Ok) resultDescription = Translate("i18n_wizOk");
            else if (result == WizardResult.Fail) resultDescription = Translate("i18n_nspb");
            else resultDescription = "";
            _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, result, resultDescription);
            StringBuilder portStatus = new StringBuilder(PrintPortStatus());
            portStatus.Append(_wizardSwitchPort.Status);
            if (_wizardSwitchPort.MacList?.Count > 0) portStatus.Append($", ${Translate("i18n_devmac")} ").Append(_wizardSwitchPort.MacList[0]);
            _wizardReportResult.UpdatePortStatus(_wizardSwitchPort.Name, portStatus.ToString());
        }

        private string PrintPortStatus()
        {
            if (_wizardSwitchPort == null) return string.Empty;
            return $"\n{Translate("i18n_poeSt")}: {_wizardSwitchPort.Poe}, " + 
                $"{Translate("i18n_pwPst")} {_wizardSwitchPort.Status}, {Translate("i18n_power")} {_wizardSwitchPort.Power} Watts";
        }

        private void UpdatePortData()
        {
            if (_wizardSwitchPort == null) return;
            GetSlotPowerAndConfig(_wizardSwitchSlot);
            _dictList = SendCommand(new CmdRequest(Command.SHOW_PORT_STATUS, ParseType.Htable3, _wizardSwitchPort.Name)) as List<Dictionary<string, string>>;
            if (_dictList?.Count > 0) _wizardSwitchPort.UpdatePortStatus(_dictList[0]);
            _dictList = SendCommand(new CmdRequest(Command.SHOW_PORT_MAC_ADDRESS, ParseType.Htable, _wizardSwitchPort.Name)) as List<Dictionary<string, string>>;
            _wizardSwitchPort.UpdateMacList(_dictList, MAX_SCAN_NB_MAC_PER_PORT);
            Dictionary<string, List<Dictionary<string, string>>> lldpList = SendCommand(new CmdRequest(Command.SHOW_LLDP_REMOTE, ParseType.LldpRemoteTable,
                new string[] { _wizardSwitchPort.Name })) as Dictionary<string, List<Dictionary<string, string>>>;
            if (lldpList.ContainsKey(_wizardSwitchPort.Name)) _wizardSwitchPort.LoadLldpRemoteTable(lldpList[_wizardSwitchPort.Name]);
        }

        private void GetLanPower()
        {
            SendProgressReport(Translate("i18n_rpoe"));
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
                        slot.PoeStatus = SlotPoeStatus.NotSupported;
                        continue;
                    }
                    GetSlotPowerAndConfig(slot);
                    if (!slot.IsInitialized)
                    {
                        slot.IsPoeModeEnable = false;
                        if (slot.SupportsPoE)
                        {
                            slot.PoeStatus = SlotPoeStatus.Off;
                            _wizardReportResult.CreateReportResult(slot.Name, WizardResult.Warning, $"\n{Translate("i18n_spoeoff")} {slot.Name}");
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
                    _dict = SendCommand(new CmdRequest(Command.SHOW_POWER_SUPPLY, ParseType.Vtable, psId)) as Dictionary<string, string>;
                    ps.LoadFromDictionary(_dict);
                }
            }
            SwitchModel.SupportsPoE = (nbChassisPoE > 0);
            if (!SwitchModel.SupportsPoE) _wizardReportResult.CreateReportResult(SWITCH, WizardResult.Warning, $"{Translate("i18n_switch")} {SwitchModel.Name} {Translate("i18n_nopoe")}");
        }

        private void CheckPowerClassDetection(SlotModel slot)
        {
            try
            {
                if (slot.PowerClassDetection == ConfigType.Disable) SendCommand(new CmdRequest(Command.POWER_CLASS_DETECTION_ENABLE, slot.Name));
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
                _dictList = SendCommand(new CmdRequest(Command.SHOW_CHASSIS_LAN_POWER_STATUS, ParseType.Htable2, chassis.Number.ToString())) as List<Dictionary<string, string>>;
                chassis.LoadFromList(_dictList);
                chassis.PowerBudget = 0;
                chassis.PowerConsumed = 0;
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("lanpower") && (ex.Message.ToLower().Contains("not supported") || ex.Message.ToLower().Contains("invalid entry")))
                {
                    chassis.SupportsPoE = false;
                }
                else
                {
                    Logger.Error(ex);
                }
            }
        }

        private void GetSlotPowerAndConfig(SlotModel slot)
        {
            GetSlotPowerConfig(slot);
            GetSlotLanPower(slot);
        }

        private void GetSlotPowerConfig(SlotModel slot)
        {
            if (!slot.SupportsPoE) return;
            try
            {
                _dictList = SendCommand(new CmdRequest(Command.SHOW_LAN_POWER_CONFIG, ParseType.Htable2, slot.Name)) as List<Dictionary<string, string>>;
                slot.LoadFromList(_dictList, DictionaryType.LanPowerCfg);
                return;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            slot.Is8023btSupport = false;
            slot.PowerClassDetection = StringToConfigType(GetLanPowerFeature(slot.Name, "capacitor-detection", "capacitor-detection"));
            slot.IsHiResDetection = StringToConfigType(GetLanPowerFeature(slot.Name, "high-resistance-detection", "high-resistance-detection")) == ConfigType.Enable;
            slot.PPoE = StringToConfigType(GetLanPowerFeature(slot.Name, "fpoe", "Fast-PoE"));
            slot.FPoE = StringToConfigType(GetLanPowerFeature(slot.Name, "ppoe", "Perpetual-PoE"));
            slot.Threshold = StringToDouble(GetLanPowerFeature(slot.Name, "usage-threshold", "usage-threshold"));
            string capacitorDetection = GetLanPowerFeature(slot.Name, "capacitor-detection", "capacitor-detection");
            _dict = new Dictionary<string, string> { [POWER_4PAIR] = "NA", [POWER_OVER_HDMI] = "NA", [POWER_CAPACITOR_DETECTION] = capacitorDetection, [POWER_823BT] = "NA" };
            foreach (PortModel port in slot.Ports)
            {
                port.LoadPoEConfig(_dict);
            }
        }

        private string GetLanPowerFeature(string slotNr, string feature, string key)
        {
            try
            {
                _dictList = SendCommand(new CmdRequest(Command.SHOW_LAN_POWER_FEATURE, ParseType.Htable, new string[2] { slotNr, feature })) as List<Dictionary<string, string>>;
                if (_dictList?.Count > 0)
                {
                    _dict = _dictList[0];
                    if (_dict?.Count > 1 && _dict.ContainsKey("Chas/Slot") && _dict["Chas/Slot"] == slotNr && _dict.ContainsKey(key))
                    {
                        return _dict[key];
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return string.Empty;
        }

        private void GetSlotLanPower(SlotModel slot)
        {
            try
            {
                GetSlotPowerStatus(slot);
                if (slot.Budget > 1)
                {
                    _dictList = SendCommand(new CmdRequest(Command.SHOW_LAN_POWER, ParseType.Htable, slot.Name)) as List<Dictionary<string, string>>;
                }
                else
                {
                    Dictionary<string, object> resp = SendRequest(GetRestUrlEntry(new CmdRequest(Command.SHOW_LAN_POWER, ParseType.Htable, slot.Name)));
                    string data = resp.ContainsKey(STRING) ? resp[STRING].ToString() : string.Empty;
                    _dictList = CliParseUtils.ParseHTable(data, 1);
                    string[] lines = Regex.Split(data, @"\r\n\r|\n");
                    for (int idx = lines.Length - 1; idx > 0; idx--)
                    {
                        if (string.IsNullOrEmpty(lines[idx])) continue;
                        if (lines[idx].Contains("Power Budget Available"))
                        {
                            string[] split = lines[idx].Split(new string[] { "Watts" }, StringSplitOptions.None);
                            if (string.IsNullOrEmpty(split[0])) continue;
                            slot.Budget = StringToDouble(split[0].Trim());
                            break;
                        }
                    }
                }
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
                _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed, $"\n    {Translate("i18n_cmdnosup")} {SwitchModel.Name}");
                return;
            }
            Logger.Error(ex);
            progressReport.Type = ReportType.Error;
            progressReport.Message += $"{Translate("i18n_nspb")}{WebUtility.UrlDecode($"\n{ex.Message}")}";
            PowerDevice(Command.POWER_UP_PORT);
        }

        private string ChangePerpetualOrFastPoe(Command cmd)
        {
            if (_wizardSwitchSlot == null) return string.Empty;
            bool enable = cmd == Command.POE_PERPETUAL_ENABLE || cmd == Command.POE_FAST_ENABLE;
            string i18n = (cmd == Command.POE_PERPETUAL_ENABLE || cmd == Command.POE_PERPETUAL_DISABLE) ? "i18n_ppoe" : "i18n_fpoe";
            string txt = $"{Translate(i18n)} {Translate("i18n_onslot")} {_wizardSwitchSlot.Name}";
            i18n = enable ? "i18n_noen" : "i18n_nodis";
            string error = $"{Translate(i18n)} {txt}";
            if (!_wizardSwitchSlot.IsInitialized) throw new SwitchCommandError($"{error} {Translate("i18n_pwdown")}");
            if (_wizardSwitchSlot.Is8023btSupport && enable) throw new SwitchCommandError($"{error} {Translate("i18n_isbt")}");
            bool ppoe = _wizardSwitchSlot.PPoE == ConfigType.Enable;
            bool fpoe = _wizardSwitchSlot.FPoE == ConfigType.Enable;
            i18n = enable ? "i18n_enable" : "i18n_disable";
            string wizardAction = $"{Translate(i18n)} {txt}";
            if (cmd == Command.POE_PERPETUAL_ENABLE && ppoe || cmd == Command.POE_FAST_ENABLE && fpoe ||
                cmd == Command.POE_PERPETUAL_DISABLE && !ppoe || cmd == Command.POE_FAST_DISABLE && !fpoe)
            {
                _progress.Report(new ProgressReport(wizardAction));
                i18n = enable ? "i18n_isen" : "i18n_isdis";
                txt = $"{txt} {Translate(i18n)}";
                Logger.Info(txt);
                return $"\n - {txt} ";
            }
            _progress.Report(new ProgressReport(wizardAction));
            string result = $"\n - {wizardAction} ";
            Logger.Info(wizardAction);
            SendCommand(new CmdRequest(cmd, _wizardSwitchSlot.Name));
            WaitSec(wizardAction, 3);
            GetSlotPowerStatus(_wizardSwitchSlot);
            if (cmd == Command.POE_PERPETUAL_ENABLE && _wizardSwitchSlot.PPoE == ConfigType.Enable ||
                cmd == Command.POE_FAST_ENABLE && _wizardSwitchSlot.FPoE == ConfigType.Enable ||
                cmd == Command.POE_PERPETUAL_DISABLE && _wizardSwitchSlot.PPoE == ConfigType.Disable ||
                cmd == Command.POE_FAST_DISABLE && _wizardSwitchSlot.FPoE == ConfigType.Disable)
            {
                result += Translate("i18n_exec");
            }
            else
            {
                result += Translate("i18n_notex");
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
                if (_wizardSwitchSlot.FPoE == ConfigType.Enable) SendCommand(new CmdRequest(Command.POE_FAST_DISABLE, _wizardSwitchSlot.Name));
            }
        }

        private void Change823BT(Command cmd)
        {
            StringBuilder txt = new StringBuilder();
            string i18n = cmd == Command.POWER_823BT_ENABLE ? "i18n_sbten" : "i18n_sbtdis";
            txt.Append(Translate(i18n)).Append(_wizardSwitchSlot.Name).Append($" {Translate("i18n_onsw")} ").Append(SwitchModel.Name);
            _progress.Report(new ProgressReport($"{txt}{WAITING}"));
            PowerSlotDown();
            WaitSlotPower(false);
            SendCommand(new CmdRequest(cmd, _wizardSwitchSlot.Name));
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
            string i18n = powerUp ? "i18n_poeon" : "i18n_poeoff";
            StringBuilder txt = new StringBuilder(Translate(i18n)).Append(" ").Append(Translate("i18n_onsl"));
            txt.Append(_wizardSwitchSlot.Name).Append($" {Translate("i18n_onsw")} ").Append(SwitchModel.Name);
            _progress.Report(new ProgressReport($"{txt}{WAITING}"));
            int dur = 0;
            while (dur < 50)
            {
                Thread.Sleep(1000);
                dur = (int)GetTimeDuration(startTime);
                _progress.Report(new ProgressReport($"{txt} ({dur} {Translate("i18n_sec")}){WAITING}"));
                if (dur % 5 == 0)
                {
                    GetSlotPowerStatus(_wizardSwitchSlot);
                    if (powerUp && _wizardSwitchSlot.IsInitialized || !powerUp && !_wizardSwitchSlot.IsInitialized) break;
                }
            }
        }

        private void GetSlotPowerStatus(SlotModel slot)
        {
            _dictList = SendCommand(new CmdRequest(Command.SHOW_SLOT_LAN_POWER_STATUS, ParseType.Htable2, slot.Name)) as List<Dictionary<string, string>>;
            if (_dictList?.Count > 0) slot.LoadFromDictionary(_dictList[0]);
        }

        private void PowerDevice(Command cmd)
        {
            try
            {
                SendCommand(new CmdRequest(cmd, _wizardSwitchPort.Name));
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
            string msg = $"{progrMsg} {Translate("i18n_onsw")} {(!string.IsNullOrEmpty(SwitchModel.Name) ? SwitchModel.Name : SwitchModel.IpAddress)}";
            _progress.Report(new ProgressReport(msg));
            Logger.Info(msg);
        }

        private void SendProgressError(string title, string error)
        {
            string errorMessage = $"{error} on switch {SwitchModel.Name}";
            _progress.Report(new ProgressReport(ReportType.Error, title, $"{errorMessage} {Translate("i18n_onsw")} {SwitchModel.Name}"));
            Logger.Error(errorMessage);
        }

        public void Close()
        {
            DisconnectAosSsh();
            RestApiClient?.Close();
            LogActivity("Switch disconnected");
            RestApiClient = null;
        }

        private void LogActivity(string action, string data = null)
        {
            string txt = $"Switch {SwitchModel.Name} ({SwitchModel.IpAddress}): {action}";
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
            StringBuilder txt = new StringBuilder("API Request sent by ").Append(PrintMethodClass(3)).Append(":\n").Append(entry.ToString());
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
                txt.Append("\nSwitch Response:\n").Append(new string('=', 132)).Append("\n").Append(PrintXMLDoc(response[RESULT]));
                txt.Append("\n").Append(new string('=', 132));
            }
            Logger.Debug(txt.ToString());
        }
    }

}

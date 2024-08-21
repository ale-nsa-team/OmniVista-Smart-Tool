using PoEWizard.Data;
using PoEWizard.Device;
using PoEWizard.Exceptions;
using System;
using System.Collections.Generic;
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
        private Dictionary<string, object> _response = new Dictionary<string, object>();
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
                this.IsReady = true;
                Logger.Info($"Connecting Rest API");
                StartProgressBar($"Connecting to switch {SwitchModel.IpAddress} ...", 23);
                _progress.Report(new ProgressReport($"Connecting to switch {SwitchModel.IpAddress} ..."));
                RestApiClient.Login();
                UpdateProgressBar(++progressBarCnt); //  1
                if (!RestApiClient.IsConnected()) throw new SwitchConnectionFailure($"Could not connect to switch {SwitchModel.IpAddress}!");
                SwitchModel.IsConnected = true;
                _progress.Report(new ProgressReport($"Reading system information on switch {SwitchModel.IpAddress}"));
                _response = SendRequest(GetRestUrlEntry(Command.SHOW_MICROCODE));
                UpdateProgressBar(++progressBarCnt); //  2
                if (_response[STRING] != null) SwitchModel.LoadFromDictionary(CliParseUtils.ParseHTable(_response[STRING].ToString())[0], DictionaryType.MicroCode);
                _response = SendRequest(GetRestUrlEntry(Command.SHOW_CMM));
                UpdateProgressBar(++progressBarCnt); //  3
                if (_response[STRING] != null) SwitchModel.LoadFromDictionary(CliParseUtils.ParseVTable(_response[STRING].ToString()), DictionaryType.Cmm);
                _response = SendRequest(GetRestUrlEntry(Command.DEBUG_SHOW_APP_LIST));
                if (_response[DATA] != null) SwitchModel.LoadFromList(CliParseUtils.ParseSwitchDebugAppTable((Dictionary<string, string>)_response[DATA], new string[2] { LPNI, LPCMM }), DictionaryType.SwitchDebugAppList);
                UpdateProgressBar(++progressBarCnt); //  4
                ScanSwitch($"Connect to switch {SwitchModel.IpAddress}", reportResult);
            }
            catch (Exception ex)
            {
                SendSwitchError("Connect", ex);
            }
            progressBarCnt = 0;
            totalProgressBar = 0;
        }

        public void ScanSwitch(string source, WizardReport reportResult = null)
        {
            try
            {
                if (totalProgressBar == 0) StartProgressBar($"Scanning switch {SwitchModel.IpAddress} ...", 18);
                GetCurrentSwitchDebugLevel();
                progressBarCnt += 2;
                UpdateProgressBar(progressBarCnt); //  5 , 6
                GetSnapshot();
                progressBarCnt += 2;
                UpdateProgressBar(progressBarCnt); //  7, 8
                this._wizardReportResult = reportResult;
                GetSystemInfo();
                UpdateProgressBar(++progressBarCnt); //  9
                SendProgressReport("Reading chassis and port information");
                _response = SendRequest(GetRestUrlEntry(Command.SHOW_CHASSIS));
                UpdateProgressBar(++progressBarCnt); // 10
                if (_response[STRING] != null) SwitchModel.LoadFromList(CliParseUtils.ParseMultipleTables(_response[STRING].ToString(), DictionaryType.Chassis), DictionaryType.Chassis);
                _response = SendRequest(GetRestUrlEntry(Command.SHOW_TEMPERATURE));
                UpdateProgressBar(++progressBarCnt); // 11
                if (_response[STRING] != null) SwitchModel.LoadFromList(CliParseUtils.ParseHTable(_response[STRING].ToString(), 1), DictionaryType.TemperatureList);
                _response = SendRequest(GetRestUrlEntry(Command.SHOW_HEALTH_CONFIG));
                UpdateProgressBar(++progressBarCnt); // 12
                if (_response[STRING] != null) SwitchModel.UpdateCpuThreshold(CliParseUtils.ParseETable(_response[STRING].ToString()));
                _response = SendRequest(GetRestUrlEntry(Command.SHOW_PORTS_LIST));
                UpdateProgressBar(++progressBarCnt); // 13
                if (_response[STRING] != null) SwitchModel.LoadFromList(CliParseUtils.ParseHTable(_response[STRING].ToString(), 3), DictionaryType.PortsList);
                SendProgressReport("Reading power supply information");
                _response = SendRequest(GetRestUrlEntry(Command.SHOW_POWER_SUPPLIES));
                UpdateProgressBar(++progressBarCnt); // 14
                if (_response[STRING] != null) SwitchModel.LoadFromList(CliParseUtils.ParseHTable(_response[STRING].ToString(), 2), DictionaryType.PowerSupply);
                _response = SendRequest(GetRestUrlEntry(Command.SHOW_HEALTH));
                UpdateProgressBar(++progressBarCnt); // 15
                if (_response[STRING] != null) SwitchModel.LoadFromList(CliParseUtils.ParseHTable(_response[STRING].ToString(), 2), DictionaryType.CpuTrafficList);
                GetLanPower();
                progressBarCnt += 3;
                UpdateProgressBar(progressBarCnt); // 16, 17, 18
                GetMacAndLldpInfo();
                progressBarCnt += 3;
                UpdateProgressBar(progressBarCnt); // 19, 20, 21
                string title = string.IsNullOrEmpty(source) ? $"Refresh switch {SwitchModel.IpAddress}" : source;
            }
            catch (Exception ex)
            {
                SendSwitchError(source, ex);
            }
            CloseProgressBar();
        }

        public void GetSystemInfo()
        {
            SendProgressReport("Reading system information");
            _response = SendRequest(GetRestUrlEntry(Command.SHOW_SYSTEM_RUNNING_DIR));
            if (_response[DATA] != null) SwitchModel.LoadFromDictionary((Dictionary<string, string>)_response[DATA], DictionaryType.SystemRunningDir);
        }

        public void GetSnapshot()
        {
            try
            {
                SendProgressReport("Reading configuration snapshot");
                _response = SendRequest(GetRestUrlEntry(Command.SHOW_CONFIGURATION));
                if (_response[STRING] != null) SwitchModel.ConfigSnapshot = _response[STRING].ToString();
                if (!SwitchModel.ConfigSnapshot.Contains(RestUrl.CMD_TBL[Command.LLDP_SYSTEM_DESCRIPTION_ENABLE]))
                {
                    SendProgressReport("Enabling LLDP description");
                    SendRequest(GetRestUrlEntry(Command.LLDP_SYSTEM_DESCRIPTION_ENABLE));
                    WriteMemory();
                }
            }
            catch (Exception ex)
            {
                SendSwitchError("Get Snapshot", ex);
            }
        }

        public object RunSwichCommand(CmdRequest cmdReq)
        {
            string mibReq = null;
            switch (cmdReq.Command)
            {
                case Command.SHOW_DNS_CONFIG:       // 207
                    mibReq = SWITCH_CFG_DNS;
                    break;

                case Command.SHOW_DHCP_CONFIG:      // 208
                case Command.SHOW_DHCP_RELAY:       // 209
                    mibReq = SWITCH_CFG_DHCP;
                    break;

                case Command.SHOW_NTP_CONFIG:       // 210
                    mibReq = SWITCH_CFG_NTP;
                    break;

            }
            Dictionary<string, object> resp = SendRequest(GetRestUrlEntry(cmdReq.Command));
            if (!string.IsNullOrEmpty(mibReq))
            {
                return CliParseUtils.ParseListFromDictionary((Dictionary<string, string>)resp[DATA], mibReq);
            }
            else if (resp.ContainsKey(STRING))
            {
                switch (cmdReq.ParseType)
                {

                    case ParseType.Htable:
                        return CliParseUtils.ParseHTable(resp[STRING].ToString(), 1);
                    case ParseType.Htable2:
                        return CliParseUtils.ParseHTable(resp[STRING].ToString(), 2);
                    case ParseType.Vtable:
                        return CliParseUtils.ParseVTable(resp[STRING].ToString());
                    case ParseType.MVTable:
                        return CliParseUtils.ParseMultipleTables(resp[STRING].ToString(), cmdReq.DictionaryType);
                    case ParseType.Etable:
                        return CliParseUtils.ParseETable(resp[STRING].ToString());
                    default:
                        return resp;
                }
            }
            else return null;
        }

        public void RunGetSwitchLog(string port, SwitchDebugModel debugLog)
        {
            try
            {
                _debugSwitchLog = debugLog;
                GetSwitchSlotPort(port);
                if (_wizardSwitchPort == null)
                {
                    SendProgressError("Get Switch Log", $"Couldn't get data for port {port}");
                    return;
                }
                progressStartTime = DateTime.Now;
                StartProgressBar($"Collecting logs on switch {SwitchModel.IpAddress} ...", MAX_GENERATE_LOG_DURATION);
                ConnectAosSsh();
                UpdateSwitchLogBar();
                int debugSelected = _debugSwitchLog.IntDebugLevelSelected;
                SendProgressReport($"Getting lan power information of slot {_wizardSwitchSlot.Name}");
                // Getting current lan power status
                _response = SendRequest(GetRestUrlEntry(Command.DEBUG_SHOW_LAN_POWER_STATUS, new string[1] { _wizardSwitchSlot.Name }));
                UpdateSwitchLogBar();
                if (_response[STRING] != null) _debugSwitchLog.LanPowerStatus = _response[STRING].ToString();
                // Getting current switch debug level
                GetCurrentSwitchDebugLevel();
                int prevLpNiDebug = SwitchModel.LpNiDebugLevel;
                int prevLpCmmDebug = SwitchModel.LpCmmDebugLevel;
                // Setting switch debug level
                SetAppDebugLevel($"Setting PoE debug log level to {Utils.IntToSwitchDebugLevel(debugSelected)}", Command.DEBUG_UPDATE_LPNI_LEVEL, debugSelected);
                SetAppDebugLevel($"Setting CMM debug log level to {Utils.IntToSwitchDebugLevel(debugSelected)}", Command.DEBUG_UPDATE_LPCMM_LEVEL, debugSelected);
                // Recycling power on switch port
                RestartDeviceOnPort($"Resetting port {_wizardSwitchPort.Name} to capture log", 5);
                UpdateSwitchLogBar();
                // Setting switch debug level back to the previous values
                SetAppDebugLevel($"Resetting PoE debug level back to {Utils.IntToSwitchDebugLevel(prevLpNiDebug)}", Command.DEBUG_UPDATE_LPNI_LEVEL, prevLpNiDebug);
                SetAppDebugLevel($"Resetting CMM debug level back to {Utils.IntToSwitchDebugLevel(prevLpCmmDebug)}", Command.DEBUG_UPDATE_LPCMM_LEVEL, prevLpCmmDebug);
                // Generating tar file
                SendProgressReport($"Generating tar file");
                Thread.Sleep(3000);
                SendRequest(GetRestUrlEntry(Command.DEBUG_CREATE_LOG));
                Logger.Activity($"Generated log file in {SwitchDebugLogLevel.Debug3} level on switch {SwitchModel.IpAddress}\nDuration: {Utils.CalcStringDuration(progressStartTime)}");
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

        private void ConnectAosSsh()
        {
            try
            {
                SshService = new AosSshService(SwitchModel);
                SshService.ConnectSshClient();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void DisconnectAosSsh()
        {
            try
            {
                SshService?.DisconnectSshClient();
            }
            catch (Exception ex)
            {
                SendSwitchError("Connect", ex);
            }
        }

        private void SetAppDebugLevel(string progressMsg, Command cmd, int dbgLevel)
        {
            Command showDbgCmd = cmd == Command.DEBUG_UPDATE_LPCMM_LEVEL ? Command.DEBUG_SHOW_LPCMM_LEVEL : Command.DEBUG_SHOW_LPNI_LEVEL;
            _progress.Report(new ProgressReport($"{progressMsg} ..."));
            DateTime startCmdTime = DateTime.Now;
            SendSshCliCommand(cmd, new string[1] { dbgLevel.ToString() });
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
                    _response = SendRequest(GetRestUrlEntry(Command.DEBUG_SHOW_LEVEL, new string[2] { SwitchModel.DebugApp[appName].Index, SwitchModel.DebugApp[appName].NbSubApp }));
                    List<Dictionary<string, string>> list = CliParseUtils.ParseListFromDictionary((Dictionary<string, string>)_response[DATA], DEBUG_SWITCH_LOG);
                    if (_response[DATA] != null) _debugSwitchLog.LoadFromDictionary(list);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Dictionary<string, string> response = SendSshCliCommand(cmd);
                if (response != null && response.ContainsKey(OUTPUT) && !string.IsNullOrEmpty(response[OUTPUT]))
                {
                    _debugSwitchLog.LoadFromDictionary(CliParseUtils.ParseCliSwitchDebugLevel(response[OUTPUT]));
                }
            }
            return cmd == Command.DEBUG_SHOW_LPCMM_LEVEL ? _debugSwitchLog.LpCmmLogLevel : _debugSwitchLog.LpNiLogLevel;
        }

        private Dictionary<string, string> SendSshCliCommand(Command cmd, string[] data = null)
        {
            try
            {
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

        public void WriteMemory(int waitSec = 40)
        {
            try
            {
                if (SwitchModel.SyncStatus == SyncStatusType.Synchronized) return;
                string msg = $"Writing memory on switch {SwitchModel.IpAddress}";
                StartProgressBar($"{msg} ...", 25);
                SendRequest(GetRestUrlEntry(Command.WRITE_MEMORY));
                progressStartTime = DateTime.Now;
                double dur = 0;
                while (dur < waitSec)
                {
                    Thread.Sleep(1000);
                    dur = Utils.GetTimeDuration(progressStartTime);
                    try
                    {
                        int period = (int)dur;
                        if (period > 15 && period % 5 == 0) GetSystemInfo();
                    }
                    catch { }
                    if (SwitchModel.SyncStatus != SyncStatusType.NotSynchronized || dur >= waitSec) break;
                    UpdateProgressBarMessage($"{msg} ({(int)dur} sec) ...", dur);
                }
                Logger.Activity($"Write memory on switch {SwitchModel.IpAddress} completed (Duration: {dur} sec)");
            }
            catch (Exception ex)
            {
                SendSwitchError("Write memory", ex);
            }
            CloseProgressBar();
        }

        public string RebootSwitch(int waitSec)
        {
            progressStartTime = DateTime.Now;
            try
            {
                string msg = $"Rebooting switch {SwitchModel.IpAddress}";
                Logger.Info(msg);
                StartProgressBar($"{msg} ...", 320);
                SendRequest(GetRestUrlEntry(Command.REBOOT_SWITCH));
                if (waitSec <= 0) return string.Empty;
                msg = $"Waiting switch {SwitchModel.IpAddress} reboot ";
                _progress.Report(new ProgressReport($"{msg}..."));
                double dur = 0;
                while (dur <= 60)
                {
                    Thread.Sleep(1000);
                    dur = Utils.GetTimeDuration(progressStartTime);
                    UpdateProgressBarMessage($"{msg}({Utils.CalcStringDuration(progressStartTime, true)}) ...", dur);
                }
                while (dur < waitSec)
                {
                    Thread.Sleep(1000);
                    dur = (int)Utils.GetTimeDuration(progressStartTime);
                    UpdateProgressBarMessage($"{msg}({Utils.CalcStringDuration(progressStartTime, true)}) ...", dur);
                    if (dur >= waitSec) break;
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
                Logger.Activity($"Switch {SwitchModel.IpAddress} rebooted after {Utils.CalcStringDuration(progressStartTime, true)}");
            }
            catch (Exception ex)
            {
                SendSwitchError($"Reboot switch {SwitchModel.IpAddress}", ex);
            }
            CloseProgressBar();
            return Utils.CalcStringDuration(progressStartTime, true);
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
                Logger.Info($"Getting traffic information, traffic analysis duration: {duration} sec");
                while (Utils.GetTimeDuration(startTime) <= duration)
                {
                    if (stopTrafficAnalysis != AbortType.Running) break;
                    Thread.Sleep(250);
                }
                if (stopTrafficAnalysis == AbortType.Close)
                {
                    Logger.Warn($"Traffic analysis on switch {SwitchModel.IpAddress} was canceled because the switch is disconnected!");
                    return null;
                }
                GetMacAndLldpInfo();
                GetPortsTrafficInformation();
                report = new TrafficReport(_switchTraffic, stopTrafficAnalysisReason, duration);
                if (stopTrafficAnalysis == AbortType.CanceledByUser)
                {
                    Logger.Warn($"Traffic analysis on switch {SwitchModel.IpAddress} was {stopTrafficAnalysisReason}, selected duration: {duration / 60} minutes!");
                }
                Logger.Activity(report.Summary);
            }
            catch (Exception ex)
            {
                SendSwitchError($"Traffic analysis on switch {SwitchModel.IpAddress}", ex);
                return null;
            }
            return report;
        }

        private void GetMacAndLldpInfo()
        {
            SendProgressReport("Reading lldp remote information");
            _response = SendRequest(GetRestUrlEntry(Command.SHOW_LLDP_REMOTE));
            if (_response[STRING] != null) SwitchModel.LoadLldpFromList(CliParseUtils.ParseLldpRemoteTable(_response[STRING].ToString()), DictionaryType.LldpRemoteList);
            _response = SendRequest(GetRestUrlEntry(Command.SHOW_LLDP_INVENTORY));
            if (_response[STRING] != null) SwitchModel.LoadLldpFromList(CliParseUtils.ParseLldpRemoteTable(_response[STRING].ToString()), DictionaryType.LldpInventoryList);
            SendProgressReport("Reading MAC address information");
            _response = SendRequest(GetRestUrlEntry(Command.SHOW_MAC_LEARNING));
            if (_response[STRING] != null) SwitchModel.LoadFromList(CliParseUtils.ParseHTable(_response[STRING].ToString(), 1), DictionaryType.MacAddressList);
        }

        private void GetPortsTrafficInformation()
        {
            try
            {
                this._response = SendRequest(GetRestUrlEntry(Command.SHOW_INTERFACES));
                if (_response[STRING] != null)
                {
                    List<Dictionary<string, string>> dictList = CliParseUtils.ParseTrafficTable(_response[STRING].ToString());
                    if (_switchTraffic == null)
                    {
                        _switchTraffic = new SwitchTrafficModel(SwitchModel, dictList);
                    }
                    else
                    {
                        _switchTraffic.UpdateTraffic(dictList);
                    }
                }
            }
            catch (Exception ex)
            {
                SendSwitchError($"Traffic analysis on switch {SwitchModel.IpAddress}", ex);
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
                //if (!string.IsNullOrEmpty(result)) Thread.Sleep(5000);
                progressReport.Message += $"\n - Duration: {Utils.PrintTimeDurationSec(startTime)}";
                _progress.Report(progressReport);
                Logger.Activity($"{action} on Slot {_wizardSwitchSlot.Name}\n{progressReport.Message}");
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
                if (_wizardSwitchSlot == null) return false;
                RefreshPoEData();
                UpdatePortData();
                DateTime startTime = DateTime.Now;
                if (_wizardSwitchPort == null) return false;
                if (_wizardSwitchPort.PriorityLevel == priority) return false;
                _wizardSwitchPort.PriorityLevel = priority;
                SendRequest(GetRestUrlEntry(Command.POWER_PRIORITY_PORT, new string[2] { port, _wizardSwitchPort.PriorityLevel.ToString() }));
                RefreshPortsInformation();
                progressReport.Message += $"\n - Priority on port {port} set to {priority}";
                progressReport.Message += $"\n - Duration: {Utils.PrintTimeDurationSec(startTime)}";
                _progress.Report(progressReport);
                Logger.Activity($"Changed priority to {priority} on port {port}\n{progressReport.Message}");
                return true;
            }
            catch (Exception ex)
            {
                SendSwitchError("Change power priority", ex);
            }
            return false;
        }

        public void RefreshSwitchPorts()
        {
            GetSystemInfo();
            GetLanPower();
            RefreshPortsInformation();
        }

        private void RefreshPortsInformation()
        {
            _progress.Report(new ProgressReport($"Refreshing ports information on switch {SwitchModel.IpAddress}"));
            _response = SendRequest(GetRestUrlEntry(Command.SHOW_PORTS_LIST));
            if (_response[STRING] != null) SwitchModel.LoadFromList(CliParseUtils.ParseHTable(_response[STRING].ToString(), 3), DictionaryType.PortsList);
        }

        public void RunPowerUpSlot(string slotNr)
        {
            _wizardProgressReport = new ProgressReport("PoE Wizard Report:");
            try
            {
                _wizardSwitchSlot = SwitchModel.GetSlot(slotNr);
                if (_wizardSwitchSlot == null)
                {
                    SendProgressError("Power UP slot", $"Couldn't get data for slot {slotNr}");
                    return;
                }
                if (!_wizardSwitchSlot.IsInitialized)
                {
                    PowerUpSlot();
                }
            }
            catch (Exception ex)
            {
                SendSwitchError("Power UP slot", ex);
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
                if (!_wizardSwitchSlot.IsInitialized) PowerUpSlot();
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
                _progress.Report(new ProgressReport("Running PoE Wizard..."));
                if (!_wizardSwitchSlot.IsInitialized) PowerUpSlot();
                _wizardReportResult = reportResult;
                if (_wizardSwitchPort.Poe == PoeStatus.NoPoe)
                {
                    CreateReportPortNoPoe();
                    return;
                }
                if (_wizardSwitchPort.Poe != PoeStatus.Fault && _wizardSwitchPort.Poe != PoeStatus.Deny)
                {
                    Thread.Sleep(5000);
                    GetSlotLanPower(_wizardSwitchSlot);
                    if (_wizardSwitchPort.Poe != PoeStatus.Fault && _wizardSwitchPort.Poe != PoeStatus.Deny)
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
            }
        }

        private void CheckCapacitorDetection(int waitSec)
        {
            try
            {
                _progress.Report(new ProgressReport($"Checking capacitor detection on port {_wizardSwitchPort.Name}"));
                GetSlotLanPower(_wizardSwitchSlot);
                Thread.Sleep(3000);
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
                SendRequest(GetRestUrlEntry(_wizardCommand, new string[1] { _wizardSwitchPort.Name }));
                Thread.Sleep(3000);
                RestartDeviceOnPort(wizardAction);
                CheckPortUp(waitSec, wizardAction);
                string txt = string.Empty;
                if (_wizardReportResult.GetReportResult(_wizardSwitchPort.Name) == WizardResult.Ok)
                    txt = $"{wizardAction} solved the problem";
                else
                {
                    txt = $"{wizardAction} didn't solve the problem\nDisabling capacitor detection on port {_wizardSwitchPort.Name} to restore the previous config";
                    _wizardCommand = Command.CAPACITOR_DETECTION_DISABLE;
                    SendRequest(GetRestUrlEntry(_wizardCommand, new string[1] { _wizardSwitchPort.Name }));
                    wizardAction = $"Disabling capacitor detection on port {_wizardSwitchPort.Name}";
                    RestartDeviceOnPort(wizardAction);
                    WaitPortUp(waitSec, wizardAction);
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
                SendRequest(GetRestUrlEntry(Command.SET_MAX_POWER_PORT, new string[2] { _wizardSwitchPort.Name, $"{maxDefaultPower * 1000}" }));
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
                SendRequest(GetRestUrlEntry(Command.SET_MAX_POWER_PORT, new string[2] { _wizardSwitchPort.Name, "0" }));
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            return !string.IsNullOrEmpty(error) ? Utils.StringToDouble(Utils.ExtractSubString(error, "to ", "mW").Trim()) / 1000 : _wizardSwitchPort.MaxPower;
        }

        private void RefreshPoEData()
        {
            _progress.Report(new ProgressReport($"Refreshing PoE information on switch {SwitchModel.IpAddress}"));
            GetSlotPowerStatus();
            GetSlotPower(_wizardSwitchSlot);
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
            if (fastPoe) SendRequest(GetRestUrlEntry(Command.POE_FAST_DISABLE, new string[1] { _wizardSwitchSlot.Name }));
            double prevMaxPower = _wizardSwitchPort.MaxPower;
            if (!_wizardSwitchPort.Is4Pair)
            {
                SendRequest(GetRestUrlEntry(Command.POWER_4PAIR_PORT, new string[1] { _wizardSwitchPort.Name }));
                Thread.Sleep(3000);
                ExecuteActionOnPort($"Re-enabling 2-Pair power on port {_wizardSwitchPort.Name}", waitSec, Command.POWER_2PAIR_PORT);
            }
            else
            {
                Command init4Pair = _wizardSwitchPort.Is4Pair ? Command.POWER_4PAIR_PORT : Command.POWER_2PAIR_PORT;
                _wizardCommand = _wizardSwitchPort.Is4Pair ? Command.POWER_2PAIR_PORT : Command.POWER_4PAIR_PORT;
                ExecuteActionOnPort($"Enabling {(_wizardSwitchPort.Is4Pair ? "2-Pair" : "4-Pair")} power on port {_wizardSwitchPort.Name}", waitSec, init4Pair);
            }
            if (prevMaxPower != _wizardSwitchPort.MaxPower) SetMaxPowerToDefault(prevMaxPower);
            if (fastPoe) SendRequest(GetRestUrlEntry(Command.POE_FAST_ENABLE, new string[1] { _wizardSwitchSlot.Name }));
            _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.PrintTimeDurationSec(startTime));
        }

        private void SendSwitchError(string title, Exception ex)
        {
            string error = ex.Message;
            if (ex is SwitchConnectionFailure || ex is SwitchConnectionDropped || ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure)
            {
                if (ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure)
                {
                    error = $"Switch {SwitchModel.IpAddress} login failed (username: {SwitchModel.Login})";
                    this.SwitchModel.Status = SwitchStatus.LoginFail;
                }
                else
                {
                    error = $"Switch {SwitchModel.IpAddress} unreachable";
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
                StringBuilder txt = new StringBuilder(wizardAction);
                //txt.Append("\n").Append(_wizardSwitchPort.EndPointDevice);
                _progress.Report(new ProgressReport(txt.ToString()));
                _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
                SendRequest(GetRestUrlEntry(_wizardCommand, new string[1] { _wizardSwitchPort.Name }));
                Thread.Sleep(3000);
                CheckPortUp(waitSec, txt.ToString());
                _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.PrintTimeDurationSec(startTime));
                if (_wizardReportResult.GetReportResult(_wizardSwitchPort.Name) == WizardResult.Ok) return;
                if (restoreCmd != _wizardCommand) SendRequest(GetRestUrlEntry(restoreCmd, new string[1] { _wizardSwitchPort.Name }));
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
                    txt.Append($"\n    Switch {SwitchModel.IpAddress} doesn't support 802.3.bt");
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
                SendRequest(GetRestUrlEntry(Command.POWER_UP_SLOT, new string[1] { _wizardSwitchSlot.Name }));
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
                SendRequest(GetRestUrlEntry(Command.POWER_PRIORITY_PORT, new string[2] { _wizardSwitchPort.Name, priority.ToString() }));
                CheckPortUp(waitSec, txt.ToString());
                _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.CalcStringDuration(startTime, true));
                if (_wizardReportResult.GetReportResult(_wizardSwitchPort.Name) == WizardResult.Ok) return;
                SendRequest(GetRestUrlEntry(Command.POWER_PRIORITY_PORT, new string[2] { _wizardSwitchPort.Name, prevPriority.ToString() }));
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
                _wizardReportResult.UpdateWizardReport(_wizardSwitchPort.Name, WizardResult.Proceed);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void RestartDeviceOnPort(string progressMessage, int waitTimeSec = 5)
        {
            string msg = !string.IsNullOrEmpty(progressMessage) ? progressMessage : "";
            SendRequest(GetRestUrlEntry(Command.POWER_DOWN_PORT, new string[1] { _wizardSwitchPort.Name }));
            _progress.Report(new ProgressReport($"{msg} ...\nTurning power OFF{PrintPortStatus()}"));
            Thread.Sleep(waitTimeSec * 1000);
            SendRequest(GetRestUrlEntry(Command.POWER_UP_PORT, new string[1] { _wizardSwitchPort.Name }));
            _progress.Report(new ProgressReport($"{msg} ...\nTurning power ON{PrintPortStatus()}"));
            Thread.Sleep(5000);
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
            else if (_wizardSwitchPort.Poe == PoeStatus.On || _wizardSwitchPort.Poe == PoeStatus.Off) return true;
            else if (_wizardSwitchPort.Poe == PoeStatus.Searching && _wizardCommand == Command.CAPACITOR_DETECTION_DISABLE) return true;
            return false;
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
            return $"\nPoE status: {_wizardSwitchPort.Poe}, port status: {_wizardSwitchPort.Status}, power: {_wizardSwitchPort.Power} Watts";
        }

        private void UpdatePortData()
        {
            if (_wizardSwitchPort == null) return;
            GetSlotPower(_wizardSwitchSlot);
            _response = SendRequest(GetRestUrlEntry(Command.SHOW_PORT_STATUS, new string[1] { _wizardSwitchPort.Name }));
            List<Dictionary<string, string>> dictList;
            if (_response[STRING] != null)
            {
                dictList = CliParseUtils.ParseHTable(_response[STRING].ToString(), 3);
                if (dictList?.Count > 0) _wizardSwitchPort.UpdatePortStatus(dictList[0]);
            }
            _response = SendRequest(GetRestUrlEntry(Command.SHOW_PORT_MAC_ADDRESS, new string[1] { _wizardSwitchPort.Name }));
            if (_response[STRING] != null) _wizardSwitchPort.UpdateMacList(CliParseUtils.ParseHTable(_response[STRING].ToString(), 1));
            _response = SendRequest(GetRestUrlEntry(Command.SHOW_PORT_LLDP_REMOTE, new string[] { _wizardSwitchPort.Name }));
            if (_response[STRING] != null)
            {
                Dictionary<string, List<Dictionary<string, string>>> lldpList = CliParseUtils.ParseLldpRemoteTable(_response[STRING].ToString());
                if (lldpList?.Count > 0) _wizardSwitchPort.LoadLldpRemoteTable(lldpList[_wizardSwitchPort.Name]);
            }
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
                    if (slot.PowerClassDetection == ConfigType.Disable)
                    {
                        SendRequest(GetRestUrlEntry(Command.POWER_CLASS_DETECTION_ENABLE, new string[1] { $"{slot.Name}" }));
                    }
                    GetSlotPower(slot);
                    if (!slot.IsInitialized)
                    {
                        slot.IsPoeModeEnable = false;
                        _wizardReportResult.CreateReportResult(slot.Name, WizardResult.Warning, $"\nSlot {slot.Name} is turned Off!");
                    }
                    chassis.PowerBudget += slot.Budget;
                    chassis.PowerConsumed += slot.Power;
                }
                chassis.PowerRemaining = chassis.PowerBudget - chassis.PowerConsumed;
                foreach (var ps in chassis.PowerSupplies)
                {
                    _response = SendRequest(GetRestUrlEntry(Command.SHOW_POWER_SUPPLY, new string[1] { ps.Id.ToString() }));
                    if (_response[STRING] != null) ps.LoadFromDictionary(CliParseUtils.ParseVTable(_response[STRING].ToString()));
                }
            }
            SwitchModel.SupportsPoE = (nbChassisPoE > 0);
            if (!SwitchModel.SupportsPoE) _wizardReportResult.CreateReportResult(SWITCH, WizardResult.Fail, $"Switch {SwitchModel.IpAddress} doesn't support PoE!");
        }

        private void GetLanPowerStatus(ChassisModel chassis)
        {
            try
            {
                _response = SendRequest(GetRestUrlEntry(Command.SHOW_CHASSIS_LAN_POWER_STATUS, new string[] { chassis.Number.ToString() }));
                if (_response[STRING] != null) chassis.LoadFromList(CliParseUtils.ParseHTable(_response[STRING].ToString(), 2));
                chassis.PowerBudget = 0;
                chassis.PowerConsumed = 0;
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("lanpower not supported")) chassis.SupportsPoE = false;
                Logger.Error(ex);
            }
        }

        private void GetSlotPower(SlotModel slot)
        {
            GetSlotLanPower(slot);
            _response = SendRequest(GetRestUrlEntry(Command.SHOW_LAN_POWER_CONFIG, new string[1] { $"{slot.Name}" }));
            if (_response[STRING] != null) slot.LoadFromList(CliParseUtils.ParseHTable(_response[STRING].ToString(), 2), DictionaryType.LanPowerCfg);
        }

        private void GetSlotLanPower(SlotModel slot)
        {
            _response = SendRequest(GetRestUrlEntry(Command.SHOW_LAN_POWER, new string[1] { $"{slot.Name}" }));
            if (_response[STRING] != null) slot.LoadFromList(CliParseUtils.ParseHTable(_response[STRING].ToString(), 1), DictionaryType.LanPower);
        }

        private void ParseException(ProgressReport progressReport, Exception ex)
        {
            if (ex.Message.ToLower().Contains("command not supported"))
            {
                _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed, $"\n    Command not supported by the switch {SwitchModel.IpAddress}");
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
            if (cmd == Command.POE_PERPETUAL_ENABLE && ppoe || cmd == Command.POE_FAST_ENABLE && fpoe ||
                cmd == Command.POE_PERPETUAL_DISABLE && !ppoe || cmd == Command.POE_FAST_DISABLE && !fpoe)
            {
                _progress.Report(new ProgressReport($"{(enable ? "Enabling" : "Disabling")} {txt}"));
                txt = $"{txt} is already {(enable ? "enabled" : "disabled")}";
                Logger.Info(txt);
                return $"\n - {txt} ";
            }
            txt = $"{(enable ? "Enabling" : "Disabling")} {txt}";
            _progress.Report(new ProgressReport(txt));
            string result = $"\n - {txt} ";
            Logger.Info(txt);
            CheckFPOEand823BT(cmd);
            SendRequest(GetRestUrlEntry(cmd, new string[1] { $"{_wizardSwitchSlot.Name}" }));
            Thread.Sleep(2000);
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
            if (!_wizardSwitchSlot.IsInitialized) PowerUpSlot();
            if (cmd == Command.POE_FAST_ENABLE)
            {
                if (_wizardSwitchSlot.Is8023btSupport && _wizardSwitchSlot.Ports?.FirstOrDefault(p => p.Protocol8023bt == ConfigType.Enable) != null)
                {
                    Change823BT(Command.POWER_823BT_DISABLE);
                }
            }
            else if (cmd == Command.POWER_823BT_ENABLE)
            {
                if (_wizardSwitchSlot.FPoE == ConfigType.Enable) SendRequest(GetRestUrlEntry(Command.POE_FAST_DISABLE, new string[1] { _wizardSwitchSlot.Name }));
            }
        }

        private void Change823BT(Command cmd)
        {
            StringBuilder txt = new StringBuilder();
            if (cmd == Command.POWER_823BT_ENABLE) txt.Append("Enabling");
            else if (cmd == Command.POWER_823BT_DISABLE) txt.Append("Disabling");
            txt.Append(" 802.3bt on slot ").Append(_wizardSwitchSlot.Name).Append(" of switch ").Append(SwitchModel.IpAddress);
            _progress.Report(new ProgressReport($"{txt} ..."));
            PowerDownSlot();
            WaitSlotPower(false);
            SendRequest(GetRestUrlEntry(cmd, new string[1] { _wizardSwitchSlot.Name }));
            PowerUpSlot();
        }

        private void PowerDownSlot()
        {
            try
            {
                SendRequest(GetRestUrlEntry(Command.POWER_DOWN_SLOT, new string[1] { _wizardSwitchSlot.Name }));
            }
            catch
            {
                WriteMemory();
            }
            SendRequest(GetRestUrlEntry(Command.POWER_DOWN_SLOT, new string[1] { _wizardSwitchSlot.Name }));
        }

        private void PowerUpSlot()
        {
            SendRequest(GetRestUrlEntry(Command.POWER_UP_SLOT, new string[1] { _wizardSwitchSlot.Name }));
            WaitSlotPower(true);
        }

        private void WaitSlotPower(bool powerUp)
        {
            DateTime startTime = DateTime.Now;
            StringBuilder txt = new StringBuilder("Powering ");
            if (powerUp) txt.Append("UP"); else txt.Append("DOWN");
            txt.Append(" slot ").Append(_wizardSwitchSlot.Name).Append(" on switch ").Append(SwitchModel.IpAddress);
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
            _response = SendRequest(GetRestUrlEntry(Command.SHOW_SLOT_LAN_POWER_STATUS, new string[] { _wizardSwitchSlot.Name }));
            if (_response[STRING] != null)
            {
                List<Dictionary<string, string>> dictList = CliParseUtils.ParseHTable(_response[STRING].ToString(), 2);
                _wizardSwitchSlot.LoadFromDictionary(dictList[0]);
            }
        }

        private void PowerDevice(Command cmd)
        {
            try
            {
                SendRequest(GetRestUrlEntry(cmd, new string[1] { _wizardSwitchSlot.Name }));
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
            string msg = $"{progrMsg} on switch {SwitchModel.IpAddress}";
            _progress.Report(new ProgressReport(msg));
            Logger.Info(msg);
        }

        private void SendProgressError(string title, string error)
        {
            string errorMessage = $"{error} on switch {SwitchModel.IpAddress}";
            _progress.Report(new ProgressReport(ReportType.Error, title, $"{errorMessage} on switch {SwitchModel.IpAddress}"));
            Logger.Error(errorMessage);
        }

        public void Close()
        {
            Logger.Info($"Closing Rest API");
        }

        private RestUrlEntry GetRestUrlEntry(Command url)
        {
            return GetRestUrlEntry(url, new string[1] { null });
        }

        private RestUrlEntry GetRestUrlEntry(Command url, string[] data)
        {
            Dictionary<string, string> body = GetContent(url, data);
            RestUrlEntry entry = new RestUrlEntry(url, data) { Method = body == null ? HttpMethod.Get : HttpMethod.Post, Content = body };
            return entry;
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
            Dictionary<string, string> result = CliParseUtils.ParseXmlToDictionary(respReq[RESULT]);
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

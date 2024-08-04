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
        private CommandType _wizardCommand = CommandType.SHOW_SYSTEM;
        private WizardReport _wizardReportResult;
        private SwitchDebugModel _debugSwitchLog;

        public bool IsReady { get; set; } = false;
        public int Timeout { get; set; }
        public ResultCallback Callback { get; set; }
        public SwitchModel SwitchModel { get; set; }
        public RestApiClient RestApiClient { get; set; }

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
                _progress.Report(new ProgressReport("Connecting to switch..."));
                RestApiClient.Login();
                if (!RestApiClient.IsConnected()) throw new SwitchConnectionFailure($"Could not connect to switch {SwitchModel.IpAddress}!");
                SwitchModel.IsConnected = true;
                _progress.Report(new ProgressReport($"Reading system information on switch {SwitchModel.IpAddress}"));
                _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_MICROCODE));
                if (_response[STRING] != null) SwitchModel.LoadFromDictionary(CliParseUtils.ParseHTable(_response[STRING].ToString())[0], DictionaryType.MicroCode);
                _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_CMM));
                if (_response[STRING] != null) SwitchModel.LoadFromDictionary(CliParseUtils.ParseVTable(_response[STRING].ToString()), DictionaryType.Cmm);
                ScanSwitch($"Connect to switch {SwitchModel.IpAddress}", reportResult);
            }
            catch (Exception ex)
            {
                SendSwitchError("Connect", ex);
            }
        }

        public void ScanSwitch(string source, WizardReport reportResult = null)
        {
            try
            {
                GetSnapshot();
                this._wizardReportResult = reportResult;
                GetSystemInfo();
                SendProgressReport("Reading chassis and port information");
                _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_CHASSIS));
                if (_response[STRING] != null) SwitchModel.LoadFromList(CliParseUtils.ParseChassisTable(_response[STRING].ToString()), DictionaryType.Chassis);
                _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_TEMPERATURE));
                if (_response[STRING] != null) SwitchModel.LoadFromList(CliParseUtils.ParseHTable(_response[STRING].ToString(), 1), DictionaryType.TemperatureList);
                _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_HEALTH_CONFIG));
                if (_response[STRING] != null) SwitchModel.UpdateCpuThreshold(CliParseUtils.ParseETable(_response[STRING].ToString()));
                _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_PORTS_LIST));
                if (_response[STRING] != null) SwitchModel.LoadFromList(CliParseUtils.ParseHTable(_response[STRING].ToString(), 3), DictionaryType.PortsList);
                SendProgressReport("Reading power supply information");
                _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_POWER_SUPPLIES));
                if (_response[STRING] != null) SwitchModel.LoadFromList(CliParseUtils.ParseHTable(_response[STRING].ToString(), 2), DictionaryType.PowerSupply);
                _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_HEALTH));
                if (_response[STRING] != null) SwitchModel.LoadFromList(CliParseUtils.ParseHTable(_response[STRING].ToString(), 2), DictionaryType.CpuTrafficList);
                GetLanPower();
                SendProgressReport("Reading lldp remote information");
                _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_LLDP_REMOTE));
                if (_response[STRING] != null) SwitchModel.LoadFromList(CliParseUtils.ParseLldpRemoteTable(_response[STRING].ToString()), DictionaryType.LldpRemoteList);
                _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_LLDP_INVENTORY));
                if (_response[STRING] != null) SwitchModel.LoadFromList(CliParseUtils.ParseLldpRemoteTable(_response[STRING].ToString()), DictionaryType.LldpInventoryList);
                SendProgressReport("Reading MAC address information");
                _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_MAC_LEARNING));
                if (_response[STRING] != null) SwitchModel.LoadFromList(CliParseUtils.ParseHTable(_response[STRING].ToString(), 1), DictionaryType.MacAddressList);
                string title = string.IsNullOrEmpty(source) ? $"Refresh switch {SwitchModel.IpAddress}" : source;
            }
            catch (Exception ex)
            {
                SendSwitchError(source, ex);
            }
        }

        private void GetSystemInfo()
        {
            SendProgressReport("Reading system information");
            _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_SYSTEM_RUNNING_DIR));
            if (_response[DATA] != null) SwitchModel.LoadFromDictionary((Dictionary<string, string>)_response[DATA], DictionaryType.SystemRunningDir);
        }

        public void GetSnapshot()
        {
            try
            {
                SendProgressReport("Reading configuration snapshot");
                _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_CONFIGURATION));
                if (_response[STRING] != null) SwitchModel.ConfigSnapshot = _response[STRING].ToString();
                if (!SwitchModel.ConfigSnapshot.Contains(RestUrl.CMD_TBL[CommandType.LLDP_SYSTEM_DESCRIPTION_ENABLE]))
                {
                    SendProgressReport("Enabling LLDP description");
                    SendRequest(GetRestUrlEntry(CommandType.LLDP_SYSTEM_DESCRIPTION_ENABLE));
                    WriteMemory();
                }
            }
            catch (Exception ex)
            {
                SendSwitchError("Get Snapshot", ex);
            }
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
                SendProgressReport($"Getting tar file");
                _response = SendRequest(GetRestUrlEntry(CommandType.DEBUG_SHOW_LAN_POWER_STATUS, new string[1] { _wizardSwitchSlot.Name }));
                if (_response[STRING] != null) _debugSwitchLog.LanPowerStatus = _response[STRING].ToString();
                _response = SendRequest(GetRestUrlEntry(CommandType.DEBUG_SHOW_LLDPNI_LEVEL));
                if (_response[DATA] != null) _debugSwitchLog.LoadLldpNiFromDictionary(_response[DATA] as Dictionary<string, string>);
                _response = SendRequest(GetRestUrlEntry(CommandType.DEBUG_SHOW_LPNI_LEVEL));
                if (_response[DATA] != null) _debugSwitchLog.LoadLpNiFromDictionary(CliParseUtils.ParseListFromDictionary((Dictionary<string, string>)_response[DATA], DEBUG_SWITCH_LOG));
                SendProgressReport($"Enabling debug level to {_debugSwitchLog.DebugLevelSelected}");
                SendRequest(GetRestUrlEntry(CommandType.DEBUG_UPDATE_LLDPNI_LEVEL, new string[1] { _debugSwitchLog.SwitchDebugLevelSelected }));
                SendRequest(GetRestUrlEntry(CommandType.DEBUG_UPDATE_LPNI_LEVEL, new string[1] { _debugSwitchLog.SwitchDebugLevelSelected }));
                RestartDeviceOnPort($"Resetting port {_wizardSwitchPort.Name} to capture log", 5);
                SendProgressReport($"Resetting debug level back to {_debugSwitchLog.LldpNiApp.DebugLevel}");
                SendRequest(GetRestUrlEntry(CommandType.DEBUG_UPDATE_LLDPNI_LEVEL, new string[1] { _debugSwitchLog.LldpNiApp.SwitchLogLevel }));
                SendRequest(GetRestUrlEntry(CommandType.DEBUG_UPDATE_LPNI_LEVEL, new string[1] { _debugSwitchLog.LpNiApp.SwitchLogLevel }));
                SendProgressReport($"Generating tar file");
                Thread.Sleep(3000);
                SendRequest(GetRestUrlEntry(CommandType.DEBUG_CREATE_LOG));
            }
            catch (Exception ex)
            {
                SendSwitchError("Get Switch Log", ex);
            }
        }

        public string RebootSwitch(int waitSec)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                Logger.Info($"Rebooting switch {SwitchModel.IpAddress}");
                _progress.Report(new ProgressReport($"Rebooting switch {SwitchModel.IpAddress}"));
                SendRequest(GetRestUrlEntry(CommandType.REBOOT_SWITCH));
                if (waitSec <= 0) return "";
                _progress.Report(new ProgressReport($"Waiting switch {SwitchModel.IpAddress} reboot..."));
                int dur = 0;
                while (dur <= 60)
                {
                    Thread.Sleep(1000);
                    dur = (int)Utils.GetTimeDuration(startTime);
                    _progress.Report(new ProgressReport($"Waiting switch {SwitchModel.IpAddress} reboot ({Utils.CalcStringDuration(startTime, true)}) ..."));
                }
                while (dur < waitSec)
                {
                    Thread.Sleep(1000);
                    dur = (int)Utils.GetTimeDuration(startTime);
                    if (dur >= waitSec) break;
                    _progress.Report(new ProgressReport($"Waiting switch {SwitchModel.IpAddress} reboot ({Utils.CalcStringDuration(startTime, true)}) ..."));
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
                Logger.Info($"Switch {SwitchModel.IpAddress} rebooted after {Utils.CalcStringDuration(startTime, true)}");
            }
            catch (Exception ex)
            {
                SendSwitchError($"Reboot switch {SwitchModel.IpAddress}", ex);
            }
            return Utils.CalcStringDuration(startTime, true);
        }

        public void WriteMemory(int waitSec = 40)
        {
            if (SwitchModel.SyncStatus != SyncStatusType.NotSynchronized) return;
            SendProgressReport("Writing memory");
            SendRequest(GetRestUrlEntry(CommandType.WRITE_MEMORY));
            DateTime startTime = DateTime.Now;
            int dur = 0;
            while (dur < waitSec)
            {
                Thread.Sleep(1000);
                dur = (int)Utils.GetTimeDuration(startTime);
                if (SwitchModel.SyncStatus != SyncStatusType.NotSynchronized || dur >= waitSec) break;
                _progress.Report(new ProgressReport($"Writing memory on switch {SwitchModel.IpAddress} ({dur} sec) ..."));
                try
                {
                    if (dur > 15 && dur % 5 == 0) GetSystemInfo();
                }
                catch { }
            }
            Logger.Info($"Write memory on switch {SwitchModel.IpAddress} completed (Duration: {dur} sec)");
        }

        public bool SetPerpetualOrFastPoe(SlotModel slot, CommandType cmd)
        {
            bool enable = cmd == CommandType.POE_PERPETUAL_ENABLE || cmd == CommandType.POE_FAST_ENABLE;
            string poeType = (cmd == CommandType.POE_PERPETUAL_ENABLE || cmd == CommandType.POE_PERPETUAL_DISABLE) ? "Perpetual" : "Fast";
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
                Logger.Info($"{action} on Slot {_wizardSwitchSlot.Name}\n{progressReport.Message}");
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
                SendRequest(GetRestUrlEntry(CommandType.POWER_PRIORITY_PORT, new string[2] { port, _wizardSwitchPort.PriorityLevel.ToString() }));
                RefreshPortsInformation();
                progressReport.Message += $"\n - Priority on port {port} set to {priority}";
                progressReport.Message += $"\n - Duration: {Utils.PrintTimeDurationSec(startTime)}";
                _progress.Report(progressReport);
                Logger.Info($"Changed priority to {priority} on port {port}\n{progressReport.Message}");
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
            _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_PORTS_LIST));
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

        public void RunWizardCommands(string port, WizardReport reportResult, List<CommandType> commands, int waitSec)
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

        public void RunPoeWizard(string port, WizardReport reportResult, List<CommandType> commands, int waitSec)
        {
            if (reportResult.GetReportResult(port) == WizardResult.NothingToDo || reportResult.GetReportResult(port) == WizardResult.Ok) return;
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

        private void ExecuteWizardCommands(List<CommandType> commands, int waitSec)
        {
            foreach (CommandType command in commands)
            {
                _wizardCommand = command;
                switch (_wizardCommand)
                {
                    case CommandType.POWER_823BT_ENABLE:
                        Enable823BT(waitSec);
                        break;

                    case CommandType.POWER_2PAIR_PORT:
                        TryEnable2PairPower(waitSec);
                        break;

                    case CommandType.POWER_HDMI_ENABLE:
                        if (_wizardSwitchPort.IsPowerOverHdmi)
                        {
                            _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed);
                            continue;
                        }
                        ExecuteActionOnPort($"Enabling power HDMI on port {_wizardSwitchPort.Name}", waitSec, CommandType.POWER_HDMI_DISABLE);
                        break;

                    case CommandType.LLDP_POWER_MDI_ENABLE:
                        if (_wizardSwitchPort.IsLldpMdi)
                        {
                            _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed);
                            continue;
                        }
                        ExecuteActionOnPort($"Enabling LLDP power via MDI on port {_wizardSwitchPort.Name}", waitSec, CommandType.LLDP_POWER_MDI_DISABLE);
                        break;

                    case CommandType.LLDP_EXT_POWER_MDI_ENABLE:
                        if (_wizardSwitchPort.IsLldpExtMdi)
                        {
                            _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed);
                            continue;
                        }
                        ExecuteActionOnPort($"Enabling LLDP extended power via MDI on port {_wizardSwitchPort.Name}", waitSec, CommandType.LLDP_EXT_POWER_MDI_DISABLE);
                        break;

                    case CommandType.CHECK_POWER_PRIORITY:
                        CheckPriority();
                        return;

                    case CommandType.CHECK_823BT:
                        Check823BT();
                        break;

                    case CommandType.POWER_PRIORITY_PORT:
                        TryChangePriority(waitSec);
                        break;

                    case CommandType.CAPACITOR_DETECTION_ENABLE:
                        EnableCapacitorDetection(waitSec);
                        break;

                    case CommandType.RESET_POWER_PORT:
                        ResetPortPower(waitSec);
                        break;

                    case CommandType.CHECK_MAX_POWER:
                        CheckMaxPower();
                        break;

                    case CommandType.CHANGE_MAX_POWER:
                        ChangePortMaxPower();
                        break;
                }
            }
        }

        private void EnableCapacitorDetection(int waitSec)
        {
            if (_wizardSwitchPort.IsCapacitorDetection)
            {
                _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed);
                return;
            }
            try
            {
                string wizardAction = $"Enabling capacitor detection on port {_wizardSwitchPort.Name}";
                StringBuilder txt = new StringBuilder(wizardAction);
                //txt.Append("\n").Append(_wizardSwitchPort.EndPointDevice);
                _progress.Report(new ProgressReport(txt.ToString()));
                _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, WizardResult.Starting, wizardAction);
                PortSubType portSubType = _wizardSwitchPort.EndPointDevice != null ? _wizardSwitchPort.EndPointDevice.PortSubType : PortSubType.Unknown;
                switch(portSubType)
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
                SendRequest(GetRestUrlEntry(_wizardCommand, new string[1] { _wizardSwitchPort.Name }));
                Thread.Sleep(3000);
                RestartDeviceOnPort(wizardAction);
                WaitPortUp(waitSec, wizardAction);
                if (_wizardReportResult.GetReportResult(_wizardSwitchPort.Name) == WizardResult.Ok)
                {
                    return;
                }
                SendRequest(GetRestUrlEntry(CommandType.CAPACITOR_DETECTION_DISABLE, new string[1] { _wizardSwitchPort.Name }));
                Logger.Info($"{wizardAction} didn't solve the problem\nDisabling capacitor detection on port {_wizardSwitchPort.Name} to restore the previous config");
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
                SendRequest(GetRestUrlEntry(CommandType.SET_MAX_POWER_PORT, new string[2] { _wizardSwitchPort.Name, $"{maxDefaultPower * 1000}" }));
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
                SendRequest(GetRestUrlEntry(CommandType.SET_MAX_POWER_PORT, new string[2] { _wizardSwitchPort.Name, "0" }));
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            return !string.IsNullOrEmpty(error) ? Utils.StringToDouble(Utils.ExtractSubString(error, "to ", "mW").Trim())/1000 : _wizardSwitchPort.MaxPower;
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
            if (fastPoe) SendRequest(GetRestUrlEntry(CommandType.POE_FAST_DISABLE, new string[1] { _wizardSwitchSlot.Name }));
            if (!_wizardSwitchPort.Is4Pair)
            {
                SendRequest(GetRestUrlEntry(CommandType.POWER_4PAIR_PORT, new string[1] { _wizardSwitchPort.Name }));
                Thread.Sleep(3000);
                ExecuteActionOnPort($"Re-enabling 2-Pair power on port {_wizardSwitchPort.Name}", waitSec, CommandType.POWER_2PAIR_PORT);
            }
            else
            {
                CommandType init4Pair = _wizardSwitchPort.Is4Pair ? CommandType.POWER_4PAIR_PORT : CommandType.POWER_2PAIR_PORT;
                _wizardCommand = _wizardSwitchPort.Is4Pair ? CommandType.POWER_2PAIR_PORT : CommandType.POWER_4PAIR_PORT;
                ExecuteActionOnPort($"Enabling {(_wizardSwitchPort.Is4Pair ? "2-Pair" : "4-Pair")} power on port {_wizardSwitchPort.Name}", waitSec, init4Pair);
            }
            if (fastPoe) SendRequest(GetRestUrlEntry(CommandType.POE_FAST_ENABLE, new string[1] { _wizardSwitchSlot.Name }));
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

        private void ExecuteActionOnPort(string wizardAction, int waitSec, CommandType restoreCmd)
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
                WaitPortUp(waitSec, txt.ToString());
                _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.PrintTimeDurationSec(startTime));
                if (_wizardReportResult.GetReportResult(_wizardSwitchPort.Name) == WizardResult.Ok)
                {
                    return;
                }
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
                CheckFPOEand823BT(CommandType.POWER_823BT_ENABLE);
                Change823BT(CommandType.POWER_823BT_ENABLE);
                WaitPortUp(waitSec, wizardAction);
                _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.PrintTimeDurationSec(startTime));
                if (_wizardReportResult.GetReportResult(_wizardSwitchPort.Name) == WizardResult.Ok)
                {
                    return;
                }
                Change823BT(CommandType.POWER_823BT_DISABLE);
                Logger.Info($"{wizardAction} didn't solve the problem\nDisabling 802.3.bt on port {_wizardSwitchPort.Name} to restore the previous config");
            }
            catch (Exception ex)
            {
                SendRequest(GetRestUrlEntry(CommandType.POWER_UP_SLOT, new string[1] { _wizardSwitchSlot.Name }));
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
                SendRequest(GetRestUrlEntry(CommandType.POWER_PRIORITY_PORT, new string[2] { _wizardSwitchPort.Name, priority.ToString() }));
                WaitPortUp(waitSec, txt.ToString());
                _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.CalcStringDuration(startTime, true));
                if (_wizardReportResult.GetReportResult(_wizardSwitchPort.Name) == WizardResult.Ok)
                {
                    return;
                }
                SendRequest(GetRestUrlEntry(CommandType.POWER_PRIORITY_PORT, new string[2] { _wizardSwitchPort.Name, prevPriority.ToString() }));
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
                WaitPortUp(waitSec, wizardAction);
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
            SendRequest(GetRestUrlEntry(CommandType.POWER_DOWN_PORT, new string[1] { _wizardSwitchPort.Name }));
            _progress.Report(new ProgressReport($"{msg} ...\nTurning power OFF{PrintPortStatus()}"));
            Thread.Sleep(waitTimeSec * 1000);
            SendRequest(GetRestUrlEntry(CommandType.POWER_UP_PORT, new string[1] { _wizardSwitchPort.Name }));
            _progress.Report(new ProgressReport($"{msg} ...\nTurning power ON{PrintPortStatus()}"));
            Thread.Sleep(5000);
        }

        private void WaitPortUp(int waitSec, string progressMessage)
        {
            string msg = !string.IsNullOrEmpty(progressMessage) ? progressMessage: "";
            msg += $"\nWaiting port {_wizardSwitchPort.Name} to come UP";
            _progress.Report(new ProgressReport($"{msg} ...{PrintPortStatus()}"));
            DateTime startTime = DateTime.Now;
            UpdatePortData();
            int dur = 0;
            while (dur < waitSec)
            {
                Thread.Sleep(1000);
                dur = (int)Utils.GetTimeDuration(startTime);
                _progress.Report(new ProgressReport($"{msg} ({Utils.CalcStringDuration(startTime, true)}) ...{PrintPortStatus()}"));
                if (_wizardSwitchPort.Status == PortStatus.Up && _wizardSwitchPort.Poe == PoeStatus.On) break;
                if (dur % 5 == 0) UpdatePortData();
            }
            UpdateProgressReport();
            StringBuilder text = new StringBuilder("Port ").Append(_wizardSwitchPort.Name).Append(" Status: ").Append(_wizardSwitchPort.Status).Append(", PoE Status: ");
            text.Append(_wizardSwitchPort.Poe).Append(", Power: ").Append(_wizardSwitchPort.Power).Append(" (Duration: ").Append(Utils.CalcStringDuration(startTime));
            text.Append(", MAC List: ").Append(String.Join(",", _wizardSwitchPort.MacList)).Append(")");
            Logger.Info(text.ToString());
        }

        private void UpdateProgressReport()
        {
            WizardResult result;
            switch (_wizardSwitchPort.Poe)
            {
                case PoeStatus.On:
                    if (_wizardSwitchPort.Status == PortStatus.Up) result = WizardResult.Ok; else result = WizardResult.Fail;
                    break;

                case PoeStatus.Conflict:
                case PoeStatus.Searching:
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
            _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_PORT_STATUS, new string[1] { _wizardSwitchPort.Name }));
            List<Dictionary<string, string>> dictList;
            if (_response[STRING] != null)
            {
                dictList = CliParseUtils.ParseHTable(_response[STRING].ToString(), 3);
                if (dictList?.Count > 0) _wizardSwitchPort.UpdatePortStatus(dictList[0]);
            }
            _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_PORT_MAC_ADDRESS, new string[1] { _wizardSwitchPort.Name }));
            if (_response[STRING] != null) _wizardSwitchPort.UpdateMacList(CliParseUtils.ParseHTable(_response[STRING].ToString(), 1));
            _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_PORT_LLDP_REMOTE, new string[] { _wizardSwitchPort.Name }));
            if (_response[STRING] != null)
            {
                dictList = CliParseUtils.ParseLldpRemoteTable(_response[STRING].ToString());
                if (dictList?.Count > 0) _wizardSwitchPort.LoadLldpRemoteTable(dictList[0]);
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
                        SendRequest(GetRestUrlEntry(CommandType.POWER_CLASS_DETECTION_ENABLE, new string[1] { $"{slot.Name}" }));
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
                    _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_POWER_SUPPLY, new string[1] { ps.Id.ToString() }));
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
                _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_CHASSIS_LAN_POWER_STATUS, new string[] { chassis.Number.ToString() }));
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
            _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_LAN_POWER_CONFIG, new string[1] { $"{slot.Name}" }));
            if (_response[STRING] != null) slot.LoadFromList(CliParseUtils.ParseHTable(_response[STRING].ToString(), 2), DictionaryType.LanPowerCfg);
        }

        private void GetSlotLanPower(SlotModel slot)
        {
            _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_LAN_POWER, new string[1] { $"{slot.Name}" }));
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
            PowerDevice(CommandType.POWER_UP_PORT);
        }

        private string ChangePerpetualOrFastPoe(CommandType cmd)
        {
            if (_wizardSwitchSlot == null) return "";
            bool enable = cmd == CommandType.POE_PERPETUAL_ENABLE || cmd == CommandType.POE_FAST_ENABLE;
            string poeType = (cmd == CommandType.POE_PERPETUAL_ENABLE || cmd == CommandType.POE_PERPETUAL_DISABLE) ? "Perpetual" : "Fast";
            string txt = $"{poeType} PoE on Slot {_wizardSwitchSlot.Name}";
            bool ppoe = _wizardSwitchSlot.PPoE == ConfigType.Enable;
            bool fpoe = _wizardSwitchSlot.FPoE == ConfigType.Enable;
            if (cmd == CommandType.POE_PERPETUAL_ENABLE && ppoe || cmd == CommandType.POE_FAST_ENABLE && fpoe ||
                cmd == CommandType.POE_PERPETUAL_DISABLE && !ppoe || cmd == CommandType.POE_FAST_DISABLE && !fpoe)
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
            if (cmd == CommandType.POE_PERPETUAL_ENABLE && _wizardSwitchSlot.PPoE == ConfigType.Enable ||
                cmd == CommandType.POE_FAST_ENABLE && _wizardSwitchSlot.FPoE == ConfigType.Enable ||
                cmd == CommandType.POE_PERPETUAL_DISABLE && _wizardSwitchSlot.PPoE == ConfigType.Disable ||
                cmd == CommandType.POE_FAST_DISABLE && _wizardSwitchSlot.FPoE == ConfigType.Disable)
            {
                result += "executed";
            }
            else
            {
                result += "failed to execute";
            }
            return result;
        }

        private void CheckFPOEand823BT(CommandType cmd)
        {
            if (!_wizardSwitchSlot.IsInitialized) PowerUpSlot();
            if (cmd == CommandType.POE_FAST_ENABLE)
            {
                if (_wizardSwitchSlot.Is8023btSupport && _wizardSwitchSlot.Ports?.FirstOrDefault(p => p.Protocol8023bt == ConfigType.Enable) != null)
                {
                    Change823BT(CommandType.POWER_823BT_DISABLE);
                }
            }
            else if (cmd == CommandType.POWER_823BT_ENABLE)
            {
                if (_wizardSwitchSlot.FPoE == ConfigType.Enable) SendRequest(GetRestUrlEntry(CommandType.POE_FAST_DISABLE, new string[1] { _wizardSwitchSlot.Name }));
            }
        }

        private void Change823BT(CommandType cmd)
        {
            StringBuilder txt = new StringBuilder();
            if (cmd == CommandType.POWER_823BT_ENABLE) txt.Append("Enabling");
            else if (cmd == CommandType.POWER_823BT_DISABLE) txt.Append("Disabling");
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
                SendRequest(GetRestUrlEntry(CommandType.POWER_DOWN_SLOT, new string[1] { _wizardSwitchSlot.Name }));
            }
            catch
            {
                WriteMemory();
            }
            SendRequest(GetRestUrlEntry(CommandType.POWER_DOWN_SLOT, new string[1] { _wizardSwitchSlot.Name }));
        }

        private void PowerUpSlot()
        {
            SendRequest(GetRestUrlEntry(CommandType.POWER_UP_SLOT, new string[1] { _wizardSwitchSlot.Name }));
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
            _response = SendRequest(GetRestUrlEntry(CommandType.SHOW_SLOT_LAN_POWER_STATUS, new string[] { _wizardSwitchSlot.Name }));
            if (_response[STRING] != null)
            {
                List<Dictionary<string, string>> dictList = CliParseUtils.ParseHTable(_response[STRING].ToString(), 2);
                _wizardSwitchSlot.LoadFromDictionary(dictList[0]);
            }
        }

        private void PowerDevice(CommandType cmd)
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

        private RestUrlEntry GetRestUrlEntry(CommandType url)
        {
            return GetRestUrlEntry(url, new string[1] { null });
        }

        private RestUrlEntry GetRestUrlEntry(CommandType url, string[] data)
        {
            Dictionary<string, string> body = GetContent(url, data);
            RestUrlEntry entry = new RestUrlEntry(url, data) { Method = body == null ? HttpMethod.Get : HttpMethod.Post, Content = body };
            return entry;
        }

        private Dictionary<string, object> SendRequest(RestUrlEntry entry)
        {
            Dictionary<string, object> response = new Dictionary<string, object>
            {
                [STRING] = null, [DATA] = null
            };
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

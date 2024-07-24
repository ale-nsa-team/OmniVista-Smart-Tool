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
        private Dictionary<string, string> _response = new Dictionary<string, string>();
        private readonly IProgress<ProgressReport> _progress;
        private PortModel _wizardSwitchPort;
        private SlotModel _wizardSwitchSlot;
        private ProgressReport _wizardProgressReport;
        private CommandType _wizardCommand = CommandType.SHOW_SYSTEM;
        private WizardReport _wizardReportResult;

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

        public void Connect()
        {
            try
            {
                this.IsReady = true;
                Logger.Debug($"Connecting Rest API");
                _progress.Report(new ProgressReport("Connecting to switch..."));
                RestApiClient.Login();
                if (!RestApiClient.IsConnected()) throw new SwitchConnectionFailure($"Could not connect to Switch {SwitchModel.IpAddress}!");
                SwitchModel.IsConnected = true;
                _progress.Report(new ProgressReport($"Reading System information on Switch {SwitchModel.IpAddress}"));
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_SYSTEM));
                Dictionary<string, string> dict = CliParseUtils.ParseVTable(_response[RESULT]);
                SwitchModel.LoadFromDictionary(dict, DictionaryType.System);
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_MICROCODE));
                List<Dictionary<string, string>> diclist = CliParseUtils.ParseHTable(_response[RESULT]);
                SwitchModel.LoadFromDictionary(diclist[0], DictionaryType.MicroCode);
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_CMM));
                dict = CliParseUtils.ParseVTable(_response[RESULT]);
                SwitchModel.LoadFromDictionary(dict, DictionaryType.Cmm);
                SendRequest(GetRestUrlEntry(CommandType.LLDP_SYSTEM_DESCRIPTION_ENABLE));
                ScanSwitch();
            }
            catch (Exception ex)
            {
                SendSwitchConnectionFailed(ex);
            }
        }

        public void ScanSwitch()
        {
            try
            {
                _progress.Report(new ProgressReport($"Scanning switch {SwitchModel.IpAddress}"));
                List<Dictionary<string, string>> diclist;
                GetRunningDir();
                _progress.Report(new ProgressReport($"Reading chassis and port information on Switch {SwitchModel.IpAddress}"));
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_CHASSIS));
                diclist = CliParseUtils.ParseChassisTable(_response[RESULT]);
                SwitchModel.LoadFromList(diclist, DictionaryType.Chassis);
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_TEMPERATURE));
                diclist = CliParseUtils.ParseHTable(_response[RESULT], 1);
                SwitchModel.LoadFromList(diclist, DictionaryType.TemperatureList);
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_HEALTH_CONFIG));
                SwitchModel.UpdateCpuThreshold(CliParseUtils.ParseETable(_response[RESULT]));
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_PORTS_LIST));
                diclist = CliParseUtils.ParseHTable(_response[RESULT], 3);
                SwitchModel.LoadFromList(diclist, DictionaryType.PortsList);
                _progress.Report(new ProgressReport($"Reading power supply information on Switch {SwitchModel.IpAddress}"));
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_POWER_SUPPLIES));
                diclist = CliParseUtils.ParseHTable(_response[RESULT], 2);
                SwitchModel.LoadFromList(diclist, DictionaryType.PowerSupply);
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_HEALTH));
                diclist = CliParseUtils.ParseHTable(_response[RESULT], 2);
                SwitchModel.LoadFromList(diclist, DictionaryType.CpuTrafficList);
                GetLanPower();
                GetSnapshot();
                _progress.Report(new ProgressReport($"Reading lldp remote information on Switch {SwitchModel.IpAddress}"));
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_LLDP_REMOTE));
                diclist = CliParseUtils.ParseLldpRemoteTable(_response[RESULT]);
                SwitchModel.LoadFromList(diclist, DictionaryType.LldpRemoteList);
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_LLDP_INVENTORY));
                diclist = CliParseUtils.ParseLldpRemoteTable(_response[RESULT]);
                SwitchModel.LoadFromList(diclist, DictionaryType.LldpInventoryList);
                _progress.Report(new ProgressReport($"Reading MAC Address information on Switch {SwitchModel.IpAddress}"));
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_MAC_LEARNING));
                diclist = CliParseUtils.ParseHTable(_response[RESULT], 1);
                SwitchModel.LoadFromList(diclist, DictionaryType.MacAddressList);
            }
            catch (Exception ex)
            {
                SendSwitchConnectionFailed(ex);
            }
        }

        private void GetRunningDir()
        {
            this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_RUNNING_DIR));
            SwitchModel.LoadFromDictionary(CliParseUtils.ParseVTable(_response[RESULT]), DictionaryType.RunningDir);
        }

        public void GetSnapshot()
        {
            try
            {
                string txt = $"Reading configuration snapshot on Switch {SwitchModel.IpAddress}";
                _progress.Report(new ProgressReport(txt));
                Logger.Debug(txt);
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_CONFIGURATION));
                SwitchModel.ConfigSnapshot = _response[RESULT];
            }
            catch (Exception ex)
            {
                SendSwitchConnectionFailed(ex);
            }
        }

        public string RebootSwitch(int waitSec)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                _progress.Report(new ProgressReport($"Rebooting Switch {SwitchModel.IpAddress}"));
                SendRequest(GetRestUrlEntry(CommandType.REBOOT_SWITCH));
                if (waitSec <= 0) return "";
                _progress.Report(new ProgressReport($"Waiting Switch {SwitchModel.IpAddress} reboot..."));
                int dur = 0;
                while (dur <= 60)
                {
                    Thread.Sleep(1000);
                    dur = (int)Utils.GetTimeDuration(startTime);
                    _progress.Report(new ProgressReport($"Waiting Switch {SwitchModel.IpAddress} reboot ({Utils.CalcStringDuration(startTime, true)}) ..."));
                }
                while (dur < waitSec)
                {
                    Thread.Sleep(1000);
                    dur = (int)Utils.GetTimeDuration(startTime);
                    if (dur >= waitSec) break;
                    _progress.Report(new ProgressReport($"Waiting Switch {SwitchModel.IpAddress} reboot ({Utils.CalcStringDuration(startTime, true)}) ..."));
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
                Logger.Info($"Rebooting Switch {SwitchModel.IpAddress} (Duration: {Utils.CalcStringDuration(startTime, true)})");
            }
            catch (Exception ex)
            {
                SendSwitchConnectionFailed(ex);
            }
            return Utils.CalcStringDuration(startTime, true);
        }

        public void WriteMemory(int waitSec = 25)
        {
            try
            {
                if (SwitchModel.ConfigChanged)
                {
                    SwitchModel.ConfigChanged = false;
                    WriteFlashSynchro(waitSec);
                }
            }
            catch (Exception ex)
            {
                SendSwitchConnectionFailed(ex);
            }
        }

        private void WriteFlashSynchro(int waitSec = 25)
        {
            _progress.Report(new ProgressReport($"Writing memory on Switch {SwitchModel.IpAddress}"));
            SendRequest(GetRestUrlEntry(CommandType.WRITE_MEMORY));
            DateTime startTime = DateTime.Now;
            int dur = 0;
            while (dur < waitSec)
            {
                Thread.Sleep(1000);
                dur = (int)Utils.GetTimeDuration(startTime);
                if (dur >= waitSec) break;
                _progress.Report(new ProgressReport($"Writing memory on Switch {SwitchModel.IpAddress} ({dur} sec) ..."));
                try
                {
                    if (dur % 5 == 0)
                    {
                        bool done = false;
                        GetRunningDir();
                        done = SwitchModel.SyncStatus == "Synchronized";
                    }
                }
                catch { }
            }
            Logger.Info($"Writing memory on Switch {SwitchModel.IpAddress} (Duration: {dur} sec)");
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
                Logger.Info($"{result}\n{progressReport.Message}");
                SwitchModel.ConfigChanged = true;
                Logger.Info($"{action} on Slot {_wizardSwitchSlot.Name}\n{progressReport.Message}");
                return true;
            }
            catch (Exception ex)
            {
                SendSwitchConnectionFailed(ex);
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
                SwitchModel.ConfigChanged = true;
                Logger.Info($"Changed priority to {priority} on port {port}\n{progressReport.Message}");
                return true;
            }
            catch (Exception ex)
            {
                SendSwitchConnectionFailed(ex);
            }
            return false;
        }

        public void RefreshSwitchPorts()
        {
            GetLanPower();
            RefreshPortsInformation();
        }

        private void RefreshPortsInformation()
        {
            _progress.Report(new ProgressReport($"Refreshing Ports Information on Switch {SwitchModel.IpAddress}"));
            this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_PORTS_LIST));
            List<Dictionary<string, string>> dictList = CliParseUtils.ParseHTable(_response[RESULT], 3);
            SwitchModel.LoadFromList(dictList, DictionaryType.PortsList);
        }

        public void RunWizardCommands(string port, WizardReport reportResult, List<CommandType> commands, int waitSec)
        {
            if (string.IsNullOrEmpty(port) || reportResult.Result == WizardResult.NothingToDo || reportResult.Result == WizardResult.Ok) return;
            _wizardProgressReport = new ProgressReport("PoE Wizard Report:");
            try
            {
                GetSwitchSlotPort(port);
                if (_wizardSwitchPort == null || _wizardSwitchSlot == null)
                {
                    reportResult.CreateReportResult(_wizardSwitchPort.Name, $"Port {port} not found!");
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
                SendSwitchConnectionFailed(ex);
            }
        }

        public void RunPoeWizard(string port, WizardReport reportResult, List<CommandType> commands, int waitSec)
        {
            if (string.IsNullOrEmpty(port) || reportResult.Result == WizardResult.NothingToDo || reportResult.Result == WizardResult.Ok) return;
            _wizardProgressReport = new ProgressReport("PoE Wizard Report:");
            try
            {
                GetSwitchSlotPort(port);
                if (_wizardSwitchPort == null || _wizardSwitchSlot == null)
                {
                    reportResult.CreateReportResult(_wizardSwitchPort.Name, $"Port {port} not found!");
                    return;
                }
                if (!_wizardSwitchSlot.IsInitialized) PowerUpSlot();
                _wizardReportResult = reportResult;
                if (_wizardSwitchPort.Poe == PoeStatus.NoPoe)
                {
                    CreateReportPortNoPoe();
                    return;
                }
                if (_wizardSwitchPort.Poe != PoeStatus.Fault && _wizardSwitchPort.Poe != PoeStatus.Deny)
                {
                    StringBuilder txt = new StringBuilder(PrintPortStatus());
                    if (_wizardSwitchPort.EndPointDevice != null && !string.IsNullOrEmpty(_wizardSwitchPort.EndPointDevice.MacAddress))
                    {
                        txt.Append(", Device MAC: ").Append(_wizardSwitchPort.EndPointDevice.MacAddress);
                        if (!string.IsNullOrEmpty(_wizardSwitchPort.EndPointDevice.IpAddress)) txt.Append(", IP: ").Append(_wizardSwitchPort.EndPointDevice.IpAddress);
                    }
                    else if (_wizardSwitchPort.MacList?.Count > 0 && !string.IsNullOrEmpty(_wizardSwitchPort.MacList[0]))
                    {
                        txt.Append(", Device MAC: ").Append(_wizardSwitchPort.MacList[0]);
                    }
                    string wizardAction = $"Nothing to do on port {_wizardSwitchPort.Name}.{txt}";
                    _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, wizardAction);
                    _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.NothingToDo);
                    Logger.Debug($"{wizardAction}\n{_wizardProgressReport.Message}");
                }
                else
                {
                    ExecuteWizardCommands(commands, waitSec);
                }
            }
            catch (Exception ex)
            {
                SendSwitchConnectionFailed(ex);
            }
        }

        private void CreateReportPortNoPoe()
        {
            string wizardAction = $"Nothing to do\n    Port {_wizardSwitchPort.Name} doesn't have PoE";
            _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, wizardAction);
            _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.NothingToDo);
        }

        private void ExecuteWizardCommands(List<CommandType> commands, int waitSec)
        {
            foreach (CommandType command in commands)
            {
                _wizardCommand = command;
                switch (_wizardCommand)
                {
                    case CommandType.POWER_2PAIR_PORT:
                        TryEnable2PairPower(waitSec);
                        break;

                    case CommandType.POWER_HDMI_ENABLE:
                        if (_wizardSwitchPort.IsPowerOverHdmi  || _wizardSwitchPort.Protocol8023bt == ConfigType.Unavailable) continue;
                        ExecuteActionOnPort($"Enabling Power HDMI on Port {_wizardSwitchPort.Name}", waitSec, CommandType.POWER_HDMI_DISABLE);
                        break;

                    case CommandType.LLDP_POWER_MDI_ENABLE:
                        if (_wizardSwitchPort.IsLldpMdi) continue;
                        ExecuteActionOnPort($"Enabling LLDP Power via MDI on Port {_wizardSwitchPort.Name}", waitSec, CommandType.LLDP_POWER_MDI_DISABLE);
                        break;

                    case CommandType.LLDP_EXT_POWER_MDI_ENABLE:
                        if (_wizardSwitchPort.IsLldpExtMdi) continue;
                        ExecuteActionOnPort($"Enabling LLDP Extended Power via MDI on Port {_wizardSwitchPort.Name}", waitSec, CommandType.LLDP_EXT_POWER_MDI_DISABLE);
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

                    case CommandType.POWER_823BT_ENABLE:
                        Enable823BT(waitSec);
                        break;

                    case CommandType.RESET_POWER_PORT:
                        ResetPortPower(_wizardSwitchPort.Name, waitSec);
                        break;

                    case CommandType.CHECK_MAX_POWER:
                        CheckMaxPower();
                        break;

                    case CommandType.CHANGE_MAX_POWER:
                        ChangePortMaxPower();
                        break;
                }
                switch (_wizardReportResult.Result)
                {
                    case WizardResult.Ok:
                        SwitchModel.ConfigChanged = true;
                        return;
                    case WizardResult.Warning:
                    case WizardResult.NothingToDo:
                        return;
                    default:
                        break;
                }
            }
            Logger.Info($"PoE Wizard completed on port {_wizardSwitchPort.Name}, Waiting Time: {waitSec} sec\n{_wizardProgressReport.Message}");
        }

        private void CheckMaxPower()
        {
            string wizardAction = $"Checking Max. Power on port {_wizardSwitchPort.Name}";
            _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, wizardAction);
            _progress.Report(new ProgressReport(wizardAction));
            double maxDefaultPower = GetMaxDefaultPower();
            if (_wizardSwitchPort.MaxPower < maxDefaultPower)
            {
                _wizardReportResult.SetReturnParameter(_wizardSwitchPort.Name, maxDefaultPower);
                string alert = $"Max. Power on port {_wizardSwitchPort.Name} is {_wizardSwitchPort.MaxPower} Watts, it should be {maxDefaultPower} Watts";
                _wizardReportResult.UpdateAlert(_wizardSwitchPort.Name, WizardResult.Warning, alert);
            }
            else
            {
                _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Proceed);
            }
            Logger.Debug($"{wizardAction}\n{_wizardProgressReport.Message}");
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
            string error = null;
            string wizardAction = $"Restoring Max. Power on port {_wizardSwitchPort.Name} from {_wizardSwitchPort.MaxPower} Watts to default {maxDefaultPower} Watts";
            _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, wizardAction);
            double prevMaxPower = _wizardSwitchPort.MaxPower;
            string powerSet = $"default {maxDefaultPower} Watts";
            double maxPowerAllowed = SetMaxPowerToDefault(maxDefaultPower);
            if (maxPowerAllowed > 0)
            {
                double maxAllowed = SetMaxPowerToDefault(maxPowerAllowed);
                if (maxAllowed > 0)
                {
                    error = $"\n    Couldn't restore Max. Power on port {_wizardSwitchPort.Name} to {maxPowerAllowed} Watts (Max. Allowed: {maxAllowed} Watts)";
                }
                else
                {
                    powerSet = $"{maxPowerAllowed} Watts{(!_wizardSwitchPort.Is4Pair ? " (2-pair enable)" : "")}";
                }
            }
            if (!string.IsNullOrEmpty(error))
            {
                wizardAction += $"{error}";
            }
            else if (prevMaxPower != _wizardSwitchPort.MaxPower)
            {
                SwitchModel.ConfigChanged = true;
                wizardAction += $"\n    Restoring Max. Power on port {_wizardSwitchPort.Name} from {_wizardSwitchPort.MaxPower} Watts to {powerSet}";
            }
            else
            {
                wizardAction += $"\n    Max. Power on port {_wizardSwitchPort.Name} is already the Max. Power allowed {powerSet}";
            }
            _wizardReportResult.UpdateWizardReport(_wizardSwitchPort.Name, WizardResult.Proceed, wizardAction);
            Logger.Info($"{wizardAction}\n{_wizardProgressReport.Message}");
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
                string error;
                string[] split = ex.Message.Split('\n');
                if (split.Length < 2)
                {
                    error = ex.Message;
                    Logger.Warn(error);
                    return 0;
                }
                else
                {
                    error = split[2];
                    return Utils.StringToDouble(Utils.ExtractSubString(error, "power not exceed ", " when").Trim()) / 1000;
                }
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
                string[] split = ex.Message.Split('\n');
                if (split.Length < 2) throw ex;
                error = split[2];
            }
            return !string.IsNullOrEmpty(error) ? Utils.StringToDouble(Utils.ExtractSubString(error, "to ", "mW").Trim())/1000 : _wizardSwitchPort.MaxPower;
        }

        private void RefreshPoEData()
        {
            _progress.Report(new ProgressReport($"Refreshing PoE information on Switch {SwitchModel.IpAddress}"));
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
                ExecuteActionOnPort($"Re-enabling 2-Pair Power on Port {_wizardSwitchPort.Name}", waitSec, CommandType.POWER_2PAIR_PORT);
            }
            else
            {
                CommandType init4Pair = _wizardSwitchPort.Is4Pair ? CommandType.POWER_4PAIR_PORT : CommandType.POWER_2PAIR_PORT;
                _wizardCommand = _wizardSwitchPort.Is4Pair ? CommandType.POWER_2PAIR_PORT : CommandType.POWER_4PAIR_PORT;
                ExecuteActionOnPort($"Enabling {(_wizardSwitchPort.Is4Pair ? "2-Pair" : "4-Pair")} Power on Port {_wizardSwitchPort.Name}", waitSec, init4Pair);
            }
            if (fastPoe) SendRequest(GetRestUrlEntry(CommandType.POE_FAST_ENABLE, new string[1] { _wizardSwitchSlot.Name }));
            _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.PrintTimeDurationSec(startTime));
        }

        private void SendSwitchConnectionFailed(Exception ex)
        {
            string error;
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
            else
            {
                Logger.Error(ex);
                error = $"Switch {SwitchModel.IpAddress} connection error:\n - {WebUtility.UrlDecode(ex.Message)}";
            }
            _progress?.Report(new ProgressReport(ReportType.Error, "Connect", error));
        }

        private void ExecuteActionOnPort(string wizardAction, int waitSec, CommandType restoreCmd)
        {
            try
            {
                StringBuilder txt = new StringBuilder(wizardAction);
                //txt.Append("\n").Append(_wizardSwitchPort.EndPointDevice);
                _progress.Report(new ProgressReport(txt.ToString()));
                _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, wizardAction);
                SendRequest(GetRestUrlEntry(_wizardCommand, new string[1] { _wizardSwitchPort.Name }));
                Thread.Sleep(3000);
                WaitPortUp(waitSec, txt.ToString());
                if (_wizardReportResult.Result == WizardResult.Ok)
                {
                    SwitchModel.ConfigChanged = true;
                    return;
                }
                if (restoreCmd != _wizardCommand) SendRequest(GetRestUrlEntry(restoreCmd, new string[1] { _wizardSwitchPort.Name }));
            }
            catch (Exception ex)
            {
                ParseException(_wizardSwitchPort.Name, _wizardProgressReport, ex);
            }
        }

        private void Check823BT()
        {
            string wizardAction = $"Checking 802.3.bt on Port {_wizardSwitchPort.Name}";
            _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, wizardAction);
            DateTime startTime = DateTime.Now;
            StringBuilder txt = new StringBuilder();
            switch (_wizardSwitchPort.Protocol8023bt)
            {
                case ConfigType.Disable:
                    string alert = _wizardSwitchSlot.FPoE == ConfigType.Enable ? $"Fast PoE is enabled on Slot {_wizardSwitchSlot.Name}" : null;
                    _wizardReportResult.UpdateAlert(_wizardSwitchPort.Name, WizardResult.Warning, alert);
                    break;
                case ConfigType.Unavailable:
                    txt.Append($"\n    Switch {SwitchModel.IpAddress} doesn't support 802.3.bt");
                    _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Skip, txt.ToString());
                    break;
                case ConfigType.Enable:
                    txt.Append($"\n    802.3.bt already enabled on Port {_wizardSwitchPort.Name}");
                    _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, WizardResult.Skip, txt.ToString());
                    break;
            }
            _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.PrintTimeDurationSec(startTime));
            Logger.Debug(wizardAction + txt.ToString());
        }

        private void Enable823BT(int waitSec)
        {
            try
            {
                string wizardAction = $"Enabling 802.3.bt on Slot {_wizardSwitchSlot.Name}";
                _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, wizardAction);
                DateTime startTime = DateTime.Now;
                StringBuilder txt = new StringBuilder(wizardAction);
                //txt.Append("\n").Append(_wizardSwitchPort.EndPointDevice);
                _progress.Report(new ProgressReport(txt.ToString()));
                CheckFPOEand823BT(CommandType.POWER_823BT_ENABLE);
                Change823BT(CommandType.POWER_823BT_ENABLE);
                WaitPortUp(waitSec, txt.ToString());
                _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.PrintTimeDurationSec(startTime));
            }
            catch (Exception ex)
            {
                SendRequest(GetRestUrlEntry(CommandType.POWER_UP_SLOT, new string[1] { _wizardSwitchSlot.Name }));
                ParseException(_wizardSwitchPort.Name, _wizardProgressReport, ex);
            }
        }

        private void CheckPriority()
        {
            string wizardAction = $"Checking power priority on Port {_wizardSwitchPort.Name}";
            _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, wizardAction);
            DateTime startTime = DateTime.Now;
            double powerRemaining = _wizardSwitchSlot.Budget - _wizardSwitchSlot.Power;
            double maxPower = _wizardSwitchPort.MaxPower;
            StringBuilder txt = new StringBuilder();
            WizardResult changePriority;
            string remainingPower = $"Remaining Power = {powerRemaining} Watts, Max. Power = {maxPower} Watts";
            string text;
            if (_wizardSwitchPort.PriorityLevel < PriorityLevelType.High && powerRemaining < maxPower)
            {
                changePriority = WizardResult.Warning;
                string alert = $"Changing power priority on Port {_wizardSwitchPort.Name} may solve the problem";
                text = $"\n    {remainingPower}";
                _wizardReportResult.UpdateAlert(_wizardSwitchPort.Name, WizardResult.Warning, alert);
            }
            else
            {
                changePriority = WizardResult.Skip;
                text = $"\n    No need to change power priority on Port {_wizardSwitchPort.Name} (";
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
            Logger.Debug(txt.ToString());
        }

        private void TryChangePriority(int waitSec)
        {
            try
            {
                PriorityLevelType priority = PriorityLevelType.High;
                string wizardAction = $"Changing power priority to {priority} on Port {_wizardSwitchPort.Name}";
                _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, wizardAction);
                PriorityLevelType prevPriority = _wizardSwitchPort.PriorityLevel;
                DateTime startTime = DateTime.Now;
                StringBuilder txt = new StringBuilder(wizardAction);
                _progress.Report(new ProgressReport(txt.ToString()));
                SendRequest(GetRestUrlEntry(CommandType.POWER_PRIORITY_PORT, new string[2] { _wizardSwitchPort.Name, priority.ToString() }));
                RestartDeviceOnPort(_wizardSwitchPort.Name, waitSec, txt.ToString());
                string actionResult;
                if (_wizardReportResult.Result == WizardResult.Fail)
                {
                    SendRequest(GetRestUrlEntry(CommandType.POWER_PRIORITY_PORT, new string[2] { _wizardSwitchPort.Name, prevPriority.ToString() }));
                    actionResult = "didn't solve the problem";
                }
                else
                {
                    actionResult = "solved the problem";
                }
                _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, _wizardReportResult.Result, actionResult);
                _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.CalcStringDuration(startTime, true));
            }
            catch (Exception ex)
            {
                ParseException(_wizardSwitchPort.Name, _wizardProgressReport, ex);
            }
        }

        private void ResetPortPower(string port, int waitSec)
        {
            try
            {
                string wizardAction = $"Resetting Power on Port {port}";
                _wizardReportResult.CreateReportResult(port, wizardAction);
                DateTime startTime = DateTime.Now;
                _progress.Report(new ProgressReport(wizardAction));
                RestartDeviceOnPort(port, waitSec + 15, wizardAction);
                _wizardReportResult.UpdateDuration(port, Utils.PrintTimeDurationSec(startTime));
                _wizardReportResult.UpdateWizardReport(_wizardSwitchPort.Name, WizardResult.Proceed, $"Resetting Power on Port {port} completed");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void RestartDeviceOnPort(string port, int waitSec, string progressMessage)
        {
            string msg = !string.IsNullOrEmpty(progressMessage) ? progressMessage : "";
            SendRequest(GetRestUrlEntry(CommandType.POWER_DOWN_PORT, new string[1] { port }));
            _progress.Report(new ProgressReport($"{msg} ...\nTurning power OFF{PrintPortStatus()}"));
            Thread.Sleep(5000);
            SendRequest(GetRestUrlEntry(CommandType.POWER_UP_PORT, new string[1] { port }));
            _progress.Report(new ProgressReport($"{msg} ...\nTurning power ON{PrintPortStatus()}"));
            Thread.Sleep(5000);
            WaitPortUp(waitSec, progressMessage);
        }

        private void WaitPortUp(int waitSec, string progressMessage)
        {
            string msg = !string.IsNullOrEmpty(progressMessage) ? progressMessage: "";
            msg += $"\nWaiting Port {_wizardSwitchPort.Name} to come UP";
            _progress.Report(new ProgressReport($"{msg} ..."));
            DateTime startTime = DateTime.Now;
            UpdatePortData();
            while (Utils.GetTimeDuration(startTime) <= waitSec)
            {
                _progress.Report(new ProgressReport($"{msg} ({Utils.CalcStringDuration(startTime, true)}) ...{PrintPortStatus()}"));
                if (_wizardSwitchPort != null && _wizardSwitchPort.Status == PortStatus.Up && _wizardSwitchPort.Power >= 0.5) break;
                Thread.Sleep(5000);
                UpdatePortData();
            }
            UpdateProgressReport();
            StringBuilder text = new StringBuilder("Port ").Append(_wizardSwitchPort.Name).Append(" Status: ").Append(_wizardSwitchPort.Status).Append(", PoE Status: ");
            text.Append(_wizardSwitchPort.Poe).Append(", Power: ").Append(_wizardSwitchPort.Power).Append(" (Duration: ").Append(Utils.CalcStringDuration(startTime));
            text.Append(", MAC List: ").Append(String.Join(",", _wizardSwitchPort.MacList)).Append(")");
            Logger.Debug(text.ToString());
        }

        private string PrintPortStatus()
        {
            return $"\nPoE status: {_wizardSwitchPort.Poe}, Port Status: {_wizardSwitchPort.Status}, Power: {_wizardSwitchPort.Power} Watts";
        }

        private void UpdateProgressReport()
        {
            string resultDescription;
            WizardResult result;
            if (_wizardSwitchPort != null && _wizardSwitchPort.Status == PortStatus.Up)
            {
                result = WizardResult.Ok;
                resultDescription = "solved the problem";
            }
            else
            {
                result = WizardResult.Fail;
                resultDescription = "didn't solve the problem";
            }
            _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, result, resultDescription);
            StringBuilder portStatus = new StringBuilder(PrintPortStatus());
            portStatus.Append(_wizardSwitchPort.Status);
            if (_wizardSwitchPort.MacList?.Count > 0) portStatus.Append(", Device MAC Address: ").Append(_wizardSwitchPort.MacList[0]);
            _wizardReportResult.UpdatePortStatus(_wizardSwitchPort.Name, portStatus.ToString());
        }

        private void UpdatePortData()
        {
            if (_wizardSwitchPort == null) return;
            GetSlotPower(_wizardSwitchSlot);
            this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_PORT_STATUS, new string[1] { _wizardSwitchPort.Name }));
            List<Dictionary<string, string>> dictList = CliParseUtils.ParseHTable(_response[RESULT], 3);
            if (dictList?.Count > 0) _wizardSwitchPort.UpdatePortStatus(dictList[0]);
            this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_PORT_MAC_ADDRESS, new string[1] { _wizardSwitchPort.Name }));
            dictList = CliParseUtils.ParseHTable(_response[RESULT], 1);
            if (dictList?.Count > 0) _wizardSwitchPort.UpdateMacList(dictList);
            this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_PORT_LLDP_REMOTE, new string[] { _wizardSwitchPort.Name }));
            dictList = CliParseUtils.ParseLldpRemoteTable(_response[RESULT]);
            if (dictList?.Count > 0) _wizardSwitchPort.LoadLldpRemoteTable(dictList[0]);
        }

        private void GetLanPower()
        {
            List<Dictionary<string, string>> diclist;
            Dictionary<string, string> dict;
            _progress.Report(new ProgressReport($"Reading PoE information on Switch {SwitchModel.IpAddress}"));
            foreach (var chassis in SwitchModel.ChassisList)
            {
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_CHASSIS_LAN_POWER_STATUS, new string[] { chassis.Number.ToString() }));
                diclist = CliParseUtils.ParseHTable(_response[RESULT], 2);
                chassis.LoadFromList(diclist);
                chassis.PowerBudget = 0;
                chassis.PowerConsumed = 0;
                foreach (var slot in chassis.Slots)
                {
                    if (slot.Ports.Count == 0) continue;
                    if (slot.PowerClassDetection == ConfigType.Disable)
                    {
                        SwitchModel.ConfigChanged = true;
                        SendRequest(GetRestUrlEntry(CommandType.POWER_CLASS_DETECTION_ENABLE, new string[1] { $"{slot.Name}" }));
                    }
                    GetSlotPower(slot);
                    chassis.PowerBudget += slot.Budget;
                    chassis.PowerConsumed += slot.Power;
                }
                chassis.PowerRemaining = chassis.PowerBudget - chassis.PowerConsumed;
                foreach (var ps in chassis.PowerSupplies)
                {
                    this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_POWER_SUPPLY, new string[1] { ps.Id.ToString() }));
                    dict = CliParseUtils.ParseVTable(_response[RESULT]);
                    ps.LoadFromDictionary(dict);
                }
            }
        }

        private void GetSlotPower(SlotModel slot)
        {
            this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_LAN_POWER, new string[1] { $"{slot.Name}" }));
            List<Dictionary<string, string>> diclist = CliParseUtils.ParseHTable(_response[RESULT], 1);
            slot.LoadFromList(diclist, DictionaryType.LanPower);
            this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_LAN_POWER_CONFIG, new string[1] { $"{slot.Name}" }));
            diclist = CliParseUtils.ParseHTable(_response[RESULT], 2);
            slot.LoadFromList(diclist, DictionaryType.LanPowerCfg);
        }

        private void ParseException(string port, ProgressReport progressReport, Exception ex)
        {
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
                Logger.Debug(txt);
                return $"\n - {txt} ";
            }
            txt = $"{(enable ? "Enabling" : "Disabling")} {txt}";
            _progress.Report(new ProgressReport(txt));
            string result = $"\n - {txt} ";
            Logger.Debug(txt);
            CheckFPOEand823BT(cmd);
            SendRequest(GetRestUrlEntry(cmd, new string[1] { $"{_wizardSwitchSlot.Name}" }));
            Thread.Sleep(2000);
            GetSlotPowerStatus();
            if (cmd == CommandType.POE_PERPETUAL_ENABLE && _wizardSwitchSlot.PPoE == ConfigType.Enable ||
                cmd == CommandType.POE_FAST_ENABLE && _wizardSwitchSlot.FPoE == ConfigType.Enable ||
                cmd == CommandType.POE_PERPETUAL_DISABLE && _wizardSwitchSlot.PPoE == ConfigType.Disable ||
                cmd == CommandType.POE_FAST_DISABLE && _wizardSwitchSlot.FPoE == ConfigType.Disable)
            {
                SwitchModel.ConfigChanged = true;
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
            txt.Append(" 802.3bt on Slot ").Append(_wizardSwitchSlot.Name).Append(" of Switch ").Append(SwitchModel.IpAddress);
            _progress.Report(new ProgressReport($"{txt} ..."));
            try
            {
                SendRequest(GetRestUrlEntry(CommandType.POWER_DOWN_SLOT, new string[1] { _wizardSwitchSlot.Name }));
            }
            catch
            {
                WriteFlashSynchro();
            }
            _progress.Report(new ProgressReport($"{txt} ..."));
            SendRequest(GetRestUrlEntry(CommandType.POWER_DOWN_SLOT, new string[1] { _wizardSwitchSlot.Name }));
            WaitSlotPower(false);
            SendRequest(GetRestUrlEntry(cmd, new string[1] { _wizardSwitchSlot.Name }));
            PowerUpSlot();
        }

        private void PowerUpSlot()
        {
            SendRequest(GetRestUrlEntry(CommandType.POWER_UP_SLOT, new string[1] { _wizardSwitchSlot.Name }));
            WaitSlotPower(true);
        }

        private void WaitSlotPower(bool waitInit)
        {
            DateTime startTime = DateTime.Now;
            StringBuilder txt = new StringBuilder("Powering ");
            if (waitInit) txt.Append("UP"); else txt.Append("DOWN");
            txt.Append(" Slot ").Append(_wizardSwitchSlot.Name).Append(" on Switch ").Append(SwitchModel.IpAddress);
            _progress.Report(new ProgressReport($"{txt} ..."));
            int dur = 0;
            while (dur < 30)
            {
                Thread.Sleep(1000);
                dur = (int)Utils.GetTimeDuration(startTime);
                if (dur >= 30) break;
                _progress.Report(new ProgressReport($"{txt} ({dur} sec) ..."));
                if (dur % 5 == 0)
                {
                    GetSlotPowerStatus();
                    if (waitInit && _wizardSwitchSlot.IsInitialized || !waitInit && !_wizardSwitchSlot.IsInitialized) break;
                }
            }
        }

        private void GetSlotPowerStatus()
        {
            this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_SLOT_LAN_POWER_STATUS, new string[] { _wizardSwitchSlot.Name }));
            List<Dictionary<string, string>> dictList = CliParseUtils.ParseHTable(_response[RESULT], 2);
            _wizardSwitchSlot.LoadFromDictionary(dictList[0]);
        }

        private void PowerDevice(CommandType cmd)
        {
            try
            {
                this._response = SendRequest(GetRestUrlEntry(cmd, new string[1] { _wizardSwitchSlot.Name }));
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

        public void Close()
        {
            Logger.Debug($"Closing Rest API");
        }

        private RestUrlEntry GetRestUrlEntry(CommandType url)
        {
            return GetRestUrlEntry(url, new string[1] { null });
        }

        private RestUrlEntry GetRestUrlEntry(CommandType url, string[] data)
        {
            RestUrlEntry entry = new RestUrlEntry(url, data) { Method = HttpMethod.Get };
            return entry;
        }

        private Dictionary<string, string> SendRequest(RestUrlEntry entry)
        {
            Dictionary<string, string> response = this.RestApiClient.SendRequest(entry);
            if (response == null) return null;
            if (response.ContainsKey(ERROR) && !string.IsNullOrEmpty(response[ERROR]))
            {
                throw new SwitchCommandError(response[ERROR]);
            }
            LogSendRequest(entry, response);
            Dictionary<string, string> result = CliParseUtils.ParseXmlToDictionary(response[RESULT], "//nodes//result//*");
            if (result != null && result.ContainsKey(OUTPUT) && !string.IsNullOrEmpty(result[OUTPUT]))
            {
                response[RESULT] = result[OUTPUT];
            }
            return response;
        }

        private void LogSendRequest(RestUrlEntry entry, Dictionary<string, string> response)
        {
            StringBuilder txt = new StringBuilder("API Request sent").Append(Utils.PrintMethodClass(3)).Append(" with ").Append(entry.ToString());
            Logger.Info(txt.ToString());
            txt = new StringBuilder("Request API URL: ").Append(response[REST_URL]);
            if (entry.Response.ContainsKey(RESULT))
            {
                txt.Append("\nSwitch Response:\n").Append(new string('=', 132)).Append("\n").Append(Utils.PrintXMLDoc(response[RESULT]));
                txt.Append("\n").Append(new string('=', 132));
            }
            Logger.Debug(txt.ToString());
        }
    }

}

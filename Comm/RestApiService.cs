using PoEWizard.Data;
using PoEWizard.Device;
using PoEWizard.Exceptions;
using System;
using System.Collections.Generic;
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
        private DateTime _wizardStartTime;
        private PortModel _wizardSwitchPort;
        private SlotModel _wizardSwitchSlot;
        private ProgressReport _wizardProgressReport;
        private CommandType _wizardCommand = CommandType.SHOW_SYSTEM;
        private ProgressReportResult _wizardReportResult;

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
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_PORTS_LIST));
                diclist = CliParseUtils.ParseHTable(_response[RESULT], 3);
                SwitchModel.LoadFromList(diclist, DictionaryType.PortsList);
                _progress.Report(new ProgressReport($"Reading power supply information on Switch {SwitchModel.IpAddress}"));
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_POWER_SUPPLIES));
                diclist = CliParseUtils.ParseHTable(_response[RESULT], 2);
                SwitchModel.LoadFromList(diclist, DictionaryType.PowerSupply);
                GetLanPower();
                GetSnapshot();
                _progress.Report(new ProgressReport($"Reading lldp remote information on Switch {SwitchModel.IpAddress}"));
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_LLDP_REMOTE));
                diclist = CliParseUtils.ParseLldpRemoteTable(_response[RESULT]);
                SwitchModel.LoadFromList(diclist, DictionaryType.LldpRemoteList);
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
            }
            catch (Exception ex)
            {
                SendSwitchConnectionFailed(ex);
            }
        }

        public bool SetPerpetualOrFastPoe(SlotModel slot, CommandType cmd)
        {
            bool enable = cmd == CommandType.POE_PERPETUAL_ENABLE || cmd == CommandType.POE_FAST_ENABLE;
            string poeType = (cmd == CommandType.POE_PERPETUAL_ENABLE || cmd == CommandType.POE_PERPETUAL_DISABLE) ? "Perpetual" : "Fast";
            ProgressReport progressReport = new ProgressReport($"{(enable ? "Enable" : "Disable")} {poeType} PoE Report:")
            {
                Type = ReportType.Info
            };
            try
            {
                _wizardSwitchSlot = slot;
                if (_wizardSwitchSlot == null) return false;
                DateTime startTime = DateTime.Now;
                GetLanPower();
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_PORTS_LIST));
                List<Dictionary<string, string>> dictList = CliParseUtils.ParseHTable(_response[RESULT], 3);
                SwitchModel.LoadFromList(dictList, DictionaryType.PortsList);
                string result = ChangePerpetualOrFastPoe(cmd);
                progressReport.Message += result;
                //if (!string.IsNullOrEmpty(result)) Thread.Sleep(5000);
                progressReport.Message += $"\n - Duration: {Utils.PrintTimeDurationSec(startTime)}";
                _progress.Report(progressReport);
                Logger.Info($"{result}\n{progressReport.Message}");
                SwitchModel.ConfigChanged = true;
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
                if (_wizardSwitchPort == null || _wizardSwitchSlot == null) return false;
                DateTime startTime = DateTime.Now;
                GetLanPower();
                UpdatePortData();
                if (_wizardSwitchPort == null) return false;
                if (_wizardSwitchPort.PriorityLevel == priority) return false;
                SendRequest(GetRestUrlEntry(CommandType.POWER_PRIORITY_PORT, new string[2] { port, priority.ToString() }));
                progressReport.Message += $"\n - Priority on port {port} set to {priority}";
                progressReport.Message += $"\n - Duration: {Utils.PrintTimeDurationSec(startTime)}";
                UpdatePortData();
                _progress.Report(progressReport);
                Logger.Info($"Changed priority to {priority} on port {port}\n{progressReport.Message}");
                SwitchModel.ConfigChanged = true;
                return true;
            }
            catch (Exception ex)
            {
                SendSwitchConnectionFailed(ex);
            }
            return false;
        }

        public void RunPoeWizard(string port, ProgressReportResult reportResult, List<CommandType> commands, int waitSec)
        {
            if (string.IsNullOrEmpty(port)) return;
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
                _wizardStartTime = DateTime.Now;
                StringBuilder txt = new StringBuilder("\n    PoE status: ").Append(_wizardSwitchPort.Poe).Append(", Power: ").Append(_wizardSwitchPort.Power);
                txt.Append(" mW, Port Status: ").Append(_wizardSwitchPort.Status);
                if (_wizardSwitchPort.EndPointDevice != null && !string.IsNullOrEmpty(_wizardSwitchPort.EndPointDevice.MacAddress))
                {
                    txt.Append(", Device MAC: ").Append(_wizardSwitchPort.EndPointDevice.MacAddress);
                    if (!string.IsNullOrEmpty(_wizardSwitchPort.EndPointDevice.IpAddress))
                    {
                        txt.Append(", IP: ").Append(_wizardSwitchPort.EndPointDevice.IpAddress);
                    }
                }
                else if (_wizardSwitchPort.MacList?.Count > 0 && !string.IsNullOrEmpty(_wizardSwitchPort.MacList[0]))
                {
                    txt.Append(", Device MAC: ").Append(_wizardSwitchPort.MacList[0]);
                }
                if (_wizardSwitchPort.Poe != PoeStatus.Fault && _wizardSwitchPort.Poe != PoeStatus.Deny)
                {
                    string wizardAction = $"Nothing to do on port {_wizardSwitchPort.Name}.{txt}";
                    _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, $"Nothing to do on port {_wizardSwitchPort.Name}.{txt}");
                    _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, false);
                    Logger.Info($"{wizardAction}\n{_wizardProgressReport.Message}");
                    return;
                }
                foreach (CommandType command in commands)
                {
                    _wizardCommand = command;
                    switch (_wizardCommand)
                    {

                        case CommandType.POWER_2PAIR_PORT:
                            TryEnable2PairPower(waitSec);
                            break;

                        case CommandType.POWER_HDMI_ENABLE:
                            if (_wizardSwitchPort.Protocol8023bt == ConfigType.Unavailable) continue;
                            if (_wizardSwitchPort.IsPowerOverHdmi) continue;
                            ExecuteActionOnPort($"Enabling Power HDMI on Port {_wizardSwitchPort.Name}", waitSec, CommandType.POWER_HDMI_DISABLE);
                            break;

                        case CommandType.LLDP_POWER_MDI_ENABLE:
                            if (_wizardSwitchPort.IsLldpMdi) continue;
                            ExecuteActionOnPort($"Enabling LLDP Power via MDI on Port {port}", waitSec, CommandType.LLDP_POWER_MDI_DISABLE);
                            break;

                        case CommandType.LLDP_EXT_POWER_MDI_ENABLE:
                            if (_wizardSwitchPort.IsLldpExtMdi) continue;
                            ExecuteActionOnPort($"Enabling LLDP Extended Power via MDI on Port {port}", waitSec, CommandType.LLDP_EXT_POWER_MDI_DISABLE);
                            break;

                        case CommandType.CHECK_POWER_PRIORITY:
                            CheckPriority();
                            return;

                        case CommandType.POWER_PRIORITY_PORT:
                            TryChangePriority(port, waitSec);
                            break;

                        case CommandType.POWER_823BT_ENABLE:
                            Enable823BT(waitSec);
                            break;

                        case CommandType.RESET_POWER_PORT:
                            ResetPortPower(port, waitSec);
                            break;

                    }
                    if (!_wizardReportResult.Proceed)
                    {
                        SwitchModel.ConfigChanged = true;
                        break;
                    }
                }
                Logger.Info($"PoE Wizard completed on port {port}, Waiting Time: {waitSec} sec\n{_wizardProgressReport.Message}");
            }
            catch (Exception ex)
            {
                SendSwitchConnectionFailed(ex);
            }
        }

        private void GetSwitchSlotPort(string port)
        {
            _wizardSwitchPort = SwitchModel.GetPort(port);
            if (_wizardSwitchPort == null) return;
            ChassisSlotPort chassisSlotPort = new ChassisSlotPort(port);
            ChassisModel chassis = SwitchModel.GetChassis(chassisSlotPort.ChassisNr);
            if (chassis == null) return;
            _wizardSwitchSlot = chassis.GetSlot(chassisSlotPort.SlotNr);
        }

        private void TryEnable2PairPower(int waitSec)
        {
            DateTime startTime = DateTime.Now;
            bool fastPoe = _wizardSwitchSlot.IsFPoE;
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
                Logger.Error(ex.Message + ":\n" + ex.StackTrace);
                error = $"Switch {SwitchModel.IpAddress} connection error:\n - {WebUtility.UrlDecode(ex.Message)}";
            }
            _progress?.Report(new ProgressReport(ReportType.Error, "Connect", error));
        }

        private void ExecuteActionOnPort(string wizardAction, int waitSec, CommandType restoreCmd)
        {
            try
            {
                StringBuilder txt = new StringBuilder(wizardAction).Append("\nPoE status: ");
                txt.Append(_wizardSwitchPort.Poe).Append(", Port Status: ").Append(_wizardSwitchPort.Status);
                //txt.Append("\n").Append(_wizardSwitchPort.EndPointDevice);
                _progress.Report(new ProgressReport(txt.ToString()));
                _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, wizardAction);
                SendRequest(GetRestUrlEntry(_wizardCommand, new string[1] { _wizardSwitchPort.Name }));
                Thread.Sleep(3000);
                WaitPortUp(waitSec);
                if (_wizardReportResult.Proceed && restoreCmd != _wizardCommand)
                {
                    SendRequest(GetRestUrlEntry(restoreCmd, new string[1] { _wizardSwitchPort.Name }));
                }
            }
            catch (Exception ex)
            {
                ParseException(_wizardSwitchPort.Name, _wizardProgressReport, ex);
            }
        }

        private void Enable823BT(int waitSec)
        {
            try
            {
                string wizardAction = $"Enabling 802.3.bt on Slot {_wizardSwitchSlot.Name}";
                _wizardReportResult.CreateReportResult(_wizardSwitchPort.Name, wizardAction);
                DateTime startTime = DateTime.Now;
                StringBuilder txt = new StringBuilder(wizardAction).Append("\nPoE status: ");
                txt.Append(_wizardSwitchPort.Poe).Append(", Port Status: ").Append(_wizardSwitchPort.Status);
                //txt.Append("\n").Append(_wizardSwitchPort.EndPointDevice);
                _progress.Report(new ProgressReport(txt.ToString()));
                if (_wizardSwitchPort.Protocol8023bt != ConfigType.Unavailable)
                {
                    //CheckFPOEand823BT(CommandType.POWER_823BT_ENABLE);
                    Change823BT(CommandType.POWER_823BT_ENABLE);
                    WaitPortUp(waitSec);
                }
                else
                {
                    Thread.Sleep(3000);
                    UpdateProgressReport();
                    _wizardReportResult.UpdateError(_wizardSwitchPort.Name, $"Switch {SwitchModel.IpAddress} doesn't support 802.3.bt!");
                }
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
            StringBuilder txt = new StringBuilder(wizardAction).Append("\nPoE status: ");
            txt.Append(_wizardSwitchPort.Poe).Append(", Port Status: ").Append(_wizardSwitchPort.Status);
            double powerRemaining = _wizardSwitchSlot.Budget - _wizardSwitchSlot.Power;
            double maxPower = _wizardSwitchPort.MaxPower / 1000;
            txt = new StringBuilder();
            bool changePriority;
            if (_wizardSwitchPort.PriorityLevel < PriorityLevelType.High)
            {
                changePriority = powerRemaining >= maxPower ? false : true;
                txt.Append("\n    Remaining Power = ").Append(powerRemaining).Append(" W, Max. Power = ").Append(maxPower).Append(" W");
            }
            else
            {
                changePriority = false;
                txt.Append($"\n    No need to change priority on Port {_wizardSwitchPort.Name} (Priority is already {_wizardSwitchPort.PriorityLevel})");
            }
            _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, changePriority, txt.ToString());
            _wizardReportResult.UpdateDuration(_wizardSwitchPort.Name, Utils.PrintTimeDurationSec(startTime));
            Logger.Debug(txt.ToString());
        }

        private void TryChangePriority(string port, int waitSec)
        {
            try
            {
                PriorityLevelType priority = PriorityLevelType.High;
                string wizardAction = $"Changing power priority to {priority} on Port {port}";
                _wizardReportResult.CreateReportResult(port, wizardAction);
                PriorityLevelType prevPriority = _wizardSwitchPort.PriorityLevel;
                DateTime startTime = DateTime.Now;
                StringBuilder txt = new StringBuilder(wizardAction).Append("\nPoE status: ");
                txt.Append(_wizardSwitchPort.Poe).Append(", Port Status: ").Append(_wizardSwitchPort.Status);
                _progress.Report(new ProgressReport(txt.ToString()));
                SendRequest(GetRestUrlEntry(CommandType.POWER_PRIORITY_PORT, new string[2] { port, priority.ToString() }));
                RestartDeviceOnPort(port, waitSec);
                string result;
                if (_wizardReportResult.Proceed)
                {
                    SendRequest(GetRestUrlEntry(CommandType.POWER_PRIORITY_PORT, new string[2] { port, prevPriority.ToString() }));
                    result = "didn't solve the problem";
                }
                else
                {
                    result = "solved the problem";
                }
                _wizardReportResult.UpdateResult(port, _wizardReportResult.Proceed, result);
                _wizardReportResult.UpdateDuration(port, Utils.CalcStringDuration(startTime, true));
            }
            catch (Exception ex)
            {
                ParseException(port, _wizardProgressReport, ex);
            }
        }

        private void ResetPortPower(string port, int waitSec)
        {
            try
            {
                string wizardAction = $"Resetting Power on Port {port}";
                _wizardReportResult.CreateReportResult(port, wizardAction);
                DateTime startTime = DateTime.Now;
                _progress.Report(new ProgressReport($"{wizardAction}\nPoE status: {_wizardSwitchPort.Poe}, Port Status: {_wizardSwitchPort.Status}"));
                RestartDeviceOnPort(port, waitSec);
                _wizardReportResult.UpdateDuration(port, Utils.PrintTimeDurationSec(startTime));
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ":\n" + ex.StackTrace);
            }
        }

        private void RestartDeviceOnPort(string port, int waitSec)
        {
            SendRequest(GetRestUrlEntry(CommandType.POWER_DOWN_PORT, new string[1] { port }));
            Thread.Sleep(5000);
            SendRequest(GetRestUrlEntry(CommandType.POWER_UP_PORT, new string[1] { port }));
            WaitPortUp(waitSec);
        }

        private void WaitPortUp(int waitSec)
        {
            DateTime startTime = DateTime.Now;
            UpdatePortData();
            while (Utils.GetTimeDuration(startTime) <= waitSec)
            {
                UpdatePortData();
                if (_wizardSwitchPort != null && _wizardSwitchPort.Status == PortStatus.Up && _wizardSwitchPort.Power > 500) break;
                Thread.Sleep(5000);
            }
            UpdateProgressReport();
            StringBuilder text = new StringBuilder("Port ").Append(_wizardSwitchPort.Name).Append(" Status: ").Append(_wizardSwitchPort.Status).Append(", PoE Status: ");
            text.Append(_wizardSwitchPort.Poe).Append(", Power: ").Append(_wizardSwitchPort.Power).Append(" (Duration: ").Append(Utils.CalcStringDuration(startTime));
            text.Append(", MAC List: ").Append(String.Join(",", _wizardSwitchPort.MacList)).Append(")");
            Logger.Debug(text.ToString());
        }

        private void UpdateProgressReport()
        {
            string result;
            bool proceed;
            if (_wizardSwitchPort != null && _wizardSwitchPort.Status == PortStatus.Up)
            {
                proceed = false;
                result = "solved the problem";
            }
            else
            {
                proceed = true;
                result = "didn't solve the problem";
            }
            _wizardReportResult.UpdateResult(_wizardSwitchPort.Name, proceed, result);
            StringBuilder portStatus = new StringBuilder();
            portStatus.Append("\n    PoE Status: ").Append(_wizardSwitchPort.Poe).Append(", Power: ").Append(_wizardSwitchPort.Power).Append(" mW, Port Status: ");
            portStatus.Append(_wizardSwitchPort.Status);
            if (_wizardSwitchPort.MacList?.Count > 0) portStatus.Append(", Device MAC Address: ").Append(_wizardSwitchPort.MacList[0]);
            _wizardReportResult.UpdatePortStatus(_wizardSwitchPort.Name, portStatus.ToString());
        }

        private void UpdatePortData()
        {
            if (_wizardSwitchPort == null || _wizardSwitchSlot == null) return;
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
                this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_LAN_POWER_STATUS, new string[] { chassis.Number.ToString() }));
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
            List<Dictionary<string, string>> diclist;
            this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_LAN_POWER, new string[1] { $"{slot.Name}" }));
            diclist = CliParseUtils.ParseHTable(_response[RESULT], 1);
            slot.LoadFromList(diclist, DictionaryType.LanPower);
            this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_LAN_POWER_CONFIG, new string[1] { $"{slot.Name}" }));
            diclist = CliParseUtils.ParseHTable(_response[RESULT], 2);
            slot.LoadFromList(diclist, DictionaryType.LanPowerCfg);
        }

        private void ParseException(string port, ProgressReport progressReport, Exception ex)
        {
            Logger.Error(ex.Message + ":\n" + ex.StackTrace);
            progressReport.Type = ReportType.Error;
            progressReport.Message += $"didn't solve the problem{WebUtility.UrlDecode($"\n{ex.Message}")}";
            PowerDevice(CommandType.POWER_UP_PORT);
        }

        private string ChangePerpetualOrFastPoe(CommandType cmd)
        {
            if (_wizardSwitchSlot == null) return "";
            ChassisSlotPort chassisSlotPort = new ChassisSlotPort($"{_wizardSwitchSlot.Name}/0");
            ChassisModel chassis = SwitchModel.GetChassis(chassisSlotPort.ChassisNr);
            if (chassis == null) return "";
            bool enable = cmd == CommandType.POE_PERPETUAL_ENABLE || cmd == CommandType.POE_FAST_ENABLE;
            string poeType = (cmd == CommandType.POE_PERPETUAL_ENABLE || cmd == CommandType.POE_PERPETUAL_DISABLE) ? "Perpetual" : "Fast";
            string txt = $"{poeType} PoE on Slot {chassisSlotPort.ChassisNr}/{chassisSlotPort.SlotNr}";
            if (cmd == CommandType.POE_PERPETUAL_ENABLE && _wizardSwitchSlot.IsPPoE || cmd == CommandType.POE_FAST_ENABLE && _wizardSwitchSlot.IsFPoE ||
                cmd == CommandType.POE_PERPETUAL_DISABLE && !_wizardSwitchSlot.IsPPoE || cmd == CommandType.POE_FAST_DISABLE && !_wizardSwitchSlot.IsFPoE)
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
            //CheckFPOEand823BT(cmd);
            SendRequest(GetRestUrlEntry(cmd, new string[1] { $"{_wizardSwitchSlot.Name}" }));
            Thread.Sleep(2000);
            this._response = SendRequest(GetRestUrlEntry(CommandType.SHOW_LAN_POWER_STATUS, new string[] { chassis.Number.ToString() }));
            List<Dictionary<string, string>> dictList = CliParseUtils.ParseHTable(_response[RESULT], 2);
            chassis.LoadFromList(dictList);
            if (cmd == CommandType.POE_PERPETUAL_ENABLE && _wizardSwitchSlot.IsPPoE || cmd == CommandType.POE_FAST_ENABLE && _wizardSwitchSlot.IsFPoE ||
                cmd == CommandType.POE_PERPETUAL_DISABLE && !_wizardSwitchSlot.IsPPoE || cmd == CommandType.POE_FAST_DISABLE && !_wizardSwitchSlot.IsFPoE)
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
                if (_wizardSwitchSlot.Is8023bt)
                {
                    WriteMemory();
                    Change823BT(CommandType.POWER_823BT_DISABLE);
                }
            }
            else if (cmd == CommandType.POWER_823BT_ENABLE)
            {
                if (_wizardSwitchSlot.IsFPoE) SendRequest(GetRestUrlEntry(CommandType.POE_FAST_DISABLE, new string[1] { _wizardSwitchSlot.Name }));
            }
        }

        private void Change823BT(CommandType cmd)
        {
            PowerDownSlot();
            SendRequest(GetRestUrlEntry(cmd, new string[1] { _wizardSwitchSlot.Name }));
            PowerUpSlot();
        }

        private void PowerDownSlot()
        {
            SendRequest(GetRestUrlEntry(CommandType.POWER_DOWN_SLOT, new string[1] { _wizardSwitchSlot.Name }));
            Thread.Sleep(5000);
        }

        private void PowerUpSlot()
        {
            SendRequest(GetRestUrlEntry(CommandType.POWER_UP_SLOT, new string[1] { _wizardSwitchSlot.Name }));
            Thread.Sleep(3000);
        }

        private void PowerDevice(CommandType cmd)
        {
            try
            {
                this._response = SendRequest(GetRestUrlEntry(cmd, new string[1] { _wizardSwitchSlot.Name }));
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ":\n" + ex.StackTrace);
                if (ex is SwitchConnectionFailure || ex is SwitchConnectionDropped || ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure)
                {
                    if (ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure) this.SwitchModel.Status = SwitchStatus.LoginFail;
                    else this.SwitchModel.Status = SwitchStatus.Unreachable;
                }
                else
                {
                    Logger.Error(ex.Message + ":\n" + ex.StackTrace);
                }
            }
        }

        private void SetPoePriority(string port, PriorityLevelType priority)
        {
            try
            {
                this._response = SendRequest(GetRestUrlEntry(CommandType.POWER_PRIORITY_PORT, new string[2] { port, priority.ToString() }));
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ":\n" + ex.StackTrace);
                if (ex is SwitchConnectionFailure || ex is SwitchConnectionDropped || ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure)
                {
                    if (ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure) this.SwitchModel.Status = SwitchStatus.LoginFail;
                    else this.SwitchModel.Status = SwitchStatus.Unreachable;
                }
                else
                {
                    throw ex;
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

using PoEWizard.Data;
using PoEWizard.Device;
using PoEWizard.Exceptions;
using System;
using System.Collections;
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
        private ProgressReport _wizardProgressReport;

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
                List<Dictionary<string, string>> diclist;
                Dictionary<string, string> dict;
                this.IsReady = true;
                Logger.Debug($"Connecting Rest API");
                _progress.Report(new ProgressReport("Connecting to switch..."));
                RestApiClient.Login();
                if (!RestApiClient.IsConnected()) throw new SwitchConnectionFailure($"Could not connect to Switch {SwitchModel.IpAddress}!");
                SwitchModel.IsConnected = true;
                _progress.Report(new ProgressReport("Reading System information..."));
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_SYSTEM));
                dict = CliParseUtils.ParseVTable(_response[RESULT]);
                SwitchModel.LoadFromDictionary(dict, DictionaryType.System);
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_MICROCODE));
                diclist = CliParseUtils.ParseHTable(_response[RESULT]);
                SwitchModel.LoadFromDictionary(diclist[0], DictionaryType.MicroCode);
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_RUNNING_DIR));
                dict = CliParseUtils.ParseVTable(_response[RESULT]);
                SwitchModel.LoadFromDictionary(dict, DictionaryType.RunningDir);
                _progress.Report(new ProgressReport("Reading chassis and port information..."));
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_CHASSIS));
                diclist = CliParseUtils.ParseChassisTable(_response[RESULT]);
                SwitchModel.LoadFromList(diclist, DictionaryType.Chassis);
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_PORTS_LIST));
                diclist = CliParseUtils.ParseHTable(_response[RESULT], 3);
                SwitchModel.LoadFromList(diclist, DictionaryType.PortsList);
                _progress.Report(new ProgressReport("Reading power supply information"));
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_POWER_SUPPLIES));
                diclist = CliParseUtils.ParseHTable(_response[RESULT], 2);
                SwitchModel.LoadFromList(diclist, DictionaryType.PowerSupply);
                GetLanPower();
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_CONFIGURATION));
                SwitchModel.ConfigSnapshot = _response[RESULT];
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_LLDP_REMOTE));
                diclist = CliParseUtils.ParseLldpRemoteTable(_response[RESULT]);
                SwitchModel.LoadFromList(diclist, DictionaryType.LldpRemoteList);
            }
            catch (Exception ex)
            {
                if (ex is SwitchConnectionFailure || ex is SwitchConnectionDropped || ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure)
                {
                    if (ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure) this.SwitchModel.Status = SwitchStatus.LoginFail;
                    else this.SwitchModel.Status = SwitchStatus.Unreachable;
                }
                else
                {
                    Logger.Error(ex.Message + ":\n" + ex.StackTrace);
                    _progress.Report(new ProgressReport(ReportType.Error, "Connect", WebUtility.UrlDecode(ex.Message)));
                }
            }
        }

        public bool RunPoeWizard(string port, List<RestUrlId> commands, int waitSec)
        {
            _wizardProgressReport = new ProgressReport("PoE Wizard Report:");
            try
            {
                _wizardStartTime = DateTime.Now;
                GetLanPower();
                UpdatePortData(port);
                StringBuilder txt = new StringBuilder("\n    PoE status: ").Append(_wizardSwitchPort.Poe).Append(", Power: ").Append(_wizardSwitchPort.Power);
                txt.Append(" mW, Port Status: ").Append(_wizardSwitchPort.Status);
                if (_wizardSwitchPort.MacList?.Count > 0) txt.Append(", Device MAC Address: ").Append(_wizardSwitchPort.MacList[0]);
                if (_wizardSwitchPort.Poe != PoeStatus.Fault && _wizardSwitchPort.Poe != PoeStatus.Deny)
                {
                    _wizardProgressReport.Type = ReportType.Info;
                    _wizardProgressReport.Message += $"\n - Nothing to do on port {port}.{txt}";
                    _progress.Report(_wizardProgressReport);
                    Logger.Info($"PoE Wizard completed on port {port}\n{_wizardProgressReport.Message}");
                    return false;
                }
                ChassisSlotPort chassisSlotPort = new ChassisSlotPort(port);
                foreach (RestUrlId command in commands)
                {
                    switch (command)
                    {

                        case RestUrlId.POWER_2PAIR_PORT:
                            if (!_wizardSwitchPort.Is4Pair)
                            {
                                SendRequest(GetRestUrlEntry(RestUrlId.POWER_4PAIR_PORT, new string[1] { port }));
                                Thread.Sleep(3000);
                                ExecuteActionOnPort($"Re-enabling 2-Pair Power on Port {port}", port, RestUrlId.POWER_2PAIR_PORT, waitSec);
                                if (_wizardProgressReport.Type != ReportType.Error) break;
                            }
                            RestUrlId init4Pair = _wizardSwitchPort.Is4Pair ? RestUrlId.POWER_4PAIR_PORT : RestUrlId.POWER_2PAIR_PORT;
                            ExecuteActionOnPort($"Enabling {(_wizardSwitchPort.Is4Pair ? "2-Pair" : "4-Pair")} Power on Port {port}", port,
                                                _wizardSwitchPort.Is4Pair ? RestUrlId.POWER_2PAIR_PORT : RestUrlId.POWER_4PAIR_PORT, waitSec);
                            TryRestartPortPower(port, waitSec, init4Pair);
                            break;

                        case RestUrlId.POWER_HDMI_ENABLE:
                            if (!_wizardSwitchPort.IsPowerOverHdmi)
                            {
                                ExecuteActionOnPort($"Enabling Power HDMI on Port {port}", port, RestUrlId.POWER_HDMI_ENABLE, waitSec);
                                TryRestartPortPower(port, waitSec, RestUrlId.POWER_HDMI_DISABLE);
                            }
                            break;

                        case RestUrlId.LLDP_POWER_MDI_ENABLE:
                            if (!_wizardSwitchPort.IsLldpMdi)
                            {
                                ExecuteActionOnPort($"Enabling LLDP Power via MDI on Port {port}", port, RestUrlId.LLDP_POWER_MDI_ENABLE, waitSec);
                                TryRestartPortPower(port, waitSec, RestUrlId.LLDP_POWER_MDI_DISABLE);
                            }
                            break;

                        case RestUrlId.LLDP_EXT_POWER_MDI_ENABLE:
                            if (!_wizardSwitchPort.IsLldpExtMdi)
                            {
                                ExecuteActionOnPort($"Enabling LLDP Ext Power via MDI on Port {port}", port, RestUrlId.LLDP_EXT_POWER_MDI_ENABLE, waitSec);
                                TryRestartPortPower(port, waitSec, RestUrlId.LLDP_EXT_POWER_MDI_DISABLE);
                            }
                            break;

                        case RestUrlId.CHECK_POWER_PRIORITY:
                            CheckPowerPriority($"Checking power priority on Port {port}", port, chassisSlotPort);
                            break;

                        case RestUrlId.POWER_PRIORITY_PORT:
                            TryChangePriority($"Changing priority on Port {port}", port, waitSec);
                            break;

                        case RestUrlId.POWER_823BT_ENABLE:
                            if (_wizardSwitchPort.Protocol8023bt == ConfigType.Disabled)
                            {
                                Config823BT($"Enabling 802.3.bt on Slot {chassisSlotPort.ChassisNr}/{chassisSlotPort.SlotNr}",
                                            chassisSlotPort, RestUrlId.POWER_823BT_ENABLE, waitSec);
                                Thread.Sleep(3000);
                                WaitPortUp(port, waitSec);
                            }
                            break;

                    }
                    if (_wizardProgressReport.Type != ReportType.Error) break;
                }
                _wizardProgressReport.Message += $"\n - Duration: {Utils.PrintTimeDurationSec(_wizardStartTime)}";
                _progress.Report(_wizardProgressReport);
                Logger.Info($"PoE Wizard completed on port {port}, Waiting Time: {waitSec} sec\n{_wizardProgressReport.Message}");
            }
            catch (Exception ex)
            {
                if (ex is SwitchConnectionFailure || ex is SwitchConnectionDropped || ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure)
                {
                    if (ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure) this.SwitchModel.Status = SwitchStatus.LoginFail;
                    else this.SwitchModel.Status = SwitchStatus.Unreachable;
                }
                else
                {
                    Logger.Error(ex.Message + ":\n" + ex.StackTrace);
                    _progress?.Report(new ProgressReport(ReportType.Error, "Connect", WebUtility.UrlDecode(ex.Message)));
                }
            }
            return _wizardProgressReport.Type == ReportType.Error;
        }

        private void TryRestartPortPower(string port, int waitSec, RestUrlId command)
        {
            if (_wizardProgressReport.Type == ReportType.Error)
            {
                RestartPortPowerWait(port, waitSec);
                if (_wizardProgressReport.Type == ReportType.Error) SendRequest(GetRestUrlEntry(command, new string[1] { port }));
            }
        }

        private void RestartPortPowerWait(string port, int waitSec)
        {
            SendRequest(GetRestUrlEntry(RestUrlId.POWER_DOWN_PORT, new string[1] { port }));
            Thread.Sleep(5000);
            SendRequest(GetRestUrlEntry(RestUrlId.POWER_UP_PORT, new string[1] { port }));
            _progress.Report(new ProgressReport($"Restarting Power on Port {port}\nPoE status: {_wizardSwitchPort.Poe}, Port Status: {_wizardSwitchPort.Status}"));
            WaitPortUp(port, waitSec);
        }

        private void GetLanPower()
        {
            List<Dictionary<string, string>> diclist;
            Dictionary<string, string> dict;

            _progress.Report(new ProgressReport("Reading PoE information"));
            foreach (var chassis in SwitchModel.ChassisList)
            {
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_LAN_POWER_STATUS, new string[] { chassis.Number.ToString() }));
                diclist = CliParseUtils.ParseHTable(_response[RESULT], 2);
                chassis.LoadFromList(diclist);
                foreach (var slot in chassis.Slots)
                {
                    if (slot.Ports.Count == 0) continue;
                    GetSlotPower(chassis, slot);
                    this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_LAN_POWER_CONFIG, new string[1] { $"{chassis.Number}/{slot.Number}" }));
                    diclist = CliParseUtils.ParseHTable(_response[RESULT], 2);
                    slot.LoadFromList(diclist, DictionaryType.LanPowerCfg);
                }
                foreach (var ps in chassis.PowerSupplies)
                {
                    this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_POWER_SUPPLY, new string[1] { ps.Id.ToString() }));
                    dict = CliParseUtils.ParseVTable(_response[RESULT]);
                    ps.LoadFromDictionary(dict);
                }
            }
        }

        private void ExecuteActionOnPort(string wizardAction, string port, RestUrlId command, int waitSec)
        {
            try
            {
                StringBuilder txt = new StringBuilder(wizardAction).Append("\nPoE status: ");
                txt.Append(_wizardSwitchPort.Poe).Append(", Port Status: ").Append(_wizardSwitchPort.Status);
                _progress.Report(new ProgressReport(txt.ToString()));
                _wizardProgressReport.Message += $"\n - {wizardAction} ";
                SendRequest(GetRestUrlEntry(command, new string[1] { port }));
                Thread.Sleep(3000);
                WaitPortUp(port, waitSec);
            }
            catch (Exception ex)
            {
                ParseException(port, _wizardProgressReport, ex);
            }
        }

        private void Config823BT(string wizardAction, ChassisSlotPort slotPort, RestUrlId command, int waitSec)
        {
            string slotNr = $"{slotPort.ChassisNr}/{slotPort.SlotNr}";
            string port = $"{slotNr}/{slotPort.PortNr}";
            try
            {
                StringBuilder txt = new StringBuilder(wizardAction).Append("\nPoE status: ");
                txt.Append(_wizardSwitchPort.Poe).Append(", Port Status: ").Append(_wizardSwitchPort.Status);
                _progress.Report(new ProgressReport(txt.ToString()));
                _wizardProgressReport.Message += $"\n - {wizardAction} ";
                SendRequest(GetRestUrlEntry(RestUrlId.POWER_DOWN_SLOT, new string[1] { slotNr }));
                SendRequest(GetRestUrlEntry(command, new string[1] { slotNr }));
                Thread.Sleep(5000);
                SendRequest(GetRestUrlEntry(RestUrlId.POWER_UP_SLOT, new string[1] { slotNr }));
            }
            catch (Exception ex)
            {
                SendRequest(GetRestUrlEntry(RestUrlId.POWER_UP_SLOT, new string[1] { slotNr }));
                ParseException(port, _wizardProgressReport, ex);
            }
        }

        private void CheckPowerPriority(string wizardAction, string port, ChassisSlotPort slotPort)
        {
            StringBuilder txt = new StringBuilder(wizardAction).Append("\nPoE status: ");
            txt.Append(_wizardSwitchPort.Poe).Append(", Port Status: ").Append(_wizardSwitchPort.Status);
            _progress.Report(new ProgressReport(txt.ToString()));
            _wizardProgressReport.Message += $"\n - {wizardAction} ";
            _wizardProgressReport.Type = ReportType.Info;
            _wizardProgressReport.Message += "completed";
            ChassisModel chassis = this.SwitchModel.GetChassis(slotPort.ChassisNr);
            if (chassis == null) return;
            PortModel switchPort = this.SwitchModel.GetPort(port);
            if (switchPort == null) return;
            if (chassis.PowerRemaining < switchPort.MaxPower) _wizardProgressReport.Type = ReportType.Error;
        }

        private void TryChangePriority(string wizardAction, string port, int waitSec)
        {
            try
            {
                StringBuilder txt = new StringBuilder(wizardAction).Append("\nPoE status: ");
                txt.Append(_wizardSwitchPort.Poe).Append(", Port Status: ").Append(_wizardSwitchPort.Status);
                _progress.Report(new ProgressReport(txt.ToString()));
                _wizardProgressReport.Message += $"\n - {wizardAction} ";
                SendRequest(GetRestUrlEntry(RestUrlId.POWER_DOWN_PORT, new string[1] { port }));
                PriorityLevelType priorityLevel = PriorityLevelType.High;
                SetPoePriority(port, priorityLevel);
                Thread.Sleep(5000);
                SendRequest(GetRestUrlEntry(RestUrlId.POWER_UP_PORT, new string[1] { port }));
                Thread.Sleep(3000);
                _wizardProgressReport.Message += $"to {priorityLevel} ";
                WaitPortUp(port, waitSec);
            }
            catch (Exception ex)
            {
                ParseException(port, _wizardProgressReport, ex);
            }
        }

        private void WaitPortUp(string port, int waitSec)
        {
            DateTime startTime = DateTime.Now;
            UpdatePortData(port);
            while (Utils.GetTimeDuration(startTime) <= waitSec)
            {
                UpdatePortData(port);
                if (_wizardSwitchPort != null && _wizardSwitchPort.Status == PortStatus.Up && _wizardSwitchPort.Power > 500) break;
                Thread.Sleep(5000);
            }
            StringBuilder txt = new StringBuilder("Port ").Append(port).Append(" Status: ").Append(_wizardSwitchPort.Status).Append(", PoE Status: ");
            txt.Append(_wizardSwitchPort.Poe).Append(", Power: ").Append(_wizardSwitchPort.Power).Append(" (Duration: ").Append(Utils.CalcStringDuration(startTime));
            txt.Append(", MAC List: ").Append(String.Join(",", _wizardSwitchPort.MacList)).Append(")");
            Logger.Debug(txt.ToString());
            txt = new StringBuilder();
            if (_wizardSwitchPort != null && _wizardSwitchPort.Status == PortStatus.Up)
            {
                _wizardProgressReport.Type = ReportType.Info;
                txt.Append("solved the problem");
            }
            else
            {
                _wizardProgressReport.Type = ReportType.Error;
                txt.Append("didn't solve the problem");
            }
            txt.Append("\n    PoE Status: ").Append(_wizardSwitchPort.Poe).Append(", Power: ").Append(_wizardSwitchPort.Power).Append(" mW, Port Status: ");
            txt.Append(_wizardSwitchPort.Status);
            if (_wizardSwitchPort.MacList?.Count > 0) txt.Append(", Device MAC Address: ").Append(_wizardSwitchPort.MacList[0]);
            _wizardProgressReport.Message += txt;
        }

        private void UpdatePortData(string port)
        {
            ChassisSlotPort slotPort = new ChassisSlotPort(port);
            ChassisModel chassis = SwitchModel.GetChassis(slotPort.ChassisNr);
            SlotModel slot = chassis.GetSlot(slotPort.SlotNr);
            GetSlotPower(chassis, slot);
            _wizardSwitchPort = this.SwitchModel.GetPort(port) ?? throw new Exception($"Port {port} not found!");
            this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_PORT_STATUS, new string[1] { port }));
            List<Dictionary<string, string>> dictList = CliParseUtils.ParseHTable(_response[RESULT], 3);
            if (dictList?.Count > 0) _wizardSwitchPort.UpdatePortStatus(dictList[0]);
            this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_PORT_MAC_ADDRESS, new string[1] { port }));
            dictList = CliParseUtils.ParseHTable(_response[RESULT], 3);
            if (dictList?.Count > 0) _wizardSwitchPort.UpdateMacList(dictList);
            this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_PORT_LLDP_REMOTE, new string[] { "1/1/26" }));
            dictList = CliParseUtils.ParseLldpRemoteTable(_response[RESULT]);
            if (dictList?.Count > 0) _wizardSwitchPort.LoadLldpRemoteTable(dictList[0]);
        }

        private void GetSlotPower(ChassisModel chassis, SlotModel slot)
        {
            List<Dictionary<string, string>> diclist;
            this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_LAN_POWER, new string[1] { $"{chassis.Number}/{slot.Number}" }));
            diclist = CliParseUtils.ParseHTable(_response[RESULT], 1);
            slot.LoadFromList(diclist, DictionaryType.LanPower);
        }

        private void ParseException(string port, ProgressReport progressReport, Exception ex)
        {
            Logger.Error(ex.Message + ":\n" + ex.StackTrace);
            progressReport.Type = ReportType.Error;
            progressReport.Message += WebUtility.UrlDecode($"didn't solve the problem\n{ex.Message}");
            PowerDevice(RestUrlId.POWER_UP_PORT, port);
        }

        private void SetPoeConfiguration(RestUrlId cmd, string port)
        {
            try
            {
                this._response = SendRequest(GetRestUrlEntry(cmd, new string[1] { port }));
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

        private void PowerDevice(RestUrlId cmd, string slotPort)
        {
            try
            {
                this._response = SendRequest(GetRestUrlEntry(cmd, new string[1] { slotPort }));
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
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.POWER_PRIORITY_PORT, new string[2] { port, priority.ToString() }));
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

        private RestUrlEntry GetRestUrlEntry(RestUrlId url)
        {
            return GetRestUrlEntry(url, new string[1] { null });
        }

        private RestUrlEntry GetRestUrlEntry(RestUrlId url, string[] data)
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

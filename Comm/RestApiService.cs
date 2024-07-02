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

        public bool RunPoeWizard(string port, List<RestUrlId> commands)
        {
            GetLanPower();
            ProgressReport progressReport = new ProgressReport("PoE Wizard Report:");
            try
            {
                Dictionary<string, object> slotPort = Utils.GetChassisSlotPort(port);
                Logger.Debug($"Starting PoE Wizard");
                foreach (RestUrlId command in commands)
                {
                    switch (command)
                    {

                        case RestUrlId.POWER_2PAIR_PORT:
                            _progress.Report(new ProgressReport($"Enabling 2-Pair Power on Port {port}"));
                            progressReport.Message += $"\n - Enabling 2-Pair Power on Port {port} ";
                            ExecuteActionOnPort(port, RestUrlId.POWER_2PAIR_PORT, progressReport);
                            break;

                        case RestUrlId.POWER_HDMI_ENABLE:
                            _progress.Report(new ProgressReport($"Enabling Power HDMI on Port {port}"));
                            progressReport.Message += $"\n - Enabling Power HDMI on Port {port} ";
                            ExecuteActionOnPort(port, RestUrlId.POWER_HDMI_ENABLE, progressReport);
                            break;

                        case RestUrlId.LLDP_POWER_MDI_ENABLE:
                            _progress.Report(new ProgressReport($"Enabling LLDP Power via MDI on Port {port}"));
                            progressReport.Message += $"\n - Enabling LLDP Power via MDI on Port {port} ";
                            ExecuteActionOnPort(port, RestUrlId.LLDP_POWER_MDI_ENABLE, progressReport);
                            break;

                        case RestUrlId.LLDP_EXT_POWER_MDI_ENABLE:
                            _progress.Report(new ProgressReport($"Enabling LLDP Ext Power via MDI on Port {port}"));
                            progressReport.Message += $"\n - Enabling LLDP Ext Power via MDI on Port {port} ";
                            ExecuteActionOnPort(port, RestUrlId.LLDP_EXT_POWER_MDI_ENABLE, progressReport);
                            break;

                        case RestUrlId.CHECK_POWER_PRIORITY:
                            _progress.Report(new ProgressReport($"Checking power priority on Port {port}"));
                            progressReport.Message += $"\n - Checking power priority on Port {port} ";
                            CheckPowerPriority(port, progressReport, slotPort);
                            break;

                        case RestUrlId.POWER_PRIORITY_PORT:
                            _progress.Report(new ProgressReport($"Changing priority on Port {port}"));
                            progressReport.Message += $"\n - Changing priority on Port {port} ";
                            TryChangePriority(port, progressReport);
                            break;

                        case RestUrlId.POWER_823BT_ENABLE:
                            string slotNr = $"{slotPort[P_CHASSIS]}/{slotPort[P_SLOT]}";
                            _progress.Report(new ProgressReport($"Enabling 802.3.bt on slot {slotNr}"));
                            progressReport.Message += $"\n - Enabling 802.3.bt on slot {slotNr} ";
                            TryEnable823BT(port, progressReport, slotNr);
                            break;

                    }
                    if (progressReport.Type != ReportType.Error) break;
                }
                _progress.Report(progressReport);
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
                    _progress?.Report(new ProgressReport(ReportType.Error, "Connect", WebUtility.UrlDecode(ex.Message)));
                }
            }
            return progressReport.Type == ReportType.Error;
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
                    this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_LAN_POWER, new string[1] { $"{chassis.Number}/{slot.Number}" }));
                    diclist = CliParseUtils.ParseHTable(_response[RESULT], 1);
                    slot.LoadFromList(diclist, DictionaryType.LanPower);
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

        private void ExecuteActionOnPort(string port, RestUrlId action, ProgressReport progressReport)
        {
            try
            {
                SetPoeConfiguration(RestUrlId.POWER_DOWN_PORT, port);
                SetPoeConfiguration(action, port);
                Thread.Sleep(5000);
                SetPoeConfiguration(RestUrlId.POWER_UP_PORT, port);
                Thread.Sleep(3000);
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_PORT_STATUS, new string[1] { port }));

                //progressReport.Type = ReportType.Info;
                //progressReport.Message += "solved the problem";
                progressReport.Type = ReportType.Error;
                progressReport.Message += "didn't solve the problem";
            }
            catch (Exception ex)
            {
                ParseException(port, progressReport, ex);
            }
        }

        private void TryEnable823BT(string port, ProgressReport progressReport, string slotNr)
        {
            try
            {
                PowerPort(RestUrlId.POWER_DOWN_SLOT, slotNr);
                SetPoeConfiguration(RestUrlId.POWER_823BT_ENABLE, slotNr);
                Thread.Sleep(5000);
                PowerPort(RestUrlId.POWER_UP_SLOT, slotNr);
                Thread.Sleep(3000);
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_PORT_STATUS, new string[1] { port }));

                //progressReport.Type = ReportType.Info;
                //progressReport.Message += "solved the problem";
                progressReport.Type = ReportType.Error;
                progressReport.Message += "didn't solve the problem";
            }
            catch (Exception ex)
            {
                ParseException(port, progressReport, ex);
            }
        }

        private void CheckPowerPriority(string port, ProgressReport progressReport, Dictionary<string, object> slotPort)
        {
            progressReport.Type = ReportType.Info;
            progressReport.Message += "completed";
            ChassisModel chassis = this.SwitchModel.GetChassis((int)slotPort[P_CHASSIS]);
            if (chassis == null) return;
            PortModel switchPort = this.SwitchModel.GetPort(port);
            if (switchPort == null) return;
            if (chassis.PowerRemaining < Utils.StringToDouble(switchPort.MaxPower)) progressReport.Type = ReportType.Error;
        }

        private void TryChangePriority(string port, ProgressReport progressReport)
        {
            SetPoeConfiguration(RestUrlId.POWER_DOWN_PORT, port);
            try
            {
                PriorityLevelType priorityLevel = PriorityLevelType.High;
                SetPoePriority(port, priorityLevel);
                Thread.Sleep(5000);
                SetPoeConfiguration(RestUrlId.POWER_UP_PORT, port);
                Thread.Sleep(3000);
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_PORT_STATUS, new string[1] { port }));
                Thread.Sleep(5000);
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_MAC_LEARNING_PORT, new string[1] { port }));

                progressReport.Message += $"to {priorityLevel} ";
                //progressReport.Type = ReportType.Info;
                //progressReport.Message += "solved the problem";
                progressReport.Type = ReportType.Error;
                progressReport.Message += "didn't solve the problem";

            }
            catch (Exception ex)
            {
                ParseException(port, progressReport, ex);
            }
        }

        private void ParseException(string port, ProgressReport progressReport, Exception ex)
        {
            Logger.Error(ex.Message + ":\n" + ex.StackTrace);
            progressReport.Type = ReportType.Error;
            progressReport.Message += WebUtility.UrlDecode($"didn't solve the problem\n{ex.Message}");
            Thread.Sleep(5000);
            SetPoeConfiguration(RestUrlId.POWER_UP_PORT, port);
        }

        private void SetPoeConfiguration(RestUrlId cmd, string slot)
        {
            try
            {
                this._response = SendRequest(GetRestUrlEntry(cmd, new string[1] { slot }));
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

        private void PowerPort(RestUrlId cmd, string port)
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

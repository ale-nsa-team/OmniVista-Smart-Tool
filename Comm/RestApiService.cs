using PoEWizard.Data;
using PoEWizard.Device;
using PoEWizard.Exceptions;
using System;
using System.Collections.Generic;
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
                foreach (var chassis in SwitchModel.ChassisList)
                {
                    this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_LAN_POWER_STATUS, new string[] { chassis.Number.ToString() }));
                    diclist = CliParseUtils.ParseHTable(_response[RESULT], 2);
                    chassis.LoadFromList(diclist);
                }
                foreach (var chassis in SwitchModel.ChassisList)
                {
                    foreach (var slot in chassis.Slots)
                    {
                        this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_LAN_POWER, new string[1] { $"{chassis.Number}/{slot.Number}" }));

                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_PORTS_LIST));
                Dictionary<int, Dictionary<int, List<Dictionary<string, string>>>> portsList = CliParseUtils.ParsePortsListApi(_response[RESULT]);


                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_POWER_SUPPLIES));
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_POWER_SUPPLY, new string[1] { "1" }));

                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_LAN_POWER_STATUS, new string[1] { "1/1" }));
                diclist = CliParseUtils.ParseHTable(_response[RESULT], 2);
                SwitchModel.LoadFromList(diclist, DictionaryType.LanPower);
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_LAN_POWER, new string[1] { "1/1" }));

                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_LAN_POWER_CONFIG, new string[1] { "1/1" }));
                diclist = CliParseUtils.ParseHTable(_response[RESULT], 2);

                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_HEALTH));
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_TEMPERATURE));

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
                    _progress.Report(new ProgressReport(ReportType.Error, "Connect", ex.Message));
                }
            }
        }

        public void RunPoeWizard(string port)
        {
            try
            {
                Logger.Debug($"Starting PoE Wizard");
                _progress.Report(new ProgressReport("Starting PoE Wizard..."));
                StringBuilder report = new StringBuilder();
                report.Append("Enable Port ").Append(port);
                if (TryDisable4Pair(port))
                {
                    report.Append(" 2-Pair Power Success");
                }
                else
                {
                    report.Append("Enable 2-Pair Power Failed");
                }
                report.Append("\nChange Port ").Append(port).Append(" priority to ");
                PriorityLevelType priorityLevel = TryChangePriority(port);

                SetPoeConfiguration(RestUrlId.POWER_823BT_ENABLE, port);
                SetPoeConfiguration(RestUrlId.POWER_823BT_DISABLE, port);

                SetPoeConfiguration(RestUrlId.POWER_HDMI_ENABLE, port);
                SetPoeConfiguration(RestUrlId.POWER_HDMI_DISABLE, port);

                SetPoeConfiguration(RestUrlId.LLDP_POWER_MDI_ENABLE, port);
                SetPoeConfiguration(RestUrlId.LLDP_POWER_MDI_DISABLE, port);

                SetPoeConfiguration(RestUrlId.LLDP_EXT_POWER_MDI_ENABLE, port);
                SetPoeConfiguration(RestUrlId.LLDP_EXT_POWER_MDI_DISABLE, port);

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
                    _progress?.Report(new ProgressReport(ReportType.Error, "Connect", ex.Message));
                }
            }
        }

        private bool TryDisable4Pair(string port)
        {
            PowerPort(RestUrlId.POWER_DOWN_PORT, port);
            PowerPort(RestUrlId.POWER_2PAIR_PORT, port);
            Thread.Sleep(5000);
            PowerPort(RestUrlId.POWER_UP_PORT, port);
            Thread.Sleep(3000);
            this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_PORT_STATUS, new string[1] { port }));
            Thread.Sleep(5000);
            return true;
        }

        private PriorityLevelType TryChangePriority(string port)
        {
            PriorityLevelType priorityLevel = PriorityLevelType.High;
            PowerPort(RestUrlId.POWER_DOWN_PORT, port);
            SetPoePriority(port, priorityLevel);
            Thread.Sleep(5000);
            PowerPort(RestUrlId.POWER_UP_PORT, port);
            Thread.Sleep(3000);
            this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_PORT_STATUS, new string[1] { port }));
            Thread.Sleep(5000);
            this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_MAC_LEARNING_PORT, new string[1] { port }));
            return priorityLevel;
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

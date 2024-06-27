using PoEWizard.Data;
using PoEWizard.Device;
using PoEWizard.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;
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

        public RestApiService()
        {
            SwitchModel = new SwitchModel();
        }
        public RestApiService(SwitchModel device, IProgress<ProgressReport> progress)
        {
            this.SwitchModel = device;
            this._progress = progress;
            this.RestApiClient = new RestApiClient(SwitchModel);
            this.IsReady = false;
            _progress = progress;
        }
        public RestApiService(string ipAddr, string username, string password, int cnxTimeout)
        {
            this.SwitchModel = new SwitchModel(ipAddr, username, password, cnxTimeout);
            this.RestApiClient = new RestApiClient(SwitchModel);
            this.IsReady = false;
        }

        public void Connect()
        {
            try
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                this.IsReady = true;
                Logger.Debug($"Connecting Rest API");
                _progress?.Report(new ProgressReport("Connecting to switch..."));
                RestApiClient.Login();
                this.SwitchModel = RestApiClient.SwitchInfo;
                _progress?.Report(new ProgressReport("Reading System information..."));
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_SYSTEM));
                dict = CliParseUtils.ParseVTable(this._response["RESULT"]);
                _progress?.Report(new ProgressReport("Readin chassis and port infomration..."));
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_CHASSIS));
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_LAN_POWER_STATUS));
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_PORTS_LIST));
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_LAN_POWER, new string[1] { "1/1" }));

                //this._response = SetPoePriority("1/1/26", PriorityLevelType.High);
                //this._response = SetPoePriority("1/1/27", PriorityLevelType.High);
                //this._response = SetPoePriority("1/1/28", PriorityLevelType.High);

                //this._response = PowerPort(RestUrlId.POWER_DOWN_PORT, "1/1/26");
                //this._response = PowerPort(RestUrlId.POWER_UP_PORT, "1/1/26");
                //this._response = PowerPort(RestUrlId.POWER_4PAIR_PORT, "1/1/28");
                //this._response = PowerPort(RestUrlId.POWER_2PAIR_PORT, "1/1/28");

                //this._response = SetPoeConfiguration(RestUrlId.POWER_823BT_ENABLE, "1/1");
                //this._response = SetPoeConfiguration(RestUrlId.POWER_823BT_DISABLE, "1/1");

                //this._response = SetPoeConfiguration(RestUrlId.POE_FAST_ENABLE, "1/1");
                //this._response = SetPoeConfiguration(RestUrlId.POE_PERPETUAL_ENABLE, "1/1");

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

        private Dictionary<string, string> SetPoeConfiguration(RestUrlId cmd, string slot)
        {
            try
            {
                return SendRequest(GetRestUrlEntry(cmd, new string[1] { slot }));
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ":\n" + ex.StackTrace);
                if (ex is SwitchConnectionFailure || ex is SwitchConnectionDropped || ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure)
                {
                    if (ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure) this.SwitchModel.Status = SwitchStatus.LoginFail;
                    else this.SwitchModel.Status = SwitchStatus.Unreachable;
                    return null;
                }
                else
                {
                    throw ex;
                }
            }
        }

        private Dictionary<string, string> PowerPort(RestUrlId cmd, string port)
        {
            try
            {
                return SendRequest(GetRestUrlEntry(cmd, new string[1] { port }));
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ":\n" + ex.StackTrace);
                if (ex is SwitchConnectionFailure || ex is SwitchConnectionDropped || ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure)
                {
                    if (ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure) this.SwitchModel.Status = SwitchStatus.LoginFail;
                    else this.SwitchModel.Status = SwitchStatus.Unreachable;
                    return null;
                }
                else
                {
                    throw ex;
                }
            }
        }

        private Dictionary<string, string> SetPoePriority(string port, PriorityLevelType priority)
        {
            try
            {
                return SendRequest(GetRestUrlEntry(RestUrlId.POWER_PRIORITY_PORT, new string[2] { port, priority.ToString() }));
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ":\n" + ex.StackTrace);
                if (ex is SwitchConnectionFailure || ex is SwitchConnectionDropped || ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure)
                {
                    if (ex is SwitchLoginFailure || ex is SwitchAuthenticationFailure) this.SwitchModel.Status = SwitchStatus.LoginFail;
                    else this.SwitchModel.Status = SwitchStatus.Unreachable;
                    return null;
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

        public void Write(string text)
        {
            try
            {
                Logger.Debug($"Writing: {text.Replace("\n", "\\n")}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ":\n" + ex.StackTrace);
                Callback.OnError(ex.Message);
            }
        }
        public void Write(byte[] bytes)
        {
            try
            {
                Logger.Debug($"Writing: {Encoding.Default.GetString(bytes).Replace("\n", "\\n")}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ":\n" + ex.StackTrace);
                Callback.OnError(ex.Message);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            return sb.ToString();
        }

        private RestUrlEntry GetRestUrlEntry(RestUrlId url)
        {
            return GetRestUrlEntry(url, new string[1] { null });
        }

        private RestUrlEntry GetRestUrlEntry(RestUrlId url, string[] data)
        {
            RestUrlEntry entry = new RestUrlEntry(url, 60, data)
            {
                Method = GetHttpMethod(url)
                //, Content = GetContent(RELEASE_8, url, data)
            };
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
            Dictionary<string, string> result = CliParseUtils.ParseXmlToDictionary(response[RESULT], "//nodes//result//*");
            if (result != null && result.ContainsKey(OUTPUT) && !string.IsNullOrEmpty(result[OUTPUT]))
            {
                response[RESULT] = result[OUTPUT];
            }
            return response;
        }

    }

}

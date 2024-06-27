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
        public bool IsReady { get; set; } = false;
        public int Timeout { get; set; }
        public ResultCallback Callback { get; set; }
        public SwitchModel SwitchModel { get; set; }
        public RestApiClient RestApiClient { get; set; }

        public RestApiService()
        {
            SwitchModel = new SwitchModel();
        }
        public RestApiService(SwitchModel device)
        {
            this.SwitchModel = device;
            this.RestApiClient = new RestApiClient(SwitchModel);
            this.IsReady = false;
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
                this.IsReady = true;
                Logger.Debug($"Connecting Rest API");
                RestApiClient.Login();
                this.SwitchModel = RestApiClient.SwitchInfo;
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_SYSTEM));
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_CHASSIS));
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_PORTS_LIST));
                this._response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_LAN_POWER, new string[1] { "1/1" }));
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

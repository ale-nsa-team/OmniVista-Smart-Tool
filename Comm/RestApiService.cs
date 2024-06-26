using PoEWizard.Data;
using PoEWizard.Device;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using static PoEWizard.Data.RestUrl;
using static System.Net.Mime.MediaTypeNames;

namespace PoEWizard.Comm
{
    public class RestApiService
    {
        public bool IsReady { get; set; } = false;
        public int Timeout { get; set; }
        public ResultCallback Callback { get; set; }
        public SwitchInfo SwitchInfo { get; set; }
        public RestApiClient RestApiClient { get; set; }

        public RestApiService()
        {
        }
        public RestApiService(SwitchInfo device)
        {
            this.SwitchInfo = device;
        }
        public RestApiService(string ipAddr, string username, string password, int cnxTimeout)
        {
            this.SwitchInfo = new SwitchInfo(ipAddr, username, password, cnxTimeout);
            this.RestApiClient = new RestApiClient(SwitchInfo);
            this.IsReady = false;
        }

        public void Connect()
        {
            try
            {
                this.IsReady = true;
                Logger.Debug($"Connecting Rest API");
                RestApiClient.Login();
                this.SwitchInfo = RestApiClient.SwitchInfo;
                Dictionary<string, string> response = SendRequest(GetRestUrlEntry(RestUrlId.SHOW_SYSTEM));
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + ":\n" + e.StackTrace);
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
                Method = GetHttpMethod(RELEASE_8, url),
                Content = GetContent(RELEASE_8, url, data)
            };
            return entry;
        }

        private Dictionary<string, string> SendRequest(RestUrlEntry entry)
        {
            Dictionary<string, string> response = this.RestApiClient.SendRequest(entry);
            return response;
        }

    }

}

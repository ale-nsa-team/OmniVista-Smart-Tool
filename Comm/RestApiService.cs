using PoEWizard.Data;
using PoEWizard.Device;
using System;
using System.IO.Ports;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace PoEWizard.Comm
{
    public class RestApiService
    {
        public bool IsReady { get; set; } = false;
        public int Timeout { get; set; }
        public ResultCallback Callback { get; set; }
        public DeviceModel SwitchInfo { get; set; }

        public RestApiService()
        {
        }
        public RestApiService(DeviceModel device)
        {
            SwitchInfo = device;
        }
        public RestApiService(string ipAddr, int br, int db, string p, string sb, string hs)
        {
            SwitchInfo = new DeviceModel();
            IsReady = false;
        }

        public void Connect()
        {
            try
            {
                IsReady = true;
                Logger.Debug($"Connecting Rest API");
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
        private void SpDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPort sp = (SerialPort)sender;
                string data = sp.ReadExisting();
                //Logger.Debug($"Data received: {data}");
                if (Callback != null) Callback.OnData(data);
            }
            catch (Exception ex)
            {
                Logger.Error("Serial Port error", ex);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            return sb.ToString();
        }
    }

}

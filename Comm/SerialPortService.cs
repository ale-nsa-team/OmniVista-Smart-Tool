using PoEWizard.Data;
using System;
using System.IO.Ports;
using System.Text;

namespace PoEWizard.Comm
{
    public class SerialPortService : IComService
    {
        private readonly SerialPort serialPort;
        public bool IsReady { get; set; } = false;
        public string PortName
        {
            get { return serialPort.PortName; }
            set
            {
                try
                {
                    if (value != string.Empty)
                    {
                        if (serialPort.IsOpen) serialPort.Close();
                        serialPort.PortName = value;
                        serialPort.Open();
                        IsReady = true;
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message + ":\n" + e.StackTrace);
                }
            }
        }
        public int BaudRate
        {
            get { return serialPort.BaudRate; }
            set
            {
                try
                {
                    serialPort.BaudRate = value;
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message + ":\n" + e.StackTrace);
                }
            }
        }
        public int DataBits
        {
            get { return serialPort.DataBits; }
            set { serialPort.DataBits = (value - 4) * (9 - value) > 0 ? value : 8; }
        }
        public string Parity
        {
            get { return serialPort.Parity.ToString(); }
            set
            {
                Enum.TryParse<Parity>(value, out Parity parity);
                serialPort.Parity = parity;
            }
        }
        public string StopBits
        {
            get { return serialPort.StopBits.ToString(); }
            set
            {
                Enum.TryParse<StopBits>(value, out StopBits stopBits);
                serialPort.StopBits = stopBits;
            }
        }
        public string Handshake
        {
            get { return serialPort.Handshake.ToString(); }
            set
            {
                Enum.TryParse<Handshake>(value, out Handshake handshake);
                serialPort.Handshake = handshake;
            }
        }
        public int Timeout
        {
            get { return serialPort.WriteTimeout; }
            set
            {
                try
                {
                    //here we set only the write timeout as read timeout is set on the commander
                    serialPort.WriteTimeout = value;
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message + ":\n" + e.StackTrace);
                }
            }
        }
        public bool Connected => serialPort.IsOpen;
        public ResultCallback Callback { get; set; }

        public SerialPortService() : this(string.Empty) { }
        public SerialPortService(string portName) : this(portName, Constants.DEFAULT_BAUD_RATE, 8, "None", "One", "None") { }
        public SerialPortService(string portName, int br, int db, string p, string sb, string hs)
        {
            serialPort = new SerialPort();
            IsReady = false;
            PortName = portName;
            BaudRate = br;
            DataBits = db;
            Parity = p;
            StopBits = sb;
            Handshake = hs;
            serialPort.DataReceived += new SerialDataReceivedEventHandler(SpDataReceived);
        }

        public void Connect()
        {
            try
            {
                if (IsReady) serialPort.Open();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message + ":\n" + e.StackTrace);
            }
        }
        public void Close()
        {
            if (serialPort.IsOpen) serialPort.Close();
        }

        public void Write(string text)
        {
            try
            {
                if (!serialPort.IsOpen) serialPort.Open();
                Logger.Debug($"Writing: {text.Replace("\n", "\\n")}");
                serialPort.Write(text);
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
                if (!serialPort.IsOpen) serialPort.Open();
                Logger.Debug($"Writing: {Encoding.Default.GetString(bytes).Replace("\n", "\\n")}");
                serialPort.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ":\n" + ex.StackTrace);
                Callback.OnError(ex.Message);
            }
        }

        public static string[] GetParities()
        {
            return Enum.GetNames(typeof(Parity));
        }

        public static string[] GetStopBits()
        {
            return Enum.GetNames(typeof(StopBits));
        }

        public static string[] GetHandShakes()
        {
            return Enum.GetNames(typeof(Handshake));
        }
        public static string[] FindPorts()
        {
            /*         using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE ConfigManagerErrorCode = 0"))
                       {
                           try
                           {
                               return searcher.Get().OfType<ManagementBaseObject>()
                                   .Select(port => Regex.Match(port["Caption"].ToString(), @"\((COM\d*)\)"))
                                   .Where(match => match.Groups.Count >= 2)
                                   .Select(match => match.Groups[1].Value).ToArray();
                           }
                           catch (Exception ex)
                           {
                               Logger.Error("No COM ports found" + ex.ToString());
                           }
                           return new string[] { };
                       }*/
            try
            {
                return SerialPort.GetPortNames();
            }
            catch (Exception ex)
            {
                Logger.Error("No COM ports found: " + ex.ToString());
            }
            return new string[] { };
        }
        public void ClearBuffer()
        {
            if (!serialPort.IsOpen) return;
            try
            {
                if (serialPort.BytesToRead > 0)
                {
                    string data = serialPort.ReadExisting();
                }
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
            }
            catch { }
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

        public string ToShortString()
        {
            int isb = (int)serialPort.StopBits;
            string sb = isb < 3 ? isb.ToString() : "1,5";
            string fc = Handshake != "None" ? "-" + Handshake.Replace("RequestToSend", "RTS").Replace("RTSXOnXOf", "RTS/XOnXOff") : "";
            return $"{PortName}: {BaudRate}-{DataBits}{Parity[0]}{sb}{fc}";
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\tCOM Port: " + PortName);
            sb.Append("\n\tBaud Rate: " + BaudRate);
            sb.Append("\n\tData Bits: " + DataBits);
            sb.Append("\n\tParity: " + Parity);
            sb.Append("\n\tStop Bits: " + StopBits);
            sb.Append("\n\tFlow Control: " + Handshake);

            return sb.ToString();
        }
    }
}

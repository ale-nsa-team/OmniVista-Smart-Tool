using PoEWizard.Device;
using System;
using System.Collections.Generic;
using System.Text;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.Utils;

namespace PoEWizard.Data
{
    public class TrafficReport
    {

        const string HEADER = "Port,Alias,Rx Rate (Kbps),Tx Rate (Kbps),#Rx Broadcast Frames,#Rx Unicast Frames,Rx Broadcast/Unicast (%),#Rx Multicast Frames," +
            "Rx Unicast+Multicast Rate (Kbps),#Rx Lost Frames,#Rx CRC Error,#Rx Alignments Error,#Tx Broadcast Frames,#Tx Unicast Frames,Tx Broadcast/Unicast (%)," +
            "#Tx Multicast Frames,Tx Unicast+Multicast Rate (Kbps),#Tx Lost Frames,#Tx Collided Frames,#Tx Collisions,#Tx Late Collisions,#Tx Excessive Collisions," +
            "Device Type,Vendor,MAC Address List";
        const string HEADER_DEVICE = "Port,Alias,Type,Name,Description,IP Address,Vendor,Model,Software Version,Serial Number,MAC Address";
        const double MAX_PERCENT_BROADCAST = 2.0;
        const double MAX_PERCENT_RATE = 70;
        const double MAX_PERCENT_WARNING_LOST_FRAMES = 5;
        const double MAX_PERCENT_CRITICAL_LOST_FRAMES = 8;
        const double MIN_UNICAST_RATE_KBPS = 301;
        const double MIN_NB_BROADCAST_FRAMES = 300;

        private PortTrafficModel trafficPort;
        private Dictionary<string, string> alertReport;
        private readonly Dictionary<string, PortModel> switchPorts;
        private readonly List<string> chassisInfo = new List<string>();

        private double rxBroadCast = 0;
        private double rxUniCast = 0;
        private double rxBroadCastPercent = 0;
        private double rxMultiCast = 0;
        private double rxUnicastMultiCastRate = 0;
        private double rxLostFrames = 0;
        private double rxCrcError = 0;
        private double rxAlignments = 0;

        private double txBroadCast = 0;
        private double txUniCast = 0;
        private double txBroadCastPercent = 0;
        private double txMultiCast = 0;
        private double txUnicastMultiCastRate = 0;
        private double txLostFrames = 0;

        private double txCollisions = 0;

        public string Summary { get; set; }
        public StringBuilder Data { get; set; }
        public DateTime TrafficStartTime { get; set; }
        public SwitchTrafficModel SwitchTraffic { get; set; }
        public string SelectedDuration { get; set; }


        public double TrafficDuration { get; set; }
        public TrafficReport(SwitchTrafficModel switchTraffic, int selectedDur)
        {
            this.Summary = string.Empty;
            this.Data = null;
            this.alertReport = new Dictionary<string, string>();
            this.SwitchTraffic = switchTraffic;
            this.TrafficStartTime = switchTraffic.StartTime;
            this.switchPorts = new Dictionary<string, PortModel>();
            foreach (ChassisModel chassis in SwitchTraffic.ChassisList)
            {
                this.chassisInfo.Add($"Chassis {chassis.Number}: Model {chassis.Model}, Serial Number {chassis.SerialNumber}");
                foreach (SlotModel slot in chassis.Slots)
                {
                    foreach (PortModel port in slot.Ports)
                    {
                        if (port.MacList?.Count > 0) this.switchPorts[port.Name] = port;
                    }
                }
            }
            int selDuration;
            string unit;
            if (selectedDur >= 60 && selectedDur < 3600)
            {
                unit = MINUTE;
                selDuration = selectedDur / 60;
            }
            else
            {
                unit = HOUR;
                selDuration = selectedDur / 3600;
            }
            this.SelectedDuration = $"{selDuration} {unit}";
            if (selDuration > 1) this.SelectedDuration += "s";
        }

        public void Complete(string completion, string ddmReport)
        {
            this.Summary = $"Traffic analysis {completion}:";
            this.Summary += $"\r\n  Switch: {this.SwitchTraffic.Name} ({this.SwitchTraffic.IpAddress})";
            this.Summary += $"\r\n  {string.Join("\r\n  ", this.chassisInfo)}";
            this.Summary += $"\r\n  Date: {this.TrafficStartTime:MM/dd/yyyy h:mm:ss tt}";
            this.Summary += $"\r\n  Selected duration: {this.SelectedDuration}";
            this.Summary += $"\r\n  Actual duration: {CalcStringDuration(TrafficStartTime, true)}";
            this.Summary += $"\r\n\r\nNote: This tool can detect common network issues, but is not a substitute for long term monitoring and human interpretation.";
            this.Summary += $"\r\n      Your results may vary and will change over time.";
            this.Summary += $"\r\n\r\nTraffic Alert:\r\n";
            this.TrafficDuration = DateTime.Now.Subtract(this.SwitchTraffic.StartTime).TotalSeconds;
            BuildReportData();
            BuildLldpDevicesReport();
            if (!string.IsNullOrEmpty(ddmReport)) this.Data.Append("\r\n\r\n\r\n").Append(ddmReport);
        }

        private void BuildReportData()
        {
            this.Data = new StringBuilder($"Switch, ").Append(this.SwitchTraffic.Name).Append(" ").Append(this.SwitchTraffic.IpAddress);
            this.Data.Append("\r\n").Append(string.Join("\r\n", this.chassisInfo).Replace(":", ","));
            this.Data.Append("\r\nDate,").Append($" {this.TrafficStartTime:MM/dd/yyyy h:mm:ss tt}");
            this.Data.Append($"\r\nSelected duration, ").Append(this.SelectedDuration);
            this.Data.Append($"\r\nActual duration, ").Append(CalcStringDuration(TrafficStartTime, true));
            this.Data.Append("\r\n\r\n\r\n").Append(HEADER);
            this.alertReport = new Dictionary<string, string>();
            foreach (KeyValuePair<string, PortTrafficModel> keyVal in this.SwitchTraffic.Ports)
            {
                this.trafficPort = keyVal.Value;
                if (this.switchPorts.ContainsKey(this.trafficPort.Port))
                {
                    this.trafficPort.IsUplink = this.switchPorts[this.trafficPort.Port].IsUplink;
                    this.trafficPort.MacList = this.switchPorts[this.trafficPort.Port].MacList;
                    this.trafficPort.EndPointDevice = this.switchPorts[this.trafficPort.Port].EndPointDevice;
                }
                if (this.trafficPort.MacList == null || this.trafficPort.MacList.Count < 1) continue;

                // Port,Alias
                this.Data.Append("\r\n ").Append(this.trafficPort.Port).Append(",\"").Append(this.switchPorts[this.trafficPort.Port].Alias).Append("\"");
                // ,Rx Rate (Kbps)
                ParseTrafficRate("Rx Rate", this.trafficPort.RxBytes);
                // ,Tx Rate (Kbps)
                ParseTrafficRate("Tx Rate", this.trafficPort.TxBytes);
                CalculateTrafficData();

                #region RX Traffic data
                // #Rx Broadcast Frames,#Rx Unicast Frames
                this.Data.Append(",").Append(this.rxBroadCast).Append(",").Append(this.rxUniCast);
                // ,Rx Broadcast/Unicast (%)
                if (this.rxBroadCastPercent > 0 && this.rxUnicastMultiCastRate >= MIN_UNICAST_RATE_KBPS) this.Data.Append(",").Append(this.rxBroadCastPercent); else this.Data.Append(",");
                // ,#Rx Multicast Frames,Rx Unicast+Multicast Rate (Kbps),#Rx Lost Frames
                this.Data.Append(",").Append(this.rxMultiCast).Append(",").Append(this.rxUnicastMultiCastRate).Append(",").Append(this.rxLostFrames);
                // ,#Rx CRC Error,#Rx Alignments Error
                this.Data.Append(",").Append(this.rxCrcError).Append(",").Append(this.rxAlignments);
                #endregion

                #region TX Traffic data
                // ,#Tx Broadcast Frames,#Tx Unicast Frames
                this.Data.Append(",").Append(this.txBroadCast).Append(",").Append(this.txUniCast);
                // ,Tx Broadcast/Unicast (%)
                if (this.txBroadCastPercent > 0 && this.txUnicastMultiCastRate >= MIN_UNICAST_RATE_KBPS) this.Data.Append(",").Append(this.txBroadCastPercent); else this.Data.Append(",");
                // ,#Tx Multicast Frames,Tx Unicast+Multicast Rate (Kbps),#Tx Lost Frames
                this.Data.Append(",").Append(this.txMultiCast).Append(",").Append(this.txUnicastMultiCastRate).Append(",").Append(this.txLostFrames);
                // ,#Tx Collided Frames,#Tx Collisions
                this.Data.Append(",").Append(GetDiffTrafficSamples(this.trafficPort.TxCollidedFrames)).Append(",").Append(GetDiffTrafficSamples(this.trafficPort.TxCollisions));
                // ,#Tx Late Collisions,#Tx Excessive Collisions
                this.Data.Append(",").Append(GetDiffTrafficSamples(this.trafficPort.TxLateCollisions)).Append(",").Append(GetDiffTrafficSamples(this.trafficPort.TxExcCollisions));
                #endregion

                // ,Device Type,Vendor;
                if (this.trafficPort.MacList?.Count > 0 && this.trafficPort.EndPointDevice != null && !IsDeviceTypeUnknown(this.trafficPort.EndPointDevice))
                {
                    this.Data.Append(",\"").Append(this.trafficPort.EndPointDevice.Type).Append("\",\"").Append(this.trafficPort.EndPointDevice.Vendor).Append("\"");
                }
                else
                {
                    this.Data.Append(",,");
                }
                // ,MAC Address List
                this.Data.Append(",\"").Append(PrintMacAdresses()).Append("\"");
                ParseAlertConditions();
            }
            if (this.alertReport?.Count > 0) foreach (KeyValuePair<string, string> keyVal in this.alertReport) this.Summary += keyVal.Value;
            else
            {
                this.Summary += $"\r\nNo traffic anomalies detected:";
                this.Summary += $"\r\n Rx Rate was less than {MAX_PERCENT_RATE}% of the Bandwidth.";
                this.Summary += $"\r\n Tx Rate was less than {MAX_PERCENT_RATE}% of the Bandwidth.";
                this.Summary += $"\r\n #Rx Broadcast Frames was less than {MAX_PERCENT_BROADCAST}% of #Rx Unicast Frames.";
                this.Summary += $"\r\n #Rx Lost Frames was less than {MAX_PERCENT_WARNING_LOST_FRAMES}% of #Rx Unicast and Multicast Frames.";
                this.Summary += $"\r\n #Tx Broadcast Frames was less than {MAX_PERCENT_BROADCAST}% of #Tx Unicast Frames.";
                this.Summary += $"\r\n #Tx Lost Frames was less than {MAX_PERCENT_WARNING_LOST_FRAMES}% of #Tx Unicast and Multicast Frames";
            }
            this.Summary += "\r\n";
        }

        private void CalculateTrafficData()
        {

            #region Calculation of RX Traffic data
            this.rxBroadCast = GetDiffTrafficSamples(this.trafficPort.RxBroadcastFrames);
            this.rxUniCast = GetDiffTrafficSamples(this.trafficPort.RxUnicastFrames);
            this.rxMultiCast = GetDiffTrafficSamples(this.trafficPort.RxMulticastFrames);
            this.rxUnicastMultiCastRate = GetUnicastMulticastRate(this.rxUniCast, this.rxMultiCast);
            this.rxBroadCastPercent = CalcPercent(this.rxBroadCast, this.rxUniCast, 2);
            this.rxLostFrames = GetDiffTrafficSamples(this.trafficPort.RxLostFrames);
            this.rxCrcError = GetDiffTrafficSamples(this.trafficPort.RxCrcErrorFrames);
            this.rxAlignments = GetDiffTrafficSamples(this.trafficPort.RxAlignmentsError);
            #endregion

            #region Calculation of TX Traffic data
            this.txBroadCast = GetDiffTrafficSamples(this.trafficPort.TxBroadcastFrames);
            this.txUniCast = GetDiffTrafficSamples(this.trafficPort.TxUnicastFrames);
            this.txMultiCast = GetDiffTrafficSamples(this.trafficPort.TxMulticastFrames);
            this.txUnicastMultiCastRate = GetUnicastMulticastRate(this.txUniCast, this.txMultiCast);
            this.txBroadCastPercent = CalcPercent(this.txBroadCast, this.txUniCast, 2);
            this.txLostFrames = GetDiffTrafficSamples(this.trafficPort.TxLostFrames);
            this.txCollisions = GetDiffTrafficSamples(this.trafficPort.TxCollidedFrames) + GetDiffTrafficSamples(this.trafficPort.TxCollisions) +
                                GetDiffTrafficSamples(this.trafficPort.TxLateCollisions) + GetDiffTrafficSamples(this.trafficPort.TxExcCollisions);
            #endregion
        }

        private double GetUnicastMulticastRate(double uniCast, double multicast)
        {
            return RoundUp((uniCast + multicast) * 8 / this.TrafficDuration, 2);
        }

        private void ParseAlertConditions()
        {
            AddAlertBroadcastLostFrames("Rx");
            AddAlertBroadcastLostFrames("Tx");
            if (this.rxCrcError > 1) AddPortAlert($"#Rx CRC Error detected: {this.rxCrcError}");
            if (this.txCollisions > 0) AddPortAlert($"#Tx Collisions detected: {this.txCollisions}");
            if (this.rxAlignments > 1) AddPortAlert($"#Rx Alignments Error detected: {this.rxAlignments}");
            if (this.alertReport?.Count > 0 && this.alertReport.ContainsKey(this.trafficPort.Port) && this.trafficPort.MacList?.Count > 0)
            {
                AddPortAlert(PrintMacAdresses("MAC Address"));
            }
        }

        private void AddAlertBroadcastLostFrames(string source)
        {
            double val1 = 0;
            double val2 = 0;
            if (this.trafficPort.IsUplink)
            {
                double percent = 0;
                if (source == "Rx")
                {
                    val1 = this.rxBroadCast;
                    val2 = this.rxUniCast;
                    percent = this.rxUnicastMultiCastRate < MIN_UNICAST_RATE_KBPS ? 0 : this.rxBroadCastPercent;
                }
                else if (source == "Tx")
                {
                    val1 = this.txBroadCast;
                    val2 = this.txUniCast;
                    percent = this.txUnicastMultiCastRate < MIN_UNICAST_RATE_KBPS ? 0 : this.txBroadCastPercent;
                }
                if (percent > MAX_PERCENT_BROADCAST && val1 > MIN_NB_BROADCAST_FRAMES)
                {
                    AddPortAlert($"#{source} Broadcast Frames ({val1}) > {MAX_PERCENT_BROADCAST}% of #{source} Unicast Frames ({val2}), Percentage: {percent}%");
                }
            }
            if (source == "Rx")
            {
                val1 = this.rxLostFrames;
                val2 = this.rxUniCast + this.rxMultiCast;
            }
            else if (source == "Tx")
            {
                val1 = this.txLostFrames;
                val2 = this.txUniCast + this.txMultiCast;
            }
            else
            {
                return;
            }
            if (!AddAlertPercent($"#Critical {source} Lost Frames", val1, $"#{source} Unicast and Multicast Frames", val2, MAX_PERCENT_CRITICAL_LOST_FRAMES))
            {
                AddAlertPercent($"#Warning {source} Lost Frames", val1, $"#{source} Unicast and Multicast Frames", val2, MAX_PERCENT_WARNING_LOST_FRAMES);
            }
        }

        private bool AddAlertPercent(string label1, double val1, string label2, double val2, double maxPercent)
        {
            double percent = CalcPercent(val1, val2, 2);
            if (percent >= maxPercent)
            {
                AddPortAlert($"{label1} ({val1}) > {maxPercent}% of {label2} ({val2}), Percentage: {percent}%");
                return true;
            }
            return false;
        }

        private string PrintMacAdresses(string title = null)
        {
            string text = string.IsNullOrEmpty(title) ? string.Empty : title;
            if (this.trafficPort.MacList?.Count > 1)
            {
                if (!string.IsNullOrEmpty(title)) text += "es: ";
                int cnt = 0;
                foreach (string mac in this.trafficPort.MacList)
                {
                    cnt++;
                    if (cnt > 1)
                    {
                        if (string.IsNullOrEmpty(title)) text += ", ";
                        else
                        {
                            if (cnt % 5 == 0) text += "\n\t  "; else text += ", ";
                        }
                    }
                    text = AddVendorName(text, mac);
                }
                if (this.trafficPort.MacList.Count > 9)
                {
                    if (!string.IsNullOrEmpty(title)) text += "\n\t\t..."; else text += " ...";
                }
            }
            else if (this.trafficPort.MacList?.Count > 0)
            {
                if (!string.IsNullOrEmpty(title)) text += ": ";
                string mac;
                mac = this.trafficPort.MacList[0];
                text = AddVendorName(text, mac);
            }
            return text;
        }

        private string AddVendorName(string text, string mac)
        {
            text += mac;
            string vendor = string.Empty;
            if (string.IsNullOrEmpty(this.trafficPort.EndPointDevice.Vendor) || this.trafficPort.MacList?.Count > 1) vendor = GetVendorName(mac);
            if (!string.IsNullOrEmpty(vendor) && !IsValidMacAddress(vendor)) text += $" ({vendor})";
            return text;
        }

        private void ParseTrafficRate(string title, List<double> samples)
        {
            double traffRate = AddTrafficRate(samples);
            this.Data.Append(",");
            if (traffRate > 0)
            {
                this.Data.Append(traffRate);
                double origTraffRate = traffRate;
                traffRate /= 1024;
                double percent = CalcPercent(traffRate, this.trafficPort.BandWidth, 2);
                if (percent >= MAX_PERCENT_RATE)
                {
                    string txt1 = $"{origTraffRate} Kbps";
                    string txt2 = $"{this.trafficPort.BandWidth * 1000} Kbps";
                    if (origTraffRate >= 1024)
                    {
                        txt1 = $"{RoundUp(origTraffRate / 1024, 2)} Mbps";
                        txt2 = $"{this.trafficPort.BandWidth} Mbps";
                    }
                    AddPortAlert($"{title} ({txt1}) > {MAX_PERCENT_RATE}% of Bandwidth ({txt2}), Percentage: {percent}%");
                }
            }
        }

        private void AddPortAlert(string alertMsg)
        {
            if (!this.alertReport.ContainsKey(this.trafficPort.Port)) AddAlert($"\n  Port {this.trafficPort.Port}:\n\t{alertMsg}"); else AddAlert($"\n\t{alertMsg}");
        }

        private void AddAlert(string alertMsg)
        {
            string txt = string.Empty;
            if (this.alertReport.ContainsKey(this.trafficPort.Port)) txt = this.alertReport[this.trafficPort.Port];
            txt += $"{alertMsg}";
            this.alertReport[this.trafficPort.Port] = txt;
        }

        private double AddTrafficRate(List<double> nbBytes)
        {
            double dVal = (GetDiffTrafficSamples(nbBytes) * 8 / this.TrafficDuration) / 1024;
            double avg = Math.Round(dVal, 2, MidpointRounding.ToEven);
            return avg;
        }

        private double GetDiffTrafficSamples(List<double> list)
        {
            if (list?.Count > 0) return list[list.Count - 1] - list[0];
            return 0;
        }

        private void BuildLldpDevicesReport()
        {
            if (this.switchPorts?.Count > 0)
            {
                this.Data.Append("\r\n\r\n\r\n").Append(HEADER_DEVICE);
                foreach (KeyValuePair<string, PortModel> keyVal in this.switchPorts)
                {
                    PortModel port = keyVal.Value;
                    if (port == null || port.EndPointDevice == null || string.IsNullOrEmpty(port.EndPointDevice.Type)) continue;
                    EndPointDeviceModel device = port.EndPointDevice;
                    if (IsDeviceTypeUnknown(device)) continue;
                    // Port,Alias,Type
                    this.Data.Append("\n ").Append(port.Name).Append(",\"").Append(GetDeviceInfo(port.Alias)).Append("\",\"").Append(GetDeviceInfo(device.Type)).Append("\"");
                    // ,Name,Description
                    this.Data.Append(",\"").Append(GetDeviceInfo(device.Name)).Append("\",\"").Append(GetDeviceInfo(device.Description)).Append("\"");
                    // ,IP Address,Vendor
                    this.Data.Append(",\"").Append(GetDeviceInfo(device.IpAddress)).Append("\",\"").Append(GetDeviceInfo(device.Vendor)).Append("\"");
                    // ,Model,Software Version
                    this.Data.Append(",\"").Append(GetDeviceInfo(device.Model)).Append("\",\"").Append(GetDeviceInfo(device.SoftwareVersion)).Append("\"");
                    // ,Serial Number";
                    this.Data.Append(",\"").Append(GetDeviceInfo(device.SerialNumber)).Append("\"");
                    // ,MAC Address
                    this.Data.Append(",\"");
                    if (!string.IsNullOrEmpty(device.MacAddress))
                    {
                        StringBuilder sb = new StringBuilder();
                        if (device.MacAddress.Contains(","))
                        {
                            string[] split = device.MacAddress.Split(',');
                            foreach (string mac in split)
                            {
                                if (sb.Length > 0) sb.Append(",");
                                string vendor = GetVendorName(mac);
                                if (!string.IsNullOrEmpty(vendor) && !IsValidMacAddress(vendor))
                                {
                                    sb.Append(mac).Append(" (").Append(vendor).Append(")");
                                }
                                else sb.Append(mac);
                            }
                            this.Data.Append(sb);
                        }
                        else this.Data.Append(device.MacAddress);
                    }
                    this.Data.Append("\"");
                }
            }
        }

        private string GetDeviceInfo(string info)
        {
            return !string.IsNullOrEmpty(info) ? info : "-";
        }
        private bool IsDeviceTypeUnknown(EndPointDeviceModel device)
        {
            return device == null || string.IsNullOrEmpty(device.Type) || device.Type == NO_LLDP || device.Type == MED_UNKNOWN || device.Type == MED_UNSPECIFIED;
        }
    }

}

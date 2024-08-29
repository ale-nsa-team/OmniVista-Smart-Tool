using PoEWizard.Device;
using System;
using System.Collections.Generic;
using System.Text;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Data
{
    public class TrafficReport
    {

        const string HEADER = "Port,Rx Rate (Kbps),Tx Rate (Kbps),#Rx Broadcast Frames,#Rx Unicast Frames,Rx Broadcast/Unicast (%),#Rx Multicast Frames,#Rx Lost Frames,#Rx CRC Error,#Rx Alignments Error," +
            "#Tx Broadcast Frames,#Tx Unicast Frames,Tx Broadcast/Unicast (%),#Tx Multicast Frames,#Tx Lost Frames,#Tx Collided Frames,#Tx Collisions,#Tx Late Collisions,#Tx Excessive Collisions," +
            "Device Type,Vendor,MAC Address List";
        const string HEADER_DEVICE = "Port,Type,Name,Description,IP Address,Vendor,Model,Software Version,Serial Number,MAC Address";
        const double MAX_PERCENT_BROADCAST = 0.5;
        const double MAX_PERCENT_RATE = 70;
        const double MAX_PERCENT_WARNING_LOST_FRAMES = 5;
        const double MAX_PERCENT_CRITICAL_LOST_FRAMES = 8;
        const double MIN_NB_BROADCAST_FRAMES = 300;
        const double MIN_UNICAST_RATE_KBPS = 301;

        private PortTrafficModel trafficPort;
        private Dictionary<string, string> alertReport;
        private readonly Dictionary<string, PortModel> switchPorts;

        private double rxBroadCast = 0;
        private double rxUniCast = 0;
        private string rxBroadCastPercent = string.Empty;
        private double rxMultiCast = 0;
        private double rxLostFrames = 0;
        private double rxCrcError = 0;
        private double rxAlignments = 0;

        private double txBroadCast = 0;
        private double txUniCast = 0;
        private string txBroadCastPercent = string.Empty;
        private double txMultiCast = 0;
        private double txLostFrames = 0;

        private double txCollisions = 0;

        public string Summary { get; set; }
        public StringBuilder Data { get; set; }
        public DateTime TrafficStartTime { get; set; }
        public SwitchTrafficModel SwitchTraffic { get; set; }


        public double TrafficDuration { get; set; }
        public TrafficReport(SwitchTrafficModel switchTraffic, string completion, int selectedDur)
        {
            this.Summary = string.Empty;
            this.Data = null;
            this.alertReport = new Dictionary<string, string>();
            this.SwitchTraffic = switchTraffic;
            this.TrafficStartTime = switchTraffic.StartTime;
            this.switchPorts = new Dictionary<string, PortModel>();
            foreach (ChassisModel chassis in SwitchTraffic.ChassisList)
            {
                foreach (SlotModel slot in chassis.Slots)
                {
                    foreach (PortModel port in slot.Ports)
                    {
                        if (port.MacList?.Count > 0) this.switchPorts[port.Name] = port;
                    }
                }
            }
            this.Summary = $"Traffic analysis {completion}:";
            int selectedDuration;
            string unit;
            if (selectedDur >= 60 && selectedDur < 3600)
            {
                unit = MINUTE;
                selectedDuration = selectedDur / 60;
            }
            else
            {
                unit = HOUR;
                selectedDuration = selectedDur / 3600;
            }
            this.Summary += $"\n  Selected duration: {selectedDuration} {unit}";
            if (selectedDuration > 1) this.Summary += "s";
            this.Summary += $"\n  Switch: {this.SwitchTraffic.Name} ({this.SwitchTraffic.IpAddress}), Serial Number: {this.SwitchTraffic.SerialNumber}";
            this.Summary += $"\n  Date: {this.TrafficStartTime:MM/dd/yyyy hh:mm:ss tt}";
            this.Summary += $"\n  Duration: {Utils.CalcStringDuration(TrafficStartTime, true)}\n\nTraffic Alert:\n";
            this.TrafficDuration = DateTime.Now.Subtract(this.SwitchTraffic.StartTime).TotalSeconds;
            BuildReportData();
            BuildLldpDevicesReport();
        }

        private void BuildReportData()
        {
            this.Data = new StringBuilder(HEADER);
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
                this.Data.Append("\r\n ").Append(this.trafficPort.Port);
                ParseTrafficRate("Rx Rate", this.trafficPort.RxBytes);
                ParseTrafficRate("Tx Rate", this.trafficPort.TxBytes);

                #region RX Traffic data
                // #Rx Broadcast Frames,#Rx Unicast Frames,Rx Broadcast/Unicast (%),#Rx Multicast Frames,#Rx Lost Frames,#Rx CRC Error,#Rx Alignments Error,
                this.rxBroadCast = GetDiffTrafficSamples(this.trafficPort.RxBroadcastFrames);
                this.Data.Append(",").Append(this.rxBroadCast);
                this.rxUniCast = GetDiffTrafficSamples(this.trafficPort.RxUnicastFrames);
                this.Data.Append(",").Append(this.rxUniCast);
                this.rxMultiCast = GetDiffTrafficSamples(this.trafficPort.RxMulticastFrames);
                this.rxBroadCastPercent = GetBroadcastPercent(this.rxBroadCast, this.rxUniCast, this.rxMultiCast);
                this.Data.Append(",").Append(this.rxBroadCastPercent);
                this.Data.Append(",").Append(this.rxMultiCast);
                this.rxLostFrames = GetDiffTrafficSamples(this.trafficPort.RxLostFrames);
                this.Data.Append(",").Append(this.rxLostFrames);
                this.rxCrcError = GetDiffTrafficSamples(this.trafficPort.RxCrcErrorFrames);
                this.Data.Append(",").Append(this.rxCrcError);
                this.rxAlignments = GetDiffTrafficSamples(this.trafficPort.RxAlignmentsError);
                this.Data.Append(",").Append(this.rxAlignments);
                #endregion

                #region TX Traffic data
                this.txBroadCast = GetDiffTrafficSamples(this.trafficPort.TxBroadcastFrames);
                this.Data.Append(",").Append(this.txBroadCast);
                this.txUniCast = GetDiffTrafficSamples(this.trafficPort.TxUnicastFrames);
                this.Data.Append(",").Append(this.txUniCast);
                this.txMultiCast = GetDiffTrafficSamples(this.trafficPort.TxMulticastFrames);
                this.txBroadCastPercent = GetBroadcastPercent(this.rxBroadCast, this.rxUniCast, this.rxMultiCast);
                this.Data.Append(",").Append(this.txBroadCastPercent);
                this.Data.Append(",").Append(this.txMultiCast);
                this.txLostFrames = GetDiffTrafficSamples(this.trafficPort.TxLostFrames);
                this.Data.Append(",").Append(this.txLostFrames);
                this.Data.Append(",").Append(GetDiffTrafficSamples(this.trafficPort.TxCollidedFrames));
                this.Data.Append(",").Append(GetDiffTrafficSamples(this.trafficPort.TxCollisions));
                this.Data.Append(",").Append(GetDiffTrafficSamples(this.trafficPort.TxLateCollisions));
                this.Data.Append(",").Append(GetDiffTrafficSamples(this.trafficPort.TxExcCollisions));
                this.txCollisions = GetDiffTrafficSamples(this.trafficPort.TxCollidedFrames) + GetDiffTrafficSamples(this.trafficPort.TxCollisions) +
                                    GetDiffTrafficSamples(this.trafficPort.TxLateCollisions) + GetDiffTrafficSamples(this.trafficPort.TxExcCollisions);
                #endregion

                this.Data.Append(",\"");
                if (this.trafficPort.MacList?.Count > 0 && this.trafficPort.EndPointDevice != null && !IsDeviceTypeUnknown(this.trafficPort.EndPointDevice))
                {
                    this.Data.Append(this.trafficPort.EndPointDevice.Type).Append("\",\"").Append(this.trafficPort.EndPointDevice.Vendor);
                }
                else
                {
                    this.Data.Append("\",\"");
                }
                this.Data.Append("\",\"").Append(PrintMacAdresses()).Append("\"");
                ParseAlertConditions();
            }
            if (this.alertReport?.Count > 0) foreach (KeyValuePair<string, string> keyVal in this.alertReport) this.Summary += keyVal.Value;
            else this.Summary += $"\nNo traffic anomalies detected.";
            this.Summary += "\n";
        }

        private string GetBroadcastPercent(double broadCast, double uniCast, double multicast)
        {
            double rate = ((uniCast + multicast) * 8) / this.TrafficDuration;
            if (uniCast > 0 && rate >= MIN_UNICAST_RATE_KBPS) return Utils.CalcPercent(broadCast, uniCast, 2).ToString();
            return string.Empty;
        }

        private void ParseAlertConditions()
        {
            if (this.rxBroadCast > MIN_NB_BROADCAST_FRAMES && this.trafficPort.IsUplink && !string.IsNullOrEmpty(this.rxBroadCastPercent))
            {
                AddPortAlert($"#Rx Broadcast Frames ({this.rxUniCast}) > {MAX_PERCENT_BROADCAST}% of #Rx Unicast Frames ({this.rxUniCast}), Percentage: {this.rxBroadCastPercent}%");
            }
            if (!AddAlertPercent(this.rxLostFrames, "Critical #Rx Lost Frames", this.rxUniCast + this.rxMultiCast, "#Rx Unicast and #Rx Multicast Frames", MAX_PERCENT_CRITICAL_LOST_FRAMES))
            {
                AddAlertPercent(this.rxLostFrames, "Warning #Rx Lost Frames", this.rxUniCast + this.rxMultiCast, "#Rx Unicast and #Rx Multicast Frames", MAX_PERCENT_WARNING_LOST_FRAMES);
            }
            if (this.txBroadCast > MIN_NB_BROADCAST_FRAMES && this.trafficPort.IsUplink && !string.IsNullOrEmpty(this.txBroadCastPercent))
            {
                AddPortAlert($"#Tx Broadcast Frames ({this.txUniCast}) > {MAX_PERCENT_BROADCAST}% of #Tx Unicast Frames ({this.txUniCast}), Percentage: {this.txBroadCastPercent}%");
            }
            if (!AddAlertPercent(this.txLostFrames, "Critical #Tx Lost Frames", this.txUniCast + this.txMultiCast, "#Tx Unicast and #Tx Multicast Frames", MAX_PERCENT_CRITICAL_LOST_FRAMES))
            {
                AddAlertPercent(this.rxLostFrames, "Warning #Tx Lost Frames", this.txUniCast + this.txMultiCast, "#Tx Unicast and #Rx Multicast Frames", MAX_PERCENT_WARNING_LOST_FRAMES);
            }
            if (this.rxCrcError > 1) AddPortAlert($"#Rx CRC Error detected: {this.rxCrcError}");
            if (this.txCollisions > 0) AddPortAlert($"#Tx Collisions detected: {this.txCollisions}");
            if (this.rxAlignments > 1) AddPortAlert($"#Rx Alignments Error detected: {this.rxAlignments}");
            if (this.alertReport?.Count > 0 && this.alertReport.ContainsKey(this.trafficPort.Port) && this.trafficPort.MacList?.Count > 0)
            {
                AddPortAlert(PrintMacAdresses("MAC Address"));
            }
        }

        private string PrintVendor()
        {
            return this.trafficPort.MacList?.Count == 1 ? Utils.GetVendorName(this.trafficPort.MacList[0]) : string.Empty;
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

        private string AddVendorName(string txt, string mac)
        {
            txt += mac;
            string vendor = string.Empty;
            if (string.IsNullOrEmpty(this.trafficPort.EndPointDevice.Vendor) || this.trafficPort.MacList?.Count > 1) vendor = Utils.GetVendorName(mac);
            if (!string.IsNullOrEmpty(vendor) && !Utils.IsValidMacAddress(vendor)) txt += $" ({vendor})";
            return txt;
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
                double percent = Utils.CalcPercent(traffRate, this.trafficPort.BandWidth, 2);
                if (percent >= MAX_PERCENT_RATE)
                {
                    string txt1 = $"{origTraffRate} Kbps";
                    string txt2 = $"{this.trafficPort.BandWidth * 1000} Kbps";
                    if (origTraffRate >= 1024)
                    {
                        txt1 = $"{Utils.RoundUp(origTraffRate / 1024, 2)} Mbps";
                        txt2 = $"{this.trafficPort.BandWidth} Mbps";
                    }
                    AddPortAlert($"{title} ({txt1}) > {MAX_PERCENT_RATE}% of Bandwidth ({txt2}), Percentage: {percent}%");
                }
            }
        }

        private bool AddAlertPercent(double val1, string label1, double val2, string label2, double maxPercent)
        {
            double percent = Utils.CalcPercent(val1, val2, 2);
            if (percent >= maxPercent)
            {
                AddPortAlert($"{label1} ({val1}) > {maxPercent}% of {label2} ({val2}), Percentage: {percent}%");
                return true;
            }
            return false;
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
                this.Data.Append("\n\n").Append(HEADER_DEVICE);
                foreach (KeyValuePair<string, PortModel> keyVal in this.switchPorts)
                {
                    if (keyVal.Value == null || keyVal.Value.EndPointDevice == null || string.IsNullOrEmpty(keyVal.Value.EndPointDevice.Type)) continue;
                    PortModel port = keyVal.Value;
                    EndPointDeviceModel device = port.EndPointDevice;
                    string macList = IsDeviceTypeUnknown(device) ? device.Name : string.Empty;
                    if (macList.Contains(",")) continue;
                    if (port.EndPointDevicesList.Count > 1) continue;
                    // HEADER_DEVICE = "Port,Type,Name,Description,IP Address,Vendor,Model,Software Version,Serial Number,MAC Address";
                    this.Data.Append("\n ").Append(port.Name).Append(",\"").Append(device.Type).Append("\"");
                    if (IsDeviceTypeUnknown(device))
                    {
                        this.Data.Append(",\"\",\"\",\"\"");
                    }
                    else
                    {
                        this.Data.Append(",\"").Append(device.Name).Append("\",\"").Append(device.Description).Append("\",\"").Append(device.IpAddress).Append("\"");
                    }
                    string vendor = !string.IsNullOrEmpty(device.Vendor) ? device.Vendor :
                                    !string.IsNullOrEmpty(device.MacAddress) ? Utils.GetVendorName(device.MacAddress) : string.Empty;
                    if (string.IsNullOrEmpty(vendor) && !string.IsNullOrEmpty(macList))
                    {
                        vendor = Utils.GetVendorName(macList);
                    }
                    this.Data.Append(",\"").Append(vendor).Append("\",\"").Append(device.Model).Append("\",\"").Append(device.SoftwareVersion).Append("\"");
                    this.Data.Append(",\"").Append(device.SerialNumber).Append("\",\"");
                    if (!string.IsNullOrEmpty(device.MacAddress)) this.Data.Append(device.MacAddress); else this.Data.Append(macList);
                    this.Data.Append("\"");
                }
            }
        }

        private bool IsDeviceTypeUnknown(EndPointDeviceModel device)
        {
            return device == null || string.IsNullOrEmpty(device.Type) || device.Type == MED_UNKNOWN;
        }
    }

}

using PoEWizard.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoEWizard.Data
{
    public class TrafficReport
    {

        const string HEADER = "Port,Rx Rate (Kbps),Tx Rate (Kbps),#Broadcast Frames,#Unicast Frames,Broadcast/Unicast (%),#Multicast Frames,#Lost Frames,#CRC Error,#Collisions,#Alignments,MAC Address List";
        const double MAX_PERCENT_BROADCAST = 0.5;
        const double MAX_PERCENT_RATE = 70;
        const double MAX_PERCENT_WARNING_LOST_FRAMES = 3;
        const double MAX_PERCENT_CRITICAL_LOST_FRAMES = 6;
        const double MIN_NB_BROADCAST_FRAMES = 300;

        private PortTrafficModel trafficPort;
        private Dictionary<string, string> alertReport;
        private Dictionary<string, List<string>> portsMacList = new Dictionary<string, List<string>>();
        private double broadCast =0;
        private double uniCast = 0;
        private double multiCast = 0;
        private double lostFrames = 0;
        private double crcError = 0;
        private double collisions = 0;
        private double alignments = 0;

        public string Summary { get; set; }
        public StringBuilder Data { get; set; }
        public DateTime TrafficStartTime { get; set; }

        public double TrafficDuration { get; set; }
        public TrafficReport()
        {
            this.Summary = string.Empty;
            this.Data = null;
            this.alertReport = new Dictionary<string, string>();
        }

        public void BuildReportData(SwitchTrafficModel switchTraffic, Dictionary<string, List<string>> portsMacList)
        {
            this.TrafficStartTime = switchTraffic.StartTime;
            if (portsMacList?.Count > 0) this.portsMacList = portsMacList;
            this.Summary = $"Traffic analysis completed on switch {switchTraffic.Name} ({switchTraffic.IpAddress}), Serial Number: {switchTraffic.SerialNumber}:";
            this.Summary += $"\nDate: {this.TrafficStartTime.ToString("MM/dd/yyyy hh:mm:ss tt")}";
            this.Summary += $"\nDuration: {Utils.CalcStringDuration(TrafficStartTime, true)}\n\nTraffic Alert:\n";
            this.TrafficDuration = DateTime.Now.Subtract(switchTraffic.StartTime).TotalSeconds;
            this.Data = new StringBuilder(HEADER);
            this.alertReport = new Dictionary<string, string>();
            foreach (KeyValuePair<string, PortTrafficModel> keyVal in switchTraffic.Ports)
            {
                this.trafficPort = keyVal.Value;
                if (this.portsMacList.ContainsKey(this.trafficPort.Port)) this.trafficPort.MacList = this.portsMacList[this.trafficPort.Port];
                this.Data.Append("\r\n ").Append(this.trafficPort.Port);
                ParseTrafficRate("Rx Rate", this.trafficPort.RxBytes);
                ParseTrafficRate("Tx Rate", this.trafficPort.TxBytes);
                this.broadCast = GetAvgTrafficSamples(this.trafficPort.BroadcastFrames);
                this.Data.Append(",").Append(this.broadCast);
                this.uniCast = GetAvgTrafficSamples(this.trafficPort.UnicastFrames);
                this.Data.Append(",").Append(uniCast);
                this.Data.Append(",").Append(Utils.CalcPercent(this.broadCast, this.uniCast, 2));
                this.multiCast = GetAvgTrafficSamples(this.trafficPort.MulticastFrames);
                this.Data.Append(",").Append(this.multiCast);
                this.lostFrames = GetAvgTrafficSamples(this.trafficPort.LostFrames);
                this.Data.Append(",").Append(this.lostFrames);
                this.crcError = GetMaxTrafficSamples(this.trafficPort.CrcErrorFrames);
                this.Data.Append(",").Append(this.crcError);
                this.collisions = GetMaxTrafficSamples(this.trafficPort.CollidedFrames) + GetMaxTrafficSamples(this.trafficPort.Collisions) +
                                  GetMaxTrafficSamples(this.trafficPort.ExcCollisions) + GetMaxTrafficSamples(this.trafficPort.LateCollisions);
                this.Data.Append(",").Append(this.collisions);
                this.alignments = GetMaxTrafficSamples(this.trafficPort.AlignmentsError);
                this.Data.Append(",").Append(this.alignments);
                this.Data.Append(",\"").Append(PrintMacAdresses()).Append("\"");
                ParseAlertConditions();
            }
            if (this.alertReport?.Count > 0) foreach (KeyValuePair<string, string> keyVal in this.alertReport) this.Summary += keyVal.Value;
            else this.Summary += $"\nNo traffic anomalies detected.";
        }

        private void ParseAlertConditions()
        {
            if (this.broadCast > MIN_NB_BROADCAST_FRAMES && this.trafficPort.MacList.Count > 1)
            {
                AddAlertPercent(this.broadCast, "#Broadcast Frames", this.uniCast, "#Unicast Frames", MAX_PERCENT_BROADCAST);
            }
            if (!AddAlertPercent(this.lostFrames, "Critical #Lost Frames", this.uniCast + this.multiCast, "#Unicast and #Multicast Frames", MAX_PERCENT_CRITICAL_LOST_FRAMES))
            {
                AddAlertPercent(this.lostFrames, "Warning #Lost Frames", this.uniCast + this.multiCast, "#Unicast and #Multicast Frames", MAX_PERCENT_WARNING_LOST_FRAMES);
            }
            if (this.crcError > 1) AddPortAlert($"#Rx CRC Error detected: {this.crcError}");
            if (this.collisions > 0) AddPortAlert($"#Collisions detected: {this.collisions}");
            if (this.alignments > 1) AddPortAlert($"#Alignments Error detected: {this.alignments}");
            if (this.alertReport?.Count > 0 && this.alertReport.ContainsKey(this.trafficPort.Port)) AddPortAlert(PrintMacAdresses("MAC Address"));
        }

        private string PrintMacAdresses(string title = null)
        {
            string txt = string.IsNullOrEmpty(title) ? string.Empty : title;
            if (this.trafficPort.MacList?.Count > 1)
            {
                if (!string.IsNullOrEmpty(title)) txt += "es: ";
                txt += string.Join(",", this.trafficPort.MacList);
                if (this.trafficPort.MacList.Count > 9) txt += " ...";
            }
            else if (this.trafficPort.MacList?.Count > 0)
            {
                if (!string.IsNullOrEmpty(title)) txt += ": ";
                txt += this.trafficPort.MacList[0];
            }
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
            double dVal = (GetAvgTrafficSamples(nbBytes) * 8 / this.TrafficDuration) / 1024;
            double avg = Math.Round(dVal, 2, MidpointRounding.ToEven);
            return avg;
        }

        private double GetAvgTrafficSamples(List<double> list)
        {

            if (list?.Count > 0) return list[list.Count - 1] - list[0];
            return 0;
        }

        private double GetMaxTrafficSamples(List<double> list)
        {
            if (list?.Count > 0)
            {
                List<double> traffDiff = BuildDiffSample(list);
                if (traffDiff?.Count > 1)
                {
                    double maxValue = traffDiff.Max();
                    double avg = GetAvgTrafficSamples(list);
                    if (maxValue < avg) maxValue = avg;
                    return maxValue;
                }
            }
            return 0;
        }

        private List<double> BuildDiffSample(List<double> list)
        {
            List<double> diffSamples = new List<double>();
            double prevBytes = list[0];
            for (int idx = 1; idx < list.Count; idx++)
            {
                if (list[idx] > prevBytes) diffSamples.Add(list[idx] - prevBytes);
                prevBytes = list[idx];
            }
            return diffSamples;
        }
    }

}

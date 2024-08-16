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
        const double MIN_NB_UNICAST_FRAMES = 300;

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
                string slotPortNr = keyVal.Key;
                this.trafficPort = keyVal.Value;
                if (this.portsMacList.ContainsKey(slotPortNr)) this.trafficPort.MacList = this.portsMacList[slotPortNr];
                this.Data.Append("\r\n ").Append(this.trafficPort.Port);
                ParseTrafficRate("Rx Rate", slotPortNr, this.trafficPort.RxBytes);
                ParseTrafficRate("Tx Rate", slotPortNr, this.trafficPort.TxBytes);
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
                this.Data.Append(",\"");
                if (this.trafficPort.MacList.Count > 1) this.Data.Append(string.Join(",", this.trafficPort.MacList)); else this.Data.Append(this.trafficPort.MacList[0]);
                this.Data.Append("\"");
                ParseAlertConditions(slotPortNr);
            }
            if (this.alertReport?.Count > 0) foreach (KeyValuePair<string, string> keyVal in this.alertReport) this.Summary += keyVal.Value;
            else this.Summary += $"\nNo traffic anomalies detected.";
        }

        private void ParseAlertConditions(string slotPortNr)
        {
            if (uniCast > MIN_NB_UNICAST_FRAMES && this.trafficPort.MacList.Count > 1)
            {
                AddAlertPercent(slotPortNr, this.broadCast, "#Broadcast Frames", this.uniCast, "#Unicast Frames", MAX_PERCENT_BROADCAST);
            }
            if (!AddAlertPercent(slotPortNr, this.lostFrames, "Critical #Lost Frames", this.uniCast + this.multiCast, "#Unicast and #Multicast Frames", MAX_PERCENT_CRITICAL_LOST_FRAMES))
            {
                AddAlertPercent(slotPortNr, this.lostFrames, "Warning #Lost Frames", this.uniCast + this.multiCast, "#Unicast and #Multicast Frames", MAX_PERCENT_WARNING_LOST_FRAMES);
            }
            if (crcError > 1) AddAlert(slotPortNr, $"#Rx CRC Error detected: {crcError}");
            if (collisions > 0) AddAlert(slotPortNr, $"#Collisions detected: {collisions}");
            if (alignments > 1) AddAlert(slotPortNr, $"#Alignments Error detected: {alignments}");
        }

        private void ParseTrafficRate(string title, string slotPortNr, List<double> samples)
        {
            double traffRate = AddTrafficRate(samples);
            this.Data.Append(",");
            if (traffRate > 0)
            {
                this.Data.Append(traffRate);
                traffRate /= 1024;
                double percent = Utils.CalcPercent(traffRate, this.trafficPort.BandWidth, 2);
                if (percent >= MAX_PERCENT_RATE)
                {
                    AddAlert(slotPortNr, $"{title} ({traffRate} Mbps) > {MAX_PERCENT_RATE}% of Bandwidth ({this.trafficPort.BandWidth} Mbps), Percentage: {percent}%");
                }
            }
        }

        private bool AddAlertPercent(string slotPortNr, double val1, string label1, double val2, string label2, double maxPercent)
        {
            double percent = Utils.CalcPercent(val1, val2, 2);
            if (percent >= maxPercent)
            {
                AddAlert(slotPortNr, $"{label1} ({val1}) > {maxPercent}% of {label2} ({val2}), Percentage: {percent}%");
                return true;
            }
            return false;
        }

        private void AddAlert(string slotPortNr, string alertMsg)
        {
            string txt = string.Empty;
            if (this.alertReport.ContainsKey(slotPortNr)) txt = this.alertReport[slotPortNr];
            txt += $"\n  Port {slotPortNr}:\n\t{alertMsg}";
            this.alertReport[slotPortNr] = txt;
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

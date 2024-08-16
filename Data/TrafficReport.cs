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

        private PortTrafficModel _trafficPort;
        private Dictionary<string, string> _alertReport;
        private Dictionary<string, List<string>> _portsMacList = new Dictionary<string, List<string>>();

        public string Summary { get; set; }
        public StringBuilder Data { get; set; }
        public DateTime TrafficStartTime { get; set; }

        public double TrafficDuration { get; set; }
        public TrafficReport()
        {
            this.Summary = string.Empty;
            this.Data = null;
            _alertReport = new Dictionary<string, string>();
        }

        public void BuildReportData(SwitchTrafficModel switchTraffic, Dictionary<string, List<string>> portsMacList)
        {
            this.TrafficStartTime = switchTraffic.StartTime;
            if (portsMacList?.Count > 0) this._portsMacList = portsMacList;
            this.Summary = $"Traffic analysis completed on switch {switchTraffic.Name} ({switchTraffic.IpAddress}), Serial Number: {switchTraffic.SerialNumber}:";
            this.Summary += $"\nDuration: {Utils.CalcStringDuration(TrafficStartTime, true)}\n\nTraffic Alert:\n";
            this.TrafficDuration = DateTime.Now.Subtract(switchTraffic.StartTime).TotalSeconds;
            this.Data = new StringBuilder(HEADER);
            this._alertReport = new Dictionary<string, string>();
            foreach (KeyValuePair<string, PortTrafficModel> keyVal in switchTraffic.Ports)
            {
                string slotPortNr = keyVal.Key;
                this._trafficPort = keyVal.Value;
                if (this._portsMacList.ContainsKey(slotPortNr)) this._trafficPort.MacList = this._portsMacList[slotPortNr];
                this.Data.Append("\r\n ").Append(this._trafficPort.Port);
                ParseTrafficRate("Rx Rate", slotPortNr, this._trafficPort.RxBytes);
                ParseTrafficRate("Tx Rate", slotPortNr, this._trafficPort.TxBytes);
                double broadCast = GetAvgTrafficSamples(this._trafficPort.BroadcastFrames);
                this.Data.Append(",").Append(broadCast);
                double uniCast = GetAvgTrafficSamples(this._trafficPort.UnicastFrames);
                this.Data.Append(",").Append(uniCast);
                this.Data.Append(",").Append(Utils.CalcPercent(broadCast, uniCast, 2));
                if (broadCast > 500 && this._trafficPort.MacList.Count > 1)
                {
                    AddAlertPercent(slotPortNr, broadCast, "#Broadcast Frames", uniCast, "#Unicast Frames", MAX_PERCENT_BROADCAST);
                }
                double multiCast = GetAvgTrafficSamples(this._trafficPort.MulticastFrames);
                this.Data.Append(",").Append(multiCast);
                double lostFrames = GetAvgTrafficSamples(this._trafficPort.LostFrames);
                this.Data.Append(",").Append(lostFrames);
                if (!AddAlertPercent(slotPortNr, lostFrames, "Critical #Lost Frames", uniCast + multiCast, "#Unicast and #Multicast Frames", MAX_PERCENT_CRITICAL_LOST_FRAMES))
                {
                    AddAlertPercent(slotPortNr, lostFrames, "Warning #Lost Frames", uniCast + multiCast, "#Unicast and #Multicast Frames", MAX_PERCENT_WARNING_LOST_FRAMES);
                }
                double crcError = GetMaxTrafficSamples(this._trafficPort.CrcErrorFrames);
                this.Data.Append(",").Append(crcError);
                if (crcError > 1) AddAlert(slotPortNr, $"#Rx CRC Error detected: {crcError}");
                double collisions = GetMaxTrafficSamples(this._trafficPort.CollidedFrames) + GetMaxTrafficSamples(this._trafficPort.Collisions) +
                                    GetMaxTrafficSamples(this._trafficPort.ExcCollisions) + GetMaxTrafficSamples(this._trafficPort.LateCollisions);
                this.Data.Append(",").Append(collisions);
                if (collisions > 0) AddAlert(slotPortNr, $"#Collisions detected: {collisions}");
                double alignments = GetMaxTrafficSamples(this._trafficPort.AlignmentsError);
                this.Data.Append(",").Append(alignments);
                if (alignments > 1) AddAlert(slotPortNr, $"#Alignments Error detected: {alignments}");
                this.Data.Append("\"");
                if (this._trafficPort.MacList.Count > 1) this.Data.Append(string.Join(",", this._trafficPort.MacList)); else this.Data.Append(this._trafficPort.MacList[0]);
                this.Data.Append("\"");
            }
            if (this._alertReport?.Count > 0) foreach (KeyValuePair<string, string> keyVal in this._alertReport) this.Summary += keyVal.Value;
            else this.Summary += $"\nNo traffic anomalies detected.";
        }

        private void ParseTrafficRate(string title, string slotPortNr, List<double> samples)
        {
            double traffRate = AddTrafficRate(samples);
            this.Data.Append(",");
            if (traffRate > 0)
            {
                this.Data.Append(traffRate);
                traffRate /= 1024;
                double percent = Utils.CalcPercent(traffRate, this._trafficPort.BandWidth, 2);
                if (percent >= MAX_PERCENT_RATE)
                {
                    AddAlert(slotPortNr, $"{title} ({traffRate} Mbps) > {MAX_PERCENT_RATE}% of Bandwidth ({this._trafficPort.BandWidth} Mbps), Percentage: {percent}%");
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
            if (this._alertReport.ContainsKey(slotPortNr)) txt = this._alertReport[slotPortNr];
            txt += $"\nPort {slotPortNr}:\n\t{alertMsg}";
            this._alertReport[slotPortNr] = txt;
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

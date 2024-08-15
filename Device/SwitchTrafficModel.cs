using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoEWizard.Device
{
    public class TrafficReport
    {

        const string HEADER = "Port,Max. Rx Rate,Avg. Rx Rate,Max. Tx Rate,Avg. Tx Rate,#Broadcast Frames,#Unicast Frames,#Multicast Frames,#Lost Frames,#CRC Error,#Collisions,#Alignments";
        const double MAX_PERCENT_BROADCAST = 0.5;
        const double MAX_PERCENT_RATE = 70;
        const double MAX_PERCENT_WARNING_LOST_FRAMES = 3;
        const double MAX_PERCENT_CRITICAL_LOST_FRAMES = 6;

        private PortTrafficModel _trafficPort;
        private Dictionary<string, string> _alertReport { get; set; }

        public string Summary {  get; set; }
        public StringBuilder Data { get; set; }
        public DateTime TrafficStartTime { get; set; }

        public double TrafficDuration { get; set; }
        public TrafficReport()
        {
            this.Summary = string.Empty;
            this.Data = null;
            _alertReport = new Dictionary<string, string>();
        }

        public void BuildReportData(SwitchTrafficModel switchTraffic)
        {
            this.TrafficStartTime = switchTraffic.StartTime;
            this.Summary = $"Traffic analysis completed on switch {switchTraffic.Name} ({switchTraffic.IpAddress}), Serial Number: {switchTraffic.SerialNumber}:";
            this.Summary += $"\nDuration: {Utils.CalcStringDuration(TrafficStartTime, true)}";
            this.TrafficDuration = DateTime.Now.Subtract(switchTraffic.StartTime).TotalSeconds;
            this.Data = new StringBuilder(HEADER);
            this._alertReport = new Dictionary<string, string>();
            foreach (KeyValuePair<string, PortTrafficModel> keyVal in switchTraffic.Ports)
            {
                string slotPortNr = keyVal.Key;
                this._trafficPort = keyVal.Value;
                this.Data.Append("\r\n").Append(this._trafficPort.Port);
                ParseTrafficRateAlert("Rx Rate", slotPortNr, AddTrafficRate(this._trafficPort.RxBytes));
                ParseTrafficRateAlert("Tx Rate", slotPortNr, AddTrafficRate(this._trafficPort.TxBytes));
                double broadCast = GetAvgTrafficSamples(this._trafficPort.BroadcastFrames);
                this.Data.Append(",").Append(broadCast);
                double uniCast = GetAvgTrafficSamples(this._trafficPort.UnicastFrames);
                this.Data.Append(",").Append(uniCast);
                AddAlertPercent(slotPortNr, broadCast, "#Broadcast Frames", uniCast, "#Unicast Frames", MAX_PERCENT_BROADCAST);
                double multiCast = GetAvgTrafficSamples(this._trafficPort.MulticastFrames);
                this.Data.Append(",").Append(multiCast);
                double lostFrames = GetAvgTrafficSamples(this._trafficPort.LostFrames);
                this.Data.Append(",").Append(lostFrames);
                if (!AddAlertPercent(slotPortNr, lostFrames, "Critical #Lost Frames", uniCast + multiCast, "#Unicast and #Multicast Frames", MAX_PERCENT_CRITICAL_LOST_FRAMES))
                {
                    AddAlertPercent(slotPortNr, lostFrames, "Warning #Lost Frames", uniCast + multiCast, "#Unicast and #Multicast Frames", MAX_PERCENT_WARNING_LOST_FRAMES);
                }
                double crcError = GetMaxTrafficSamples(this._trafficPort.MulticastFrames);
                this.Data.Append(",").Append(crcError);
                if (crcError > 1) AddAlert(slotPortNr, $"#Rx CRC Error detected: {crcError}");
                double collisions = GetMaxTrafficSamples(this._trafficPort.CollidedFrames) + GetMaxTrafficSamples(this._trafficPort.Collisions) +
                                    GetMaxTrafficSamples(this._trafficPort.ExcCollisions) + GetMaxTrafficSamples(this._trafficPort.LateCollisions);
                this.Data.Append(",").Append(collisions);
                if (collisions > 0) AddAlert(slotPortNr, $"#Collisions detected: {collisions}");
                double alignments = GetMaxTrafficSamples(this._trafficPort.AlignmentsError);
                this.Data.Append(",").Append(alignments);
                if (alignments > 1) AddAlert(slotPortNr, $"#Alignments Error detected: {alignments}");
            }
            if (this._alertReport?.Count > 0) foreach (KeyValuePair<string, string> keyVal in this._alertReport) this.Summary += keyVal.Value;
            else this.Summary += $"\nNo traffic anomalies detected.";
        }

        private void ParseTrafficRateAlert(string title, string slotPortNr, double traffRate)
        {
            if (traffRate > 0)
            {
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
            txt += $"\nPort {slotPortNr} {alertMsg}";
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
                    return traffDiff.Max();
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

    public class SwitchTrafficModel
    {
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string SerialNumber { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime PrevTimeUpdated { get; set; }
        public Dictionary<string, PortTrafficModel> Ports { get; set; } = new Dictionary<string, PortTrafficModel>();


        public SwitchTrafficModel() { }

        public SwitchTrafficModel(string name, string ipAddr, string serialNumber, List<Dictionary<string, string>> dictList)
        {
            this.Name = name;
            this.IpAddress = ipAddr;
            this.SerialNumber = serialNumber;
            this.StartTime = DateTime.Now;
            UpdateTraffic(dictList);
        }

        public void UpdateTraffic(List<Dictionary<string, string>> dictList)
        {
            foreach (Dictionary<string, string> dict in dictList)
            {
                string port = Utils.GetDictValue(dict, Constants.PORT);
                if (!string.IsNullOrEmpty(port))
                {
                    if (!this.Ports.ContainsKey(port))
                    {
                        string status = Utils.GetDictValue(dict, Constants.OPERATIONAL_STATUS);
                        if (!string.IsNullOrEmpty(status) && status == "up")
                        {
                            this.Ports[port] = new PortTrafficModel(dict);
                        }
                    }
                    else
                    {
                        this.Ports[port].UpdateTraffic(dict);
                    }
                }
            }
        }

    }

}

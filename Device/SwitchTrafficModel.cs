using PoEWizard.Data;
using System;
using System.Collections.Generic;

namespace PoEWizard.Device
{
    public class SwitchTrafficModel
    {
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string SerialNumber { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime PrevTimeUpdated { get; set; }
        public Dictionary<string, PortTrafficModel> Ports { get; set; }


        public SwitchTrafficModel() { }

        public SwitchTrafficModel(string name, string ipAddr, string serialNumber, List<Dictionary<string, string>> dictList)
        {
            this.Ports = new Dictionary<string, PortTrafficModel>();
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
                        if (!string.IsNullOrEmpty(status) && status == "up") this.Ports[port] = new PortTrafficModel(dict);
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

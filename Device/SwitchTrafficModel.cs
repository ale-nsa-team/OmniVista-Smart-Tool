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
        public DateTime LastTimeUpdated { get; set; }
        public DateTime PrevTimeUpdated { get; set; }
        public Dictionary<string, PortTrafficModel> Ports { get; set; } = new Dictionary<string, PortTrafficModel>();


        public SwitchTrafficModel() { }

        public SwitchTrafficModel(string name, string ipAddr, string serialNumber, List<Dictionary<string, string>> dictList)
        {
            Name = name;
            IpAddress = ipAddr;
            SerialNumber = serialNumber;
            LastTimeUpdated = DateTime.Now;
            UpdateTraffic(dictList);
        }

        public void UpdateTraffic(List<Dictionary<string, string>> dictList)
        {
            PrevTimeUpdated = LastTimeUpdated;
            LastTimeUpdated = DateTime.Now;
            double duration = LastTimeUpdated.Subtract(PrevTimeUpdated).TotalSeconds;
            foreach (Dictionary<string, string> dict in dictList)
            {
                string port = Utils.GetDictValue(dict, Constants.PORT);
                if (!string.IsNullOrEmpty(port))
                {
                    if (!dict.ContainsKey(port)) Ports[port] = new PortTrafficModel(dict, duration); else Ports[port].UpdateTraffic(dict, duration);
                }
            }
        }
    }

}

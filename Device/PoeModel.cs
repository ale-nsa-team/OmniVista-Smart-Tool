using System;
using System.Collections.Generic;

namespace PoEWizard.Device
{
    [Serializable]
    public class PoeModel
    {
        public string Port { get; set; }
        public bool PoeOn { get; set; }
        public string Priority { get; set; }
        public string Current { get; set; }
        public string Maximum { get; set; }
        public List<string> PriorityList => new List<string> { "Low", "High", "Critical" };

        public PoeModel() { }

        public PoeModel(Dictionary<string, string> portData)
        {
            Port = portData["Port"];
            PoeOn = portData["On/Off"] == "ON";
            Priority = portData["Priority"];
            Current = portData["Actual Used(mW)"];
            Maximum = portData["Maximum(mW)"];
        }

        public bool Equals(PoeModel other)
        {
            return Port == other.Port && PoeOn == other.PoeOn && Priority == other.Priority;
        }
    }
}

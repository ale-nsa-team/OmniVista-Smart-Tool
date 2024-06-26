using System;
using System.Collections.Generic;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    [Serializable]
    public class SwitchPort
    {
        public string Number { get; set; }
        public List<string> MacList { get; set; }
        public bool HasPoe { get; set; }
        public PoeStatus Poe { get; set; }
        public string Power { get; set; }
        public string MaxPower { get; set; }
        public PortStatus Status { get; set; }
        public PriorityLevelType PriorityLevel { get; set; }
        public bool IsUplink { get; set; } = false;
        public bool IsLldp { get; set; } = false;
        public bool IsVfLink { get; set; } = false;
        public bool Is4Pair { get; set; } = true;
        public bool IsEnabled { get; set; } = false;

        public SwitchPort() { }
    }

}

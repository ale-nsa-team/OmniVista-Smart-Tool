using PoEWizard.Data;
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

        public SwitchPort(Dictionary<string, string> dict)
        {
            Dictionary<string, object> slotPort = Utils.GetChassisSlotPort(dict["Chas/ Slot/ Port"]);
            Number = slotPort[P_PORT].ToString();
            UpdatePortStatus(dict["Link Status"]);
            IsEnabled = dict["Admin Status"] == "enable";
            MacList = new List<string>();
            HasPoe = false;
            Power = "0 mw";
            Poe = PoeStatus.NoPoe;
            MaxPower = "0 mw";
            PriorityLevel = PriorityLevelType.Unknown;
            IsUplink = false;
            IsLldp = false;
            IsVfLink = false;
            Is4Pair = false;
        }

        private void UpdatePortStatus(string status)
        {
            string sValStatus = Utils.ToPascalCase(char.ToUpperInvariant(status[0]) + status.Substring(1));
            if (!string.IsNullOrEmpty(sValStatus) && Enum.TryParse(sValStatus, out PortStatus portStatus))
            {
                if (!Utils.IsNumber(portStatus.ToString())) Status = portStatus; else Status = PortStatus.Unknown;
                if (Status == PortStatus.Down) Power = "0 mW";
            }
            else
            {
                Status = PortStatus.Unknown;
            }
        }

    }

}

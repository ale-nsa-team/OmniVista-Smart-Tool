using PoEWizard.Data;
using System;
using System.Collections.Generic;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    [Serializable]
    public class PortModel
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

        public PortModel(Dictionary<string, string> dict)
        {

            Number = GetPortId(dict[CHAS_SLOT_PORT]);
            UpdatePortStatus(dict[LINK_STATUS]);
            IsEnabled = dict[ADMIN_STATUS] == "enable";
            MacList = new List<string>();
            HasPoe = false;
            Power = "0 mw";
            Poe = PoeStatus.NoPoe;
            MaxPower = "0 mw";
            PriorityLevel = PriorityLevelType.Low;
            IsUplink = false;
            IsLldp = false;
            IsVfLink = false;
            Is4Pair = false;
        }

        private void UpdatePortStatus(string status)
        {
            string sValStatus = Utils.FirstChToUpper(status);
            if (!string.IsNullOrEmpty(sValStatus) && Enum.TryParse(sValStatus, out PortStatus portStatus))
            {
                Status = portStatus;
            }
            else
            {
                Status = PortStatus.Unknown;
            }
        }

        private string GetPortId(string chas)
        {
            return chas.Split('/')[2] ?? "0";
        }

    }
}

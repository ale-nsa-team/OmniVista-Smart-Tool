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
        public string Class { get; set; }
        public string Type { get; set; }

        public PortModel(Dictionary<string, string> dict)
        {

            Number = GetPortId(dict.TryGetValue(CHAS_SLOT_PORT, out string s) ? s : "");
            UpdatePortStatus(dict.TryGetValue(LINK_STATUS, out s) ? s : "");
            IsEnabled = (dict.TryGetValue(ADMIN_STATUS, out s) ? s : "") == "enable";
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

        public void LoadPoEData(Dictionary<string, string> dict)
        {
            MaxPower = dict.TryGetValue(MAXIMUM, out string s) ? s : "";
            Power = dict.TryGetValue(USED, out s) ? s : "";
            HasPoe = true;
            switch (dict.TryGetValue(STATUS, out s) ? s : "")
            {
                case POWERED_ON:
                case SEARCHING:
                    Poe = PoeStatus.On;
                    break;
                case POWERED_OFF:
                    Poe = PoeStatus.Off;
                    break;
                case FAULT:
                    Poe = PoeStatus.Fault;
                    break;
                case BAD_VOLTAGE_INJECTION:
                    Poe = PoeStatus.Conflict;
                    break;
                case DENY:
                    Poe = PoeStatus.Deny;
                    break;
            }
            string level = dict.TryGetValue(PRIORITY, out s) ? s : "";
            PriorityLevel = Enum.TryParse<PriorityLevelType>(level, true, out PriorityLevelType res) ? res : PriorityLevelType.Unknown;
            Class = dict.TryGetValue(CLASS, out s) ? s : "";
            Type = dict.TryGetValue(TYPE, out s) ? s : "";
        }

        public void LoadPoEConfig(Dictionary<string, string> dict) 
        {
            
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
            return chas.Split('/')?[2] ?? "0";
        }

    }
}

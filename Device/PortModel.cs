using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    [Serializable]
    public class PortModel
    {
        public string Number { get; set; }
        public string Name { get; set; }
        public bool HasPoe { get; set; }
        public PoeStatus Poe { get; set; }
        public int Power { get; set; }
        public int MaxPower { get; set; }
        public PortStatus Status { get; set; }
        public PriorityLevelType PriorityLevel { get; set; }
        public bool IsUplink { get; set; } = false;
        public bool IsLldpMdi { get; set; } = false;
        public bool IsLldpExtMdi { get; set; } = false;
        public bool IsVfLink { get; set; } = false;
        public bool Is4Pair { get; set; } = true;
        public bool IsPowerOverHdmi { get; set; }
        public bool IsCapacitorDetection { get; set; }
        public ConfigType Protocol8023bt { get; set; }
        public bool IsEnabled { get; set; } = false;
        public string Class { get; set; }
        public string Type { get; set; }
        public List<string> MacList { get; set; }
        public EndPointDeviceModel EndPointDevice { get; set; }
        public List<PriorityLevelType> Priorities => Enum.GetValues(typeof(PriorityLevelType)).Cast<PriorityLevelType>().ToList();

        public PortModel(Dictionary<string, string> dict)
        {
            Name = dict.TryGetValue(CHAS_SLOT_PORT, out string s) ? s : "";
            Number = GetPortId(Name);
            UpdatePortStatus(dict);
            HasPoe = false;
            Power = 0;
            Poe = PoeStatus.NoPoe;
            MaxPower = 0;
            PriorityLevel = PriorityLevelType.Low;
            IsUplink = false;
            IsLldpMdi = false;
            IsLldpExtMdi = false;
            IsVfLink = false;
            Is4Pair = false;
            IsPowerOverHdmi = true;
            IsCapacitorDetection = true;
            Protocol8023bt = ConfigType.Unavailable;
            MacList = new List<string>();
            EndPointDevice = new EndPointDeviceModel();
        }

        public void LoadPoEData(Dictionary<string, string> dict)
        {
            MaxPower = ParseNumber(dict.TryGetValue(MAXIMUM, out string s) ? s : "0");
            Power = ParseNumber(dict.TryGetValue(USED, out s) ? s : "0");
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
            PriorityLevel = Enum.TryParse<PriorityLevelType>(dict.TryGetValue(PRIORITY, out s) ? s : "", true, out PriorityLevelType res) ? res : PriorityLevelType.Low;
            Class = dict.TryGetValue(CLASS, out s) ? s : "";
            Type = dict.TryGetValue(TYPE, out s) ? s : "";
        }

        public void LoadPoEConfig(Dictionary<string, string> dict) 
        {
            Is4Pair = (dict.TryGetValue(POWER_4PAIR, out string s) ? s : "") == "enabled";
            IsPowerOverHdmi = (dict.TryGetValue(POWER_OVER_HDMI, out s) ? s : "") == "enabled";
            IsCapacitorDetection = (dict.TryGetValue(POWER_CAPACITOR_DETECTION, out s) ? s : "") == "enabled";
            dict.TryGetValue(POWER_823BT, out s);
            switch (s)
            {
                case "NA":
                    Protocol8023bt = ConfigType.Unavailable;
                    break;

                case "enabled":
                    Protocol8023bt = ConfigType.Enabled;
                    break;

                case "disabled":
                    Protocol8023bt = ConfigType.Disabled;
                    break;
            }
        }
        public void LoadLldpRemoteTable(Dictionary<string, string> dict)
        {
            EndPointDevice.LoadLldpRemoteTable(dict);
        }

        public void UpdatePortStatus(Dictionary<string, string> dict)
        {
            IsEnabled = (dict.TryGetValue(ADMIN_STATUS, out string s) ? s : "") == "enable";
            string sValStatus = Utils.FirstChToUpper(dict.TryGetValue(LINK_STATUS, out s) ? s : "");
            if (!string.IsNullOrEmpty(sValStatus) && Enum.TryParse(sValStatus, out PortStatus portStatus)) Status = portStatus; else Status = PortStatus.Unknown;
        }

        public void UpdateMacList(List<Dictionary<string, string>> dictList)
        {
            MacList.Clear();
            foreach (Dictionary<string, string> dict in dictList)
            {
                if (AddMacToList(dict) >= 10) break;
            }
            IsUplink = (MacList.Count > 2);
        }

        public int AddMacToList(Dictionary<string, string> dict)
        {
            if (MacList.Count < 10)
            {
                MacList.Add(dict.TryGetValue(PORT_MAC_LIST, out string s) ? s : "");
            }
            IsUplink = (MacList.Count > 2);
            return MacList.Count;
        }

        private string GetPortId(string chas)
        {
            string[] split = chas.Split('/');
            if (split.Length < 1) return "0";
            return split[split.Length - 1];
        }

        public int ParseNumber(string val)
        {
            return int.TryParse(val.Replace("*", ""), out int n) ? n : 0;
        }

    }
}

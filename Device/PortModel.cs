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
        public PoeStatus Poe { get; set; } = PoeStatus.NoPoe;
        public double Power { get; set; }
        public double MaxPower { get; set; }
        public PortStatus Status { get; set; }
        public bool IsPoeON { get; set; }
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
        public List<EndPointDeviceModel> EndPointDevicesList { get; set; }
        public string Alias { get; set; }
        public List<PriorityLevelType> Priorities => Enum.GetValues(typeof(PriorityLevelType)).Cast<PriorityLevelType>().ToList();

        public PortModel(Dictionary<string, string> dict)
        {
            EndPointDevice = new EndPointDeviceModel();
            EndPointDevicesList = new List<EndPointDeviceModel>();
            Name = Utils.GetDictValue(dict, CHAS_SLOT_PORT);
            Number = GetPortId(Name);
            UpdatePortStatus(dict);
            Power = 0;
            Poe = PoeStatus.NoPoe;
            IsPoeON = false;
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
        }

        public void LoadPoEData(Dictionary<string, string> dict)
        {
            MaxPower = ParseNumber(Utils.GetDictValue(dict, MAXIMUM)) / 1000;
            Power = ParseNumber(Utils.GetDictValue(dict, USED)) / 1000;
            string onOff = Utils.GetDictValue(dict, ON_OFF);
            IsPoeON = onOff == "ON";
            switch (Utils.GetDictValue(dict, STATUS))
            {
                case POWERED_ON:
                    Poe = PoeStatus.On;
                    break;
                case SEARCHING:
                    Poe = PoeStatus.Searching;
                    break;
                case POWERED_OFF:
                    if (IsPoeON) Poe = PoeStatus.PoweredOff; else Poe = PoeStatus.Off;
                    break;
                case FAULT:
                    Poe = PoeStatus.Fault;
                    break;
                case TEST:
                    Poe = PoeStatus.Test;
                    break;
                case DELAYED:
                    Poe = PoeStatus.Delayed;
                    break;
                case BAD_VOLTAGE_INJECTION:
                    Poe = PoeStatus.Conflict;
                    break;
                case DENY:
                    Poe = PoeStatus.Deny;
                    break;
            }
            PriorityLevel = Enum.TryParse(Utils.GetDictValue(dict, PRIORITY), true, out PriorityLevelType res) ? res : PriorityLevelType.Low;
            string powerClass = Utils.ExtractNumber(Utils.GetDictValue(dict, CLASS));
            if (Poe != PoeStatus.NoPoe && !string.IsNullOrEmpty(powerClass))
            {
                Class = $"{powerClass}";
                if (powerClassTable.ContainsKey(powerClass)) Class += $" ({powerClassTable[powerClass]})";
            }
            else
            {
                Class = string.Empty;
            }
            Type = Utils.GetDictValue(dict, TYPE);
        }

        public void LoadPoEConfig(Dictionary<string, string> dict) 
        {
            Is4Pair = Utils.GetDictValue(dict, POWER_4PAIR) == "enabled";
            IsPowerOverHdmi = Utils.GetDictValue(dict, POWER_OVER_HDMI) == "enabled";
            IsCapacitorDetection = Utils.GetDictValue(dict, POWER_CAPACITOR_DETECTION) == "enabled";
            Protocol8023bt = Utils.ConvertToConfigType(dict, POWER_823BT);
        }

        public void LoadLldpRemoteTable(List<Dictionary<string, string>> dictList)
        {
            if (dictList == null || dictList.Count == 0) return;
            if (dictList.Count > 1)
            {
                foreach (Dictionary<string, string> dict in dictList)
                {
                    UpdateEndPointParameters(dict, DictionaryType.LldpRemoteList);
                }
                UpdateEndPointDevice(MED_ROUTER);
            }
            else
            {
                UpdateEndPointParameters(dictList[0], DictionaryType.LldpRemoteList);
                UpdateEndPointDevice(MED_ROUTER);
            }
        }

        public void LoadLldpInventoryTable(List<Dictionary<string, string>> dictList)
        {
            if (dictList == null || dictList.Count == 0) return;
            foreach (Dictionary<string, string> dict in dictList)
            {
                UpdateEndPointParameters(dict, DictionaryType.LldpInventoryList);
            }
            UpdateEndPointDevice(MED_ROUTER);
        }

        private void UpdateEndPointParameters(Dictionary<string, string> dict, DictionaryType dictType)
        {
            if (!dict.ContainsKey(MED_MAC_ADDRESS)) return;
            EndPointDeviceModel device = this.EndPointDevicesList.FirstOrDefault(ep => ep.MacAddress == dict[MED_MAC_ADDRESS]);
            if (device == null) this.EndPointDevicesList.Add(new EndPointDeviceModel(dict));
            else if (dictType == DictionaryType.LldpRemoteList) device.LoadLldpRemoteTable(dict);
            else if (dictType == DictionaryType.LldpInventoryList) device.LoadLldpInventoryTable(dict);
        }

        public void CreateVirtualDeviceEndpoint()
        {
            List<string> macList = new List<string>(this.MacList);
            EndPointDeviceModel deviceFound = null;
            foreach (EndPointDeviceModel dev in this.EndPointDevicesList)
            {
                macList.Remove(dev.MacAddress);
                if (dev.Type == NO_LLDP && !string.IsNullOrEmpty(dev.MacAddress))
                {
                    if (deviceFound == null) deviceFound = dev;
                }
            }
            if (macList.Count < 1) return;
            if (deviceFound != null && deviceFound.Type == NO_LLDP)
            {
                deviceFound.MacAddress = string.Join(",", macList);
                return;
            }
            Dictionary<string, string> dict = new Dictionary<string, string> { [REMOTE_ID] = this.Name, [LOCAL_PORT] = this.Name, [CAPABILITIES_SUPPORTED] = NO_LLDP,
                                                                               [MED_MAC_ADDRESS] = string.Join(",", macList)
            };
            this.EndPointDevicesList.Add(new EndPointDeviceModel(dict));
            UpdateEndPointDevice(NO_LLDP);
        }

        private void UpdateEndPointDevice(string type)
        {
            if (string.IsNullOrEmpty(this.EndPointDevicesList[0].Type)) this.EndPointDevicesList[0].Type = type;
            this.EndPointDevice = this.EndPointDevicesList[0].Clone();
            CheckMultipleDevicesEndPoint();
        }

        private void CheckMultipleDevicesEndPoint()
        {
            int nbDevices = 0;
            foreach (EndPointDeviceModel dev in this.EndPointDevicesList)
            {
                if (dev.Type == NO_LLDP) break;
                nbDevices++;
            }
            if (nbDevices > 1)
            {
                this.EndPointDevice.Type = MED_ROUTER;
                this.EndPointDevice.Name = MED_ROUTER;
                this.EndPointDevice.Label = MED_ROUTER;
            }
            if (this.EndPointDevice.Type == NO_LLDP || this.EndPointDevice.Type == MED_ROUTER) this.EndPointDevice.Vendor = string.Empty;
        }

        public void UpdatePortStatus(Dictionary<string, string> dict)
        {
            IsEnabled = (Utils.GetDictValue(dict, ADMIN_STATUS)) == "enable";
            string sValStatus = Utils.FirstChToUpper(Utils.GetDictValue(dict, LINK_STATUS));
            if (!string.IsNullOrEmpty(sValStatus) && Enum.TryParse(sValStatus, out PortStatus portStatus)) Status = portStatus; else Status = PortStatus.Unknown;
            Alias = Utils.GetDictValue(dict, ALIAS).Replace("\"", string.Empty);
        }

        public void UpdateMacList(List<Dictionary<string, string>> dictList)
        {
            MacList.Clear();
            foreach (Dictionary<string, string> dict in dictList)
            {
                if (AddMacToList(dict) >= 10) break;
            }
            IsUplink = MacList.Count > 2;
        }

        public int AddMacToList(Dictionary<string, string> dict)
        {
            if (MacList.Count < 10)
            {
                string mac = Utils.GetDictValue(dict, PORT_MAC_LIST);
                if (Utils.IsValidMacAddress(mac) && MacList.Contains(mac)) return MacList.Count;
                MacList.Add(mac);
            }
            IsUplink = MacList.Count > 2;
            return MacList.Count;
        }

        public bool IsSwitchUplink()
        {
            return this.EndPointDevicesList?.Count > 0 && !string.IsNullOrEmpty(this.EndPointDevicesList[0].Type) && this.EndPointDevicesList[0].Type == SWITCH;
        }

        private string GetPortId(string chas)
        {
            string[] split = chas.Split('/');
            if (split.Length < 1) return "0";
            return split[split.Length - 1];
        }

        public double ParseNumber(string val)
        {
            return double.TryParse(val.Replace("*", ""), out double n) ? n : 0;
        }

    }
}

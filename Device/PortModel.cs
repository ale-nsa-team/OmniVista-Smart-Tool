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
        public List<string> MacList { get; set; }
        public EndPointDeviceModel EndPointDevice { get; set; }
        public List<EndPointDeviceModel> EndPointDevicesList { get; set; }
        public string Alias { get; set; }
        public List<PriorityLevelType> Priorities => Enum.GetValues(typeof(PriorityLevelType)).Cast<PriorityLevelType>().ToList();

        #region Port detail information
        public string Violation { get; set; }
        public string Type { get; set; }
        public string InterfaceType { get; set; }
        public string Bandwidth { get; set; }
        public string Duplex { get; set; }
        public string AutoNegotiation { get; set; }
        public string Transceiver { get; set; }
        public string EPP {  get; set; }
        public string LinkQuality { get; set; }
        public List<string> DetailsInfo => ToTooltip();
        #endregion

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
                UpdateEndPointDevice(MED_MULTIPLE_DEVICES);
            }
            else
            {
                UpdateEndPointParameters(dictList[0], DictionaryType.LldpRemoteList);
                UpdateEndPointDevice(MED_MULTIPLE_DEVICES);
            }
        }

        public void LoadLldpInventoryTable(List<Dictionary<string, string>> dictList)
        {
            if (dictList == null || dictList.Count == 0) return;
            foreach (Dictionary<string, string> dict in dictList)
            {
                UpdateEndPointParameters(dict, DictionaryType.LldpInventoryList);
            }
            UpdateEndPointDevice(MED_MULTIPLE_DEVICES);
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
                if (dev.Type == NO_LLDP && !string.IsNullOrEmpty(dev.MacAddress))
                {
                    deviceFound = dev;
                    break;
                }
                macList.Remove(dev.MacAddress);
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
            if (this.EndPointDevice.Type == MED_SWITCH) return;
            int nbDevices = 0;
            foreach (EndPointDeviceModel dev in this.EndPointDevicesList)
            {
                if (dev.Type == NO_LLDP) break;
                nbDevices++;
            }
            if (nbDevices > 1)
            {
                this.EndPointDevice.Type = MED_MULTIPLE_DEVICES;
                this.EndPointDevice.Name = MED_MULTIPLE_DEVICES;
                this.EndPointDevice.Label = MED_MULTIPLE_DEVICES;
            }
            if (this.EndPointDevice.Type == NO_LLDP || this.EndPointDevice.Type == MED_MULTIPLE_DEVICES) this.EndPointDevice.Vendor = string.Empty;
        }

        public void UpdatePortStatus(Dictionary<string, string> dict)
        {
            IsEnabled = (Utils.GetDictValue(dict, ADMIN_STATUS)) == "enable";
            string sValStatus = Utils.FirstChToUpper(Utils.GetDictValue(dict, LINK_STATUS));
            if (!string.IsNullOrEmpty(sValStatus) && Enum.TryParse(sValStatus, out PortStatus portStatus)) Status = portStatus; else Status = PortStatus.Unknown;
            Alias = Utils.GetDictValue(dict, ALIAS).Replace("\"", string.Empty);
        }

        public void UpdateMacList(List<Dictionary<string, string>> dictList, int maxNbMacPerPort)
        {
            MacList.Clear();
            if (dictList?.Count > 0)
            {
                foreach (Dictionary<string, string> dict in dictList)
                {
                    if (MacList.Count >= maxNbMacPerPort) break;
                    AddMacToList(dict);
                }
            }
            else
            {
                IsUplink = false;
            }
        }

        public void AddMacToList(Dictionary<string, string> dict)
        {
            string mac = Utils.GetDictValue(dict, PORT_MAC_LIST);
            if (Utils.IsValidMacAddress(mac) && MacList.Contains(mac)) return;
            MacList.Add(mac);
            IsUplink = MacList.Count > MIN_NB_MAC_UPLINK;
        }

        public void LoadDetailInfo(Dictionary<string, string> dict)
        {
            Violation = GetDictValue(dict, PORT_VIOLATION);
            Type = GetDictValue(dict, PORT_TYPE);
            InterfaceType = GetDictValue(dict, PORT_INTERFACE_TYPE);
            Bandwidth = GetDictValue(dict, PORT_BANDWIDTH);
            Duplex = GetDictValue(dict, PORT_DUPLEX);
            AutoNegotiation = GetDictValue(dict, PORT_AUTO_NEGOTIATION);
            Transceiver = GetDictValue(dict, PORT_TRANSCEIVER);
            EPP = GetDictValue(dict, PORT_EPP);
            LinkQuality = GetDictValue(dict, PORT_LINK_QUALITY);
        }

        private string GetDictValue(Dictionary<string, string> dict, string param)
        {
            string sVal = Utils.GetDictValue(dict, param).Replace("-", "");
            switch (param)
            {
                case PORT_BANDWIDTH:
                    sVal = !string.IsNullOrEmpty(sVal) ? $"{sVal} Mbps" : INFO_UNAVAILABLE;
                    break;
                case PORT_AUTO_NEGOTIATION:
                    sVal = !string.IsNullOrEmpty(sVal) && !sVal.StartsWith("0") ? sVal : INFO_UNAVAILABLE;
                    break;
                default:
                    if (string.IsNullOrEmpty(sVal)) sVal = INFO_UNAVAILABLE;
                    else if (sVal.ToLower() == "notpresent" || sVal.ToLower() == "none" || sVal == NOT_AVAILABLE) sVal = INFO_UNAVAILABLE;
                    break;
            }
            return sVal;
        }

        public bool IsSwitchUplink()
        {
            return this.EndPointDevicesList?.Count > 0 && !string.IsNullOrEmpty(this.EndPointDevicesList[0].Type) && this.EndPointDevicesList[0].Type == MED_SWITCH;
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

        public List<string> ToTooltip()
        {
            List<string> tip = new List<string> {
                $"Port: {this.Name}"
            };
            if (!string.IsNullOrEmpty(this.InterfaceType)) tip.Add($"Interface Type: {this.InterfaceType}");
            if (!string.IsNullOrEmpty(this.Bandwidth)) tip.Add($"Bandwidth: {this.Bandwidth}");
            if (!string.IsNullOrEmpty(this.Duplex)) tip.Add($"Duplex: {this.Duplex}");
            if (!string.IsNullOrEmpty(this.AutoNegotiation)) tip.Add($"Auto Negotiation: {this.AutoNegotiation}");
            if (!string.IsNullOrEmpty(this.Violation)) tip.Add($"Violation: {this.Violation}");
            if (!string.IsNullOrEmpty(this.Transceiver)) tip.Add($"Transceiver: {this.Transceiver}");
            if (!string.IsNullOrEmpty(this.EPP)) tip.Add($"EPP: {this.EPP}");
            if (!string.IsNullOrEmpty(this.LinkQuality)) tip.Add($"Link Quality: {this.LinkQuality}");
            string note = "Note: Run traffic analysis to update port details information";
            tip.Add($"\n{note}\n");
            return tip;
        }

    }
}

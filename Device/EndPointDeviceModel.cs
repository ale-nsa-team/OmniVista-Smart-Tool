using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    public class EndPointDeviceModel
    {
        public string ID { get; set; } = string.Empty;
        public string Vendor { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SoftwareVersion { get; set; } = string.Empty;
        public string HardwareVersion { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;

        public string PowerClass { get; set; } = string.Empty;
        public string LocalPort { get; set; } = string.Empty;
        public PortSubType PortSubType { get; set; } = PortSubType.Unknown;
        public string MacAddress { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string EthernetType { get; set; } = string.Empty;
        public string RemotePort { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PortDescription { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new List<string>();
        public string MEDPowerType { get; set; } = string.Empty;
        public string MEDPowerSource { get; set; } = string.Empty;
        public string MEDPowerPriority { get; set; } = string.Empty;
        public string MEDPowerValue { get; set; } = string.Empty;
        public bool IsMacName { get; set; } = false;
        public string Label { get; set; } = string.Empty;

        public EndPointDeviceModel() { }
        public EndPointDeviceModel Clone()
        {
            return this.MemberwiseClone() as EndPointDeviceModel;
        }
        public EndPointDeviceModel(Dictionary<string, string> dict)
        {
            LoadLldpRemoteTable(dict);
        }

        public void LoadLldpRemoteTable(Dictionary<string, string> dict)
        {
            ID = Utils.GetDictValue(dict, REMOTE_ID);
            LocalPort = GetDictValue(dict, LOCAL_PORT, LocalPort);
            MacAddress = Utils.GetDictValue(dict, MED_MAC_ADDRESS);
            Vendor = Utils.GetVendorName(MacAddress);
            if (Vendor == MacAddress) Vendor = string.Empty;
            string macId = MacAddress.Replace(":", string.Empty);
            string remoteId = Utils.GetDictValue(dict, REMOTE_PORT).Replace(":", string.Empty);
            if (remoteId != macId) RemotePort = remoteId;
            string[] portSplit = Utils.GetDictValue(dict, PORT_SUBTYPE).Split(' ');
            if (portSplit.Length > 1) PortSubType = (PortSubType)Enum.ToObject(typeof(PortSubType), Utils.StringToInt(portSplit[0]));
            PortDescription = GetDictValue(dict, PORT_DESCRIPTION, PortDescription);
            Type = GetDictValue(dict, CAPABILITIES_SUPPORTED, Type);
            if (Type.Contains("none")) Type = MED_UNKNOWN;
            else if (string.IsNullOrEmpty(Type)) Type = MED_UNSPECIFIED;
            IpAddress = GetDictValue(dict, MED_IP_ADDRESS, IpAddress);
            EthernetType = GetDictValue(dict, MAU_TYPE, EthernetType);
            Label = Name = GetDictValue(dict, SYSTEM_NAME, Name);
            Description = GetDictValue(dict, SYSTEM_DESCRIPTION, Description).Replace("-", string.Empty);
            if (string.IsNullOrEmpty(Label)) Label = Description;
            int ifIndex = Utils.StringToInt(RemotePort);
            if (PortSubType == PortSubType.LocallyAssigned && (ifIndex >= 1000))
            {
                RemotePort = Utils.ParseIfIndex(ifIndex.ToString());
                if (string.IsNullOrEmpty(Label)) Label = $"Remote port {RemotePort}";
                Type = MED_SWITCH;
            }
            else if (!Utils.IsNumber(RemotePort))
            {
                RemotePort = string.Empty;
            }
            IsMacName = (string.IsNullOrEmpty(Label) || Type == MED_UNKNOWN || Type == NO_LLDP) && dict.ContainsKey(MED_MAC_ADDRESS);
            if (IsMacName) Label = MacAddress;
            string[] capList = Utils.GetDictValue(dict, MED_CAPABILITIES).Split('|');
            if (capList.Length > 0 && !string.IsNullOrEmpty(capList[0]))
            {
                foreach (string val in capList)
                {
                    if (string.IsNullOrEmpty(val)) continue;
                    string cap = val.Replace("Capabilities", string.Empty).Replace("(", string.Empty).Replace(")", string.Empty).Trim();
                    if (!string.IsNullOrEmpty(cap))
                    {
                        string element = Capabilities.FirstOrDefault(s => s.Contains(cap));
                        if (element == null) Capabilities.Add(cap);
                    }
                }
            }
            MEDPowerType = GetDictValue(dict, MED_POWER_TYPE, MEDPowerType);
            MEDPowerSource = GetDictValue(dict, MED_POWER_SOURCE, MEDPowerSource);
            MEDPowerPriority = GetDictValue(dict, MED_POWER_PRIORITY, MEDPowerPriority);
            MEDPowerValue = GetDictValue(dict, MED_POWER_VALUE, MEDPowerValue);
            if (dict.ContainsKey(MED_MODEL) || dict.ContainsKey(MED_MANUFACTURER) || dict.ContainsKey(MED_FIRMWARE_REVISION))
            {
                LoadLldpInventoryTable(dict);
            }
        }

        public void LoadLldpInventoryTable(Dictionary<string, string> dict)
        {
            if (Utils.GetDictValue(dict, LOCAL_PORT) != LocalPort || Utils.GetDictValue(dict, MED_MAC_ADDRESS) != MacAddress) return;
            string vendorRetrieved = Utils.GetDictValue(dict, MED_MANUFACTURER);
            if (!string.IsNullOrEmpty(vendorRetrieved)) Vendor = vendorRetrieved;
            Model = GetDictValue(dict, MED_MODEL, Model);
            HardwareVersion = GetDictValue(dict, MED_HARDWARE_REVISION, HardwareVersion);
            SoftwareVersion = GetDictValue(dict, MED_SOFTWARE_REVISION, SoftwareVersion);
            if (string.IsNullOrEmpty(SoftwareVersion)) SoftwareVersion = Utils.GetDictValue(dict, MED_FIRMWARE_REVISION);
            SerialNumber = GetDictValue(dict, MED_SERIAL_NUMBER, SerialNumber);
        }

        private string GetDictValue(Dictionary<string, string> dict, string parameter, string valueDict)
        {
            string sVal = Utils.GetDictValue(dict, parameter).Replace("(null)", string.Empty).Replace("\"", string.Empty);
            if (string.IsNullOrEmpty(valueDict) || !string.IsNullOrEmpty(sVal)) return sVal;
            return valueDict;
        }

        public string ToFilterTooltip(bool isMacAddress, string searchText)
        {
            List<string> tip = ToMainTooltip(isMacAddress ? null : searchText);
            if (this.Label.Contains(","))
            {
                string[] split = this.Label.Split(',');
                tip.Add($"MAC List:");
                int cntMac = 0;
                foreach (string macAddr in split)
                {
                    string vendor = Utils.GetVendorName(macAddr);
                    if (Utils.IsValidMacAddress(vendor)) vendor = string.Empty;
                    string mac = macAddr;
                    bool found = (isMacAddress && mac.ToLower().StartsWith(searchText)) || (!string.IsNullOrEmpty(vendor) && vendor.ToLower().Contains(searchText));
                    cntMac++;
                    if (cntMac > MAX_NB_MAC_TOOL_TIP)
                    {
                        tip.Add("          . . .");
                        break;
                    }
                    if (!string.IsNullOrEmpty(vendor)) mac = $" {mac} ({vendor})";
                    if (found)
                    {
                        mac += MAC_MATCH_MARK; ;
                    }
                    tip.Add($" {mac}");
                }
            }
            else if (!string.IsNullOrEmpty(this.MacAddress))
            {
                string mac = this.MacAddress;
                string vendor = Utils.GetVendorName(this.MacAddress);
                if (!string.IsNullOrEmpty(this.Vendor))
                {
                    mac += $" ({vendor})";
                    if ((!isMacAddress && vendor.ToLower().Contains(searchText)) || (isMacAddress && this.MacAddress.StartsWith(searchText)))
                    {
                        mac = AddSearchMark(tip, mac);
                    }
                }
                else if (isMacAddress && this.MacAddress.StartsWith(searchText))
                {
                    mac = AddSearchMark(tip, mac);
                }
                tip.Add($"MAC: {mac}");
            }
            if (this.Capabilities.Count > 0) tip.Add($"Capabilities: {string.Join(",", this.Capabilities)}");
            return tip.Count > 0 ? string.Join("\n", tip) : null;
        }

        private string AddSearchMark(List<string> tip, string mac)
        {
            bool found = tip.Any(s => s.Contains(MAC_MATCH_MARK));
            if (!found) mac += MAC_MATCH_MARK;
            return mac;
        }

        public string ToTooltip()
        {
            List<string> tip = ToMainTooltip();
            if (this.Label.Contains(","))
            {
                string[] split = this.Label.Split(',');
                tip.Add($"MAC List:");
                int cntMac = 0;
                foreach (string mac in split)
                {
                    cntMac++;
                    if (cntMac > MAX_NB_MAC_TOOL_TIP)
                    {
                        tip.Add("          . . .");
                        break;
                    }
                    string vendor = Utils.GetVendorName(mac);
                    if (!string.IsNullOrEmpty(vendor) && !Utils.IsValidMacAddress(vendor)) tip.Add($" {mac} ({vendor})"); else tip.Add($" {mac}");
                }
            }
            else if (!string.IsNullOrEmpty(this.MacAddress)) tip.Add($"MAC: {this.MacAddress}");
            if (this.Capabilities.Count > 0) tip.Add($"Capabilities: {string.Join(",", this.Capabilities)}");
            return tip.Count > 0 ? string.Join("\n", tip) : null;
        }

        private List<string> ToMainTooltip(string searchText = null)
        {
            List<string> tip = new List<string>();
            if (!string.IsNullOrEmpty(this.Type)) tip.Add($"Type: {this.Type}");
            string nameVendor;
            bool addedMark = false;
            if (!string.IsNullOrEmpty(this.Label))
            {
                if (!IsMacName)
                {
                    nameVendor = this.Label;
                    if (!string.IsNullOrEmpty(searchText) && this.Label.ToLower().Contains(searchText))
                    {
                        nameVendor += MAC_MATCH_MARK;
                        addedMark = true;
                    }
                    if (!this.Label.Contains("Remote port")) tip.Add($"Name: {nameVendor}");
                }
                else if (string.IsNullOrEmpty(this.Vendor) && !this.Label.Contains(","))
                {
                    this.Vendor = Utils.GetVendorName(this.Label);
                }
            }
            if (!string.IsNullOrEmpty(this.Vendor))
            {
                nameVendor = this.Vendor;
                if (!string.IsNullOrEmpty(searchText) && this.Vendor.ToLower().Contains(searchText) && !addedMark)
                {
                    nameVendor += MAC_MATCH_MARK;
                }
                tip.Add($"Vendor: {nameVendor}");
            }
            if (!string.IsNullOrEmpty(this.Description)) tip.Add($"Description: {this.Description}");
            if (!string.IsNullOrEmpty(this.Model)) tip.Add($"Model: {this.Model}");
            if (!string.IsNullOrEmpty(this.SoftwareVersion)) tip.Add($"Version: {this.SoftwareVersion}");
            if (!string.IsNullOrEmpty(this.SerialNumber)) tip.Add($"Serial #: {this.SerialNumber}");
            if (!string.IsNullOrEmpty(this.IpAddress)) tip.Add($"IP: {this.IpAddress}");
            if (!string.IsNullOrEmpty(this.RemotePort)) tip.Add($"Remote Port: {this.RemotePort}");
            if (!string.IsNullOrEmpty(this.PortDescription)) tip.Add($"Port Description: {this.PortDescription}");
            return tip;
        }
    }
}

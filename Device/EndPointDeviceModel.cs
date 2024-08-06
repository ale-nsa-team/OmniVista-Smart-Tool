using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Text;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    public class EndPointDeviceModel
    {
        public string ID { get; set; } = string.Empty;
        public string Vendor {  get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string SoftwareVersion { get; set; } = string.Empty;
        public string HardwareVersion { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;

        public string PowerClass {  get; set; } = string.Empty;
        public string LocalPort { get; set; } = string.Empty;
        public PortSubType PortSubType { get; set; } = PortSubType.Unknown;
        public string MacAddress { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string EthernetType { get; set; } = string.Empty;
        public string RemotePort { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description {  get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new List<string>();
        public string MEDPowerType { get; set; } = string.Empty;
        public string MEDPowerSource { get; set; } = string.Empty;
        public string MEDPowerPriority { get; set; } = string.Empty;
        public string MEDPowerValue { get; set; } = string.Empty;

        public EndPointDeviceModel() { }
        public EndPointDeviceModel(Dictionary<string, string> dict)
        {
            LoadLldpRemoteTable(dict);
        }

        public void LoadLldpRemoteTable(Dictionary<string, string> dict)
        {
            ID = Utils.GetDictValue(dict, REMOTE_ID);
            LocalPort = Utils.GetDictValue(dict, LOCAL_PORT);
            MacAddress = Utils.GetDictValue(dict, CHASSIS_MAC_ADDRESS);
            string macId = MacAddress.Replace(":", "");
            string remoteId = Utils.GetDictValue(dict, REMOTE_PORT).Replace(":", "");
            if (remoteId != macId) RemotePort = remoteId;
            string[] portSplit = Utils.GetDictValue(dict, PORT_SUBTYPE).Split(' ');
            if (portSplit.Length > 1) PortSubType = (PortSubType)Enum.ToObject(typeof(PortSubType), Utils.StringToInt(portSplit[0]));
            Type = Utils.GetDictValue(dict, CAPABILITIES_ENABLED);
            IpAddress = Utils.GetDictValue(dict, MED_IP_ADDRESS);
            EthernetType = Utils.GetDictValue(dict, MAU_TYPE);
            Name = Utils.GetDictValue(dict, SYSTEM_NAME).Replace("(null)", string.Empty);
            int ifIndex = Utils.StringToInt(RemotePort);
            if (PortSubType == PortSubType.LocallyAssigned && (ifIndex >= 1000))
            {
                RemotePort = Utils.ParseIfIndex(ifIndex.ToString());
                if (string.IsNullOrEmpty(Name)) Name = $"Remote port {RemotePort}";
                Type = SWITCH;
            }
            else if (!Utils.IsNumber(RemotePort))
            {
                RemotePort = string.Empty;
            }
            Description = Utils.GetDictValue(dict, SYSTEM_DESCRIPTION).Replace("(null)", string.Empty).Replace("-", string.Empty);
            string[] capList = Utils.GetDictValue(dict, MED_CAPABILITIES).Split('|');
            if (capList.Length > 1)
            {
                Capabilities.Clear();
                foreach (string val in capList)
                {
                    if (string.IsNullOrEmpty(val) || val.Contains("Capabilities")) continue;
                    Capabilities.Add(val.Trim());
                }
            }
            MEDPowerType = Utils.GetDictValue(dict, MED_POWER_TYPE);
            MEDPowerSource = Utils.GetDictValue(dict, MED_POWER_SOURCE);
            MEDPowerPriority = Utils.GetDictValue(dict, MED_POWER_PRIORITY);
            MEDPowerValue = Utils.GetDictValue(dict, MED_POWER_VALUE);
        }

        public void LoadLldpInventoryTable(Dictionary<string, string> dict)
        {
            if (Utils.GetDictValue(dict, LOCAL_PORT) != LocalPort || Utils.GetDictValue(dict, CHASSIS_MAC_ADDRESS) != MacAddress) return;
            Vendor = Utils.GetDictValue(dict, MED_MANUFACTURER).Replace("\"", "");
            Model = Utils.GetDictValue(dict, MED_MODEL).Replace("\"", "");
            HardwareVersion = Utils.GetDictValue(dict, MED_HARDWARE_REVISION).Replace("\"", "");
            SoftwareVersion = Utils.GetDictValue(dict, MED_SOFTWARE_REVISION).Replace("\"", "");
            if (string.IsNullOrEmpty(SoftwareVersion)) SoftwareVersion = Utils.GetDictValue(dict, MED_FIRMWARE_REVISION).Replace("\"", "");
            SerialNumber = Utils.GetDictValue(dict, MED_SERIAL_NUMBER).Replace("\"", "");
        }

        public override string ToString()
        {
            StringBuilder txt = new StringBuilder("Device Type connected: ");
            txt.Append(string.IsNullOrEmpty(Type) ? $"Unknown ({PortSubType})" : Type);
            if (!string.IsNullOrEmpty(Name)) txt.Append(", Name: ").Append(Name);
            if (!string.IsNullOrEmpty(Description)) txt.Append(", Description: ").Append(Description);
            if (!string.IsNullOrEmpty(Vendor)) txt.Append(", Vendor: ").Append(Vendor);
            if (!string.IsNullOrEmpty(SoftwareVersion)) txt.Append(", Version: ").Append(SoftwareVersion);
            if (!string.IsNullOrEmpty(SerialNumber)) txt.Append(", Serial #: ").Append(SerialNumber);
            if (!string.IsNullOrEmpty(MacAddress)) txt.Append(", MAC: ").Append(MacAddress);
            if (!string.IsNullOrEmpty(IpAddress)) txt.Append(", IP: ").Append(IpAddress);
            return txt.ToString();
        }

        public string ToTooltip()
        {
            List<string> tip = new List<string>();
            if (!string.IsNullOrEmpty(Type)) tip.Add($"Type: {Type}");
            if (!string.IsNullOrEmpty(Name))
            {
                if (Name.Contains(","))
                {
                    string[] split = Name.Split(',');
                    tip.Add(string.Join("\n", split));
                    if (split.Length >= 10) tip.Add("          . . .");
                }
                else
                {
                    tip.Add($"Name: {Name}");
                }
            }
            if (!string.IsNullOrEmpty(Description)) tip.Add($"Description: {Description}");
            if (!string.IsNullOrEmpty(Vendor)) tip.Add($"Vendor: {Vendor}");
            if (!string.IsNullOrEmpty(Model)) tip.Add($"Model: {Model}");
            if (!string.IsNullOrEmpty(SoftwareVersion)) tip.Add($"Version: {SoftwareVersion}");
            if (!string.IsNullOrEmpty(SerialNumber)) tip.Add($"Serial #: {SerialNumber}");
            if (!string.IsNullOrEmpty(MacAddress)) tip.Add($"MAC: {MacAddress}");
            if (!string.IsNullOrEmpty(IpAddress)) tip.Add($"IP: {IpAddress}");
            //if (Capabilities?.Count > 0) tip.Add($"Capabilities: [{string.Join(",", Capabilities)}]");
            if(!string.IsNullOrEmpty(MEDPowerValue)) tip.Add($"Power Value: {MEDPowerValue}");
            if (!string.IsNullOrEmpty(MEDPowerPriority)) tip.Add($"Power Priority: {MEDPowerPriority}");
            if (!string.IsNullOrEmpty(RemotePort)) tip.Add($"Remote Port: {RemotePort}");
            return tip.Count > 0 ? string.Join("\n", tip) : null;
        }

    }
}

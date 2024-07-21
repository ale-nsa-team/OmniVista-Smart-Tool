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
        public string DeviceType { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
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

        public void LoadLldpRemoteTable(Dictionary<string, string> dict)
        {
            ID = Utils.GetDictValue(dict, REMOTE_ID);
            LocalPort = Utils.GetDictValue(dict, LOCAL_PORT);
            MacAddress = Utils.GetDictValue(dict, CHASSIS_MAC_ADDRESS);
            string[] portSplit = Utils.GetDictValue(dict, PORT_SUBTYPE).Split(' ');
            if (portSplit.Length > 1) PortSubType = (PortSubType)Enum.ToObject(typeof(PortSubType), Utils.StringToInt(portSplit[0]));
            DeviceType = Utils.GetDictValue(dict, CAPABILITIES_ENABLED);
            if (PortSubType == PortSubType.LocallyAssigned) DeviceType = "Switch";
            IpAddress = Utils.GetDictValue(dict, MED_IP_ADDRESS);
            EthernetType = Utils.GetDictValue(dict, MAU_TYPE);
            RemotePort = Utils.GetDictValue(dict, LOCAL_PORT);
            Name = Utils.GetDictValue(dict, SYSTEM_NAME).Replace("(null)", string.Empty);
            if (string.IsNullOrEmpty(Name)) Name = DeviceType;
            Description = Utils.GetDictValue(dict, SYSTEM_DESCRIPTION).Replace("(null)", string.Empty);
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
            if (Utils.GetDictValue(dict, LOCAL_PORT) != LocalPort) return;
            Vendor = Utils.GetDictValue(dict, MED_MANUFACTURER).Replace("\"", "");
            Model = Utils.GetDictValue(dict, MED_MODEL).Replace("\"", "");
            HardwareVersion = Utils.GetDictValue(dict, MED_HARDWARE_REVISION).Replace("\"", "");
            SoftwareVersion = Utils.GetDictValue(dict, MED_SOFTWARE_REVISION).Replace("\"", "");
            SerialNumber = Utils.GetDictValue(dict, MED_SERIAL_NUMBER).Replace("\"", "");
        }

        public override string ToString()
        {
            StringBuilder txt = new StringBuilder("Device Type: ");
            txt.Append(string.IsNullOrEmpty(DeviceType) ? "Unknown" : DeviceType);
            if (!string.IsNullOrEmpty(MacAddress)) txt.Append(", MAC: ").Append(MacAddress);
            if (DeviceType.Contains("none")) txt.Append(", Remote Port: ").Append(RemotePort);
            if (!string.IsNullOrEmpty(IpAddress)) txt.Append(", IP: ").Append(IpAddress);
            if (Capabilities?.Count > 0) txt.Append(", Capabilities: [").Append(string.Join(",", Capabilities)).Append("]");
            if (!string.IsNullOrEmpty(Name)) txt.Append(", Class: ").Append(Name);
            if (!string.IsNullOrEmpty(MEDPowerValue)) txt.Append(", Power Value: ").Append(MEDPowerValue);
            if (!string.IsNullOrEmpty(MEDPowerPriority)) txt.Append(", Power Priority: ").Append(MEDPowerPriority);
            return txt.ToString();
        }

    }
}

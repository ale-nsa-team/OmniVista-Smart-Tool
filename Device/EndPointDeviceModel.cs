using System.Collections.Generic;
using System.Text;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    public class EndPointDeviceModel
    {
        public string LocalPort { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string EthernetType { get; set; } = string.Empty;
        public string RemotePort { get; set; } = string.Empty;
        public string MEDType { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new List<string>();
        public string MEDPowerType { get; set; } = string.Empty;
        public string MEDPowerSource { get; set; } = string.Empty;
        public string MEDPowerPriority { get; set; } = string.Empty;
        public string MEDPowerValue { get; set; } = string.Empty;

        public EndPointDeviceModel() { }

        public void LoadLldpRemoteTable(Dictionary<string, string> dict)
        {
            LocalPort = (dict.TryGetValue(LOCAL_PORT, out string s) ? s : "").Trim();
            MacAddress = (dict.TryGetValue(CHASSIS_MAC_ADDRESS, out s) ? s : "").Trim();
            DeviceType = (dict.TryGetValue(CAPABILITIES_ENABLED, out s) ? s : "").Trim();
            IpAddress = (dict.TryGetValue(MED_IP_ADDRESS, out s) ? s : "").Trim();
            EthernetType = (dict.TryGetValue(MAU_TYPE, out s) ? s : "").Trim();
            RemotePort = (dict.TryGetValue(REMOTE_PORT, out s) ? s : "").Trim();
            MEDType = (dict.TryGetValue(MED_DEVICE_TYPE, out s) ? s : "").Trim();
            string[] capList = (dict.TryGetValue(MED_CAPABILITIES, out s) ? s : "").Trim().Split('|');
            if (capList.Length > 1)
            {
                Capabilities.Clear();
                foreach (string val in capList)
                {
                    if (string.IsNullOrEmpty(val) || val.Contains("Capabilities")) continue;
                    Capabilities.Add(val.Trim());
                }
            }
            MEDPowerType = (dict.TryGetValue(MED_POWER_TYPE, out s) ? s : "").Trim();
            MEDPowerSource = (dict.TryGetValue(MED_POWER_SOURCE, out s) ? s : "").Trim();
            MEDPowerPriority = (dict.TryGetValue(MED_POWER_PRIORITY, out s) ? s : "").Trim();
            MEDPowerValue = (dict.TryGetValue(MED_POWER_VALUE, out s) ? s : "").Trim();
        }

        public override string ToString()
        {
            StringBuilder txt = new StringBuilder("Device Type: ");
            txt.Append(string.IsNullOrEmpty(DeviceType) ? "Unknown" : DeviceType);
            if (!string.IsNullOrEmpty(MacAddress)) txt.Append(", MAC: ").Append(MacAddress);
            if (DeviceType.Contains("none")) txt.Append(", Remote Port: ").Append(RemotePort);
            if (!string.IsNullOrEmpty(IpAddress)) txt.Append(", IP: ").Append(IpAddress);
            if (Capabilities?.Count > 0) txt.Append(", Capabilities: [").Append(string.Join(",", Capabilities)).Append("]");
            if (!string.IsNullOrEmpty(MEDType)) txt.Append(", Class: ").Append(MEDType);
            if (!string.IsNullOrEmpty(MEDPowerValue)) txt.Append(", Power Value: ").Append(MEDPowerValue);
            if (!string.IsNullOrEmpty(MEDPowerPriority)) txt.Append(", Power Priority: ").Append(MEDPowerPriority);
            return txt.ToString();
        }

    }
}

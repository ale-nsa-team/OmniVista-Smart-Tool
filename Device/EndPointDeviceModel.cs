using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    public class EndPointDeviceModel
    {
        public string LocalPort { get; set; }
        public string MacAddress { get; set; }
        public string DeviceType { get; set; }
        public string EthernetType { get; set; }
        public string RemotePort { get; set; }
        public string MEDType { get; set; }
        public string MEDCapabilities { get; set; } = string.Empty;
        public string MEDPowerType { get; set; }
        public string MEDPowerSource { get; set; }
        public string MEDPowerPriority { get; set; }
        public string MEDPowerValue { get; set; }

        public EndPointDeviceModel(Dictionary<string, string> dict)
        {
            LocalPort = (dict.TryGetValue(LOCAL_PORT, out string s) ? s : "").Trim();
            MacAddress = (dict.TryGetValue(CHASSIS_MAC_ADDRESS, out s) ? s : "").Trim();
            DeviceType = (dict.TryGetValue(CAPABILITIES_ENABLED, out s) ? s : "").Trim();
            EthernetType = (dict.TryGetValue(MAU_TYPE, out s) ? s : "").Trim();
            RemotePort = (dict.TryGetValue(REMOTE_PORT, out s) ? s : "").Trim();
            MEDType = (dict.TryGetValue(MED_DEVICE_TYPE, out s) ? s : "").Trim();
            MEDCapabilities = (dict.TryGetValue(MED_CAPABILITIES, out s) ? s : "").Trim();
            MEDPowerType = (dict.TryGetValue(MED_POWER_TYPE, out s) ? s : "").Trim();
            MEDPowerSource = (dict.TryGetValue(MED_POWER_SOURCE, out s) ? s : "").Trim();
            MEDPowerPriority = (dict.TryGetValue(MED_POWER_PRIORITY, out s) ? s : "").Trim();
            MEDPowerValue = (dict.TryGetValue(MED_POWER_VALUE, out s) ? s : "").Trim();
        }

    }
}

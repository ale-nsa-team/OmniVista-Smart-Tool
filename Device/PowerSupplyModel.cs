using System;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    [Serializable]
    public class PowerSupplyInfo
    {
        public int ChassisNumber { get; set; }
        public int PowerSupplyNumber { get; set; }
        public string Model { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public int PowerProvision { get; set; }
        public PowerSupplyState Status { get; set; }
        public string PartNumber { get; set; }
        public string HardwareRevision { get; set; }
        public string SerialNumber { get; set; }

        public PowerSupplyInfo() { }
    }
}

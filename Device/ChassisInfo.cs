using System.Collections.Generic;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    public class ChassisInfo
    {
        public int Number { get; set; }
        public string Model { get; set; }
        public string Type { get; set; }
        public bool IsMaster { get; set; }
        public string AdminStatus { get; set; }
        public ChassisStatus Status { get; set; }
        public double PowerBudget { get; set; }
        public double PowerConsumed { get; set; }
        public double PowerRemaining { get; set; }
        public List<SlotInfo> Slots { get; set; }
        public List<PowerSupplyInfo> PowerSupplies { get; set; } = new List<PowerSupplyInfo>();
        public string SerialNumber { get; set; }
        public string PartNumber { get; set; }
        public string HardwareRevision { get; set; }
        public string MacAddress { get; set; }

        public ChassisInfo(Dictionary<string, string> dict)
        {
            Number = int.TryParse(dict["ID"], out int n) ? n : 1;
            Model = dict["Model Name"];
            Type = dict["Module Type"];
            IsMaster = dict["Role"] == "Master";
            AdminStatus = dict["Admin Status"];
            SerialNumber = dict["Serial Number"];
            PartNumber = dict["Part Number"];
            HardwareRevision = dict["Hardware Revision"];
            MacAddress = dict["MAC Address"];
        }

        public ChassisInfo(string sn, string mac, string model)
        {
            SerialNumber = sn;
            MacAddress = mac;
            Model = model;
        }

        private int ParseNumber(string chassis)
        {
            return int.Parse(chassis.Split('/')[0]);
        }
        
    }
}

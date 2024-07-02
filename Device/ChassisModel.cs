using System.Collections.Generic;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    public class ChassisModel
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
        public List<SlotModel> Slots { get; set; }
        public List<PowerSupplyInfo> PowerSupplies { get; set; }
        public string SerialNumber { get; set; }
        public string PartNumber { get; set; }
        public string HardwareRevision { get; set; }
        public string MacAddress { get; set; }

        public ChassisModel(Dictionary<string, string> dict)
        {
            Number = int.TryParse(dict[ID], out int n) ? n : 1;
            Model = dict[MODEL_NAME];
            Type = dict[MODULE_TYPE];
            IsMaster = dict[ROLE] == "Master";
            Status = (dict[OPERATIONAL_STATUS] == "UP") ? ChassisStatus.Up : ChassisStatus.Down;
            AdminStatus = dict[ADMIN_STATUS];
            SerialNumber = dict[SERIAL_NUMBER];
            PartNumber = dict[PART_NUMBER];
            HardwareRevision = dict[HARDWARE_REVISION];
            MacAddress = dict[CHASSIS_MAC_ADDRESS];
            Slots = new List<SlotModel>();
            PowerSupplies = new List<PowerSupplyInfo>();
        }

        public ChassisModel(string sn, string mac, string model)
        {
            SerialNumber = sn;
            MacAddress = mac;
            Model = model;
        }

        public void LoadFromList(List<Dictionary<string, string>> list)
        {
            foreach (Dictionary<string, string> dict in list)
            {
                var slot = this.Slots[ParseIndex(dict[CHAS_SLOT_PORT])];
                if (slot == null) return;
                slot.LoadFromDictionary(dict);
            }
        }

        private int ParseIndex(string chassis)
        {
            return int.TryParse(chassis.Split('/')[1], out int n) ? n -1 : 0;
        }
        
    }
}

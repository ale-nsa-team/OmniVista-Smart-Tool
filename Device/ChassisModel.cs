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
        public List<PowerSupplyModel> PowerSupplies { get; set; }
        public string SerialNumber { get; set; }
        public string PartNumber { get; set; }
        public string HardwareRevision { get; set; }
        public string MacAddress { get; set; }

        public ChassisModel(Dictionary<string, string> dict)
        {
            string nb = dict.TryGetValue(ID, out string s) ? s : "";
            Number = int.TryParse(nb, out int n) ? n : 1;
            Model =  dict.TryGetValue(MODEL_NAME, out s) ? s : "";
            Type = dict.TryGetValue(MODULE_TYPE, out s) ? s : "";
            string role = dict.TryGetValue(ROLE, out s) ? s : "";
            IsMaster = role == "Master";
            AdminStatus = dict.TryGetValue(ADMIN_STATUS, out s) ? s : "";
            SerialNumber = dict.TryGetValue(SERIAL_NUMBER, out s) ? s : "";
            PartNumber = dict.TryGetValue(PART_NUMBER, out s) ? s : "";
            HardwareRevision = dict.TryGetValue(HARDWARE_REVISION, out s) ? s : "";
            MacAddress = dict.TryGetValue(CHASSIS_MAC_ADDRESS, out s) ? s : "";
            Slots = new List<SlotModel>();
            PowerSupplies = new List<PowerSupplyModel>();
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
                int idx = ParseIndex(dict.TryGetValue(CHAS_SLOT_PORT, out string s) ? s : "");
                if (idx < 0) continue;
                var slot = this.Slots[idx];
                if (slot == null) return;
                slot.LoadFromDictionary(dict);
            }
        }

        private int ParseIndex(string chassis)
        {
            string[] parts = chassis.Split('/');
            return parts.Length > 1 ? (int.TryParse(parts[1], out int n) ? n - 1 : -1) : -1;
        }
        
    }
}

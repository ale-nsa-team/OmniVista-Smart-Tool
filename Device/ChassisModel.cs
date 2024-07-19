using System.Collections.Generic;
using System.Linq;
using PoEWizard.Data;
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
        public string OperationalStatus { get; set; }
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
        public SwitchTemperature Temperature { get; set; }

        public ChassisModel(Dictionary<string, string> dict)
        {
            string nb = Utils.GetDictValue(dict, ID);
            Number = int.TryParse(nb, out int n) ? n : 1;
            Model =  Utils.GetDictValue(dict, MODEL_NAME);
            Type = Utils.GetDictValue(dict, MODULE_TYPE);
            string role = Utils.GetDictValue(dict, ROLE);
            IsMaster = role == "Master";
            AdminStatus = Utils.GetDictValue(dict, ADMIN_STATUS);
            OperationalStatus = Utils.GetDictValue(dict, OPERATIONAL_STATUS);
            SerialNumber = Utils.GetDictValue(dict, SERIAL_NUMBER);
            PartNumber = Utils.GetDictValue(dict, PART_NUMBER);
            HardwareRevision = Utils.GetDictValue(dict, HARDWARE_REVISION);
            MacAddress = Utils.GetDictValue(dict, CHASSIS_MAC_ADDRESS);
            Slots = new List<SlotModel>();
            PowerSupplies = new List<PowerSupplyModel>();
            Temperature = null;
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
                int slotId = Utils.ParseNumber(dict.TryGetValue(CHAS_SLOT_PORT, out string s) ? s : "", 1);
                var slot = this.Slots.FirstOrDefault(x => x.Number == slotId);
                if (slot == null) return;
                slot.LoadFromDictionary(dict);
            }
        }

        public void LoadTemperature(Dictionary<string, string> dict)
        {
            Temperature = new SwitchTemperature(dict);
        }

        public SlotModel GetSlot(int slotNumber)
        {
            return Slots.FirstOrDefault(c => c.Number == slotNumber);
        }        
    }
}

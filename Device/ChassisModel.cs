using System.Collections.Generic;
using System.Linq;
using PoEWizard.Data;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.Utils;

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
        public bool SupportsPoE { get; set; }
        public string Fpga { get; set; }
        public string Cpld { get; set; }
        public int Cpu { get; set; }
        public string FlashSize { get; set; }
        public string FlashUsage { get; set; }
        public string FlashSizeUsed { get; set; }
        public string FlashSizeFree { get; set; }
        public string FreeFlash { get; set; }

        public ChassisModel(Dictionary<string, string> dict)
        {
            string nb = GetDictValue(dict, ID);
            Number = int.TryParse(nb, out int n) ? n : 1;
            Model = GetDictValue(dict, MODEL_NAME);
            Type = GetDictValue(dict, MODULE_TYPE);
            string role = GetDictValue(dict, ROLE);
            IsMaster = role == "Master";
            AdminStatus = GetDictValue(dict, ADMIN_STATUS);
            OperationalStatus = GetDictValue(dict, OPERATIONAL_STATUS);
            SerialNumber = GetDictValue(dict, SERIAL_NUMBER);
            PartNumber = GetDictValue(dict, PART_NUMBER);
            HardwareRevision = GetDictValue(dict, HARDWARE_REVISION);
            MacAddress = GetDictValue(dict, CHASSIS_MAC_ADDRESS);
            Slots = new List<SlotModel>();
            PowerSupplies = new List<PowerSupplyModel>();
            Temperature = null;
            SupportsPoE = true;
        }

        public void LoadFromList(List<Dictionary<string, string>> list)
        {
            foreach (Dictionary<string, string> dict in list)
            {
                int slotId = ParseNumber(GetDictValue(dict, CHAS_SLOT_PORT), 1);
                var slot = this.Slots.FirstOrDefault(x => x.Number == slotId);
                if (slot == null) return;
                slot.LoadFromDictionary(dict);
                slot.IsMaster = this.IsMaster;
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

using System;
using System.Collections.Generic;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    [Serializable]
    public class PowerSupplyModel
    {
        public int Id { get; set; }
        public string Model { get; set; }
        public string Type { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string PowerProvision { get; set; }
        public PowerSupplyState Status { get; set; }
        public string PartNumber { get; set; }
        public string HardwareRevision { get; set; }
        public string SerialNumber { get; set; }

        public PowerSupplyModel() { }

        public PowerSupplyModel(int id, string location)
        {
            this.Id = id;
            this.Location = location;
        }

        public void LoadFromDictionary(Dictionary<string, string> dict)
        {
            this.Model = dict.TryGetValue(MODEL_NAME, out string s) ? s : "";
            this.Type = dict.TryGetValue(MODULE_TYPE, out s) ? s : "";
            this.Description = dict.TryGetValue(DESCRIPTION, out s) ? s : "";
            this.PowerProvision = dict.TryGetValue(POWER, out s) ? s : "";
            this.PartNumber = dict.TryGetValue(PART_NUMBER, out s) ? s : "";
            this.SerialNumber = dict.TryGetValue(SERIAL_NUMBER, out s) ? s : "";
            this.HardwareRevision = dict.TryGetValue(HARDWARE_REVISION, out s) ? s : "";
        }
    }
}

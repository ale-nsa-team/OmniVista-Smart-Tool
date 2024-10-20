using System;
using System.Collections.Generic;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.Utils;

namespace PoEWizard.Device
{
    [Serializable]
    public class PowerSupplyModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
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
            this.Model = GetDictValue(dict, MODEL_NAME);
            this.Type = GetDictValue(dict, MODULE_TYPE);
            this.Description = GetDictValue(dict, DESCRIPTION);
            this.PowerProvision = GetDictValue(dict, POWER);
            this.PartNumber = GetDictValue(dict, PART_NUMBER);
            this.SerialNumber = GetDictValue(dict, SERIAL_NUMBER);
            this.HardwareRevision = GetDictValue(dict, HARDWARE_REVISION) ;
        }
    }
}

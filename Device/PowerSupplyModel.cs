using PoEWizard.Data;
using System;
using System.Collections.Generic;
using static PoEWizard.Data.Constants;

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
            this.Model = Utils.GetDictValue(dict, MODEL_NAME);
            this.Type = Utils.GetDictValue(dict, MODULE_TYPE);
            this.Description = Utils.GetDictValue(dict, DESCRIPTION);
            this.PowerProvision = Utils.GetDictValue(dict, POWER);
            this.PartNumber = Utils.GetDictValue(dict, PART_NUMBER);
            this.SerialNumber = Utils.GetDictValue(dict, SERIAL_NUMBER);
            this.HardwareRevision = Utils.GetDictValue(dict, HARDWARE_REVISION) ;
        }
    }
}

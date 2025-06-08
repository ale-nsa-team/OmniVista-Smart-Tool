using System.Collections.Generic;

namespace PoEWizard.Device
{
    public class TransceiverModel
    {
        public int ChassisNumber { get; set; }
        public int SlotNumber { get; set; }
        public int TransceiverNumber { get; set; }
        public string AluModelName { get; set; }
        public string AluModelNumber { get; set; }
        public string HardwareRevision { get; set; }
        public string SerialNumber { get; set; }
        public string ManufactureDate { get; set; }
        public string LaserWaveLength { get; set; }
        public string AdminStatus { get; set; }
        public string OperationalStatus { get; set; }

        public TransceiverModel(Dictionary<string, string> dict)
        {
            ChassisNumber = ParseNumber(GetDictValue(dict, "CHASSIS"));
            SlotNumber = ParseNumber(GetDictValue(dict, "SLOT"));
            TransceiverNumber = ParseNumber(GetDictValue(dict, "TRANSCEIVER"));
            AluModelName = GetDictValue(dict, "ALU MODEL NAME");
            AluModelNumber = GetDictValue(dict, "ALU MODEL NUMBER");
            HardwareRevision = GetDictValue(dict, "HARDWARE REVISION");
            SerialNumber = GetDictValue(dict, "SERIAL NUMBER");
            ManufactureDate = GetDictValue(dict, "MANUFACTURE DATE");
            LaserWaveLength = GetDictValue(dict, "LASER WAVE LENGTH");
            AdminStatus = GetDictValue(dict, "ADMIN STATUS");
            OperationalStatus = GetDictValue(dict, "OPERATIONAL STATUS");
        }

        private int ParseNumber(string value)
        {
            return int.TryParse(value.Replace("*", string.Empty), out int n) ? n : 0;
        }
        
        private string GetDictValue(Dictionary<string, string> dict, string key)
        {
            return dict.ContainsKey(key) ? dict[key] : string.Empty;
        }
    }
}
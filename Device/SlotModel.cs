using System;
using System.Collections.Generic;
using System.Linq;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    [Serializable]
    public class SlotModel
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public int NbPorts { get; set; }
        public int NbPoePorts { get; set; }
        public SlotPoeStatus PoeStatus { get; set; }  
        public double Power { get; set; }
        public double Budget { get; set; }
        public List<PortModel> Ports { get; set; }
        public double Threshold { get; set; }
        public bool Is8023bt {get;set;}
        public bool IsPriorityDisconnect { get; set; }
        public bool IsFPoE { get; set; }
        public bool IsPPoE { get; set; }
        public bool IsClassDetection { get; set; }
        public bool IsHiResDetection { get; set; }

        public SlotModel() { }

        public SlotModel(string slotString)
        {
            this.Number = ParseNumber(slotString, 1);
            this.Name = slotString.Substring(0, slotString.Length - 2);
            this.Ports = new List<PortModel>();
        }

        public void LoadFromDictionary(Dictionary<string, string> dict)
        {
            this.Budget = ParseDouble(dict.TryGetValue(MAX_POWER, out string s) ? s: "0");
            this.Is8023bt = (dict.TryGetValue(BT_SUPPORT, out s) ? s : "") == "Yes";
            this.IsClassDetection = (dict.TryGetValue(CLASS_DETECTION, out s) ? s : "") == "enable";
            this.IsHiResDetection = (dict.TryGetValue(HI_RES_DETECTION, out s) ? s : "") == "enable";
            this.IsPPoE = (dict.TryGetValue(PPOE, out s) ? s : "") == "enable";
            this.IsFPoE = (dict.TryGetValue(FPOE, out s) ? s : "") == "enable";
            this.Threshold = ParseDouble(dict.TryGetValue(USAGE_THRESHOLD, out s) ? s : "0");
        }

        public void LoadFromList(List<Dictionary<string, string>> list, DictionaryType dt)
        {
            foreach (var dict in list)
            {
                string[] split = (dict.TryGetValue((dt == DictionaryType.LanPower) ? PORT : CHAS_SLOT_PORT, out string s) ? s : "0").Split('/');
                if (split.Length < 2) continue;
                string port = (split.Length == 3) ? split[2] : split[1];
                if (port == null) continue;
                if (dt == DictionaryType.LanPower) GetPort(port).LoadPoEData(dict); else GetPort(port).LoadPoEConfig(dict);
            }
            this.NbPoePorts = list.Count;
            this.Power = Ports.Sum(p => p.Power) / 1000;
            double powerConsumedMetric = 100 * this.Power / this.Budget;
            double nearThreshold = 0.9 * this.Threshold;
            if (powerConsumedMetric < nearThreshold)
            {
                this.PoeStatus = SlotPoeStatus.Normal;
            }
            else if (powerConsumedMetric >= nearThreshold && powerConsumedMetric < Threshold)
            {
                this.PoeStatus = SlotPoeStatus.NearThreshold;
            }
            else
            {
                this.PoeStatus = SlotPoeStatus.Critical;
            }
        }

        public PortModel GetPort(string portNumber)
        {
            return Ports.FirstOrDefault(p => p.Number == portNumber);
        }

        public void Clone(SlotModel slot)
        {
            var props = GetType().GetProperties().Where(p => p.CanWrite && p.CanRead);
            foreach (var p in props)
            {
                var value = p.GetValue(slot, null);
                if (value != null) p.SetValue(this, value, null);
            }
        }

        public int ParseNumber(string slot, int index)
        {
            string[] parts = slot.Split('/');
            return parts.Length > index ? (int.TryParse(parts[index], out int n) ? n : 0) : 0;
        }

        public double ParseDouble(string val)
        {
            return double.TryParse(val, out double d) ? d : 0;
        }
    }
}

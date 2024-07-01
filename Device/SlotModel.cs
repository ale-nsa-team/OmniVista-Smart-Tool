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
        public int NbPorts { get; set; }
        public int NbPoePorts { get; set; }
        public double Power { get; set; }
        public double Budget { get; set; }
        public List<PortModel> Ports { get; set; }
        public int Threshold { get; set; }
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
            this.Ports = new List<PortModel>();
        }

        public SlotModel(Dictionary<string, string> dict)
        {
            this.Number = ParseNumber(dict[CHAS_SLOT_PORT], 0);
            this.Budget = ParseDouble(dict[MAX_POWER]);
            this.Threshold = ParseNumber(dict[USAGE_THRESHOLD], 0);
        }

        public void LoadFromDictionary(Dictionary<string, string> dict)
        {
            this.Is8023bt = dict[BT_SUPPORT] == "Yes";
            this.IsClassDetection = dict[CLASS_DETECTION] == "enable";
            this.IsHiResDetection = dict[HI_RES_DETECTION] == "enable";
            this.IsPPoE = dict[PPOE] == "enable";
            this.IsFPoE = dict[FPOE] == "enable";
        }

        public void LoadFromList(List<Dictionary<string, string>> list)
        {
            foreach (var dict in list)
            {
                int p = ParseNumber(dict[PORT], 2) - 1;
                this.Ports[p].LoadFromDictionary(dict);
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
            return int.TryParse(slot.Split('/')[index], out int n) ? n : 0;
        }

        public double ParseDouble(string val)
        {
            return double.TryParse(val, out double d) ? d : 0;
        }
    }
}

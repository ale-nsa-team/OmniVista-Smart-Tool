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

        public SlotModel() { }

        public SlotModel(string slotString)
        {
            this.Number = ParseNumber(slotString);
            this.Ports = new List<PortModel>();
        }

        public SlotModel(Dictionary<string, string> dict)
        {
            this.Number = ParseNumber(dict[CHAS_SLOT_PORT]);
            this.Budget = ParseDouble(dict[MAX_POWER]);
            this.Threshold = ParseNumber(dict[USAGE_THRESHOLD]);
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

        public int ParseNumber(string slot)
        {
            return int.TryParse(slot.Split('/')[0], out int n) ? n : 0;
        }

        public double ParseDouble(string val)
        {
            return double.TryParse(val, out double d) ? d : 0;
        }
    }
}

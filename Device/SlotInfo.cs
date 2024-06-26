using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    [Serializable]
    public class SlotInfo
    {
        public int Number { get; set; }
        public int NbPorts { get; set; }
        public int NbPoePorts { get; set; }
        public double Power { get; set; }
        public double Budget { get; set; }
        public List<SwitchPort> SwitchPorts { get; set; } = new List<SwitchPort>();
        public int Threshold { get; set; }

        public SlotInfo() { }

        public SwitchPort GetPort(string portNumber)
        {
            return SwitchPorts.FirstOrDefault(p => p.Number == portNumber);
        }

        public void Clone(SlotInfo slot)
        {
            var props = GetType().GetProperties().Where(p => p.CanWrite && p.CanRead);
            foreach (var p in props)
            {
                var value = p.GetValue(slot, null);
                if (value != null) p.SetValue(this, value, null);
            }
        }

    }
}

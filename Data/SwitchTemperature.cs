using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Data
{
    public class SwitchTemperature
    {
        public string Device { get; set; }
        public string Current { get; set; }
        public string Range { get; set; }
        public string Threshold { get; set; }
        public string Danger { get; set; }
        public ThresholdType Status { get; set; }

        public SwitchTemperature()
        {
            Device = "";
            Current = "";
            Range = "";
            Threshold = "";
            Status = ThresholdType.Unknown;
        }

        public SwitchTemperature(Dictionary<string, string> dict)
        {
            
            string[] split = (dict.TryGetValue(CHAS_DEVICE, out string s) ? s : "").Trim().Split('/');
            if (split.Length > 1) Device = split[1].Trim();
            Current = (dict.TryGetValue(CURRENT, out s) ? s : "").Trim();
            Range = dict[RANGE].Trim();
            Threshold = dict[THRESHOLD].Trim();
            Danger = dict[DANGER].Trim();
            Current = dict[CURRENT].Trim();
        }
    }
}

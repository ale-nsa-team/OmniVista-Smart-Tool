using System.Collections.Generic;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.Utils;

namespace PoEWizard.Data
{
    public class SwitchTemperature
    {
        public string Device { get; set; }
        public int Current { get; set; }
        public string Range { get; set; }
        public int Threshold { get; set; }
        public int Danger { get; set; }
        public ThresholdType Status { get; set; }

        public SwitchTemperature()
        {
            Device = "";
            Current = 0;
            Range = "";
            Threshold = 0;
            Status = ThresholdType.Unknown;
        }

        public SwitchTemperature(Dictionary<string, string> dict)
        {
            string[] split = GetDictValue(dict, CHAS_DEVICE).Split('/');
            if (split.Length > 1) Device = split[1].Trim();
            Current = StringToInt(GetDictValue(dict, CURRENT));
            Range = GetDictValue(dict, RANGE);
            Threshold = StringToInt(GetDictValue(dict, THRESHOLD));
            Danger = StringToInt(GetDictValue(dict, DANGER));
            if (Current < Threshold) Status = ThresholdType.UnderThreshold;
            else if (Current >= Threshold && Current < Danger) Status = ThresholdType.OverThreshold;
            else Status = ThresholdType.Danger;
        }
    }
}

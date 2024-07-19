using System.Collections.Generic;
using static PoEWizard.Data.Constants;

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
            string[] split = Utils.GetDictValue(dict, CHAS_DEVICE).Split('/');
            if (split.Length > 1) Device = split[1].Trim();
            Current = Utils.StringToInt(Utils.GetDictValue(dict, CURRENT));
            Range = Utils.GetDictValue(dict, RANGE);
            Threshold = Utils.StringToInt(Utils.GetDictValue(dict, THRESHOLD));
            Danger = Utils.StringToInt(Utils.GetDictValue(dict, DANGER));
            if (Current <= Threshold) Status = ThresholdType.UnderThreshold;
            else if (Current >= Danger) Status = ThresholdType.Danger;
            else Status = ThresholdType.OverThreshold;
        }
    }
}

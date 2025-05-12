using PoEWizard.Device;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Data
{
    public static class ConfigChanges
    {

        private static int nbLines;

        public static string GetChanges(SwitchModel switchModel, string prevCfgSnapshot)
        {
            Dictionary<string, List<string>> addedChanges = new Dictionary<string, List<string>>();
            Dictionary<string, List<string>> removedChanges = new Dictionary<string, List<string>>();
            Dictionary<string, List<string>> modifiedChanges = new Dictionary<string, List<string>>();
            Dictionary<string, List<string>> currConfig = CliParseUtils.ParseSwitchConfigChanges(switchModel.ConfigSnapshot);
            Dictionary<string, List<string>> prevConfig = CliParseUtils.ParseSwitchConfigChanges(prevCfgSnapshot);
            foreach (KeyValuePair<string, List<string>> keyVal in currConfig)
            {
                List<string> currList = keyVal.Value;
                if (prevConfig.ContainsKey(keyVal.Key))
                {
                    List<string> prevList = prevConfig[keyVal.Key];
                    if (currList.Count == prevList.Count) modifiedChanges[keyVal.Key] = currList.Except(prevList).ToList();
                    else if (currList.Count > prevList.Count) addedChanges[keyVal.Key] = currList.Except(prevList).ToList();
                    else removedChanges[keyVal.Key] = prevList.Except(currList).ToList();
                }
            }
            nbLines = 0;
            Dictionary<string, StringBuilder> featureChanges = PrintChanges(addedChanges, "Added");
            featureChanges = PrintChanges(removedChanges, "Removed", featureChanges);
            featureChanges = PrintChanges(modifiedChanges, "Modified", featureChanges);
            StringBuilder text = new StringBuilder();
            foreach (KeyValuePair<string, StringBuilder> keyVal in featureChanges)
            {
                text.Append(keyVal.Value);
            }
            return text.ToString();
        }

        private static Dictionary<string, StringBuilder> PrintChanges(Dictionary<string, List<string>> cfgChanges, string action, Dictionary<string, StringBuilder> featureChanges = null)
        {
            if (featureChanges == null) featureChanges = new Dictionary<string, StringBuilder>();
            foreach (KeyValuePair<string, List<string>> keyVal in cfgChanges)
            {
                if (keyVal.Value.Count < 1) continue;
                StringBuilder txt;
                if (featureChanges.ContainsKey(keyVal.Key)) txt = featureChanges[keyVal.Key]; else txt = new StringBuilder();
                txt.Append("\n - ").Append(keyVal.Key).Append(":");
                nbLines++;
                if (nbLines >= MAX_NB_LINES_CHANGES_DISPLAYED)
                {
                    txt.Append(":\n                     . . .");
                    break;
                }
                foreach (string change in keyVal.Value)
                {
                    txt.Append("\n    ").Append(action).Append(": ").Append(change);
                    nbLines++;
                    if (nbLines >= MAX_NB_LINES_CHANGES_DISPLAYED)
                    {
                        txt.Append(":\n                     . . .");
                        break;
                    }
                }
                featureChanges[keyVal.Key] = txt;
            }
            return featureChanges;
        }
    }
}

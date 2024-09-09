using PoEWizard.Device;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Data
{
    public class ConfigChanges
    {
        public SwitchModel SwitchModel { get; set; }
        public Dictionary<string, List<string>> AddedChanges { get; set; }
        public Dictionary<string, List<string>> RemovedChanges { get; set; }

        private int nbLines = 0;

        public ConfigChanges(SwitchModel switchModel, string prevCfgSnapshot)
        {
            this.SwitchModel = switchModel;
            this.AddedChanges = new Dictionary<string, List<string>>();
            this.RemovedChanges = new Dictionary<string, List<string>>();
            Dictionary<string, List<string>> currConfig = CliParseUtils.ParseSwitchConfigChanges(SwitchModel.ConfigSnapshot);
            Dictionary<string, List<string>> prevConfig = CliParseUtils.ParseSwitchConfigChanges(prevCfgSnapshot);
            foreach (KeyValuePair<string, List<string>> keyVal in currConfig)
            {
                List<string> currList = keyVal.Value;
                List<string> addedChanges = new List<string>();
                List<string> removedChanges = new List<string>();
                if (prevConfig.ContainsKey(keyVal.Key))
                {
                    List<string> prevList = prevConfig[keyVal.Key];
                    if (currList.Count > prevList.Count) addedChanges = currList.Except(prevList).ToList();
                    else removedChanges = prevList.Except(currList).ToList();
                }
                if (addedChanges.Count > 0)
                {
                    this.AddedChanges[keyVal.Key] = addedChanges;
                }
                if (removedChanges.Count > 0)
                {
                    this.RemovedChanges[keyVal.Key] = removedChanges;
                }
            }
        }

        public override string ToString()
        {
            this.nbLines = 0;
            Dictionary<string, StringBuilder> featureChanges = GetChanges(this.AddedChanges, false, new Dictionary<string, StringBuilder>());
            featureChanges = GetChanges(this.RemovedChanges, true, featureChanges);
            StringBuilder text = new StringBuilder();
            foreach (KeyValuePair<string, StringBuilder> keyVal in featureChanges)
            {
                text.Append(keyVal.Value);
            }
            return text.ToString();
        }

        private Dictionary<string, StringBuilder> GetChanges(Dictionary<string, List<string>> cfgChanges, bool removed, Dictionary<string, StringBuilder> featureChanges)
        {
            foreach (KeyValuePair<string, List<string>> keyVal in cfgChanges)
            {
                StringBuilder txt;
                if (featureChanges.ContainsKey(keyVal.Key))
                {
                    txt = featureChanges[keyVal.Key];
                }
                else
                {
                    txt = new StringBuilder();
                }
                txt.Append("\n - ").Append(keyVal.Key).Append(":");
                this.nbLines++;
                if (this.nbLines >= MAX_NB_LINES_CHANGES_DISPLAYED)
                {
                    txt.Append(":\n                     . . .");
                    break;
                }
                List<string> changes = keyVal.Value;
                foreach (string change in changes)
                {
                    txt.Append("\n   ");
                    if (removed) txt.Append("Removed: "); else txt.Append("Added: ");
                    txt.Append(change);
                    this.nbLines++;
                    if (this.nbLines >= MAX_NB_LINES_CHANGES_DISPLAYED)
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

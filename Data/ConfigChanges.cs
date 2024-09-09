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
        public Dictionary<string, List<string>> ModifiedChanges { get; set; }

        private int nbLines = 0;

        public ConfigChanges(SwitchModel switchModel, string prevCfgSnapshot)
        {
            this.SwitchModel = switchModel;
            this.AddedChanges = new Dictionary<string, List<string>>();
            this.RemovedChanges = new Dictionary<string, List<string>>();
            this.ModifiedChanges = new Dictionary<string, List<string>>();
            Dictionary<string, List<string>> currConfig = CliParseUtils.ParseSwitchConfigChanges(SwitchModel.ConfigSnapshot);
            Dictionary<string, List<string>> prevConfig = CliParseUtils.ParseSwitchConfigChanges(prevCfgSnapshot);
            foreach (KeyValuePair<string, List<string>> keyVal in currConfig)
            {
                List<string> currList = keyVal.Value;
                if (prevConfig.ContainsKey(keyVal.Key))
                {
                    List<string> prevList = prevConfig[keyVal.Key];
                    if (currList.Count == prevList.Count) this.ModifiedChanges[keyVal.Key] = currList.Except(prevList).ToList();
                    else if (currList.Count > prevList.Count) this.AddedChanges[keyVal.Key] = currList.Except(prevList).ToList();
                    else this.RemovedChanges[keyVal.Key] = prevList.Except(currList).ToList();
                }
            }
        }

        public override string ToString()
        {
            this.nbLines = 0;
            Dictionary<string, StringBuilder> featureChanges = GetChanges(this.AddedChanges, "Added", new Dictionary<string, StringBuilder>());
            featureChanges = GetChanges(this.RemovedChanges, "Removed", featureChanges);
            featureChanges = GetChanges(this.ModifiedChanges, "Modified", featureChanges);
            StringBuilder text = new StringBuilder();
            foreach (KeyValuePair<string, StringBuilder> keyVal in featureChanges)
            {
                text.Append(keyVal.Value);
            }
            return text.ToString();
        }

        private Dictionary<string, StringBuilder> GetChanges(Dictionary<string, List<string>> cfgChanges, string action, Dictionary<string, StringBuilder> featureChanges)
        {
            foreach (KeyValuePair<string, List<string>> keyVal in cfgChanges)
            {
                if (keyVal.Value.Count < 1) continue;
                StringBuilder txt;
                if (featureChanges.ContainsKey(keyVal.Key)) txt = featureChanges[keyVal.Key]; else txt = new StringBuilder();
                txt.Append("\n - ").Append(keyVal.Key).Append(":");
                this.nbLines++;
                if (this.nbLines >= MAX_NB_LINES_CHANGES_DISPLAYED)
                {
                    txt.Append(":\n                     . . .");
                    break;
                }
                foreach (string change in keyVal.Value)
                {
                    txt.Append("\n    ").Append(action).Append(": ").Append(change);
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

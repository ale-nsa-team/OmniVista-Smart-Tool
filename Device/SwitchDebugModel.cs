using PoEWizard.Data;
using System;
using System.Collections.Generic;
using static PoEWizard.Data.Constants;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace PoEWizard.Device
{

    public class DebugAppModel
    {
        public string ApplicationId { get; set; }
        public string ApplicationName { get; set; }
        public string SubApplicationId { get; set; }
        public string SubApplicationName { get; set; }
        public string DebugLevel { get; set; }

        public DebugAppModel()
        {
            this.ApplicationId = string.Empty;
            this.ApplicationName = string.Empty;
            this.SubApplicationId = string.Empty;
            this.SubApplicationName = string.Empty;
            this.DebugLevel = "info";
        }
        public DebugAppModel(Dictionary<string, string> dict)
        {
            LoadFromDictionary(dict);
        }

        public void LoadFromDictionary(Dictionary<string, string> dict)
        {
            
            this.ApplicationId = Utils.GetDictValue(dict, DEBUG_APP_ID);
            this.ApplicationName = Utils.GetDictValue(dict, DEBUG_APP_NAME);
            this.SubApplicationId = Utils.GetDictValue(dict, DEBUG_SUB_APP_ID);
            this.SubApplicationName = Utils.GetDictValue(dict, DEBUG_SUB_APP_NAME);
            this.DebugLevel = Utils.GetDictValue(dict, DEBUG_SUB_APP_LEVEL);
        }
    }

    public class LlpNiModel
    {
        public DebugAppModel LanNi { get; set; }
        public DebugAppModel LanXtr { get; set; }
        public DebugAppModel LanNiUtl { get; set; }
        public string DebugLevel => GetDebugLevel();

        public LlpNiModel()
        {
            this.LanNi = new DebugAppModel();
            this.LanXtr = new DebugAppModel();
            this.LanNiUtl = new DebugAppModel();
        }

        public void LoadLanNiFromDictionary(Dictionary<string, string> dict)
        {
            this.LanNi.LoadFromDictionary(dict);
        }
        public void LoadLanXtrFromDictionary(Dictionary<string, string> dict)
        {
            this.LanXtr.LoadFromDictionary(dict);
        }
        public void LoadLanNiUtlFromDictionary(Dictionary<string, string> dict)
        {
            this.LanNiUtl.LoadFromDictionary(dict);
        }

        private string GetDebugLevel()
        {
            return this.LanNi.DebugLevel;
        }
    }

    public class SwitchDebugModel
    {
        public string LanPowerStatus { get; set; }
        public string LogFilePath { get; set; }
        public string SaveFilePath { get; set; }
        public DebugAppModel LldpNiApp { get; set; }
        public LlpNiModel LpNiApp { get; set; }
        public SwitchDebugLogLevel DebugLevelSelected { get; set; }

        public SwitchDebugModel()
        {
            LanPowerStatus = string.Empty;
            LogFilePath = "/flash/tech_support_complete.tar";
            SaveFilePath = string.Empty;
            LldpNiApp = new DebugAppModel();
            LpNiApp = new LlpNiModel();
            DebugLevelSelected = SwitchDebugLogLevel.Info;
        }

        public void LoadLldpNiFromDictionary(Dictionary<string, string> dict)
        {
            this.LldpNiApp.LoadFromDictionary(dict);
        }

        public void LoadLpNiFromDictionary(List<Dictionary<string, string>> dictList)
        {
            foreach (Dictionary<string, string> dict in dictList)
            {
                string appName = Utils.GetDictValue(dict, DEBUG_SUB_APP_NAME);
                if (appName == DEBUG_SUB_APP_LANNI) this.LpNiApp.LoadLanNiFromDictionary(dict);
                else if (appName == DEBUG_SUB_APP_LANXTR) this.LpNiApp.LoadLanXtrFromDictionary(dict);
                else this.LpNiApp.LoadLanNiUtlFromDictionary(dict);
            }
        }

    }
}

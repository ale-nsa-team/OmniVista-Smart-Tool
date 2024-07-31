using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoEWizard.Device
{
    public class SwitchDebugModel
    {
        public string LanPowerStatus { get; set; }
        public string LogFilePath { get; set; }
        public string SaveFilePath { get; set; }
        public string PrevLLDPNILevel { get; set; }
        public string PrevLanniLPNILevel { get; set; }
        public string PrevLanxtrLPNILevel { get; set; }
        public string DebugLevel { get; set; }
        public SwitchDebugModel(string lanPowerStatus, string debugLevel)
        {
            LanPowerStatus = lanPowerStatus;
            LogFilePath = "/flash/tech_support_complete.tar";
            SaveFilePath = string.Empty;
            PrevLLDPNILevel = "info";
            PrevLanniLPNILevel = "info";
            PrevLanxtrLPNILevel = "info";
            DebugLevel = debugLevel;
        }
    }
}

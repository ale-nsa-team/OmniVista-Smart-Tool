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
        public string PrevDebugLevel { get; set; }
        public string DebugLevel { get; set; }
        public SwitchDebugModel(string lanPowerStatus, string debugLevel)
        {
            LanPowerStatus = lanPowerStatus;
            LogFilePath = "/flash/swlog_archive/swlogvc.tar";
            SaveFilePath = string.Empty;
            PrevDebugLevel = "info";
            DebugLevel = debugLevel;
        }
    }
}

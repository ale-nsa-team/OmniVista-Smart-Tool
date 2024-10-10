using PoEWizard.Data;
using PoEWizard.Exceptions;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{

    public class DebugAppModel
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = string.Empty;
        public string SubApplicationId { get; set; } = string.Empty;
        public string SubApplicationName { get; set; } = string.Empty;
        public SwitchDebugLogLevel DebugLevel { get; set; }
        public int SwitchLogLevel { get; set; }

        public DebugAppModel()
        {
            this.DebugLevel = SwitchDebugLogLevel.Unknown;
            int logLevel = (int)this.DebugLevel;
            SwitchLogLevel = logLevel;
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
            this.SwitchLogLevel = Utils.StringToInt(Utils.GetDictValue(dict, DEBUG_SUB_APP_LEVEL));
            this.DebugLevel = Utils.StringToSwitchDebugLevel(this.SwitchLogLevel.ToString());
        }
    }

    public class LpNiModel
    {
        public DebugAppModel LanNi { get; set; }
        public DebugAppModel LanXtr { get; set; }
        public DebugAppModel LanNiUtl { get; set; }
        public int SwitchLogLevel => GetSwitchDebugLevel();

        public LpNiModel()
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
        private int GetSwitchDebugLevel()
        {
            return this.LanNi.SwitchLogLevel;
        }
    }

    public class LpCmmModel
    {
        public DebugAppModel LanCmm { get; set; }
        public DebugAppModel LanCmmPwr { get; set; }
        public DebugAppModel LanCmmMip { get; set; }
        public DebugAppModel LanCmmUtl { get; set; }
        public int SwitchLogLevel => GetSwitchDebugLevel();

        public LpCmmModel()
        {
            this.LanCmm = new DebugAppModel();
            this.LanCmmPwr = new DebugAppModel();
            this.LanCmmMip = new DebugAppModel();
            this.LanCmmUtl = new DebugAppModel();
        }

        public void LoadLanCmmFromDictionary(Dictionary<string, string> dict)
        {
            this.LanCmm.LoadFromDictionary(dict);
        }
        public void LoadLanCmmPwrFromDictionary(Dictionary<string, string> dict)
        {
            this.LanCmmPwr.LoadFromDictionary(dict);
        }
        public void LoadLanCmmMipFromDictionary(Dictionary<string, string> dict)
        {
            this.LanCmmMip.LoadFromDictionary(dict);
        }
        public void LoadLanCmmUtlFromDictionary(Dictionary<string, string> dict)
        {
            this.LanCmmUtl.LoadFromDictionary(dict);
        }
        private int GetSwitchDebugLevel()
        {
            return this.LanCmm.SwitchLogLevel;
        }
    }

    public class SwitchDebugModel
    {
        public string LanPowerStatus { get; set; }
        public string LocalSavedFilePath { get; set; }
        public LpCmmModel LpCmmApp { get; set; }
        public LpNiModel LpNiApp { get; set; }
        public SwitchDebugLogLevel DebugLevelSelected { get; set; }
        public int IntDebugLevelSelected => GetDebugLevelSelected();
        public WizardReport WizardReport { get; set; }
        public int LpNiLogLevel => GetAppDebugLevel(LPNI);
        public int LpCmmLogLevel => GetAppDebugLevel(LPCMM);

        public SwitchDebugModel() : this(null, SwitchDebugLogLevel.Unknown) { }

        public SwitchDebugModel(WizardReport wizardReport, SwitchDebugLogLevel logLevel)
        {
            this.WizardReport = wizardReport ?? new WizardReport();
            this.LocalSavedFilePath = Path.Combine(MainWindow.dataPath, Path.GetFileName(SWLOG_PATH));
            this.LanPowerStatus = string.Empty;
            this.LpNiApp = new LpNiModel();
            this.LpCmmApp = new LpCmmModel();
            this.DebugLevelSelected = logLevel;
        }

        public void LoadFromDictionary(List<Dictionary<string, string>> dictList)
        {
            if (dictList?.Count > 0)
            {
                bool found = false;
                foreach (Dictionary<string, string> dict in dictList)
                {
                    string appName = Utils.GetDictValue(dict, DEBUG_APP_NAME);
                    if (!string.IsNullOrEmpty(appName) && (appName == LPNI || appName== LPCMM))
                    {
                        found = true;
                        string subAppName = Utils.GetDictValue(dict, DEBUG_SUB_APP_NAME);
                        switch (subAppName)
                        {
                            case DEBUG_SUB_APP_LANNI:
                                this.LpNiApp.LoadLanNiFromDictionary(dict);
                                break;

                            case DEBUG_SUB_APP_LANXTR:
                                this.LpNiApp.LoadLanXtrFromDictionary(dict);
                                break;

                            case DEBUG_SUB_APP_LANUTIL:
                                this.LpNiApp.LoadLanNiUtlFromDictionary(dict);
                                break;

                            case DEBUG_SUB_APP_LANCMM:
                                this.LpCmmApp.LoadLanCmmFromDictionary(dict);
                                break;

                            case DEBUG_SUB_APP_LANCMMPWR:
                                this.LpCmmApp.LoadLanCmmPwrFromDictionary(dict);
                                break;

                            case DEBUG_SUB_APP_LANCMMMIP:
                                this.LpCmmApp.LoadLanCmmMipFromDictionary(dict);
                                break;

                            case DEBUG_SUB_APP_LANCMMUTL:
                                this.LpCmmApp.LoadLanCmmUtlFromDictionary(dict);
                                break;

                        }
                    }
                }
                if (!found)
                {
                    throw new InvalidSwitchCommandResult($"Couldn't find the switch debug application");
                }
            }
        }

        public void UpdateLanPowerStatus(string cliCmd, string resp)
        {
            if (!string.IsNullOrEmpty(resp) && !string.IsNullOrEmpty(cliCmd)) this.LanPowerStatus += $"\n\n\t - CLI command \"{cliCmd}\" response:\n\n{resp}";
        }

        public void CreateTacTextFile(DeviceType deviceType, string localTarFilepath, SwitchModel device, PortModel port)
        {
            LocalSavedFilePath = localTarFilepath;
            string filePath = Path.Combine(Path.GetDirectoryName(LocalSavedFilePath), $"{Path.GetFileNameWithoutExtension(localTarFilepath)}.txt");
            StringBuilder txt = new StringBuilder("Hello tech support,\n");
            if (device != null)
            {
                txt.Append("\n\tThe switch name is ").Append(device.Name).Append(" (").Append(device.IpAddress).Append(") running on Release ").Append(device.Version);
                txt.Append(" with ").Append(device.ChassisList.Count).Append(" chassis:");
                foreach(ChassisModel chassisModel in device.ChassisList)
                {
                    txt.Append("\n\t - Chassis ").Append(chassisModel.Number).Append($" {(chassisModel.IsMaster ? "(Master)" : "(Slave)")} model ");
                    txt.Append(chassisModel.Model).Append(" with serial number ").Append(chassisModel.SerialNumber);
                }
            }
            txt.Append("\n\n\t");
            if (port != null)
            {
                txt.Append("I am having problems with a PoE device on port ").Append(port.Name).Append(".");
                if (port.EndPointDevice == null) txt.Append("  It is a ").Append(deviceType).Append(".");
                else if (port.EndPointDevice != null && !string.IsNullOrEmpty(port.EndPointDevice.Type))
                {
                    txt.Append("  It is a ");
                    if (!string.IsNullOrEmpty(port.EndPointDevice.Description)) txt.Append(port.EndPointDevice.Description);
                    else if (!string.IsNullOrEmpty(port.EndPointDevice.Name)) txt.Append(port.EndPointDevice.Name);
                    else txt.Append(port.EndPointDevice.Type);
                    if (!string.IsNullOrEmpty(port.EndPointDevice.EthernetType)) txt.Append(", ").Append(port.EndPointDevice.EthernetType);
                    txt.Append(".");
                }
                if (WizardReport != null)
                {
                    txt.Append("\n\tI have run the PoE wizard and it did not repair the problem.").Append("\n\nPoE wizard attempts that have failed:").Append(WizardReport.Message);
                }
            }
            else
            {
                txt.Append("I have collected the logs.");
            }
            if (!string.IsNullOrEmpty(this.LanPowerStatus)) txt.Append("\n\nLanpower current status:").Append(this.LanPowerStatus);
            txt.Append("\n\n\tThe switch log tech support .tar file is attached.\n\n\t\tThanks.\n");
            Utils.CreateTextFile(filePath, txt);
        }

        private int GetDebugLevelSelected()
        {
            return (int)this.DebugLevelSelected;
        }

        private int GetAppDebugLevel(string app)
        {
            if (!string.IsNullOrEmpty(app))
            {
                switch (app)
                {
                    case LPNI:
                        return this.LpNiApp.SwitchLogLevel;

                    case LPCMM:
                        return this.LpCmmApp.SwitchLogLevel;
                }
            }
            return (int)SwitchDebugLogLevel.Unknown;
        }

    }
}

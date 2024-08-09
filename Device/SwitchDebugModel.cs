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
        public string ApplicationId { get; set; }
        public string ApplicationName { get; set; }
        public string SubApplicationId { get; set; }
        public string SubApplicationName { get; set; }
        public SwitchDebugLogLevel DebugLevel { get; set; }
        public string SwitchLogLevel { get; set; }

        public DebugAppModel()
        {
            this.ApplicationId = string.Empty;
            this.ApplicationName = string.Empty;
            this.SubApplicationId = string.Empty;
            this.SubApplicationName = string.Empty;
            this.DebugLevel = SwitchDebugLogLevel.Info;
            int logLevel = (int)this.DebugLevel;
            SwitchLogLevel = logLevel.ToString();
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
            this.SwitchLogLevel = Utils.GetDictValue(dict, DEBUG_SUB_APP_LEVEL);
            this.DebugLevel = CliParseUtils.ParseDebugLevel(this.SwitchLogLevel);
        }
    }

    public class LpNiModel
    {
        public DebugAppModel LanNi { get; set; }
        public DebugAppModel LanXtr { get; set; }
        public DebugAppModel LanNiUtl { get; set; }
        public string SwitchLogLevel => GetSwitchDebugLevel();

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
        private string GetSwitchDebugLevel()
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
        public string SwitchLogLevel => GetSwitchDebugLevel();

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
        private string GetSwitchDebugLevel()
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
        public string SwitchDebugLevelSelected { get; set; }
        public WizardReport WizardReport { get; set; }

        public SwitchDebugModel(WizardReport wizardReport, SwitchDebugLogLevel logLevel)
        {
            this.WizardReport = wizardReport;
            this.LocalSavedFilePath = Path.Combine(MainWindow.dataPath, Path.GetFileName(SWLOG_PATH));
            this.LanPowerStatus = string.Empty;
            this.LpNiApp = new LpNiModel();
            this.LpCmmApp = new LpCmmModel();
            this.DebugLevelSelected = logLevel;
            int logDebugLevel = (int)this.DebugLevelSelected;
            this.SwitchDebugLevelSelected = logDebugLevel.ToString();
        }

        public void LoadFromDictionary(List<Dictionary<string, string>> dictList)
        {
            if (dictList?.Count > 0)
            {
                foreach (Dictionary<string, string> dict in dictList)
                {
                    string appName = Utils.GetDictValue(dict, DEBUG_APP_NAME);
                    if (!string.IsNullOrEmpty(appName) && (appName == LPNI || appName== LPCMM))
                    {
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
                    else
                    {
                        throw new InvalidSwitchCommandResult($"Unexpected switch debug application \"{appName}\"");
                    }
                }
            }
        }
        public void CreateTacTextFile(DeviceType deviceType, string localTarFilepath, SwitchModel device, PortModel port)
        {
            LocalSavedFilePath = localTarFilepath;
            string filePath = Path.Combine(Path.GetDirectoryName(LocalSavedFilePath), Constants.TAC_TEXT_FILE_NAME);
            StringBuilder txt = new StringBuilder("Hello tech support,\n\n\tI am having problems with a PoE device");
            if (port != null)
            {
                txt.Append(" on port ").Append(port.Name).Append(".");
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
            }
            else txt.Append(".");
            txt.Append("\n\tI have run the PoE wizard and it did not repair the problem.");
            if (device != null)
            {
                txt.Append("\n\tThe switch IP address is ").Append(device.IpAddress).Append(". It is a ").Append(device.Model);
                txt.Append(" model, running ").Append(device.Version).Append(" with serial number ").Append(device.SerialNumber).Append("\n");
            }
            if (WizardReport != null) txt.Append("\nPoE wizard attempts that have failed:").Append(WizardReport.Message);
            txt.Append("\n\n\tThe switch log tech support .tar file is attached.\n\n\t\tThanks.\n");
            Utils.CreateTextFile(filePath, txt);
        }

    }
}

﻿using Microsoft.Win32;
using PoEWizard.Comm;
using PoEWizard.Components;
using PoEWizard.Data;
using PoEWizard.Device;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static PoEWizard.Data.Constants;

namespace PoEWizard
{
    /// <summary>
    /// Interaction logic for Mainwindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region Private Variables
        private readonly ResourceDictionary darkDict;
        private readonly ResourceDictionary lightDict;
        private ResourceDictionary currentDict;
        private IEnumerable<string> resourceDicts = null;
        private readonly IProgress<ProgressReport> progress;
        private bool reportAck;
        private SftpService sftpService;
        private SwitchModel device;
        private SlotView slotView;
        private PortModel selectedPort;
        private int selectedPortIndex;
        private SlotModel selectedSlot;
        private int prevSlot;
        private int selectedSlotIndex;
        private bool isWaitingSlotOn = false;
        private WizardReport reportResult = new WizardReport();
        private bool isClosing = false;
        private DeviceType selectedDeviceType;
        private string lastIpAddr;
        private string lastPwd;
        private SwitchDebugModel debugSwitchLog;
        private static bool isTrafficRunning = false;
        private string stopTrafficAnalysisReason = string.Empty;
        private int selectedTrafficDuration;
        private DateTime startTrafficAnalysisTime;
        private double maxCollectLogsDur = 0;
        private string lastMacAddress = string.Empty;
        #endregion

        #region public variables
        public static Window Instance { get; private set; }
        public static ThemeType Theme { get; private set; }
        public static string DataPath { get; private set; }
        public static ResourceDictionary Strings { get; private set; }

        public static RestApiService restApiService;
        public static Dictionary<string, string> ouiTable = new Dictionary<string, string>();

        #endregion

        #region constructor and initialization
        public MainWindow()
        {
            device = new SwitchModel();
            //File Version info
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            //datapath
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            DataPath = Path.Combine(appData, fileVersionInfo.CompanyName, fileVersionInfo.ProductName);
            InitializeComponent();
            this.Title += $" (V {string.Join(".", fileVersionInfo.ProductVersion.Split('.').ToList().Take(2))})";
            lightDict = Resources.MergedDictionaries[0];
            darkDict = Resources.MergedDictionaries[1];
            Strings = Resources.MergedDictionaries[2];
            currentDict = darkDict;
            Instance = this;
            Activity.DataPath = DataPath;
            BuildOuiTable();

            // progress report handling
            progress = new Progress<ProgressReport>(report =>
            {
                reportAck = false;
                switch (report.Type)
                {
                    case ReportType.Status:
                        ShowInfoBox(report.Message);
                        break;
                    case ReportType.Error:
                        reportAck = ShowMessageBox(report.Title, report.Message, MsgBoxIcons.Error) == MsgBoxResult.Yes;
                        break;
                    case ReportType.Warning:
                        reportAck = ShowMessageBox(report.Title, report.Message, MsgBoxIcons.Warning) == MsgBoxResult.Yes;
                        break;
                    case ReportType.Info:
                        reportAck = ShowMessageBox(report.Title, report.Message) == MsgBoxResult.Yes;
                        break;
                    case ReportType.Value:
                        if (!string.IsNullOrEmpty(report.Title)) ShowProgress(report.Title, false);
                        if (report.Message == "-1") HideProgress();
                        else _progressBar.Value = double.TryParse(report.Message, out double dVal) ? dVal : 0;
                        break;
                    default:
                        break;
                }
            });
            //check cli arguments
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 3 && !string.IsNullOrEmpty(args[1]) && Utils.IsValidIP(args[1]) && !string.IsNullOrEmpty(args[2]) && !string.IsNullOrEmpty(args[3]))
            {
                device.IpAddress = args[1];
                device.Login = args[2];
                device.Password = args[3].Replace("\r\n", string.Empty);
                Connect();
            }

            SetLanguageMenuOptions();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Utils.SetTitleColor(this);
            _btnConnect.IsEnabled = false;
        }

        private async void OnWindowClosing(object sender, CancelEventArgs e)
        {
            try
            {
                e.Cancel = true;
                string confirm = Translate("i18n_closing");
                stopTrafficAnalysisReason = $"{Translate("i18n_swrsti")} {confirm}";
                bool save = StopTrafficAnalysis(AbortType.Close, $"{Translate("i18n_taDisc")} {device.Name}", Translate("i18n_taSave"), confirm);
                await WaitSaveTrafficAnalysis();
                if (!save) return;
                sftpService?.Disconnect();
                sftpService = null;
                await CloseRestApiService(confirm);
                this.Closing -= OnWindowClosing;
                this.Close();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        #endregion constructor and initialization

        #region event handlers
        private void SwitchMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Login login = new Login(device.Login)
            {
                Password = device.Password,
                IpAddress = string.IsNullOrEmpty(device.IpAddress) ? lastIpAddr : device.IpAddress,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (login.ShowDialog() == true)
            {
                device.Login = login.User;
                device.Password = login.Password;
                device.IpAddress = login.IpAddress;
                Connect();
            }
        }

        private void DisconnectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Connect();
        }

        private void ConnectBtn_Click(object sender, MouseEventArgs e)
        {
            Connect();
        }

        private void ViewLog_Click(object sender, RoutedEventArgs e)
        {
            TextViewer tv = new TextViewer(Translate("i18n_tappLog"), canClear: true)
            {
                Owner = this,
                Filename = Logger.LogPath,
            };
            tv.Show();
        }

        private void ViewActivities_Click(object sender, RoutedEventArgs e)
        {
            TextViewer tv = new TextViewer(Translate("i18n_tactLog"), canClear: true)
            {
                Owner = this,
                Filename = Activity.FilePath
            };
            tv.Show();
        }

        private async void ViewVcBoot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string title =Translate("i18n_lvcboot");
                string msg = $"{title} {Translate("i18n_fromsw")} {device.Name}";
                ShowInfoBox($"{msg}{WAITING}");
                ShowProgress($"{title}{WAITING}");
                Logger.Debug(msg);
                string res = string.Empty;
                string sftpError = null;
                await Task.Run(() =>
                {
                    sftpService = new SftpService(device.IpAddress, device.Login, device.Password);
                    sftpError = sftpService.Connect();
                    if (string.IsNullOrEmpty(sftpError)) res = sftpService.DownloadToMemory(VCBOOT_PATH);
                });
                HideProgress();
                if (!string.IsNullOrEmpty(sftpError))
                {
                    ShowMessageBox(msg, $"{Translate("i18n_noSftp")} {device.Name}!\n{sftpError}", MsgBoxIcons.Warning, MsgBoxButtons.Ok);
                    return;
                }
                TextViewer tv = new TextViewer(Translate("i18n_tvcboot"), res)
                {
                    Owner = this,
                    SaveFilename = device.Name + "-vcboot.cfg"
                };
                Logger.Debug("Displaying vcboot file.");
                tv.Show();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            HideInfoBox();
        }

        private async void ViewSnapshot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowProgress(Translate("i18n_lsnap");
                await Task.Run(() => restApiService.GetSnapshot());
                HideInfoBox();
                HideProgress();
                TextViewer tv = new TextViewer(Translate("i18n_tsnap"), device.ConfigSnapshot)
                {
                    Owner = this,
                    SaveFilename = $"{device.Name}{SNAPSHOT_SUFFIX}"
                };
                tv.Show();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void ViewPS_Click(object sender, RoutedEventArgs e)
        {
            var ps = new PowerSupply(device)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            ps.Show();
        }

        private async void SearchMac_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SelectMacAddress sm = new SelectMacAddress(this) { SearchMacAddress = lastMacAddress };
                sm.ShowDialog();
                if (sm.SearchMacAddress == null) return;
                lastMacAddress = sm.SearchMacAddress;
                await Task.Run(() => restApiService.RefreshMacAndLldpInfo());
                HideInfoBox();
                HideProgress();
                RefreshSlotAndPortsView();
                var sp = new SearchPort(device, lastMacAddress)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                if (sp.PortsFound?.Count > 0)
                {
                    sp.ShowDialog();
                    PortModel portSelected = sp.SelectedPort;
                    if (portSelected == null) return;
                    JumpToSelectedPort(portSelected);
                    return;
                }
                string msg = sp.IsMacAddress ? $"{Translate("i18n_fmac")} {lastMacAddress} " : $"{Translate("i18n_fdev")} \"{lastMacAddress}\")}}";
                ShowMessageBox(Translate("i18n_sport"), $"{msg} {Translate("i18n_onsw")} {device.Name}!", MsgBoxIcons.Warning, MsgBoxButtons.Ok);
            }
            catch (Exception ex)
            {
                HideInfoBox();
                HideProgress();
                Logger.Error(ex);
            }
        }

        private void JumpToSelectedPort(PortModel portSelected)
        {
            if (_portList.Items?.Count > 0)
            {
                string[] split = portSelected.Name.Split('/');
                string slotPortNr = $"{split[0]}/{split[1]}";
                int selIndex = -1;
                for (int idx = 0; idx < _slotsView.Items.Count; idx++)
                {
                    SlotModel slot = _slotsView.Items[idx] as SlotModel;
                    if (slot?.Name == slotPortNr)
                    {
                        selIndex = idx;
                        break;
                    }
                }
                if (selIndex < 0 || selIndex >= _slotsView.Items.Count) return;
                selectedSlotIndex = selIndex;
                _slotsView.SelectedItem = _slotsView.Items[selectedSlotIndex];
                _slotsView.ScrollIntoView(_slotsView.SelectedItem);
                selIndex = -1;
                for (int idx = 0; idx < _portList.Items.Count; idx++)
                {
                    PortModel port = _portList.Items[idx] as PortModel;
                    if (port?.Name == portSelected.Name)
                    {
                        selIndex = idx;
                        break;
                    }
                }
                if (selIndex < 0 || selIndex >= _portList.Items.Count) return;
                selectedPortIndex = selIndex;
                _portList.SelectedItem = _portList.Items[selectedPortIndex];
                _portList.ScrollIntoView(_portList.SelectedItem);
            }
        }

        private async void FactoryReset(object sender, RoutedEventArgs e)
        {
            MsgBoxResult res = ShowMessageBox(Translate("i18n_tfrst"), Translate("i18n_cfrst"), MsgBoxIcons.Question, MsgBoxButtons.OkCancel);
            if (res == MsgBoxResult.Cancel) return;
            PassCode pc = new PassCode(this);
            if (pc.ShowDialog() == false) return;
            if (pc.Password != pc.SavedPassword)
            {
                ShowMessageBox(Translate("i18n_tfrst"), Translate("i18n_badPwd"), MsgBoxIcons.Error);
                return;
            }
            Logger.Warn($"Switch S/N {device.SerialNumber} Model {device.Model}: Factory reset applied!");
            Activity.Log(device, "Factory reset applied");
            ShowProgress(Translate("i18n_afrst"));
            FactoryDefault.Progress = progress;
            await Task.Run(() => FactoryDefault.Reset(device));
            string tout = await RebootSwitch(420);
            SetDisconnectedState();
            if (string.IsNullOrEmpty(tout)) return;
            res = ShowMessageBox(Translate("i18n_rebsw"), $"{tout}\n{Translate("i18n_recsw")} {device.Name}?", MsgBoxIcons.Info, MsgBoxButtons.YesNo);
            if (res == MsgBoxResult.Yes) Connect();
        }

        private void LaunchConfigWizard(object sender, RoutedEventArgs e)
        {
            _status.Text = Translate("i18n_runCW");

            ConfigWiz wiz = new ConfigWiz(device)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            bool wasApplyed = (bool)wiz.ShowDialog();
            if (!wasApplyed)
            {
                HideProgress();
                return;
            }
            if (wiz.Errors.Count > 0)
            {
                string errMsg = wiz.Errors.Count > 1 ? Translate("i18n_cwErrs").Replace("$1", $"{wiz.Errors.Count}") : Translate("i18n_cwErr");
                ShowMessageBox("Wizard", $"{errMsg}\n\n\u2022 {string.Join("\n\u2022 ", wiz.Errors)}", MsgBoxIcons.Error);
                Logger.Warn($"Configuration from Wizard applyed with errors:\n\t{string.Join("\n\t", wiz.Errors)}");
                Activity.Log(device, "Wizard applied with errors");
            }
            else
            {
                Activity.Log(device, "Config Wizard applied");
            }
            HideInfoBox();
            if (wiz.MustDisconnect)
            {
                ShowMessageBox("Config Wiz", Translate("i18n_discWiz"));
                isClosing = true; // to avoid write memory prompt
                Connect();
                return;
            }
            if (device.SyncStatus == SyncStatusType.Synchronized) device.SyncStatus = SyncStatusType.NotSynchronized;
            _status.Text = DEFAULT_APP_STATUS;
            SetConnectedState();
        }

        private void RunWiz_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPort == null) return;
            var ds = new DeviceSelection(selectedPort.Name)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                DeviceType = selectedDeviceType
            };

            if (ds.ShowDialog() == true)
            {
                selectedDeviceType = ds.DeviceType;
                LaunchPoeWizard();
            }
        }

        private async void ResetPort_Click(object sender, RoutedEventArgs e)
        {
            string title = $"{Translate("i18n_rstpp")} {selectedPort.Name}";
            if (selectedPort == null) return;
            try
            {
                if (ShowMessageBox(title, $"{Translate("i18n_cprst")} {selectedPort.Name} {Translate("i18n_onsw")} {device.Name}?", 
                    MsgBoxIcons.Warning, MsgBoxButtons.YesNo) == MsgBoxResult.No)
                {
                    return;
                }
                DisableButtons();
                string barText = $"{title}{WAITING}";
                ShowInfoBox(barText);
                ShowProgress(barText);
                await Task.Run(() => restApiService.ResetPort(selectedPort.Name, 60));
                HideProgress();
                HideInfoBox();
                await RefreshChanges();
                HideInfoBox();
                await WaitAckProgress();
            }
            catch (Exception ex)
            {
                HideProgress();
                HideInfoBox();
                Logger.Error(ex);
            }
            EnableButtons();
        }

        private void RefreshSwitch_Click(object sender, RoutedEventArgs e)
        {
            RefreshSwitch();
        }

        private void WriteMemory_Click(object sender, RoutedEventArgs e)
        {
            WriteMemory();
        }

        private void Reboot_Click(object sender, RoutedEventArgs e)
        {
            PassCode pc = new PassCode(this);
            if (pc.ShowDialog() == true)
            {
                if (pc.Password != pc.SavedPassword)
                {
                    ShowMessageBox(Translate("i18n_reboot"), Translate("i18n_badPwd"), MsgBoxIcons.Error);
                }
                else
                {
                    LaunchRebootSwitch();
                }
            } 
        }

        private async void CollectLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MsgBoxResult restartPoE = ShowMessageBox("Collect Logs", $"Do you want to power cycle PoE on all ports of switch {device.Name} to collect the logs?", MsgBoxIcons.Warning, MsgBoxButtons.YesNoCancel);
                if (restartPoE == MsgBoxResult.Cancel) return;
                string txt = $"Collect Logs launched by the user";
                if (restartPoE == MsgBoxResult.Yes)
                {
                    maxCollectLogsDur = MAX_COLLECT_LOGS_RESET_POE_DURATION;
                    txt += " (power cycle PoE on all ports)";
                }
                else
                {
                    maxCollectLogsDur = MAX_COLLECT_LOGS_DURATION;
                }
                Logger.Activity($"{txt} on switch {device.Name}");
                Activity.Log(device, $"{txt}.");
                DisableButtons();
                string sftpError = await RunCollectLogs(restartPoE == MsgBoxResult.Yes, null);
                if (!string.IsNullOrEmpty(sftpError)) ShowMessageBox($"Collecting logs on switch {device.Name}", $"Cannot connect secure FTP on switch {device.Name}!\n{sftpError}", MsgBoxIcons.Warning, MsgBoxButtons.Ok);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            EnableButtons();
            HideProgress();
            HideInfoBox();
        }

        private void Traffic_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                isTrafficRunning = _trafficLabel.Content.ToString() != Translate("i18n_taIdle");
                if (!isTrafficRunning)
                {
                    var ds = new TrafficAnalysis() { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
                    if (ds.ShowDialog() == true)
                    {
                        selectedTrafficDuration = ds.TrafficDurationSec;
                        StartTrafficAnalysis();
                    }
                }
                else
                {
                    stopTrafficAnalysisReason = "interrupted by the user";
                    StopTrafficAnalysis(AbortType.CanceledByUser, "Traffic Analysis", "Are you sure you want to stop it");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private bool StopTrafficAnalysis(AbortType abortType, string title, string question, string confirm = null)
        {
            if (!isTrafficRunning) return true;
            try
            {
                StringBuilder txt = new StringBuilder("The traffic analysis is still running (selected duration: ");
                string strSelDuration = string.Empty;
                int duration = 0;
                if (selectedTrafficDuration >= 60 && selectedTrafficDuration < 3600)
                {
                    duration = selectedTrafficDuration / 60;
                    strSelDuration = $"{duration} {MINUTE}";
                }
                else
                {
                    duration = selectedTrafficDuration / 3600;
                    strSelDuration = $"{duration} {HOUR}";
                }
                if (duration > 1) strSelDuration += "s";
                txt.Append(strSelDuration).Append(").\nCurrent duration: ").Append(Utils.CalcStringDuration(startTrafficAnalysisTime, true));
                txt.Append("\n").Append(question).Append("?");
                MsgBoxResult res = ShowMessageBox(title, txt.ToString(), MsgBoxIcons.Warning, MsgBoxButtons.YesNo);
                if (res == MsgBoxResult.Yes)
                {
                    restApiService?.StopTrafficAnalysis(AbortType.CanceledByUser, stopTrafficAnalysisReason);
                }
                else if (abortType == AbortType.Close)
                {
                    if (!string.IsNullOrEmpty(confirm))
                    {
                        res = ShowMessageBox(title, $"Do you still want to continue {confirm}?", MsgBoxIcons.Warning, MsgBoxButtons.YesNo);
                        if (res == MsgBoxResult.No) return false;
                    }
                    restApiService?.StopTrafficAnalysis(abortType, stopTrafficAnalysisReason);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            HideInfoBox();
            HideProgress();
            return true;
        }

        private async Task WaitSaveTrafficAnalysis()
        {
            await Task.Run(() =>
            {
                while (isTrafficRunning)
                {
                    Thread.Sleep(250);
                }
            });
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            string hlpFile = Theme == ThemeType.Dark ? "help-dark.html" : "help-light.html";
            HelpViewer hv = new HelpViewer(hlpFile)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            hv.Show();
        }

        private void LogLevelItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            string level = mi.Header.ToString();
            if (mi.IsChecked) return;
            foreach (MenuItem item in _logLevels.Items)
            {
                item.IsChecked = false;
            }
            mi.IsChecked = true;
            LogLevel lvl = (LogLevel)Enum.Parse(typeof(LogLevel), level);
            Logger.LogLevel = lvl;
        }

        private void ThemeItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            string t = mi.Header.ToString();
            if (mi.IsChecked) return;
            mi.IsChecked = true;
            if (t == Translate("i18n_dark"))
            {
                _lightMenuItem.IsChecked = false;
                Theme = ThemeType.Dark;
                Resources.MergedDictionaries.Remove(lightDict);
                Resources.MergedDictionaries.Add(darkDict);
                currentDict = darkDict;
            }
            else
            {
                _darkMenuItem.IsChecked = false;
                Theme = ThemeType.Light;
                Resources.MergedDictionaries.Remove(darkDict);
                Resources.MergedDictionaries.Add(lightDict);
                currentDict = lightDict;
            }
            if (slotView?.Slots.Count == 1) //do not highlight if only one row
            {
                _slotsView.CellStyle = currentDict["gridCellNoHilite"] as Style;
            }
            Utils.SetTitleColor(this);
            //force color converters to run
            DataContext = null;
            DataContext = device;
        }

        private void LangItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            if (mi.IsChecked) return;
            string l = mi.Header.ToString().Replace("-", "").ToLower();
            var dict = resourceDicts.FirstOrDefault(d => d.Contains(l));
            Resources.MergedDictionaries.Remove(Strings);
            Strings = new ResourceDictionary
            {
                Source = new Uri(dict, UriKind.Relative)
            };
            Resources.MergedDictionaries.Add(Strings);
            mi.IsChecked = true;
            foreach (MenuItem i in _langMenu.Items)
            {
                if (i != mi) i.IsChecked = false;
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            AboutBox about = new AboutBox
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            about.Show();
        }

        private void SlotSelection_Changed(object sender, RoutedEventArgs e)
        {
            if (_slotsView.SelectedItem is SlotModel slot)
            {
                prevSlot = selectedSlotIndex;
                selectedSlot = slot;
                device.SelectedSlot = slot.Name;
                selectedSlotIndex = _slotsView.SelectedIndex;
                device.UpdateSelectedSlotData(slot.Name);
                DataContext = null;
                DataContext = device;
                _portList.ItemsSource = slot.Ports;
                _btnResetPort.IsEnabled = false;
                _btnRunWiz.IsEnabled = false;
            }
        }

        private void PortSelection_Changed(Object sender, RoutedEventArgs e)
        {
            if (_portList.SelectedItem is PortModel port)
            {
                selectedPort = port;
                selectedPortIndex = _portList.SelectedIndex;
                _btnRunWiz.IsEnabled = selectedPort.Poe != PoeStatus.NoPoe;
                _btnResetPort.IsEnabled = true;
            }
        }

        private async void Priority_Changed(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var cb = sender as ComboBox;
                PortModel port = _portList.CurrentItem as PortModel;
                if (cb.SelectedValue.ToString() != port.PriorityLevel.ToString())
                {
                    ShowMessageBox("Priority", $"Selected priority: {cb.SelectedValue}");
                    PriorityLevelType prevPriority = port.PriorityLevel;
                    port.PriorityLevel = (PriorityLevelType)Enum.Parse(typeof(PriorityLevelType), cb.SelectedValue.ToString());
                    if (port == null) return;
                    string txt = $"Changing Priority to {port.PriorityLevel} on port {port.Name}";
                    ShowProgress($"{txt}...");
                    bool ok = false;
                    await Task.Run(() => ok = restApiService.ChangePowerPriority(port.Name, port.PriorityLevel));
                    if (ok)
                    {
                        await WaitAckProgress();
                    }
                    else
                    {
                        port.PriorityLevel = prevPriority;
                        Logger.Error($"Couldn't change the Priority to {port.PriorityLevel} on port {port.Name} of Switch {device.IpAddress}");
                    }
                    await RefreshChanges();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                HideInfoBox();
                HideProgress();
            }
        }

        private async void Power_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (selectedSlot == null || !cb.IsKeyboardFocusWithin) return;
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(cb), null);
            Keyboard.ClearFocus();
            if (!selectedSlot.SupportsPoE)
            {
                ShowMessageBox("PoE OFF", $"Slot {selectedSlot.Name} doesn't support PoE!", MsgBoxIcons.Error);
                cb.IsChecked = false;
                return;
            }
            if (cb.IsChecked == false)
            {
                string msg = $"Are you sure you want to turn PoE Off on all ports in slot {selectedSlot.Name}?";
                MsgBoxResult poweroff = ShowMessageBox("PoE OFF", msg, MsgBoxIcons.Question, MsgBoxButtons.YesNo);
                if (poweroff == MsgBoxResult.Yes)
                {
                    PassCode pc = new PassCode(this);
                    if (pc.ShowDialog() == false)
                    {
                        cb.IsChecked = true;
                        return;
                    }
                    if (pc.Password != pc.SavedPassword)
                    {
                        ShowMessageBox("Slot PoE Off", "Invalid password", MsgBoxIcons.Error);
                        cb.IsChecked = true;
                        return;
                    }
                    DisableButtons();
                    await PowerSlotUpOrDown(Command.POWER_DOWN_SLOT, selectedSlot.Name);
                    Logger.Activity($"PoE on slot {selectedSlot.Name} turned off");
                    Activity.Log(device, $"PoE on slot {selectedSlot.Name} turned off");
                    return;
                }
                else 
                {
                    cb.IsChecked = true;
                    return;
                }
            }
            else if (isWaitingSlotOn)
            {
                selectedSlot = slotView.Slots[prevSlot];
                _slotsView.SelectedIndex = prevSlot;
                ShowMessageBox("PoE On", $"Please wait for slot {selectedSlot.Name} to come up");
                cb.IsChecked = false;
                return;
            }
            else
            {
                isWaitingSlotOn = true;
                DisableButtons();
                ShowProgress($"Turning slot {selectedSlot.Name} PoE ON");
                await Task.Run(() =>
                {
                    restApiService.PowerSlotUpOrDown(Command.POWER_UP_SLOT, selectedSlot.Name);
                    WaitSlotPortsUp();
                });
                Logger.Activity($"PoE on slot {selectedSlot.Name} turned on");
                Activity.Log(device, $"PoE on slot {selectedSlot.Name} turned on");
                RefreshSlotsAndPorts();
                isWaitingSlotOn = false;
            }
            slotView = new SlotView(device);
            _slotsView.ItemsSource = slotView.Slots;
        }

        private async void FPoE_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (selectedSlot == null || !cb.IsKeyboardFocusWithin) return;
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(cb), null);
            Keyboard.ClearFocus();
            Command cmd = (cb.IsChecked == true) ? Command.POE_FAST_ENABLE : Command.POE_FAST_DISABLE;
            bool res = await SetPerpetualOrFastPoe(cmd);
            if (!res) cb.IsChecked = !cb.IsChecked;
        }

        private async void PPoE_Click(Object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (selectedSlot == null || !cb.IsKeyboardFocusWithin) return;
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(cb), null);
            Keyboard.ClearFocus();
            Command cmd = (cb.IsChecked == true) ? Command.POE_PERPETUAL_ENABLE : Command.POE_PERPETUAL_DISABLE;
            bool res = await SetPerpetualOrFastPoe(cmd);
            if (!res) cb.IsChecked = !cb.IsChecked;
        }

        private async Task<bool> SetPerpetualOrFastPoe(Command cmd)
        {
            try
            {
                string action = cmd == Command.POE_PERPETUAL_ENABLE || cmd == Command.POE_FAST_ENABLE ? "Enabling" : "Disabling";
                string poeType = (cmd == Command.POE_PERPETUAL_ENABLE || cmd == Command.POE_PERPETUAL_DISABLE) ? "Perpetual" : "Fast";
                ShowProgress($"{action} {poeType} PoE on slot {selectedSlot.Name}...");
                bool ok = false;
                await Task.Run(() => ok = restApiService.SetPerpetualOrFastPoe(selectedSlot, cmd));
                await WaitAckProgress();
                await RefreshChanges();
                return ok;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                HideProgress();
                HideInfoBox();
            }
            return false;
        }

        private async Task RefreshChanges()
        {
            await Task.Run(() => restApiService.GetSystemInfo());
            RefreshSlotAndPortsView();
            EnableButtons();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion event handlers

        #region private methods

        private void SetLanguageMenuOptions()
        {
            string pattern = @"(.*)(strings-)(.+)(.xaml)";
            List<string> langs = new List<string>();
            var assembly = Assembly.GetExecutingAssembly();
            var resourcesName = assembly.GetName().Name + ".g";
            var manager = new System.Resources.ResourceManager(resourcesName, assembly);
            var resourceSet = manager.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            var resDict = resourceSet.OfType<DictionaryEntry>();
            resourceDicts =
              from entry in resourceSet.OfType<DictionaryEntry>()
              let fileName = (string)entry.Key
              where fileName.StartsWith("resources") && fileName.EndsWith(".baml")
              select fileName.Substring(0, fileName.Length - 5) + ".xaml";

            List<string> sorted = resourceDicts.ToList();
            sorted.Sort();

            foreach (var file in sorted)
            {
                Match match = Regex.Match(file, pattern);
                if (match.Success )
                {
                    string name = match.Groups[match.Groups.Count - 2].Value;
                    string iheader = name.Substring(0, name.Length - 2) + "-" + name.Substring(name.Length - 2).ToUpper();
                    MenuItem item = new MenuItem { Header = iheader };
                    if (Strings.Source.ToString().ToLower().Contains(name)) item.IsChecked = true;
                    item.Click += new RoutedEventHandler(LangItem_Click);
                    _langMenu.Items.Add(item);
                }
            }
        }

        private string Translate(string key)
        {
            return (string)Strings[key] ?? key;
        }

        private async void Connect()
        {
            try
            {
                if (string.IsNullOrEmpty(device.IpAddress))
                {
                    device.IpAddress = lastIpAddr;
                    device.Login = "admin";
                    device.Password = lastPwd;
                }
                if (device.IsConnected)
                {
                    string textMsg = $"Disconnecting switch {device.Name}";
                    string confirm = $"disconnecting the switch {device.Name}";
                    stopTrafficAnalysisReason = $"interrupted by the user before {confirm}";
                    bool save = StopTrafficAnalysis(AbortType.Close, textMsg, Translate("i18n_taSave"), confirm);
                    if (!save) return;
                    await WaitSaveTrafficAnalysis();
                    ShowProgress($"{textMsg}{WAITING}");
                    await CloseRestApiService($"{textMsg}");
                    SetDisconnectedState();
                    return;
                }
                restApiService = new RestApiService(device, progress);
                isClosing = false;
                DateTime startTime = DateTime.Now;
                reportResult = new WizardReport();
                await Task.Run(() => restApiService.Connect(reportResult));
                UpdateConnectedState();
                await CheckSwitchScanResult($"Connect to switch {device.Name}...", startTime);
                if (device.RunningDir == CERTIFIED_DIR)
                {
                    await AskRebootCertified();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                HideProgress();
                HideInfoBox();
            }
        }

        private async Task AskRebootCertified()
        {
            string msg = $"The switch booted on {CERTIFIED_DIR} directory, no changes can be saved.\n" +
                $"Do you want to reboot the switch on {WORKING_DIR} directory?";
            MsgBoxResult reboot = ShowMessageBox("Connection", msg, MsgBoxIcons.Warning, MsgBoxButtons.YesNo);
            if (reboot == MsgBoxResult.Yes)
            {
                string txt = await RebootSwitch(420);
                if (ShowMessageBox("Reboot Switch", $"{txt}\nDo you want to reconnect to the switch {device.Name}?", MsgBoxIcons.Info, MsgBoxButtons.YesNo) == MsgBoxResult.Yes)
                {
                    Connect();
                }
            }
            else
            {
                _writeMemory.IsEnabled = false;
                _cfgMenuItem.IsEnabled = false;
            }
        }

        private void BuildOuiTable()
        {
            string[] ouiEntries = null;
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, OUI_FILE);
            if (File.Exists(filePath))
            {
                ouiEntries = File.ReadAllLines(filePath);
            }
            else
            {
                filePath = Path.Combine(MainWindow.DataPath, OUI_FILE);
                if (File.Exists(filePath)) ouiEntries = File.ReadAllLines(filePath);

            }
            ouiTable = new Dictionary<string, string>();
            if (ouiEntries?.Length > 0)
            {
                for (int idx = 1; idx < ouiEntries.Length; idx++)
                {
                    string[] split = ouiEntries[idx].Split(',');
                    ouiTable[split[1].ToLower()] = split[2].Trim().Replace("\"", "");
                }
            }
        }

        private async Task CloseRestApiService(string title)
        {
            try
            {
                if (restApiService != null && !isClosing)
                {
                    string cfgChanges = await GetSyncStatus(title);
                    isClosing = true;
                    if (device.RunningDir != CERTIFIED_DIR && device.SyncStatus == SyncStatusType.NotSynchronized)
                    {
                        if (AuthorizeWriteMemory("Write Memory required", cfgChanges))
                        {
                            DisableButtons();
                            _comImg.Visibility = Visibility.Collapsed;
                            await Task.Run(() => restApiService.WriteMemory());
                            _comImg.Visibility = Visibility.Visible;
                        }
                    }
                    restApiService.Close();
                }
                await Task.Run(() => Thread.Sleep(250)); //needed for the closing event handler
            }
            catch (Exception ex)
            {
                Logger.Warn(ex.Message);
            }
            finally
            {
                HideProgress();
                HideInfoBox();
            }
        }

        private async void LaunchPoeWizard()
        {
            try
            {
                DisableButtons();
                ProgressReport wizardProgressReport = new ProgressReport("PoE Wizard Report:");
                reportResult = new WizardReport();
                string barText = $"Running PoE Wizard on port {selectedPort.Name}{WAITING}";
                ShowProgress(barText);
                DateTime startTime = DateTime.Now;
                await Task.Run(() => restApiService.ScanSwitch($"Running PoE Wizard on port {selectedPort.Name}...", reportResult));
                ShowProgress(barText);
                switch (selectedDeviceType)
                {
                    case DeviceType.Camera:
                        await RunWizardCamera();
                        break;
                    case DeviceType.Phone:
                        await RunWizardTelephone();
                        break;
                    case DeviceType.AP:
                        await RunWizardWirelessRouter();
                        break;
                    default:
                        await RunWizardOther();
                        break;
                }
                WizardResult result = reportResult.GetReportResult(selectedPort.Name);
                if (result == WizardResult.NothingToDo || result == WizardResult.Fail)
                {
                    await RunLastWizardActions();
                    result = reportResult.GetReportResult(selectedPort.Name);
                }
                string msg = $"{reportResult.Message}\n\nTotal duration: {Utils.CalcStringDuration(startTime, true)}";
                await Task.Run(() => restApiService.RefreshSwitchPorts());
                if (!string.IsNullOrEmpty(reportResult.Message))
                {
                    wizardProgressReport.Title = "PoE Wizard Report:";
                    wizardProgressReport.Type = result == WizardResult.Fail ? ReportType.Error : ReportType.Info;
                    wizardProgressReport.Message = msg;
                    progress.Report(wizardProgressReport);
                    await WaitAckProgress();
                }
                StringBuilder txt = new StringBuilder("PoE Wizard completed on port ");
                txt.Append(selectedPort.Name).Append(" with device type ").Append(selectedDeviceType).Append(":").Append(msg).Append("\nPoE status: ").Append(selectedPort.Poe);
                txt.Append(", Port Status: ").Append(selectedPort.Status).Append(", Power: ").Append(selectedPort.Power).Append(" Watts");
                if (selectedPort.EndPointDevice != null) txt.Append("\n").Append(selectedPort.EndPointDevice);
                else if (selectedPort.MacList?.Count > 0 && !string.IsNullOrEmpty(selectedPort.MacList[0])) txt.Append(", Device MAC: ").Append(selectedPort.MacList[0]);
                Logger.Activity(txt.ToString());
                Activity.Log(device, $"PoE Wizard execution {(result == WizardResult.Fail ? "failed" : "succeeded")} on port {selectedPort.Name}");
                RefreshSlotAndPortsView();
                if (result == WizardResult.Fail)
                {
                    MsgBoxResult res = ShowMessageBox("Wizard", "It looks like the wizard was unable to fix the problem.\nDo you want to collect information to send to technical support?",
                                              MsgBoxIcons.Question, MsgBoxButtons.YesNo);
                    if (res == MsgBoxResult.No) return;
                    maxCollectLogsDur = MAX_COLLECT_LOGS_WIZARD_DURATION;
                    string sftpError = await RunCollectLogs(true, selectedPort);
                    if (!string.IsNullOrEmpty(sftpError)) ShowMessageBox(barText, $"Cannot connect secure FTP on switch {device.Name}!\n{sftpError}", MsgBoxIcons.Warning, MsgBoxButtons.Ok);
                }
                await GetSyncStatus(null);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                EnableButtons();
                HideProgress();
                HideInfoBox();
                sftpService?.Disconnect();
                sftpService = null;
                debugSwitchLog = null;
            }
        }

        private async Task<string> RunCollectLogs(bool restartPoE, PortModel port)
        {
            ShowInfoBox($"Collecting logs on switch {device.Name}{WAITING}");
            string sftpError = null;
            await Task.Run(() =>
            {
                sftpService = new SftpService(device.IpAddress, device.Login, device.Password);
                sftpError = sftpService.Connect();
                if (string.IsNullOrEmpty(sftpError)) sftpService.DeleteFile(SWLOG_PATH);
            });
            if (!string.IsNullOrEmpty(sftpError)) return sftpError;
            string barText = "Cleaning up current log{WAITING}";
            ShowInfoBox(barText);
            StartProgressBar(barText);
            await Task.Run(() => sftpService.DeleteFile(SWLOG_PATH));
            await GenerateSwitchLogFile(restartPoE, port);
            return null;
        }

        private async Task GenerateSwitchLogFile(bool restartPoE, PortModel port)
        {
            const double MAX_WAIT_SFTP_RECONNECT_SEC = 60;
            const double MAX_WAIT_TAR_FILE_SEC = 180;
            const double PERIOD_SFTP_RECONNECT_SEC = 30;
            try
            {
                StartProgressBar($"Collecting logs on switch {device.Name}{WAITING}");
                DateTime initialTime = DateTime.Now;
                debugSwitchLog = new SwitchDebugModel(reportResult, SwitchDebugLogLevel.Debug3);
                await Task.Run(() => restApiService.RunGetSwitchLog(debugSwitchLog, restartPoE, maxCollectLogsDur, port?.Name));
                StartProgressBar($"Downloading tar file{WAITING}");
                long fsize = 0;
                long previousSize = -1;
                DateTime startTime = DateTime.Now;
                string strDur = string.Empty;
                string msg;
                int waitCnt = 0;
                DateTime resetSftpConnectionTime = DateTime.Now;
                while (waitCnt < 2)
                {
                    strDur = Utils.CalcStringDuration(startTime, true);
                    msg = !string.IsNullOrEmpty(strDur) ? $"Waiting for tar file ready ({strDur}){WAITING}" : $"Waiting for tar file ready{WAITING}";
                    if (fsize > 0) msg += $"\nFile size: {Utils.PrintNumberBytes(fsize)}";
                    ShowInfoBox(msg);
                    UpdateSwitchLogBar(initialTime);
                    await Task.Run(() =>
                    {
                        previousSize = fsize;
                        fsize = sftpService.GetFileSize(SWLOG_PATH);
                    });
                    if (fsize > 0 && fsize == previousSize) waitCnt++; else waitCnt = 0;
                    Thread.Sleep(2000);
                    double duration = Utils.GetTimeDuration(startTime);
                    if (fsize == 0)
                    {
                        if (Utils.GetTimeDuration(resetSftpConnectionTime) >= PERIOD_SFTP_RECONNECT_SEC)
                        {
                            sftpService.ResetConnection();
                            Logger.Warn($"Waited too long ({Utils.CalcStringDuration(startTime, true)}) for the switch {device.Name} to start creating the tar file!");
                            resetSftpConnectionTime = DateTime.Now;
                        }
                        if (duration >= MAX_WAIT_SFTP_RECONNECT_SEC)
                        {
                            ShowWaitTarFileError(fsize, startTime);
                            return;
                        }
                    }
                    UpdateSwitchLogBar(initialTime);
                    if (duration >= MAX_WAIT_TAR_FILE_SEC)
                    {
                        ShowWaitTarFileError(fsize, startTime);
                        return;
                    }
                }
                strDur = Utils.CalcStringDuration(startTime, true);
                string strTotalDuration = Utils.CalcStringDuration(initialTime, true);
                ShowInfoBox($"Downloading tar file from switch{device.Name}{WAITING}");
                DateTime startDowanloadTime = DateTime.Now;
                string fname = null;
                await Task.Run(() =>
                {
                    fname = sftpService.DownloadFile(SWLOG_PATH);
                });
                UpdateSwitchLogBar(initialTime);
                if (fname == null)
                {
                    ShowMessageBox("Downloading tar file", $"Failed to download file \"{SWLOG_PATH}\" from the switch {device.Name}!", MsgBoxIcons.Error);
                    return;
                }
                string downloadDur = Utils.CalcStringDuration(startDowanloadTime);
                string text = $"Tar file downloaded from switch{device.Name}{WAITING}\nDownload duration: {downloadDur}";
                text += $", File size: {Utils.PrintNumberBytes(fsize)}\nFile creation duration: {strDur}";
                ShowInfoBox(text);
                var sfd = new SaveFileDialog()
                {
                    Filter = "Tar File|*.tar",
                    Title = "Save switch logs",
                    InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString(),
                    FileName = $"{Path.GetFileName(fname)}-{device.Name}-{DateTime.Now:MM-dd-yyyy_hh_mm_ss}.tar"
                };
                FileInfo info = new FileInfo(fname);
                if (sfd.ShowDialog() == true)
                {
                    string saveas = sfd.FileName;
                    File.Copy(fname, saveas, true);
                    File.Delete(fname);
                    info = new FileInfo(saveas);
                }
                UpdateSwitchLogBar(initialTime);
                debugSwitchLog.CreateTacTextFile(selectedDeviceType, info.FullName, device, port);
                StringBuilder txt = new StringBuilder("Log tar file \"").Append(SWLOG_PATH).Append("\" downloaded from the switch ").Append(device.IpAddress);
                txt.Append("\n\tSaved file: \"").Append(info.FullName).Append("\"\n\tFile size: ").Append(Utils.PrintNumberBytes(info.Length));
                txt.Append("\n\tDownload duration: ").Append(downloadDur).Append("\n\tTar file creation duration: ").Append(strDur);
                txt.Append("\n\tTotal duration to generate log file in ").Append(SwitchDebugLogLevel.Debug3).Append(" level: ").Append(strTotalDuration);
                Logger.Activity(txt.ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                CloseProgressBar();
                HideInfoBox();
                HideProgress();
                sftpService?.Disconnect();
                sftpService = null;
                debugSwitchLog = null;
            }
        }

        private void UpdateSwitchLogBar(DateTime initialTime)
        {
            UpdateProgressBar(Utils.GetTimeDuration(initialTime), maxCollectLogsDur);
        }

        private void ShowWaitTarFileError(long fsize, DateTime startTime)
        {
            StringBuilder msg = new StringBuilder();
            msg.Append($"Failed to create \"").Append(SWLOG_PATH).Append("\" file by the switch ").Append(device.IpAddress).Append("!\nWaited too long for tar file (");
            msg.Append(Utils.CalcStringDuration(startTime, true)).Append(")\nFile size: ");
            if (fsize == 0) msg.Append("0 Bytes"); else msg.Append(Utils.PrintNumberBytes(fsize));
            Logger.Error(msg.ToString());
            ShowMessageBox("Waiting for tar file ready", msg.ToString(), MsgBoxIcons.Error);
        }

        private void RefreshSlotAndPortsView()
        {
            DataContext = null;
            _slotsView.ItemsSource = null;
            _portList.ItemsSource = null;
            DataContext = device;
            _switchAttributes.Text = $"Connected to: {device.Name} (Up time: {device.UpTime})";
            slotView = new SlotView(device);
            _slotsView.ItemsSource = slotView.Slots;
            if (selectedSlot != null)
            {
                _slotsView.SelectedItem = selectedSlot;
                _portList.ItemsSource = selectedSlot?.Ports ?? new List<PortModel>();
            }
        }

        private async void RefreshSwitch()
        {
            try
            {
                DisableButtons();
                DateTime startTime = DateTime.Now;
                reportResult = new WizardReport();
                await Task.Run(() => restApiService.ScanSwitch($"Refresh switch {device.Name}", reportResult));
                await CheckSwitchScanResult($"Refresh switch {device.Name}", startTime);
                RefreshSlotAndPortsView();
                if (device.RunningDir == CERTIFIED_DIR)
                {
                    await AskRebootCertified();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                EnableButtons();
                HideProgress();
                HideInfoBox();
            }
        }

        private async void WriteMemory()
        {
            try
            {
                string cfgChanges = await GetSyncStatus($"Checking configuration changes on switch {device.Name}");
                if (device.SyncStatus == SyncStatusType.Synchronized)
                {
                    ShowMessageBox("Write Memory", $"No configuration changes on switch {device.Name}", MsgBoxIcons.Info, MsgBoxButtons.Ok);
                    return;
                }
                if (AuthorizeWriteMemory("Write Memory required", cfgChanges))
                {
                    DisableButtons();
                    await Task.Run(() => restApiService.WriteMemory());
                    await Task.Run(() => restApiService.GetSyncStatus());
                    DataContext = null;
                    DataContext = device;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                EnableButtons();
                HideProgress();
                HideInfoBox();
            }
        }

        private async Task CheckSwitchScanResult(string title, DateTime startTime)
        {
            try
            {
                string duration = Utils.CalcStringDuration(startTime, true);
                if (reportResult.Result?.Count < 1) return;
                WizardResult result = reportResult.GetReportResult(SWITCH);
                if (result == WizardResult.Fail || result == WizardResult.Warning)
                {
                    progress.Report(new ProgressReport(title)
                    {
                        Title = title,
                        Type = result == WizardResult.Fail ? ReportType.Error : ReportType.Warning,
                        Message = $"{reportResult.Message}"
                    });
                    await WaitAckProgress();
                }
                else if (reportResult.Result?.Count > 0)
                {
                    int resetSlotCnt = 0;
                    foreach (var reports in reportResult.Result)
                    {
                        List<ReportResult> reportList = reports.Value;
                        if (reportList?.Count > 0)
                        {
                            ReportResult report = reportList[reportList.Count - 1];
                            string alertMsg = $"{report.AlertDescription}\nDo you want to turn it on?";
                            if (report?.Result == WizardResult.Warning && 
                                ShowMessageBox($"Slot {report.ID} warning", alertMsg, MsgBoxIcons.Question, MsgBoxButtons.YesNo) == MsgBoxResult.Yes)
                            {
                                await PowerSlotUpOrDown(Command.POWER_UP_SLOT, report.ID);
                                resetSlotCnt++;
                                Logger.Debug($"{report}\nSlot {report.ID} turned On");
                            }
                        }
                    }
                    if (resetSlotCnt > 0)
                    {
                        ShowProgress($"Waiting Ports to come UP on Switch {device.Name}");
                        await Task.Run(() => WaitSlotPortsUp());
                        RefreshSlotsAndPorts();
                    }
                }
                Logger.Debug($"{title} completed (duration: {duration})");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private async Task PowerSlotUpOrDown(Command cmd, string slotNr)
        {
            string msg = $"Turning slot {slotNr} PoE {(cmd == Command.POWER_UP_SLOT ? "ON" : "OFF")}";
            ShowProgress(msg);
            progress.Report(new ProgressReport($"{msg}{WAITING}"));
            await Task.Run(() =>
            {
                restApiService.PowerSlotUpOrDown(cmd, slotNr);
                restApiService.RefreshSwitchPorts();
            }
            );
            RefreshSlotsAndPorts();
        }

        private void RefreshSlotsAndPorts()
        {
            RefreshSlotAndPortsView();
            EnableButtons();
            HideInfoBox();
            HideProgress();
        }

        private void WaitSlotPortsUp()
        {
            string msg = $"Waiting Ports to come UP on Switch {device.Name}";
            DateTime startTime = DateTime.Now;
            int dur = 0;
            progress.Report(new ProgressReport($"{msg}{WAITING}"));
            while (dur < WAIT_PORTS_UP_SEC)
            {
                Thread.Sleep(1000);
                dur = (int)Utils.GetTimeDuration(startTime);
                progress.Report(new ProgressReport($"{msg} ({dur} sec){WAITING}"));
            }
            restApiService.RefreshSwitchPorts();
        }

        private async Task RunWizardCamera()
        {
            await CheckCapacitorDetection();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await Enable823BT();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await EnableHdmiMdi();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await ChangePriority();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await ResetPortPower();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await Enable2PairPower();
        }

        private async Task RunWizardTelephone()
        {
            await Enable2PairPower();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await ResetPortPower();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await ChangePriority();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await EnableHdmiMdi();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await Enable823BT();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await CheckCapacitorDetection();
        }

        private async Task RunWizardWirelessRouter()
        {
            await ResetPortPower();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await ChangePriority();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await Enable823BT();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await EnableHdmiMdi();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await CheckCapacitorDetection();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await Enable2PairPower();
        }

        private async Task RunWizardOther()
        {
            await ResetPortPower();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await ChangePriority();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await Enable823BT();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await EnableHdmiMdi();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await Enable2PairPower();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await CheckCapacitorDetection();
        }

        private async Task Enable823BT()
        {
            await RunPoeWizard(new List<Command>() { Command.CHECK_823BT });
            WizardResult result = reportResult.GetCurrentReportResult(selectedPort.Name);
            if (result == WizardResult.Skip) return;
            if (result == WizardResult.Warning)
            {
                string alertDescription = reportResult.GetAlertDescription(selectedPort.Name);
                string msg = !string.IsNullOrEmpty(alertDescription) ? alertDescription : "To enable 802.3.bt all devices on the same slot will restart";
                if (ShowMessageBox("Enable 802.3.bt", $"{msg}\nDo you want to proceed?", MsgBoxIcons.Warning, MsgBoxButtons.YesNo) == MsgBoxResult.No)
                    return;
            }
            await RunPoeWizard(new List<Command>() { Command.POWER_823BT_ENABLE });
            Logger.Debug($"Enable 802.3.bt on port {selectedPort.Name} completed on switch {device.Name}, S/N {device.SerialNumber}, model {device.Model}");
        }

        private async Task CheckCapacitorDetection()
        {
            await RunPoeWizard(new List<Command>() { Command.CHECK_CAPACITOR_DETECTION }, 60);
            Logger.Debug($"Enable 2-Pair Power on port {selectedPort.Name} completed on switch {device.Name}, S/N {device.SerialNumber}, model {device.Model}");
        }

        private async Task Enable2PairPower()
        {
            await RunPoeWizard(new List<Command>() { Command.POWER_2PAIR_PORT }, 30);
            Logger.Debug($"Enable 2-Pair Power on port {selectedPort.Name} completed on switch {device.Name}, S/N {device.SerialNumber}, model {device.Model}");
        }

        private async Task ResetPortPower()
        {
            await RunPoeWizard(new List<Command>() { Command.RESET_POWER_PORT }, 30);
            Logger.Debug($"Recycling Power on port {selectedPort.Name} completed on switch {device.Name}, S/N {device.SerialNumber}, model {device.Model}");
        }

        private async Task ChangePriority()
        {
            await RunPoeWizard(new List<Command>() { Command.CHECK_POWER_PRIORITY });
            WizardResult result = reportResult.GetCurrentReportResult(selectedPort.Name);
            if (result == WizardResult.Skip) return;
            if (result == WizardResult.Warning)
            {
                string alert = reportResult.GetAlertDescription(selectedPort.Name);
                if (ShowMessageBox("Power Priority Change",
                                    $"{(!string.IsNullOrEmpty(alert) ? $"{alert}" : "")}\nSome other devices with lower priority may stop\nDo you want to proceed?",
                                    MsgBoxIcons.Warning, MsgBoxButtons.YesNo) == MsgBoxResult.No) return;
            }
            await RunPoeWizard(new List<Command>() { Command.POWER_PRIORITY_PORT });
            Logger.Debug($"Change Power Priority on port {selectedPort.Name} completed on switch {device.Name}, S/N {device.SerialNumber}, model {device.Model}");
        }

        private async Task EnableHdmiMdi()
        {
            await RunPoeWizard(new List<Command>() { Command.POWER_HDMI_ENABLE, Command.LLDP_POWER_MDI_ENABLE, Command.LLDP_EXT_POWER_MDI_ENABLE }, 15);
            Logger.Debug($"Enable Power over HDMI on port {selectedPort.Name} completed on switch {device.Name}, S/N {device.SerialNumber}, model {device.Model}");
        }

        private async Task RunLastWizardActions()
        {
            await CheckDefaultMaxPower();
            reportResult.UpdateResult(selectedPort.Name, reportResult.GetReportResult(selectedPort.Name));
        }

        private async Task CheckDefaultMaxPower()
        {
            await RunPoeWizard(new List<Command>() { Command.CHECK_MAX_POWER });
            if (reportResult.GetCurrentReportResult(selectedPort.Name) == WizardResult.Warning)
            {
                string alert = reportResult.GetAlertDescription(selectedPort.Name);
                string msg = !string.IsNullOrEmpty(alert) ? alert : $"Changing Max. Power on port {selectedPort.Name} to default";
                if (ShowMessageBox("Check default Max. Power", $"{msg}\nDo you want to change?", MsgBoxIcons.Warning, MsgBoxButtons.YesNo) == MsgBoxResult.Yes)
                {
                    await RunPoeWizard(new List<Command>() { Command.CHANGE_MAX_POWER });
                }
            }
        }

        private async Task RunPoeWizard(List<Command> cmdList, int waitSec = 15)
        {
            await Task.Run(() => restApiService.RunPoeWizard(selectedPort.Name, reportResult, cmdList, waitSec));
        }

        private async void StartTrafficAnalysis()
        {
            try
            {
                startTrafficAnalysisTime = DateTime.Now;
                isTrafficRunning = true;
                _trafficLabel.Content = Translate("i18n_taRun");
                string switchName = device.Name;
                TrafficReport report = await Task.Run(() => restApiService.RunTrafficAnalysis(selectedTrafficDuration));
                if (report != null)
                {
                    TextViewer tv = new TextViewer(Translate("i18n_taIdle"), report.Summary)
                    {
                        Owner = this,
                        SaveFilename = $"{switchName}-{DateTime.Now:MM-dd-yyyy_hh_mm_ss}-traffic-analysis.txt",
                        CsvData = report.Data.ToString()
                    };
                    tv.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _trafficLabel.Content = Translate("i18n_taIdle");
                isTrafficRunning = false;
                if (device.IsConnected) _traffic.IsEnabled = true; else _traffic.IsEnabled = false;
                HideProgress();
                HideInfoBox();
            }
        }

        private async Task WaitAckProgress()
        {
            await Task.Run(() =>
            {
                DateTime startTime = DateTime.Now;
                while (!reportAck)
                {
                    if (Utils.GetTimeDuration(startTime) > 120) break;
                    Thread.Sleep(100);
                }
            });
        }

        private MsgBoxResult ShowMessageBox(string title, string message, MsgBoxIcons icon = MsgBoxIcons.Info, MsgBoxButtons buttons = MsgBoxButtons.Ok)
        {
            _infoBox.Visibility = Visibility.Collapsed;
            CustomMsgBox msgBox = new CustomMsgBox(this, buttons)
            {
                Header = title,
                Message = message,
                Img = icon
            };
            msgBox.ShowDialog();
            return msgBox.Result;
        }

        private void StartProgressBar(string barText)
        {
            try
            {
                _progressBar.IsIndeterminate = false;
                Utils.StartProgressBar(progress, barText);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void UpdateProgressBar(double currVal, double totalVal)
        {
            try
            {
                Utils.UpdateProgressBar(progress, currVal, totalVal);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void CloseProgressBar()
        {
            try
            {
                Utils.CloseProgressBar(progress);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void ShowInfoBox(string message)
        {
            _infoBlock.Inlines.Clear();
            _infoBlock.Inlines.Add(message);
            _infoBox.Visibility = Visibility.Visible;
        }

        private void ShowProgress(string message, bool isIndeterminate = true)
        {
            _progressBar.Visibility = Visibility.Visible;
            _progressBar.IsIndeterminate = isIndeterminate;
            if (!isIndeterminate)
            {
                _progressBar.Value = 0;
            }
            _status.Text = message;
        }

        private void HideProgress()
        {
            _progressBar.Visibility = Visibility.Hidden;
            _status.Text = DEFAULT_APP_STATUS;
            _progressBar.Value = 0;
        }

        private void HideInfoBox()
        {
            _infoBox.Visibility = Visibility.Collapsed;
        }

        private void UpdateConnectedState()
        {
            if (device.IsConnected) SetConnectedState(); 
            else SetDisconnectedState();
        }

        private void SetConnectedState()
        {
            try
            {
                DataContext = null;
                DataContext = device;
                Logger.Debug($"Data context set to {device.Name}");
                _comImg.Source = (ImageSource)currentDict["connected"];
                _switchAttributes.Text = $"Connected to: {device.Name} (Up time: {device.UpTime})";
                _btnConnect.Cursor = Cursors.Hand;
                _switchMenuItem.IsEnabled = false;
                _disconnectMenuItem.Visibility = Visibility.Visible;
                _tempStatus.Visibility = Visibility.Visible;
                _cpu.Visibility = Visibility.Visible;
                slotView = new SlotView(device);
                _slotsView.ItemsSource = slotView.Slots;
                Logger.Debug($"Slots view items source: {slotView.Slots.Count} slot(s)");
                if (slotView.Slots.Count == 1) //do not highlight if only one row
                {
                    _slotsView.CellStyle = currentDict["gridCellNoHilite"] as Style;
                }
                else 
                {
                    _slotsView.CellStyle = currentDict["gridCell"] as Style;
                }
                _slotsView.SelectedIndex = selectedSlotIndex >= 0 && _slotsView.Items?.Count > selectedSlotIndex ? selectedSlotIndex : 0;
                _slotsView.Visibility = Visibility.Visible;
                _portList.Visibility = Visibility.Visible;
                _fpgaLbl.Visibility = string.IsNullOrEmpty(device.Fpga) ? Visibility.Collapsed : Visibility.Visible;
                _cpldLbl.Visibility = string.IsNullOrEmpty(device.Cpld) ? Visibility.Collapsed : Visibility.Visible;
                _btnConnect.IsEnabled = true;
                _comImg.ToolTip = "Click to disconnect";
                if (device.TemperatureStatus == ThresholdType.Danger)
                {
                    _tempWarn.Source = new BitmapImage(new Uri(@"Resources\danger.png", UriKind.Relative));
                }
                else
                {
                    _tempWarn.Source = new BitmapImage(new Uri(@"Resources\warning.png", UriKind.Relative));
                }
                EnableButtons();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void EnableButtons()
        {
            ChangeButtonVisibility(true);
            ReselectPort();
            ReselectSlot();
            if (selectedPort == null)
            {
                _btnRunWiz.IsEnabled = false;
                _btnResetPort.IsEnabled = false;
            }
        }

        private async void LaunchRebootSwitch()
        {
            try
            {
                string cfgChanges = await GetSyncStatus("Rebooting Switch");
                if (ShowMessageBox("Reboot Switch", $"Are you sure you want to reboot the switch {device.Name}?", MsgBoxIcons.Warning, MsgBoxButtons.YesNo) == MsgBoxResult.Yes)
                {
                    if (device.SyncStatus != SyncStatusType.Synchronized && device.SyncStatus != SyncStatusType.NotSynchronized)
                    {
                        ShowMessageBox("Reboot Switch", $"Cannot reboot the switch {device.Name} because it's not certified!", MsgBoxIcons.Error);
                        return;
                    }
                    if (device.RunningDir != CERTIFIED_DIR && device.SyncStatus == SyncStatusType.NotSynchronized && AuthorizeWriteMemory("Reboot Switch", cfgChanges))
                    {
                        await Task.Run(() => restApiService.WriteMemory());
                    }
                    string txt = await RebootSwitch(420);
                    if (string.IsNullOrEmpty(txt)) return;
                    if (ShowMessageBox("Reboot Switch", $"{txt}\nDo you want to reconnect to the switch {device.Name}?", MsgBoxIcons.Info, MsgBoxButtons.YesNo) == MsgBoxResult.Yes)
                    {
                        Connect();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                HideInfoBox();
                HideProgress();
            }
        }

        private async Task<string> GetSyncStatus(string title)
        {
            string cfgChanges = null;
            if (!string.IsNullOrEmpty(title)) ShowInfoBox($"{title}{WAITING}");
            await Task.Run(() => cfgChanges = restApiService.GetSyncStatus());
            DataContext = null;
            DataContext = device;
            HideInfoBox();
            return cfgChanges;
        }

        private bool AuthorizeWriteMemory(string title, string cfgChanges)
        {
            StringBuilder text = new StringBuilder("Flash memory is not synchronized");
            if (!string.IsNullOrEmpty(cfgChanges))
            {
                text.Append("\nSignificant configuration changes:");
                text.Append(cfgChanges);
            }
            else
            {
                text.Append("\nNo significant configuration changes.");
            }
            text.Append("\nDo you want to save it now?\nIt may take up to 30 sec to execute Write Memory.");
            return ShowMessageBox(title, text.ToString(), MsgBoxIcons.Warning, MsgBoxButtons.YesNo) == MsgBoxResult.Yes;
        }

        private async Task<string> RebootSwitch(int waitSec)
        {
            try
            {
                string confirm = $"rebooting the switch {device.Name}";
                stopTrafficAnalysisReason = $"interrupted by the user before {confirm}";
                string title = $"Rebooting switch {device.Name}";
                bool save = StopTrafficAnalysis(AbortType.Close, title, Translate("i18n_taSave"), confirm);
                if (!save) return null;
                await WaitSaveTrafficAnalysis();
                DisableButtons();
                _switchMenuItem.IsEnabled = false;
                string duration = await Task.Run(() => restApiService.RebootSwitch(waitSec));
                SetDisconnectedState();
                if (string.IsNullOrEmpty(duration)) return null;
                return $"Switch {device.Name} ready to connect\nReboot duration: {duration}";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return ex.Message;
            }
            finally
            {
                HideProgress();
                HideInfoBox();
            }
        }

        private void SetDisconnectedState()
        {
            _comImg.Source = (ImageSource)currentDict["disconnected"];
            lastIpAddr = device.IpAddress;
            lastPwd = device.Password;
            DataContext = null;
            device = new SwitchModel();
            _switchAttributes.Text = "";
            _switchMenuItem.IsEnabled = true;
            DisableButtons();
            _comImg.ToolTip = "Click to reconnect";
            _disconnectMenuItem.Visibility = Visibility.Collapsed;
            _tempStatus.Visibility = Visibility.Hidden;
            _cpu.Visibility = Visibility.Hidden;
            _slotsView.Visibility = Visibility.Hidden;
            _portList.Visibility = Visibility.Hidden;
            _fpgaLbl.Visibility = Visibility.Visible;
            _cpldLbl.Visibility = Visibility.Collapsed;
            selectedPort = null;
            selectedPortIndex = -1;
            selectedSlotIndex = -1;
            DataContext = device;
            restApiService = null;
        }

        private void DisableButtons()
        {
            ChangeButtonVisibility(false);
            _btnRunWiz.IsEnabled = false;
            _btnResetPort.IsEnabled = false;
        }

        private void ChangeButtonVisibility(bool val)
        {
            _snapshotMenuItem.IsEnabled = val;
            _vcbootMenuItem.IsEnabled = val;
            _refreshSwitch.IsEnabled = val;
            _writeMemory.IsEnabled = val;
            _reboot.IsEnabled = val;
            _traffic.IsEnabled = val;
            _collectLogs.IsEnabled = val;
            _psMenuItem.IsEnabled = val;
            _searchMacMenuItem.IsEnabled = val;
            _factoryRst.IsEnabled = val;
            _cfgMenuItem.IsEnabled = val;
        }

        private void ReselectPort()
        {
            if (selectedPort != null && _portList.Items?.Count > selectedPortIndex)
            {
                _portList.SelectionChanged -= PortSelection_Changed;
                _portList.SelectedItem = _portList.Items[selectedPortIndex];
                _portList.SelectionChanged += PortSelection_Changed;
                _btnRunWiz.IsEnabled = selectedPort.Poe != PoeStatus.NoPoe;
                _btnResetPort.IsEnabled = true;
            }
        }

        private void ReselectSlot()
        {
            if (selectedSlot != null && _slotsView.Items?.Count > selectedSlotIndex)
            {
                _slotsView.SelectionChanged -= SlotSelection_Changed;
                _slotsView.SelectedItem = _slotsView.Items[selectedSlotIndex];
                _slotsView.SelectionChanged += SlotSelection_Changed;
                if (selectedPort != null) _btnRunWiz.IsEnabled = selectedPort.Poe != PoeStatus.NoPoe; else _btnRunWiz.IsEnabled = true;
                _btnResetPort.IsEnabled = true;
            }
        }

        #endregion private methods
    }
}

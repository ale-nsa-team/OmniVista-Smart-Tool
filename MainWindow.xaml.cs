using Microsoft.Win32;
using PoEWizard.Comm;
using PoEWizard.Components;
using PoEWizard.Data;
using PoEWizard.Device;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
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
        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        #region Private Variables
        private readonly ResourceDictionary darkDict;
        private readonly ResourceDictionary lightDict;
        private ResourceDictionary currentDict;
        private readonly string appVersion;
        private readonly IProgress<ProgressReport> progress;
        private bool reportAck;
        private SftpService sftpService;
        private SwitchModel device;
        private SlotView slotView;
        private PortModel selectedPort;
        private int selectedPortIndex;
        private SlotModel selectedSlot;
        private WizardReport reportResult = new WizardReport();
        private bool isClosing = false;
        private DeviceType selectedDeviceType;
        private string lastIpAddr;
        private string lastPwd;
        private SwitchDebugModel debugSwitchLog;
        #endregion

        #region public variables
        public static Window Instance;
        public static ThemeType theme;
        public static string dataPath;
        public static RestApiService restApiService;
        public static Dictionary<string, string> ouiTable = new Dictionary<string, string>();

        #endregion

        #region constructor and initialization
        public MainWindow()
        {
            device = new SwitchModel();
            //application info
            Assembly assembly = Assembly.GetExecutingAssembly();
            string version = assembly.GetName().Version.ToString();
            string title = assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
            string ale = assembly.GetCustomAttribute<AssemblyCompanyAttribute>().Company;
            appVersion = title + " (V." + string.Join(".", version.Split('.').ToList().Take(2)) + ")";
            //datapath
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            dataPath = Path.Combine(appData, ale, title);
            InitializeComponent();
            this.Title += $" (V {string.Join(".", version.Split('.').ToList().Take(2))})";
            lightDict = Resources.MergedDictionaries[0];
            darkDict = Resources.MergedDictionaries[1];
            currentDict = darkDict;
            Instance = this;
            BuildOuiTable();

            // progress report handling
            progress = new Progress<ProgressReport>(report =>
            {
                reportAck = false;
                HideInfoBox();
                switch (report.Type)
                {
                    case ReportType.Status:
                        ShowInfoBox(report.Message);
                        break;
                    case ReportType.Error:
                        reportAck = ShowMessageBox(report.Title, report.Message, MsgBoxIcons.Error);
                        break;
                    case ReportType.Warning:
                        reportAck = ShowMessageBox(report.Title, report.Message, MsgBoxIcons.Warning);
                        break;
                    case ReportType.Info:
                        reportAck = ShowMessageBox(report.Title, report.Message, MsgBoxIcons.Info);
                        break;
                    case ReportType.Value:
                        _progressBar.Value = int.TryParse(report.Message, out int i) ? i : 0;
                        break;
                    default:
                        break;
                }
            });
            //check cli arguments
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 3)
            {
                device.IpAddress = args[1];
                device.Login = args[2];
                device.Password = args[3];
                Connect();
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            SetTitleColor();
            _btnConnect.IsEnabled = false;
        }

        private async void OnWindowClosing(object sender, CancelEventArgs e)
        {
            try
            {
                e.Cancel = true;
                sftpService?.Disconnect();
                sftpService = null;
                await CloseRestApiService();
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
            TextViewer tv = new TextViewer("Application Log", canClear: true)
            {
                Owner = this,
                Filename = Logger.LogPath,
            };
            tv.Show();
        }

        private async void ViewVcBoot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowProgress("Loading vcboot.cfg file...");
                Logger.Debug($"Loading vcboot.cfg file from switch {device.Name}");
                string res = string.Empty;
                await Task.Run(() =>
                {
                    sftpService = new SftpService(device.IpAddress, device.Login, device.Password);
                    sftpService.Connect();
                    res = sftpService.DownloadToMemory(VCBOOT_PATH);
                });
                HideProgress();
                TextViewer tv = new TextViewer("VCBoot config file", res)
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
        }

        private async void ViewSnapshot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowProgress("Reading configuration snapshot...");
                await Task.Run(() => restApiService.GetSnapshot());
                HideInfoBox();
                HideProgress();
                TextViewer tv = new TextViewer("Configuration Snapshot", device.ConfigSnapshot)
                {
                    Owner = this,
                    SaveFilename = device.Name + "-snapshot.txt"
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

        private void LaunchConfigWizard(object sender, RoutedEventArgs e)
        {
            _status.Text = "Running wizard...";

            ConfigWiz wiz = new ConfigWiz(device)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            bool wasApplyed = (bool)wiz.ShowDialog();
            if (!wasApplyed) return;
            if (wiz.Errors.Count > 0)
            {
                string errMsg = $"The following {(wiz.Errors.Count > 1 ? "errors where" : "error was")} reported:";
                ShowMessageBox("Wizard", $"{errMsg}\n\n\u2022 {string.Join("\n\u2022 ", wiz.Errors)}", MsgBoxIcons.Error, MsgBoxButtons.Ok);
                Logger.Warn($"Configuration from Wizard applyed with errors:\n\t{string.Join("\n\t", wiz.Errors)}");
                Activity.Log($"Switch S/N {device.SerialNumber} Model {device.Model}: Wizard applied with errors");
            }
            else
            {
                Activity.Log($"Switch S/N {device.SerialNumber} Model {device.Model}: Wizard applied");
            }
            if (wiz.IsRebootSwitch) LaunchRebootSwitch();
            _status.Text = DEFAULT_APP_STATUS;
        }

        private void RunWiz_Click(object sender, RoutedEventArgs e)
        {
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
            LaunchRebootSwitch();
        }

        private void Traffic_Click(object sender, RoutedEventArgs e)
        {
            TrafficAnalysis();
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {

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
            if (t == "Dark")
            {
                _lightMenuItem.IsChecked = false;
                theme = ThemeType.Dark;
                Resources.MergedDictionaries.Remove(lightDict);
                Resources.MergedDictionaries.Add(darkDict);
                currentDict = darkDict;
            }
            else
            {
                _darkMenuItem.IsChecked = false;
                theme = ThemeType.Light;
                Resources.MergedDictionaries.Remove(darkDict);
                Resources.MergedDictionaries.Add(lightDict);
                currentDict = lightDict;
            }
            if (slotView?.Slots.Count == 1) //do not highlight if only one row
            {
                _slotsView.CellStyle = currentDict["gridCellNoHilite"] as Style;
            }
            SetTitleColor();
            //force color converters to run
            DataContext = null;
            DataContext = device;
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
                selectedSlot = slot;
                _portList.ItemsSource = slot.Ports;
            }

        }

        private void PortSelection_Changed(Object sender, RoutedEventArgs e)
        {
            if (_portList.SelectedItem is PortModel port)
            {
                selectedPort = port;
                selectedPortIndex = _portList.SelectedIndex;
                _btnRunWiz.IsEnabled = selectedPort.Poe != PoeStatus.NoPoe;
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
                        Logger.Activity($"{txt} completed on Switch {device.Name}, S/N {device.SerialNumber}, Model {device.Model}");
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
                Logger.Activity($"{action} {poeType} PoE on slot {selectedSlot.Name} completed on switch {device.Name}, S/N {device.SerialNumber}, model {device.Model}");
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
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion event handlers

        #region private methods
        private void SetTitleColor()
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            int bckgndColor = theme == ThemeType.Dark ? 0x333333 : 0xFFFFFF;
            int textColor = theme == ThemeType.Dark ? 0xFFFFFF : 0x000000;
            DwmSetWindowAttribute(handle, 35, ref bckgndColor, Marshal.SizeOf(bckgndColor));
            DwmSetWindowAttribute(handle, 36, ref textColor, Marshal.SizeOf(textColor));
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
                restApiService = new RestApiService(device, progress);
                if (device.IsConnected)
                {
                    Logger.Activity($"Disconnected switch {device.Name} ({device.IpAddress}), S/N {device.SerialNumber}, model {device.Model}");
                    ShowProgress($"Disconnecting from switch {device.IpAddress}...");
                    await CloseRestApiService();
                    SetDisconnectedState();
                    return;
                }
                ShowProgress($"Connecting to switch {device.IpAddress}...");
                isClosing = false;
                DateTime startTime = DateTime.Now;
                reportResult = new WizardReport();
                await Task.Run(() => restApiService.Connect(reportResult));
                UpdateConnectedState(true);
                await CheckSwitchScanResult($"Connect to switch {device.IpAddress}...", startTime);
                Logger.Activity($"Connected to switch {device.Name} ({device.IpAddress}), S/N {device.SerialNumber}, model {device.Model}");
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
                filePath = Path.Combine(MainWindow.dataPath, OUI_FILE);
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

        private async Task CloseRestApiService()
        {
            try
            {
                if (restApiService != null && !isClosing)
                {
                    isClosing = true;
                    if (device.SyncStatus == SyncStatusType.NotSynchronized)
                    {
                        if (ShowMessageBox("Write Memory required",
                                "Flash memory is not synchronized\nDo you want to save it now?\nIt may take up to 30 sec to execute Write Memory.",
                                MsgBoxIcons.Warning, MsgBoxButtons.OkCancel))
                        {
                            _btnRunWiz.IsEnabled = false;
                            _refreshSwitch.IsEnabled = false;
                            _writeMemory.IsEnabled = false;
                            _reboot.IsEnabled = false;
                            _traffic.IsEnabled = false;
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
                Logger.Error(ex);
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
                ProgressReport wizardProgressReport = new ProgressReport("PoE Wizard Report:");
                reportResult = new WizardReport();
                ShowProgress($"Running PoE Wizard on port {selectedPort.Name}...");
                DateTime startTime = DateTime.Now;
                await Task.Run(() => restApiService.ScanSwitch($"Running PoE Wizard on port {selectedPort.Name}...", reportResult));
                UpdateConnectedState(false);
                switch (selectedDeviceType)
                {
                    case DeviceType.Camera:
                        await RunWizardCamera();
                        break;
                    case DeviceType.Phone:
                        await RunWizardTelephone();
                        break;
                    case DeviceType.AP:
                        await RunWizardWirelessLan();
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
                RefreshSlotAndPortsView();
                if (result == WizardResult.Fail)
                {
                    bool res = ShowMessageBox("Wizard", "It looks like the wizard was unable to fix the problem.\nDo you want to collect information to send to technical support?",
                                              MsgBoxIcons.Question, MsgBoxButtons.OkCancel);
                    if (!res) return;
                    ShowInfoBox("Cleaning up current log...");
                    sftpService = new SftpService(device.IpAddress, device.Login, device.Password);
                    await Task.Run(() => sftpService.Connect());
                    await Task.Run(() => sftpService.DeleteFile(SWLOG_PATH));
                    ShowProgress("Collecting switch logs...");
                    await RunGetSwitchLog(SwitchDebugLogLevel.Debug3);
                    await DownloadSwitchLogFile();
                }
                await Task.Run(() => restApiService.GetSystemInfo());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                HideProgress();
                HideInfoBox();
                sftpService?.Disconnect();
                sftpService = null;
                debugSwitchLog = null;
            }
        }

        private async Task DownloadSwitchLogFile()
        {
            const double MAX_WAIT_SFTP_RECONNECT_SEC = 60;
            const double MAX_WAIT_TAR_FILE_SEC = 180;
            const double PERIOD_SFTP_RECONNECT_SEC = 30;
            try
            {
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
                    msg = !string.IsNullOrEmpty(strDur) ? $"Waiting for tar file ready ({strDur}) ..." : "Waiting for tar file ready ...";
                    if (fsize > 0) msg += $"\nFile size: {Utils.PrintNumberBytes(fsize)}";
                    ShowInfoBox(msg);
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
                            Logger.Warn($"Waited too long ({Utils.CalcStringDuration(startTime, true)}) for the switch {device.IpAddress} to start creating the tar file!");
                            resetSftpConnectionTime = DateTime.Now;
                        }
                        if (duration >= MAX_WAIT_SFTP_RECONNECT_SEC)
                        {
                            ShowWaitTarFileError(fsize, startTime);
                            return;
                        }
                    }
                    if (duration >= MAX_WAIT_TAR_FILE_SEC)
                    {
                        ShowWaitTarFileError(fsize, startTime);
                        return;
                    }
                }
                strDur = Utils.CalcStringDuration(startTime, true);
                ShowInfoBox($"Downloading tar file from switch ...\nFile creation duration: {strDur}, File size: {Utils.PrintNumberBytes(fsize)}");
                string fname = null;
                await Task.Run(() =>
                {
                    fname = sftpService.DownloadFile(SWLOG_PATH);
                });
                if (fname == null)
                {
                    ShowMessageBox("Downloading tar file", $"Failed to download file \"{SWLOG_PATH}\" from the switch {device.IpAddress}!", MsgBoxIcons.Error, MsgBoxButtons.Ok);
                    return;
                }
                var sfd = new SaveFileDialog()
                {
                    Filter = "Tar File|*.tar",
                    Title = "Save switch logs",
                    InitialDirectory = Environment.SpecialFolder.MyDocuments.ToString(),
                    FileName = Path.GetFileName(fname)
                };
                FileInfo info = new FileInfo(fname);
                if (sfd.ShowDialog() == true)
                {
                    string saveas = sfd.FileName;
                    File.Copy(fname, saveas, true);
                    File.Delete(fname);
                    info = new FileInfo(saveas);
                }
                debugSwitchLog.CreateTacTextFile(selectedDeviceType, info.FullName, device, selectedPort);
                StringBuilder txt = new StringBuilder("Log tar file \"").Append(SWLOG_PATH).Append("\" downloaded from the switch ").Append(device.IpAddress);
                txt.Append("\n\tSaved file: \"").Append(info.FullName).Append("\" (").Append(Utils.PrintNumberBytes(info.Length));
                txt.Append(")\n\tDuration of tar file creation: ").Append(strDur);
                Logger.Activity(txt.ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                HideInfoBox();
                HideProgress();
                sftpService?.Disconnect();
                sftpService = null;
                debugSwitchLog = null;
            }
        }

        private void ShowWaitTarFileError(long fsize, DateTime startTime)
        {
            StringBuilder msg = new StringBuilder();
            msg.Append($"Failed to create \"").Append(SWLOG_PATH).Append("\" file by the switch ").Append(device.IpAddress).Append("!\nWaited too long for tar file (");
            msg.Append(Utils.CalcStringDuration(startTime, true)).Append(")\nFile size: ");
            if (fsize == 0) msg.Append("0 Bytes"); else msg.Append(Utils.PrintNumberBytes(fsize));
            Logger.Error(msg.ToString());
            ShowMessageBox("Waiting for tar file ready", msg.ToString(), MsgBoxIcons.Error, MsgBoxButtons.Ok);
        }

        private void RefreshSlotAndPortsView()
        {
            DataContext = null;
            _slotsView.ItemsSource = null;
            _portList.ItemsSource = null;
            selectedSlot = device.GetSlot(selectedSlot.Name);
            DataContext = device;
            _slotsView.ItemsSource = device.GetChassis(selectedSlot.Name)?.Slots ?? new List<SlotModel>();
            _portList.ItemsSource = selectedSlot?.Ports ?? new List<PortModel>();
            ReselectPort();
        }

        private async void RefreshSwitch()
        {
            try
            {
                ShowProgress($"Scanning switch {device.IpAddress}...");
                DateTime startTime = DateTime.Now;
                reportResult = new WizardReport();
                await Task.Run(() => restApiService.ScanSwitch($"Refresh switch {device.IpAddress}", reportResult));
                UpdateConnectedState(false);
                await CheckSwitchScanResult($"Refresh switch {device.IpAddress}", startTime);
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

        private async void WriteMemory()
        {
            try
            {
                ShowProgress($"Writing memory on switch {device.IpAddress}...");
                await Task.Run(() => restApiService.GetSystemInfo());
                if (device.SyncStatus == SyncStatusType.Synchronized) return;
                await Task.Run(() => restApiService.WriteMemory());
                await Task.Run(() => restApiService.GetSystemInfo());
                DataContext = null;
                DataContext = device;
                ReselectPort();
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

        private async Task CheckSwitchScanResult(string title, DateTime startTime)
        {
            try
            {
                string duration = Utils.CalcStringDuration(startTime, true);
                if (reportResult.Result?.Count < 1) return;
                WizardResult result = reportResult.GetReportResult(SWITCH);
                if (result == WizardResult.Fail || result == WizardResult.Warning)
                {
                    progress.Report(new ProgressReport(title) { Title = title, Type = ReportType.Error, Message = $"{reportResult.Message}" });
                    await WaitAckProgress();
                }
                else if (reportResult.Result?.Count > 0)
                {
                    int resetSlotCnt = 0;
                    foreach (var reports in reportResult.Result)
                    {
                        List<ReportResult> reportList = reports.Value as List<ReportResult>;
                        if (reportList?.Count > 0)
                        {
                            ReportResult report = reportList[reportList.Count - 1];
                            string alertMsg = $"{report.AlertDescription}\nDo you want to turn it On?";
                            if (report?.Result == WizardResult.Warning && ShowMessageBox($"Slot {report.ID} warning", alertMsg, MsgBoxIcons.Question, MsgBoxButtons.OkCancel))
                            {
                                await Task.Run(() => restApiService.RunPowerUpSlot(report.ID));
                                resetSlotCnt++;
                                Logger.Debug($"{report}\nSlot {report.ID} turned On");
                            }
                        }
                    }
                    if (resetSlotCnt > 0)
                    {
                        await Task.Run(() => WaitTask(20, $"Waiting Ports to come UP on Switch {device.IpAddress}"));
                        await Task.Run(() => restApiService.RefreshSwitchPorts());
                        RefreshSlotAndPortsView();
                    }
                }
                Logger.Debug($"{title} completed (duration: {duration})");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private async Task RunWizardCamera()
        {
            await Enable823BT();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await EnableHdmiMdi();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await ChangePriority();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await Enable2PairPower();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
        }

        private async Task RunWizardTelephone()
        {
            await Enable2PairPower();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await ChangePriority();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await EnableHdmiMdi();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await Enable823BT();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
        }

        private async Task RunWizardWirelessLan()
        {
            await Enable823BT();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await Enable2PairPower();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await EnableHdmiMdi();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await ChangePriority();
        }

        private async Task RunWizardOther()
        {
            await ChangePriority();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await Enable823BT();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await EnableHdmiMdi();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
            await Enable2PairPower();
            if (reportResult.IsWizardStopped(selectedPort.Name)) return;
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
                if (!ShowMessageBox("Enable 802.3.bt", $"{msg}\nDo you want to proceed?", MsgBoxIcons.Warning, MsgBoxButtons.OkCancel))
                    return;
            }
            await RunPoeWizard(new List<Command>() { Command.POWER_823BT_ENABLE });
            Logger.Debug($"Enable 802.3.bt on port {selectedPort.Name} completed on switch {device.Name}, S/N {device.SerialNumber}, model {device.Model}");
        }

        private async Task Enable2PairPower()
        {
            await RunPoeWizard(new List<Command>() { Command.POWER_2PAIR_PORT }, 30);
            Logger.Debug($"Enable 2-Pair Power on port {selectedPort.Name} completed on switch {device.Name}, S/N {device.SerialNumber}, model {device.Model}");
        }

        private async Task ChangePriority()
        {
            await RunPoeWizard(new List<Command>() { Command.CHECK_POWER_PRIORITY });
            WizardResult result = reportResult.GetCurrentReportResult(selectedPort.Name);
            if (result == WizardResult.Skip) return;
            if (result == WizardResult.Warning)
            {
                string alert = reportResult.GetAlertDescription(selectedPort.Name);
                if (!ShowMessageBox("Power Priority Change",
                                    $"{(!string.IsNullOrEmpty(alert) ? $"{alert}" : "")}\nSome other devices with lower priority may stop\nDo you want to proceed?",
                                    MsgBoxIcons.Warning, MsgBoxButtons.OkCancel)) return;
            }
            await RunPoeWizard(new List<Command>() { Command.POWER_PRIORITY_PORT });
            Logger.Debug($"Change Power Priority on port {selectedPort.Name} completed on switch {device.Name}, S/N {device.SerialNumber}, model {device.Model}");
        }

        private async Task EnableHdmiMdi()
        {
            await RunPoeWizard(new List<Command>() { Command.POWER_HDMI_ENABLE, Command.LLDP_POWER_MDI_ENABLE, Command.LLDP_EXT_POWER_MDI_ENABLE }, 15);
            Logger.Debug($"Enable Power over HDMI on port {selectedPort.Name} completed on switch {device.Name}, S/N {device.SerialNumber}, model {device.Model}");
        }

        private async Task RunPoeWizard(List<Command> cmdList, int waitSec = 15)
        {
            await Task.Run(() => restApiService.RunPoeWizard(selectedPort.Name, reportResult, cmdList, waitSec));
        }

        private async Task RunLastWizardActions()
        {
            bool reset = false;
            await RunWizardCommands(new List<Command>() { Command.CHECK_CAPACITOR_DETECTION }, 45);
            WizardResult result = reportResult.GetReportResult(selectedPort.Name);
            if (result != WizardResult.Ok && result != WizardResult.Fail) reportResult.RemoveLastWizardReport(selectedPort.Name); else reset = true;
            await CheckDefaultMaxPower();
            if (selectedPort.Poe == PoeStatus.Off && (ShowMessageBox("Port PoE turned Off", $"The PoE on port {selectedPort.Name} is turned Off!\n Do you want to turn it On?",
                                                                     MsgBoxIcons.Warning, MsgBoxButtons.OkCancel)))
            {
                await RunWizardCommands(new List<Command>() { Command.RESET_POWER_PORT }, 30);
                Logger.Info($"PoE turned Off, reset power on port {selectedPort.Name} completed on switch {device.Name}, S/N {device.SerialNumber}, model {device.Model}");
                reset = true;
            }
            if (!reset && ShowMessageBox("Resetting Port", $"Do you want to recycle the power on port {selectedPort.Name}?", MsgBoxIcons.Question, MsgBoxButtons.OkCancel))
            {
                await RunWizardCommands(new List<Command>() { Command.RESET_POWER_PORT }, 30);
                Logger.Info($"Recycling the power on port {selectedPort.Name} completed on switch {device.Name}, S/N {device.SerialNumber}, model {device.Model}");
            }
            reportResult.UpdateResult(selectedPort.Name, reportResult.GetReportResult(selectedPort.Name));
        }

        private async Task CheckDefaultMaxPower()
        {
            await RunWizardCommands(new List<Command>() { Command.CHECK_MAX_POWER });
            if (reportResult.GetCurrentReportResult(selectedPort.Name) == WizardResult.Warning)
            {
                string alert = reportResult.GetAlertDescription(selectedPort.Name);
                string msg = !string.IsNullOrEmpty(alert) ? alert : $"Changing Max. Power on port {selectedPort.Name} to default";
                if (ShowMessageBox("Check default Max. Power", $"{msg}\nDo you want to change?", MsgBoxIcons.Warning, MsgBoxButtons.OkCancel))
                {
                    await RunWizardCommands(new List<Command>() { Command.CHANGE_MAX_POWER });
                }
            }
        }

        private async Task RunWizardCommands(List<Command> cmdList, int waitSec = 15)
        {
            await Task.Run(() => restApiService.RunWizardCommands(selectedPort.Name, reportResult, cmdList, waitSec));
        }

        private async Task RunGetSwitchLog(SwitchDebugLogLevel debugLevel)
        {
            debugSwitchLog = new SwitchDebugModel(reportResult, debugLevel);
            await Task.Run(() => restApiService.RunGetSwitchLog(selectedPort.Name, debugSwitchLog));
        }

        private async void TrafficAnalysis()
        {
            try
            {
                ShowProgress($"Running traffic analysis on switch {device.IpAddress}...");
                TrafficReport report = null;
                await Task.Run(() => report = restApiService.RunTrafficAnalysis(5, 15));
                if (report != null)
                {
                    TextViewer tv = new TextViewer("Traffic Analysis", report.Summary)
                    {
                        Owner = this,
                        SaveFilename = $"{device.Name}-{DateTime.Now.ToString("MM-dd-yyyy_hh_mm_ss")}-traffic-analysis.txt",
                        CsvData = report.Data.ToString()
                    };
                    tv.Show();
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

        private void WaitTask(int waitTime, string txt)
        {
            DateTime startTime = DateTime.Now;
            int dur = 0;
            progress.Report(new ProgressReport($"{txt} ..."));
            while (dur < waitTime)
            {
                Thread.Sleep(1000);
                dur = (int)Utils.GetTimeDuration(startTime);
                progress.Report(new ProgressReport($"{txt} ({dur} sec) ..."));
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

        private bool ShowMessageBox(string title, string message, MsgBoxIcons icon = MsgBoxIcons.Info, MsgBoxButtons buttons = MsgBoxButtons.Ok)
        {
            _infoBox.Visibility = Visibility.Collapsed;
            CustomMsgBox msgBox = new CustomMsgBox(this)
            {
                Header = title,
                Message = message,
                Img = icon,
                Buttons = buttons
            };
            return (bool)msgBox.ShowDialog();
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
            if (!isIndeterminate)
            {
                _progressBar.IsIndeterminate = false;
                _progressBar.Value = 0;
            }
            _status.Text = message;

        }

        private void HideProgress()
        {
            _progressBar.Visibility = Visibility.Hidden;
            _status.Text = DEFAULT_APP_STATUS;
        }

        private void HideInfoBox()
        {
            _infoBox.Visibility = Visibility.Collapsed;
        }

        private void UpdateConnectedState(bool checkCertified)
        {
            if (device.IsConnected) SetConnectedState(checkCertified); else SetDisconnectedState();
        }

        private async void SetConnectedState(bool checkCertified)
        {
            try
            {
                DataContext = null;
                DataContext = device;
                Logger.Debug($"Data context set to {device.Name}");
                if (device.RunningDir == CERTIFIED_DIR && checkCertified)
                {
                    string msg = $"The switch booted on {CERTIFIED_DIR} directory, no changes can be saved.\n" +
                        $"Do you want to reboot the switch on {WORKING_DIR} directory?";
                    bool res = ShowMessageBox("Connection", msg, MsgBoxIcons.Warning, MsgBoxButtons.OkCancel);
                    if (res)
                    {
                        string txt = await RebootSwitch();
                        ShowMessageBox("Connection", txt, MsgBoxIcons.Info, MsgBoxButtons.Ok);
                        return;
                    }
                }
                _comImg.Source = (ImageSource)currentDict["connected"];
                _switchAttributes.Text = $"Connected to: {device.Name}";
                _btnConnect.Cursor = Cursors.Hand;
                _switchMenuItem.IsEnabled = false;
                _snapshotMenuItem.IsEnabled = true;
                _vcbootMenuItem.IsEnabled = true;
                _refreshSwitch.IsEnabled = true;
                _writeMemory.IsEnabled = true;
                _reboot.IsEnabled = true;
                _traffic.IsEnabled = true;
                _psMenuItem.IsEnabled = true;
                _cfgMenuItem.IsEnabled = true;
                _disconnectMenuItem.Visibility = Visibility.Visible;
                _tempStatus.Visibility = Visibility.Visible;
                _cpu.Visibility = Visibility.Visible;
                slotView = new SlotView(device);
                _slotsView.ItemsSource = slotView.Slots;
                Logger.Debug($"Slots view items source: {slotView.Slots.Count} slot(s)");
                _slotsView.SelectedIndex = 0;
                if (slotView.Slots.Count == 1) //do not highlight if only one row
                {
                    _slotsView.CellStyle = currentDict["gridCellNoHilite"] as Style;
                }
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
                ReselectPort();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private async void LaunchRebootSwitch()
        {
            bool res = ShowMessageBox("Reboot Switch", $"Are you sure you want to reboot the switch {device.IpAddress}?", MsgBoxIcons.Warning, MsgBoxButtons.OkCancel);
            if (res)
            {
                await Task.Run(() => restApiService.GetSystemInfo());
                if (device.SyncStatus != SyncStatusType.Synchronized && device.SyncStatus != SyncStatusType.NotSynchronized)
                {
                    ShowMessageBox("Reboot Switch", $"Cannot reboot the switch {device.IpAddress} because it's not certified!", MsgBoxIcons.Error, MsgBoxButtons.Ok);
                    return;
                }
                Logger.Activity($"Rebooting switch {device.Name} ({device.IpAddress}), S/N {device.SerialNumber}, model {device.Model}");
                string txt = await RebootSwitch();
                if (string.IsNullOrEmpty(txt)) return;
                res = ShowMessageBox("Reboot Switch", $"{txt}\nDo you want to reconnect to the switch {device.IpAddress}?", MsgBoxIcons.Info, MsgBoxButtons.OkCancel);
                if (res)
                {
                    Connect();
                    return;
                }
            }
        }

        private async Task<string> RebootSwitch()
        {
            try
            {
                ShowProgress($"Rebooting switch {device.IpAddress}...");
                string duration = "";
                await Task.Run(() => duration = restApiService.RebootSwitch(600));
                SetDisconnectedState();
                string txt = $"Switch {device.IpAddress} ready to connect";
                if (!string.IsNullOrEmpty(duration)) txt += $"\nReboot duration: {duration}";
                return txt;
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
            _btnRunWiz.IsEnabled = false;
            _switchMenuItem.IsEnabled = true;
            _snapshotMenuItem.IsEnabled = false;
            _vcbootMenuItem.IsEnabled = false;
            _refreshSwitch.IsEnabled = false;
            _writeMemory.IsEnabled = false;
            _reboot.IsEnabled = false;
            _traffic.IsEnabled = false;
            _psMenuItem.IsEnabled = false;
            _cfgMenuItem.IsEnabled = false;
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
            DataContext = device;
            restApiService = null;
        }

        private void ReselectPort()
        {
            if (selectedPort != null && _portList.Items?.Count > selectedPortIndex)
            {
                _portList.SelectionChanged -= PortSelection_Changed;
                _portList.SelectedItem = _portList.Items[selectedPortIndex];
                _portList.SelectionChanged += PortSelection_Changed;
            }
        }

        #endregion private methods
    }
}

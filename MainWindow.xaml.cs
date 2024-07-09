using PoEWizard.Comm;
using PoEWizard.Components;
using PoEWizard.Data;
using PoEWizard.Device;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.RestUrl;

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
        private RestApiService restApiService;
        private SwitchModel device;
        #endregion
        #region public variables
        public static Window Instance;
        public static ThemeType theme;
        public static string dataPath;

        #endregion

        #region constructor and initialization
        public MainWindow()
        {
            InitializeComponent();
            lightDict = Resources.MergedDictionaries[0];
            darkDict = Resources.MergedDictionaries[1];
            currentDict = darkDict;
            Instance = this;
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
                        reportAck = ShowMessageBox(report.Title, report.Message, MsgBoxIcons.Error);
                        break;
                    case ReportType.Warning:
                        reportAck = ShowMessageBox(report.Title, report.Message, MsgBoxIcons.Warning);
                        break;
                    case ReportType.Info:
                        reportAck = ShowMessageBox(report.Title, report.Message, MsgBoxIcons.Info);
                        break;
                    default:
                        break;
                }
            });
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            SetTitleColor();
        }

        #endregion constructor and initialization

        #region event handlers
        private void SwitchMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Login login = new Login(device.Login)
            {
                Password = device.Password,
                IpAddress = device.IpAddress,
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
            if (!string.IsNullOrEmpty(device?.IpAddress)) Connect();
        }

        private void ViewActivity_Click(object sender, RoutedEventArgs e)
        {

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

        private async void ViewSnapshot_Click(object sender, RoutedEventArgs e)
        {
            ShowProgress("Reading configuration snapshot...");
            await Task.Run(() => restApiService.GetSnapshot());
            HideProgress();
            TextViewer tv = new TextViewer("Configuration Snapshot", device.ConfigSnapshot)
            {
                Owner = this,
                SaveFilename = device.Name + "-snapshot.txt"
            };
            tv.Show();
        }

        private void RunWiz_Click(object sender, RoutedEventArgs e)
        {
            LaunchPoeWizard();
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
            SetTitleColor();
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
                _portList.ItemsSource = slot.Ports;
            }

        }

        private void FPoE_Click(object sender, RoutedEventArgs e)
        {

        }

        private void PPoE_Click(Object sender, RoutedEventArgs e)
        {

        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion event handlers

        #region private methods
        private void SetTitleColor()
        {
            IntPtr handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int bckgndColor = theme == ThemeType.Dark ? 0x333333 : 0xFFFFFF;
            int textColor = theme == ThemeType.Dark ? 0xFFFFFF : 0x000000;
            DwmSetWindowAttribute(handle, 35, ref bckgndColor, Marshal.SizeOf(bckgndColor));
            DwmSetWindowAttribute(handle, 36, ref textColor, Marshal.SizeOf(textColor));
        }

        private async void Connect()
        {
            try
            {
                restApiService = new RestApiService(device, progress);
                if (device.IsConnected)
                {
                    await Task.Run(() => restApiService.Close());
                    SetDisconnectedState();
                    return;
                }
                await Task.Run(() => restApiService.Connect());

                if (device.IsConnected)
                {
                    Logger.Info($"Connected to switch {device.Name}, S/N {device.SerialNumber}, model {device.Model}");
                    SetConnectedState();
                }
                else
                {
                    Logger.Info($"Switch S/N {device.SerialNumber}, model {device.Model} Disconnected");
                    SetDisconnectedState();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ":\n" + ex.StackTrace);
            }
            HideProgress();
            HideInfoBox();
        }

        private async void LaunchPoeWizard()
        {
            try
            {
                bool proceed = false;
                ShowProgress("Running PoE Wizard...");
                restApiService = new RestApiService(device, progress);
                await Task.Run(() =>
                    proceed = restApiService.RunPoeWizard("1/1/28", new List<RestUrlId>() {
                                    RestUrlId.POWER_2PAIR_PORT, RestUrlId.POWER_HDMI_ENABLE, RestUrlId.LLDP_POWER_MDI_ENABLE, RestUrlId.LLDP_EXT_POWER_MDI_ENABLE
                              }, 15)
                );
                Logger.Info($"PoE Wizard 1st Step completed on switch {device.Name}, S/N {device.SerialNumber}, model {device.Model}");
                await WaitAckProgress();
                if (proceed)
                {
                    proceed = ShowMessageBox("Enable 802.3.bt", "All devices on the same slot will restart. Do you want to proceed?",
                                             MsgBoxIcons.Warning, MsgBoxButtons.OkCancel);
                    if (proceed)
                    {
                        await Task.Run(() =>
                            proceed = restApiService.RunPoeWizard("1/1/28", new List<RestUrlId>() {
                                            RestUrlId.POWER_823BT_ENABLE
                                      }, 15)
                        );
                        Logger.Info($"PoE Wizard 2nd Step completed on switch {device.Name}, S/N {device.SerialNumber}, model {device.Model}");
                    }
                }
                await WaitAckProgress();
                if (proceed)
                {
                    await Task.Run(() =>
                        proceed = restApiService.RunPoeWizard("1/1/28", new List<RestUrlId>() {
                                        RestUrlId.CHECK_POWER_PRIORITY
                                  }, 15)
                    );
                    Logger.Info($"PoE Wizard Check Power Priority completed on switch {device.Name}, S/N {device.SerialNumber}, model {device.Model}");
                    await WaitAckProgress();
                    if (!proceed) return;
                    proceed = ShowMessageBox("Power Priority Change", "Some other devices with lower priority may stop. Do you want to proceed?",
                                             MsgBoxIcons.Warning, MsgBoxButtons.OkCancel);
                    if (proceed)
                    {
                        await Task.Run(() =>
                            restApiService.RunPoeWizard("1/1/28", new List<RestUrlId>() {
                                    RestUrlId.POWER_PRIORITY_PORT
                            }, 15)
                        );
                        Logger.Info($"PoE Wizard 3rd Step completed on switch {device.Name}, S/N {device.SerialNumber}, model {device.Model}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message + ":\n" + ex.StackTrace);
            }
            HideProgress();
            HideInfoBox();
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

        private void ShowProgress(string message)
        {
            _progressBar.Visibility = Visibility.Visible;
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

        private void SetConnectedState()
        {
            DataContext = device;
            _comImg.Source = (ImageSource)currentDict["connected"];
            _switchAttributes.Text = $"Connected to: {device.Name}";
            _btnRunWiz.IsEnabled = true;
            _btnConnect.Cursor = Cursors.Hand;
            _switchMenuItem.IsEnabled = false;
            _snapshotMenuItem.IsEnabled = true;
            _disconnectMenuItem.Visibility = Visibility.Visible;
            _slotsView.ItemsSource = new SlotView(device).Slots;
            _slotsView.SelectedIndex = 0;
            _slotsView.Visibility = Visibility.Visible;
            _portList.Visibility = Visibility.Visible;
            _poeActions.Visibility = Visibility.Visible;
        }

        private void SetDisconnectedState()
        {
            _comImg.Source = (ImageSource)currentDict["disconnected"];
            string oldIp = device.IpAddress;
            device = new SwitchModel();
            device.IpAddress = oldIp;
            _switchAttributes.Text = "";
            _btnRunWiz.IsEnabled = false;
            DataContext = null;
            restApiService = null;
            _switchMenuItem.IsEnabled = true;
            _snapshotMenuItem.IsEnabled = false;
            _disconnectMenuItem.Visibility = Visibility.Collapsed;
            _slotsView.Visibility= Visibility.Hidden;
            _portList.Visibility= Visibility.Hidden;
            _poeActions.Visibility= Visibility.Hidden;
        }

        #endregion private methods
    }
}

using PoEWizard.Components;
using PoEWizard.Data;
using PoEWizard.Device;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private readonly string appPath;
        private readonly string templatePath;
        private string selectedFunction;
        private string selectedConfig;
        private DeviceModel device;
        private readonly IProgress<ProgressReport> progress;
        private bool checkPort = true;
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
            DataContext = this;
            Instance = this;
            device = new DeviceModel();
            // progress report handling
            progress = new Progress<ProgressReport>(report =>
            {
                switch (report.Type)
                {
                    case ReportType.Status:
                        ShowInfoBox(report.Message);
                        break;
                    case ReportType.Error:
                        ShowMessageBox(report.Title, report.Message, MsgBoxIcons.Error);
                        break;
                    case ReportType.Warning:
                        ShowMessageBox(report.Title, report.Message, MsgBoxIcons.Warning);
                        break;
                    case ReportType.Info:
                        ShowMessageBox(report.Title, report.Message, MsgBoxIcons.Info);
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
            Login login = new Login(DeviceModel.Username)
            {
                Password = DeviceModel.Password,
                IpAddress = DeviceModel.IpAddress,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (login.ShowDialog() == true)
            {
                DeviceModel.Username = login.User;
                DeviceModel.Password = login.Password;
                DeviceModel.IpAddress = login.IpAddress;
            }
        }

        private void ConnectMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ConnectBtn_Click(object sender, MouseEventArgs e)
        {

        }

        private void ViewActivity_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ViewLog_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ViewSnapshot_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RunWiz_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {

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

        private void HideInfoBox()
        {
            _infoBox.Visibility = Visibility.Collapsed;
        }

        #endregion private methods
    }
}

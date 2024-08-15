using PoEWizard.Data;
using PoEWizard.Device;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for ConfigWiz.xaml
    /// </summary>
    public partial class ConfigWiz : Window
    {
        private int pageNo;
        private const int pageCount = 4;
        private readonly SystemModel sysData;
        private readonly ServerModel srvData;
        private readonly FeatureModel features;
        private readonly SnmpModel snmpData;

        public bool IsRebootSwitch { get; set; } = false;

        public List<string> Errors { get; private set; }

        #region Constructor

        public ConfigWiz(SwitchModel device)
        {
            InitializeComponent();
            if (MainWindow.theme == ThemeType.Dark)
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[0]);
            }
            else
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[1]);
            }
            DataContext = this;
            Errors = new List<string>();
            sysData = new SystemModel(device);
            srvData = new ServerModel();
            features = new FeatureModel(device);
            snmpData = new SnmpModel();
            pageNo = 1;
            _btnCfgBack.IsEnabled = false;
            _cfgFrame.Navigate(new CfgWizPage1(sysData));
            _btnSubmit.IsEnabled = false;
        }
        #endregion

        #region Event Handlers
        private void CfgBack_Click(object sender, RoutedEventArgs e)
        {
            pageNo = Math.Max(1, pageNo - 1);
            if (pageNo == 1) _btnCfgBack.IsEnabled = false;
            _btnCfgNext.IsEnabled = true;
            NavigateToPage();
        }

        private void CfgNext_Click(object sender, RoutedEventArgs e)
        {
            pageNo = Math.Min(pageCount, pageNo + 1);
            if (pageNo == pageCount)
            {
                _btnCfgNext.IsEnabled = false;
                _btnSubmit.IsEnabled = true;
            }
            _btnCfgBack.IsEnabled = true;
            NavigateToPage();
        }

        private async void CfgSubmit_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                ApplyCommands(sysData.ToCommandList(), "Applying System parameters...");
                ApplyCommands(srvData.ToCommandList(), "Applying DNS and NPT parameters...");
                //ApplyCommands(features.ToCommandList(), "Applying Features...");
                //ApplyCommands(snmpData.ToCommandList(), "Applying SNMP configuration...");
            });
            HideInfoBox();
            DialogResult = true;
            Close();
        }

        private void CfgCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

        #region Private Methods
        private void NavigateToPage()
        {
            Page page = null; ;
            switch (pageNo)
            {
                case 1:
                    page = new CfgWizPage1(sysData);
                    break;
                case 2:
                    page = new CfgWizPage2(srvData);
                    break;
                case 3:
                    page = new CfgWizPage3(features);
                    break; ;
                case 4:
                    page = new CfgWizPage4(snmpData);
                    break;
                default:
                    page = new CfgWizPage4(snmpData);
                    break;
            }
            _cfgTitle.Text = page.Title;
            _cfgFrame.Navigate(page);
        }

        private void ApplyCommands(List<CmdRequest> cmds, string message)
        {
            if (cmds.Count == 0) return;
            ShowInfoBox(message);

            foreach (CmdRequest cmd in cmds)
            {
                try
                {
                    MainWindow.restApiService.RunSwichCommand(cmd);
                }
                catch (Exception ex)
                {
                    if (!Regex.IsMatch(ex.Message, MATCH_POE_RUNNING))
                        Errors.Add(ex.Message);
                }

            }
        }

        private void ShowInfoBox(string message)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                _infoBlock.Inlines.Clear();
                _infoBlock.Inlines.Add(message);
                _infoBox.Visibility = Visibility.Visible;
            }));
        }

        private void HideInfoBox()
        {
            _infoBox.Visibility = Visibility.Hidden;
        }

        #endregion
    }
}

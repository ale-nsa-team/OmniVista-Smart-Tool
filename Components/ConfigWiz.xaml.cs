using PoEWizard.Comm;
using PoEWizard.Data;
using PoEWizard.Device;
using System;
using System.Linq;
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
        private RestApiService restSrv;
        private SwitchModel device;
        private int pageNo;
        private const int pageCount = 4;
        private readonly SystemModel sysData;
        private readonly ServerModel srvData;
        private readonly FeatureModel features;
        private readonly SnmpModel snmpData;
        private ServerModel srvOrig;
        private SystemModel sysOrig;
        private FeatureModel featOrig;

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
            this.device = device;
            restSrv = MainWindow.restApiService;
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
        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            ShowInfoBox("Loading current paramaeters...");
            await Task.Run(() => 
            {
                List<Dictionary<string, string>> dicList = restSrv.RunSwichCommand(new CmdRequest(Command.SHOW_IP_INTERFACE, ParseType.Htable)) as List<Dictionary<string, string>>;
                Dictionary<string, string> dict = dicList.FirstOrDefault(d => d[IP_ADDR] == device.IpAddress);
                if (dict != null) sysData.NetMask = dict[SUBNET_MASK];
                dicList = restSrv.RunSwichCommand(new CmdRequest(Command.SHOW_IP_ROUTES, ParseType.Htable)) as List<Dictionary<string, string>>;
                dict = dicList.FirstOrDefault(d => d[DNS_DEST] == "0.0.0.0/0");
                if (dict != null) srvData.Gateway = dict[GATEWAY];
                dicList = restSrv.RunSwichCommand(new CmdRequest(Command.SHOW_DNS_CONFIG, ParseType.Htable))as List<Dictionary<string, string>>;
                if (dicList.Count > 0)
                {
                    srvData.IsDns = dicList[0][DNS_ENABLE] == "1";
                    srvData.DnsDomain = dicList[0][DNS_DOMAIN];
                    srvData.Dns1 = dicList[0][DNS1];
                    srvData.Dns2 = dicList[0][DNS2];
                    srvData.Dns3 = dicList[0][DNS3];
                }

                dict = restSrv.RunSwichCommand(new CmdRequest(Command.SHOW_NTP_STATUS, ParseType.Vtable)) as Dictionary<string, string>;
                if ( dict != null) srvData.IsNtp = dict[NTP_ENABLE] == "enabled";
                if (srvData.IsNtp)
                {
                    dicList = restSrv.RunSwichCommand(new CmdRequest(Command.SHOW_NTP_CONFIG, ParseType.Htable)) as List<Dictionary<string, string>>;
                    int n = Math.Min(dicList.Count, 3);
                    for (int i = 0; i < n; i++)
                    {
                        if (i == 0) srvData.Ntp1 = dicList[i][NTP_SERVER];
                        if (i == 1) srvData.Ntp2 = dicList[i][NTP_SERVER];
                        if (i == 2) srvData.Ntp3 = dicList[i][NTP_SERVER];
                    }
                }

                dicList = restSrv.RunSwichCommand(new CmdRequest(Command.SHOW_DHCP_CONFIG, ParseType.Htable)) as List<Dictionary<string, string>>;
                if (dicList.Count > 0) features.IsDhcpRelay = dicList[0][DHCP_ENABLE] == "1";
                if (features.IsDhcpRelay)
                {
                    dicList = restSrv.RunSwichCommand(new CmdRequest(Command.SHOW_DHCP_RELAY, ParseType.Htable)) as List<Dictionary<string, string>>;
                    if (dicList.Count > 0 && dicList[0].Count > 0) features.DhcpSrv = dicList[0][DHCP_DEST];
                }

                dicList = restSrv.RunSwichCommand(new CmdRequest(Command.SHOW_IP_SERVICE, ParseType.Htable)) as List<Dictionary<string, string>>;
                if (dicList.Count > 0)
                {
                    dict = dicList.FirstOrDefault(d => d["Name"] == "ftp");
                    bool isftp = dict != null ? dict["Status"] == "enabled" : false;
                    dict = dicList.FirstOrDefault(d => d["Name"] == "telnet");
                    bool istelnet = dict != null ? dict["Status"] == "enabled" : false;
                    features.IsInsecureProtos = isftp || istelnet;
                    dict = dict = dicList.FirstOrDefault(d => d["Name"] == "ssh");
                    features.IsSsh = dict != null ? dict["Status"] == "enabled" : false;
                }
                dict = restSrv.RunSwichCommand(new CmdRequest(Command.SHOW_MULTICAST, ParseType.Etable)) as Dictionary<string, string>;
                if (dict != null) features.IsMulticast = dict["Status"] == "enabled";
                dicList = restSrv.RunSwichCommand(new CmdRequest(Command.SHOW_USER, ParseType.MVTable, DictionaryType.User)) as  List<Dictionary<string, string>>;
                if ( dicList.Count > 0)
                {
                    foreach (var dic in dicList)
                    {
                        if (dic[SNMP_ALLOWED] == "YES")
                        {
                            SnmpUser user = new SnmpUser(dic[USER]);
                            if (dic.ContainsKey(SNMP_AUTH)) user.Protocol = dic[SNMP_AUTH];
                            if (dic.ContainsKey(SNMP_ENC)) user.Encryption = dic[SNMP_ENC];
                            snmpData.Users.Add(user);
                        }
                    }
                }

            });

            srvOrig = srvData.Clone() as ServerModel;
            sysOrig = sysData.Clone() as SystemModel;
            featOrig = features.Clone() as FeatureModel;

            HideInfoBox();
        }

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
                ApplyCommands(sysData.ToCommandList(sysOrig), "Applying System parameters...");
                ApplyCommands(srvData.ToCommandList(srvOrig), "Applying DNS and NPT parameters...");
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
                    restSrv.RunSwichCommand(cmd);
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

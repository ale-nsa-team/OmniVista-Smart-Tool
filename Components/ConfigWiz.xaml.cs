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
        private SnmpModel snmpOrig;

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
            //_btnSubmit.IsEnabled = false;
        }
        #endregion

        #region Event Handlers
        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            ShowInfoBox("Loading current paramaeters, please wait...");
            await Task.Run(() => 
            {
                GetServerData();
                GetFeaturesData();
                GetSnmpData();
            });

            srvOrig = srvData.Clone() as ServerModel;
            sysOrig = sysData.Clone() as SystemModel;
            featOrig = features.Clone() as FeatureModel;
            snmpOrig = snmpData.Clone() as SnmpModel;

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
                //_btnSubmit.IsEnabled = true;
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
                ApplyCommands(features.ToCommandList(featOrig), "Applying Features...");
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
                    restSrv.RunSwitchCommand(cmd);
                }
                catch (Exception ex)
                {
                    if (!Regex.IsMatch(ex.Message, MATCH_POE_RUNNING))
                        Errors.Add(ex.Message);
                }

            }
        }

        private void GetServerData()
        {
            List<Dictionary<string, string>> dicList = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_IP_ROUTES, ParseType.Htable)) as List<Dictionary<string, string>>;
            Dictionary<string, string>  dict = dicList.FirstOrDefault(d => d[DNS_DEST] == "0.0.0.0/0");
            if (dict != null) srvData.Gateway = dict[GATEWAY];
            dicList = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_DNS_CONFIG, ParseType.MibTable, DictionaryType.MibList)) as List<Dictionary<string, string>>;
            if (dicList?.Count > 0)
            {
                srvData.IsDns = dicList[0][DNS_ENABLE] == "1";
                srvData.DnsDomain = dicList[0][DNS_DOMAIN];
                srvData.Dns1 = GetDnsAddr(dicList[0][DNS1]);
                srvData.Dns2 = GetDnsAddr(dicList[0][DNS2]);
                srvData.Dns3 = GetDnsAddr(dicList[0][DNS3]);
            }

            dict = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_NTP_STATUS, ParseType.Vtable)) as Dictionary<string, string>;
            if (dict != null) srvData.IsNtp = dict[NTP_ENABLE] == "enabled";
            if (srvData.IsNtp)
            {
                dicList = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_NTP_CONFIG, ParseType.MibTable, DictionaryType.MibList)) as List<Dictionary<string, string>>;
                int n = Math.Min(dicList?.Count ?? 0, 3);
                for (int i = 0; i < n; i++)
                {
                    if (i == 0) srvData.Ntp1 = dicList[i].TryGetValue(NTP_SERVER, out string v) ? v : "";
                    if (i == 1) srvData.Ntp2 = dicList[i].TryGetValue(NTP_SERVER, out string v) ? v : "";
                    if (i == 2) srvData.Ntp3 = dicList[i].TryGetValue(NTP_SERVER, out string v) ? v : "";
                }
            }
        }

        private string GetDnsAddr(string dns)
        {
            return dns == "0.0.0.0" ? string.Empty : dns;
        }

        private void GetFeaturesData()
        {
            List<Dictionary<string,string>> dicList = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_DHCP_CONFIG, ParseType.MibTable, DictionaryType.MibList)) as List<Dictionary<string, string>>;
            if (dicList?.Count > 0) features.IsDhcpRelay = dicList[0][DHCP_ENABLE] == "1";
            if (features.IsDhcpRelay)
            {
                dicList = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_DHCP_RELAY, ParseType.MibTable, DictionaryType.MibList)) as List<Dictionary<string, string>>;
                if (dicList.Count > 0 && dicList[0].Count > 0) features.DhcpSrv = dicList[0][DHCP_DEST];
            }

            dicList = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_IP_SERVICE, ParseType.Htable)) as List<Dictionary<string, string>>;
            Dictionary<string, string> dict;
            if (dicList.Count > 0)
            {
                dict = dicList.FirstOrDefault(d => d["Name"] == "ftp");
                bool isftp = dict != null && dict["Status"] == "enabled";
                dict = dicList.FirstOrDefault(d => d["Name"] == "telnet");
                bool istelnet = dict != null && dict["Status"] == "enabled";
                features.IsInsecureProtos = isftp || istelnet;
                dict = dicList.FirstOrDefault(d => d["Name"] == "ssh");
                features.IsSsh = dict != null && dict["Status"] == "enabled";
            }
            dict = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_MULTICAST, ParseType.Etable)) as Dictionary<string, string>;
            if (dict != null) features.IsMulticast = dict["Status"] == "enabled";
        }

        private void GetSnmpData()
        {
            List<Dictionary<string, string>> dicList = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_USER, ParseType.MVTable, DictionaryType.User)) as List<Dictionary<string, string>>;
            if (dicList.Count > 0)
            {
                foreach (var dic in dicList)
                {
                    if (dic[SNMP_ALLOWED] == "YES")
                    {
                        SnmpUser user = new SnmpUser(dic["User name"]);
                        if (dic.ContainsKey(SNMP_AUTH)) user.Protocol = dic[SNMP_AUTH];
                        if (dic.ContainsKey(SNMP_ENC)) user.Encryption = dic[SNMP_ENC];
                        snmpData.Users.Add(user);
                    }
                }
            }

            dicList = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_SNMP_COMMUNITY, ParseType.Htable)) as List<Dictionary<string, string>>;
            if ( dicList.Count > 0)
            {
                foreach(var dic in dicList)
                {
                    snmpData.Communities.Add(new SnmpCommunity(dic[SNMP_COMMUNITY], dic["user name"], dic[SNMP_STATUS]));
                }
            }

            dicList = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_SNMP_STATION, ParseType.Htable)) as List<Dictionary<string, string>>;
            if ( dicList.Count > 0)
            {
                foreach (var dic in dicList)
                {
                    string ip_port = dic[SNMP_STATION_IP];
                    SnmpStation station = new SnmpStation(ip_port, dic[SNMP_STATUS], dic[SNMP_VERSION], dic[USER]);
                    if (station.Version == "v2")
                    {
                        SnmpCommunity comm = snmpData.Communities.FirstOrDefault(c => c.User == station.User);
                        if (comm != null)
                        {
                            station.Community = comm.Name;
                            station.User = "--";
                        }
                    }
                    else
                    {
                        station.Community += "--";
                    }

                    snmpData.Stations.Add(station);
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

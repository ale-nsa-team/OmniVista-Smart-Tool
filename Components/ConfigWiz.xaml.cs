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
using System.Threading;

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
        
        public bool MustDisconnect { get; set; } = false;

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
            srvData = new ServerModel(device.DefaultGwy);
            features = new FeatureModel(device);
            snmpData = new SnmpModel();
            pageNo = 1;
            sysOrig = sysData.Clone() as SystemModel;
            _btnCfgBack.IsEnabled = false;            
            _cfgFrame.Navigate(new CfgWizPage1(sysData));
            //_btnSubmit.IsEnabled = false;
        }
        #endregion

        #region Event Handlers
        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            ShowInfoBox("Loading current parameters, please wait...");
            await Task.Run(() =>
            {
                GetServerData();
                GetFeaturesData();
                GetSnmpData();
            });

            srvOrig = srvData.Clone() as ServerModel;            
            featOrig = features.Clone() as FeatureModel;
            snmpOrig = snmpData.Clone() as SnmpModel;
            HideInfoBox();
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (snmpData.HasChanges(snmpOrig))
            {
                DialogResult = true;
            }
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
            bool needRefresh = false;
            await Task.Run(() =>
            {

                ApplyCommands(srvData.ToCommandList(srvOrig), "Applying DNS and NPT parameters...");
                needRefresh = ApplyCommands(features.ToCommandList(featOrig), "Applying Features...");
                List<CmdRequest> cmds = sysData.ToCommandList(sysOrig);
                MustDisconnect = cmds.FirstOrDefault(c => c.Command == Command.SET_IP_INTERFACE) != null;
                needRefresh = ApplyCommands(cmds, "Applying System parameters...") || needRefresh;
            });
            HideInfoBox();

            if (needRefresh && !MustDisconnect)
            {
                ShowInfoBox("Reloading switch data, please wait...");
                await Task.Run(() => restSrv.ScanSwitch(null));
            }
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
            Page page = null;
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

        private bool ApplyCommands(List<CmdRequest> cmds, string message)
        {
            if (cmds.Count == 0) return false;
            bool res = false;
            ShowInfoBox(message);

            foreach (CmdRequest cmd in cmds)
            {
                try
                {
                    Logger.Info($"Config wizard: applying command {cmd.Command} {(cmd.Data != null ? "With data: " + string.Join(", ", cmd.Data) : "")}");
                    restSrv.RunSwitchCommand(cmd);
                    if (cmd.Command == Command.START_STOP_POE)
                    {
                        WaitChassisUp();
                    }
                    res = cmd.Command == Command.START_STOP_POE || cmd.Command == Command.STOP_POE;
                }
                catch (Exception ex)
                {
                    if (!Regex.IsMatch(ex.Message, MATCH_POE_RUNNING))
                        Errors.Add(ex.Message);
                }
            }
            return res || !message.Contains("Features");
        }

        private void GetServerData()
        {
            List<Dictionary<string, string>> dicList = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_DNS_CONFIG, ParseType.MibTable, DictionaryType.MibList)) as List<Dictionary<string, string>>;
            if (dicList?.Count > 0)
            {
                srvData.IsDns = dicList[0][DNS_ENABLE] == "1";
                srvData.DnsDomain = dicList[0][DNS_DOMAIN];
                srvData.Dns1 = GetDnsAddr(dicList[0][DNS1]);
                srvData.Dns2 = GetDnsAddr(dicList[0][DNS2]);
                srvData.Dns3 = GetDnsAddr(dicList[0][DNS3]);
            }

            Dictionary<string, string> dict = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_NTP_STATUS, ParseType.Vtable)) as Dictionary<string, string>;
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
            dict = restSrv.RunSwitchCommand(new CmdRequest(Command.SYSTEM_TIMEZONE, ParseType.Vtable, "")) as Dictionary<string, string>;
            string tz = dict.Keys.FirstOrDefault();
            if (!srvData.Timezones.Contains(tz)) timezones.Prepend(tz);
            srvData.Timezone = tz;
        }

        private string GetDnsAddr(string dns)
        {
            return dns == "0.0.0.0" ? string.Empty : dns;
        }

        private void GetFeaturesData()
        {
            List<Dictionary<string, string>> dicList = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_DHCP_CONFIG, ParseType.MibTable, DictionaryType.MibList)) as List<Dictionary<string, string>>;
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
                bool noftp = dict != null && dict["Status"] == "disabled";
                dict = dicList.FirstOrDefault(d => d["Name"] == "telnet");
                bool notelnet = dict != null && dict["Status"] == "disabled";
                features.NoInsecureProtos = noftp && notelnet;
                dict = dicList.FirstOrDefault(d => d["Name"] == "ssh");
                features.IsSsh = dict != null && dict["Status"] == "enabled";
            }
            dict = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_MULTICAST_GLOBAL, ParseType.Etable)) as Dictionary<string, string>;
            if (dict != null) features.IsMulticast = dict["Status"] == "enabled";
            dicList = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_VLAN, ParseType.Htable)) as List<Dictionary<string, string>>;
            if (dicList.Count > 0)
            {
                foreach(var dic in dicList)
                {
                    if (dic["type"] == "std")
                    {
                        string vlan = dic["vlan"];
                        dict = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_MULTICAST_VLAN, ParseType.Etable, vlan)) as Dictionary<string, string>;
                        if (dict?.Count > 0)
                        {
                            features.Vlans.Add(new EnableObj(dic["vlan"], !dict["Status"].Contains("disabled")));
                        }
                    }
                }

            }
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
            if (dicList.Count > 0)
            {
                foreach (var dic in dicList)
                {
                    snmpData.Communities.Add(new SnmpCommunity(dic[SNMP_COMMUNITY], dic["user name"], dic[SNMP_STATUS]));
                }
            }

            dicList = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_SNMP_STATION, ParseType.Htable)) as List<Dictionary<string, string>>;
            if (dicList.Count > 0)
            {
                foreach (var dic in dicList)
                {
                    string ip_port = dic[SNMP_STATION_IP];
                    SnmpStation station = new SnmpStation(ip_port, dic[SNMP_STATUS], dic[SNMP_VERSION], dic[USER], null);
                    if (station.Version == "v2")
                    {
                        SnmpCommunity comm = snmpData.Communities.FirstOrDefault(c => c.User == dic[USER]);
                        station.Community = comm?.Name ?? "--";
                    }
                    snmpData.Stations.Add(station);
                }
            }
        }

        private void WaitChassisUp()
        {
            foreach (var chassis in device.ChassisList)
            {
                foreach (var slot in chassis.Slots)
                {
                    ShowInfoBox($"Waiting for slot {slot.Name} to come up...");
                    int count = 0;
                    while (count < 15)
                    {
                        Thread.Sleep(2000);
                        GetSlotPowerStatus(slot);
                        if (slot.IsInitialized) break;
                        count++;
                    }
                }
            }
        }

        private void GetSlotPowerStatus(SlotModel slot)
        {
            var _dictList = restSrv.RunSwitchCommand(new CmdRequest(Command.SHOW_SLOT_LAN_POWER_STATUS, ParseType.Htable2, slot.Name)) as List<Dictionary<string, string>>;
            if (_dictList?.Count > 0) slot.LoadFromDictionary(_dictList[0]);
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
            _infoBox.Visibility = Visibility.Collapsed;
        }
        #endregion
    }
}

using PoEWizard.Comm;
using PoEWizard.Data;
using PoEWizard.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for CfgWizPage4.xaml
    /// </summary>
    public partial class CfgWizPage4 : Page
    {
        private readonly List<Command> enableSnmp = new List<Command>()
        {
            Command.SNMP_AUTH_LOCAL,
            Command.SNMP_NO_SECURITY,
            Command.SNMP_COMMUNITY_MODE
        };


        private readonly SnmpModel data;
        private readonly RestApiService restSrv;
        private readonly ImageSource eye_open;
        private readonly ImageSource eye_closed;

        public CfgWizPage4(SnmpModel snmpData)
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

            eye_open = (ImageSource)Resources.MergedDictionaries[0]["eye_open"];
            eye_closed = (ImageSource)Resources.MergedDictionaries[0]["eye_closed"];

            data = snmpData;
            DataContext = data;
            restSrv = MainWindow.restApiService;
        }

        private async void AddUser(object sender, RoutedEventArgs e)
        {
            NewUser usr = new NewUser()
            {
                Owner = MainWindow.Instance,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (usr.ShowDialog() == true)
            {
                string err = null;
                await Task.Run(() =>
                {
                    try
                    {
                        restSrv.RunSwitchCommand(new CmdRequest(Command.SNMP_USER, usr.Username, usr.Password, "md5+des", usr.Password));
                    }
                    catch (Exception ex)
                    {
                        err = ex.Message;
                    }

                });
                if (err != null)
                {
                    CustomMsgBox box = new CustomMsgBox(MainWindow.Instance, MsgBoxButtons.Ok)
                    {
                        Message = err,
                        Img = MsgBoxIcons.Error
                    };
                    box.ShowDialog();

                }
                else
                {
                    data.AddUser(usr.Username, usr.Password);
                }
            }
        }

        private async void AddCommunity(object sender, RoutedEventArgs e)
        {
            NewCommunity cmy = new NewCommunity()
            {
                Owner = MainWindow.Instance,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            cmy.Users = data.Users.Select(u => u.Name).ToList();
            if (cmy.ShowDialog() == true)
            {
                await Task.Run(() =>
                {
                    foreach (var cmd in enableSnmp)
                    {
                        restSrv.RunSwitchCommand(new CmdRequest(cmd));
                    }
                    restSrv.RunSwitchCommand(new CmdRequest(Command.SNMP_COMMUNITY_MAP, cmy.CommunityName, cmy.SelectedUser));
                });
                data.AddCommunity(cmy.CommunityName, cmy.SelectedUser);
            }
        }

        private async void AddStation(object sender, RoutedEventArgs e)
        {
            NewTrapReceiver recv = new NewTrapReceiver()
            {
                Owner = MainWindow.Instance,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            recv.Users = data.Users.Select(u => u.Name).ToList();
            recv.Communities = data.Communities.Select(c => c.Name).ToList();
            if (recv.ShowDialog() == true)
            {
                string user;
                string version;
                if (recv.Version.Contains("v2"))
                {
                    var comm = data.Communities.FirstOrDefault(c => c.Name == recv.SelectedCommunity);
                    user = comm?.User;
                    version = "v2";
                }
                else
                {
                    user = recv.SelectedUser;
                    version = "v3";
                }
                await Task.Run(() =>
                {
                    MainWindow.restApiService.RunSwitchCommand(new CmdRequest(Command.SNMP_STATION, recv.IpAddress, user, version));
                });
                data.AddStation(recv.IpAddress, version, user, recv.SelectedCommunity);
            }
        }
    }
}

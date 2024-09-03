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

        private void DeleteUser(object sender, RoutedEventArgs e)
        {
            if (_users.CurrentItem is SnmpUser user)
            {
                var coms = data.GetUserCommunities(user.Name);
                var sts = data.GetUserStations(user.Name);
                List<string> msg = new List<string>();
                if (coms.Count > 0) msg.Add("Communities");
                if (sts.Count > 0) msg.Add($"{(coms.Count > 0 ? "and " : "")}Trap Receivers");
                if (msg.Count > 0)
                {
                    msg.Add("related to this user will also be deleted\nPlease confirm your action.");
                    CustomMsgBox box = new CustomMsgBox(MainWindow.Instance, MsgBoxButtons.OkCancel)
                    {
                        Title = "Delete user",
                        Message = string.Join(" ", msg),
                        Img = MsgBoxIcons.Question
                    };
                    if (box.ShowDialog() == true)
                    {
                        foreach (var c in coms)
                        {
                            RunCommand(new CmdRequest(Command.DELETE_COMMUNITY, c.Name));
                            data.DeleteCommunity(c);
                        }

                        foreach (var s in sts)
                        {
                            RunCommand(new CmdRequest(Command.DELETE_STATION, s.IpAddress));
                            data.DeleteStation(s);

                        }
                    }
                    else return;
                }
                RunCommand((new CmdRequest(Command.DELETE_USER, user.Name)));
                data.DeleteUser(user);
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

        private void DeleteCommunity(object sender, RoutedEventArgs e)
        {
            if (_communities.CurrentItem is SnmpCommunity cmy)
            {
                var sts = data.GetCommunityStations(cmy.Name);
                if (sts.Count > 0)
                {
                    CustomMsgBox box = new CustomMsgBox(MainWindow.Instance, MsgBoxButtons.OkCancel)
                    {
                        Title = "Delete Community",
                        Message = "Related stations will also be deleted.\nPlease confirm your action.",
                        Img = MsgBoxIcons.Question
                    };
                    if (box.ShowDialog() == true)
                    {
                        foreach (var s in sts)
                        {
                            RunCommand(new CmdRequest(Command.DELETE_STATION, s.IpAddress));
                            data.DeleteStation(s);
                        }
                    }
                    else return;
                }
                RunCommand(new CmdRequest(Command.DELETE_COMMUNITY, cmy.Name));
                data.DeleteCommunity(cmy);
            }
        }

        private void AddStation(object sender, RoutedEventArgs e)
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
                RunCommand(new CmdRequest(Command.SNMP_STATION, recv.IpAddress, user, version));
                data.AddStation(recv.IpAddress, version, user, recv.SelectedCommunity);
            }
        }

        private void DeleteStation(object sender, RoutedEventArgs e)
        {
            if (_stations.CurrentItem is SnmpStation station)
            {
                RunCommand(new CmdRequest(Command.DELETE_STATION, station.IpAddress));
                data.DeleteStation(station);
            }
        }

        private async void RunCommand(CmdRequest req)
        {
            await Task.Run(() =>
            {
                try
                {
                    restSrv.RunSwitchCommand(req);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            });
        }
    }
}

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
using static PoEWizard.Data.Utils;

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

            if (MainWindow.Theme == ThemeType.Dark)
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[0]);
            }
            else
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[1]);
            }
            Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[1]);
            Resources.MergedDictionaries.Add(MainWindow.Strings);

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
                if (data.UserExists(usr.Username))
                {
                    ShowMsgBox($"{Translate("i18n_user")} {usr.Username} {Translate("i18n_dupObj")}.", true);
                    return;
                }
                bool noErrors = true;
                await Task.Run(() =>
                {
                    noErrors = RunCommand(new CmdRequest(Command.SNMP_USER, usr.Username, usr.Password, "md5+des", usr.Password));
                });
                if (noErrors) data.AddUser(usr.Username, usr.Password);
            }
        }

        private async void DeleteUser(object sender, RoutedEventArgs e)
        {
            if (_users.CurrentItem is SnmpUser user)
            {
                bool ok = true;
                var coms = data.GetUserCommunities(user.Name);
                var sts = data.GetUserStations(user.Name);
                List<string> msg = new List<string>();
                if (coms.Count > 0) msg.Add(Translate("i18n_comms"));
                if (sts.Count > 0) msg.Add($"{(coms.Count > 0 ? Translate("i18n_and") : "")} {Translate("i18n_recv")}");
                if (msg.Count > 0)
                {
                    msg.Add(Translate("i18n_delUser"));
                    if (ShowMsgBox(string.Join(" ", msg), false))
                    {
                        await Task.Run(() =>
                        {
                            foreach (var c in coms)
                            {
                                ok = ok && RunCommand(new CmdRequest(Command.DELETE_COMMUNITY, c.Name));
                                Application.Current.Dispatcher.Invoke(() => data.DeleteCommunity(c));
                            }

                            foreach (var s in sts)
                            {
                                ok = ok && RunCommand(new CmdRequest(Command.DELETE_STATION, s.IpAddress));
                                Application.Current.Dispatcher.Invoke(() => data.DeleteStation(s));

                            }
                        });
                    }
                    else return;
                }
                ok = ok && RunCommand((new CmdRequest(Command.DELETE_USER, user.Name)));
                if (ok) data.DeleteUser(user);
            }
        }

        private async void AddCommunity(object sender, RoutedEventArgs e)
        {
            bool ok = true;
            NewCommunity cmy = new NewCommunity
            {
                Owner = MainWindow.Instance,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Users = data.Users.Select(u => u.Name).ToList()
            };
            if (cmy.ShowDialog() == true)
            {
                if (data.CommunityExists(cmy.CommunityName))
                {
                    ShowMsgBox($"{Translate("i18n_comm")} {cmy.CommunityName} {Translate("i18n_dupObj")}.", true);
                    return;
                }
                await Task.Run(() =>
                {
                    foreach (var cmd in enableSnmp)
                    {
                        ok = ok && RunCommand(new CmdRequest(cmd));
                    }
                    ok = ok && RunCommand(new CmdRequest(Command.SNMP_COMMUNITY_MAP, cmy.CommunityName, cmy.SelectedUser));
                });
                if (ok) data.AddCommunity(cmy.CommunityName, cmy.SelectedUser);
            }
        }

        private async void DeleteCommunity(object sender, RoutedEventArgs e)
        {
            if (_communities.CurrentItem is SnmpCommunity cmy)
            {
                bool ok = true;
                var sts = data.GetCommunityStations(cmy.Name);
                if (sts.Count > 0)
                {
                    if (ShowMsgBox(Translate("i18n_delComm"), false))
                    {
                        await Task.Run(() =>
                        {
                            foreach (var s in sts)
                            {
                                ok = ok && RunCommand(new CmdRequest(Command.DELETE_STATION, s.IpAddress));
                                Application.Current.Dispatcher.Invoke(() => data.DeleteStation(s));
                            }
                        });
                    }
                    else return;
                }
                ok = await Task.Run(() => RunCommand(new CmdRequest(Command.DELETE_COMMUNITY, cmy.Name)));
                if (ok) data.DeleteCommunity(cmy);
            }
        }

        private async void AddStation(object sender, RoutedEventArgs e)
        {
            NewTrapReceiver recv = new NewTrapReceiver
            {
                Owner = MainWindow.Instance,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Users = data.Users.Select(u => u.Name).ToList(),
                Communities = data.Communities.Select(c => c.Name).ToList()
            };
            if (recv.ShowDialog() == true)
            {
                if (data.StationExists(recv.IpAddress))
                {
                    ShowMsgBox($"{Translate("i18n_withIp")} {recv.IpAddress} {Translate("i18n_dupObj")}", true);
                    return;
                }
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
                bool ok = await Task.Run(() => RunCommand(new CmdRequest(Command.SNMP_STATION, recv.IpAddress, user, version)));
                if (ok) data.AddStation(recv.IpAddress, version, user, recv.SelectedCommunity);
            }
        }

        private async void DeleteStation(object sender, RoutedEventArgs e)
        {
            if (_stations.CurrentItem is SnmpStation station)
            {
                bool ok = await Task.Run(() => RunCommand(new CmdRequest(Command.DELETE_STATION, station.IpAddress)));
                if (ok) data.DeleteStation(station);
            }
        }

        private bool RunCommand(CmdRequest req)
        {
            try
            {
                restSrv.SendCommand(req);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Application.Current.Dispatcher.Invoke(() => ShowMsgBox(ex.Message, true));
                return false;
            }
        }

        private bool ShowMsgBox(string message, bool isError)
        {
            CustomMsgBox box = new CustomMsgBox(MainWindow.Instance)
            {
                Header = Translate("i18n_snmp"),
                Message = message,
                Img = isError ? MsgBoxIcons.Error : MsgBoxIcons.Question,
                Buttons = isError ? MsgBoxButtons.Ok : MsgBoxButtons.OkCancel
            };
            return box.ShowDialog() == true;
        }
    }
}

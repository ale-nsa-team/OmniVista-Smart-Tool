using Microsoft.Win32;
using PoEWizard.Data;
using PoEWizard.Device;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.Utils;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for SearchMac.xaml
    /// </summary>
    public partial class SearchDevice : Window
    {
        private readonly SearchType searchType;

        public string SearchText { get; set; }
        public List<PortViewModel> PortsFound { get; set; }
        public PortModel SelectedPort { get; set; }
        public bool IsMacAddress => searchType == SearchType.Mac;
        public int NbPortsFound => PortsFound?.Count ?? 0;

        public SearchDevice(SwitchModel model, string srcParam)
        {
            this.SearchText = !string.IsNullOrEmpty(srcParam) ? srcParam.ToLower().Trim() : string.Empty;
            InitializeComponent();
            DataContext = this;
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

            this.SelectedPort = null;
            if (string.IsNullOrEmpty(this.SearchText)) searchType = SearchType.None;
            if (IsValidPartialIp(srcParam))
            {
                searchType = SearchType.Ip;
                if (MainWindow.IsIpScanRunning)
                {
                    CustomMsgBox cmb = new CustomMsgBox(MainWindow.Instance)
                    {
                        Title = Translate("i18n_src"),
                        Message = Translate("i18n_noIpSrc"),
                        Img = MsgBoxIcons.Warning,
                        Buttons = MsgBoxButtons.Ok
                    };
                    cmb.ShowDialog();
                    SearchText = string.Empty;
                    return;
                }
            }
            else if (IsValidPartialMac(srcParam)) searchType = SearchType.Mac;
            else searchType = SearchType.Name;
            FindDevice(model);
            if (this.PortsFound.Count == 1) this.SelectedPort = this.PortsFound[0].Port;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.MouseDown += delegate { this.DragMove(); };

            this.Height = this._portsListView.ActualHeight + 115;
            this.Top = this.Owner.Height > this.Height ? this.Owner.Top + (this.Owner.Height - this.Height) / 2 : this.Top;
        }

        private void IpAddress_Click(object sender, RoutedEventArgs e)
        {
            //let portselection event run first
            Task.Delay(TimeSpan.FromMilliseconds(250)).ContinueWith(t =>
            {
                Dispatcher.Invoke(() =>
                {
                    ConnectSelectedPort();
                });
            });
        }

        private void FindDevice(SwitchModel model)
        {
            this.PortsFound = new List<PortViewModel>();

            switch (searchType)
            {
                case SearchType.Ip:
                    SearchIpAddress(model);
                    break;
                case SearchType.Mac:
                    SearchMacAddress(model);
                    break;
                case SearchType.Name:
                    SearchNameOrVendor(model);
                    break;
                default:
                    break;
            }
        }

        private void SearchIpAddress(SwitchModel model)
        {
            foreach (var chas in model.ChassisList)
            {
                foreach (var slot in chas.Slots)
                {
                    var ports = slot.Ports.FindAll(p => p.IpAddrList.Any(kvp => kvp.Value.StartsWith(SearchText)));
                    AddPorstToList(ports);
                }
            }
        }

        private void SearchMacAddress(SwitchModel switchModel)
        {
            foreach (var chas in switchModel.ChassisList)
            {
                foreach (var slot in chas.Slots)
                {
                    var ports = slot.Ports.FindAll(p => p.EndPointDeviceList.Any(epd =>
                        epd.MacAddress.Split(',').Any(ma => ma.StartsWith(SearchText, StringComparison.CurrentCultureIgnoreCase))));
                    AddPorstToList(ports);
                }
            }
        }

        private void SearchNameOrVendor(SwitchModel model)
        {
            foreach (var chas in model.ChassisList)
            {
                foreach (var slot in chas.Slots)
                {
                    var ports = slot.Ports.FindAll(p => p.EndPointDeviceList.Any(epd => 
                            epd.Name.IndexOf(SearchText, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                            epd.Vendor.IndexOf(SearchText, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                            GetVendorNames(epd.MacAddress).Any(s => s.IndexOf(SearchText, StringComparison.CurrentCultureIgnoreCase) >= 0)));
                    AddPorstToList(ports);
                }
            }
        }

        private void AddPorstToList(List<PortModel> ports)
        {
            foreach (var port in ports)
            {
                if (!PortsFound.Any(p => p.Port == port)) PortsFound.Add(new PortViewModel(port, SearchText));
            }
        }

        private void PortSelection_Changed(Object sender, RoutedEventArgs e)
        {
            if (_portsListView.SelectedItem is PortViewModel model)
            {
                SelectedPort = model.Port;
            }
        }

        private void Mouse_DoubleClick(Object sender, RoutedEventArgs e)
        {
            if (_portsListView.SelectedItem is PortModel port)
            {
                SelectedPort = port;
                this.Close();
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ConnectSelectedPort()
        {
            int port = SelectedPort?.RemotePort ?? 0;
            string ipAddr = SelectedPort?.IpAddress.Replace("...","").Trim();
            switch (port)
            {
                case 22:
                case 23:
                    string putty = MainWindow.Config.Get("putty");
                    if (string.IsNullOrEmpty(putty))
                    {
                        var ofd = new OpenFileDialog()
                        {
                            Filter = $"{Translate("i18n_puttyFile")}|*.exe",
                            Title = Translate("i18n_puttyLoc"),
                            InitialDirectory = Environment.SpecialFolder.ProgramFiles.ToString()
                        };
                        if (ofd.ShowDialog() == false) return;
                        putty = ofd.FileName;
                        MainWindow.Config.Set("putty", putty);
                    }
                    string cnx = port == 22 ? "ssh" : "telnet";
                    Process.Start(putty, $"-{cnx} {ipAddr}");
                    break;
                case 80:
                    Process.Start("explorer.exe", $"http://{ipAddr}");
                    break;
                case 443:
                    Process.Start("explorer.exe", $"https://{ipAddr}");
                    break;
                case 3389:
                    Process.Start("mstsc", $"/v: {ipAddr}");
                    break;
                default:
                    //ShowMessageBox("", Translate("i18n_noPtOpen"));
                    break;
            }
        }
    }

    public class PortViewModel
    {
        public PortModel Port { get; set; }
        public string SearchText { get; set; }

        public PortViewModel(PortModel port, string searchText)
        {
            this.Port = port;
            this.SearchText = searchText;
        }
    }
}

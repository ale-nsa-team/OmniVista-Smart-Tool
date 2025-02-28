using Microsoft.Win32;
using PoEWizard.Data;
using PoEWizard.Device;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.Utils;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for SearchMac.xaml
    /// </summary>
    public partial class SearchDevice : Window
    {
        private int prevIdx = -1;

        public IProgress<ProgressReport> Progress { get; set; }
        public string SearchText { get; set; }
        public List<PortViewModel> PortsFound { get; set; }
        public PortModel SelectedPort { get; set; }
        public SearchType SearchType { get; set; }
        public int NbPortsFound => PortsFound?.Count ?? 0;

        public SearchDevice(SwitchModel model, string srcParam)
        {
            this.SearchText = !string.IsNullOrEmpty(srcParam) ? srcParam.ToLower().Trim() : string.Empty;
            InitializeComponent();
            PreviewKeyDown += (s,e) => { if (e.Key == Key.Escape) Close(); };
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
            if (string.IsNullOrEmpty(this.SearchText)) SearchType = SearchType.None;
            if (IsValidPartialIp(srcParam))
            {
                SearchType = SearchType.Ip;
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
            else if (IsValidPartialMac(srcParam)) SearchType = SearchType.Mac;
            else SearchType = SearchType.Name;
            FindDevice(model);
            if (this.PortsFound.Count == 1) this.SelectedPort = this.PortsFound[0].Port;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.MouseDown += delegate { this.DragMove(); };

            this.Height = this._portsListView.ActualHeight + 100;
            double maxh = this.Owner.ActualHeight - 400;
            if (maxh > 120 && maxh < 400)
            {
                this._portsListView.MaxHeight = maxh - 100;
                this.MaxHeight = maxh;
            }
            //this.Top = this.Owner.Height > this.Height ? this.Owner.Top + (this.Owner.Height - this.Height) / 2 : this.Top;
            this.Top = this.Owner.Top + 350; //leave room for the infobox
        }

        private void ShowPopup(object sender, MouseEventArgs e)
        {
            if (sender is TextBlock tb)
            {
                DataGridRow row = DataGridRow.GetRowContainingElement(tb);
                int idx = row?.GetIndex() ?? -1;
                if (idx == -1 || idx == prevIdx) return;
                prevIdx = PortsFound.Count > 1 ? idx: -1;
                if (string.IsNullOrEmpty(tb.Text)) return;
                Task.Delay(IP_LIST_POPUP_DELAY).ContinueWith(t => 
                {
                    Dispatcher.Invoke(() =>
                    {
                        PortModel port = PortsFound[idx].Port;
                        var pos = e.GetPosition(tb);
                        if (Math.Abs(pos.Y) > 100 || Math.Abs(pos.X) > 100) return; //moue is too far away.
                        PopupUserControl popup = new PopupUserControl
                        {
                            Progress = Progress,
                            Data = port.IpAddrList,
                            KeyHeader = "MAC",
                            ValueHeader = "IP",
                            Target = tb,
                            Placement = PlacementMode.Relative,
                            OffsetX = pos.X - 5,
                            OffsetY = pos.Y - 5
                        };
                        popup.Show();
                    });
                });
            }
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

            switch (SearchType)
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
            if (_portsListView.SelectedItem is PortViewModel pvm)
            {
                SelectedPort = pvm.Port;
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
            string ipAddr = SelectedPort?.IpAddress.Replace("...", "").Trim();
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

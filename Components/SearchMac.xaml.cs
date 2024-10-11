using PoEWizard.Data;
using PoEWizard.Device;
using PoEWizard.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for SearchPort.xaml
    /// </summary>
    public partial class SearchPort : Window
    {
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

        private readonly ResourceDictionary strings;
        private readonly string any;
        public string SearchText { get; set; }
        public string DeviceMac => $"{(!string.IsNullOrEmpty(this.SearchText) ? $"\"{this.SearchText}\"" : any)}";
        public ObservableCollection<PortViewModel> PortsFound { get; set; }
        public PortModel SelectedPort { get; set; }
        public bool IsMacAddress {  get; set; }
        public int NbMacAddressesFound { get; set; }
        public int NbPortsFound { get; set; }
        public int NbTotalMacAddressesFound { get; set; }

        public SearchPort(SwitchModel device, string macAddress)
        {
            this.SearchText = !string.IsNullOrEmpty(macAddress) ? macAddress.ToLower().Trim() : string.Empty;
            InitializeComponent();
            DataContext = this;
            if (MainWindow.theme == ThemeType.Dark)
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[0]);
            }
            else
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[1]);
            }
            strings = Resources.MergedDictionaries[1];
            any = (string)strings["i18n_any"];
            this.SelectedPort = null;
            SearchMacAddress(device, macAddress);
            if (this.PortsFound.Count == 1) this.SelectedPort = this.PortsFound[0].Port;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.MouseDown += delegate { this.DragMove(); };

            this.Height = this._portsListView.ActualHeight + 115;
            this.Top = this.Owner.Height> this.Height ? this.Owner.Top + (this.Owner.Height - this.Height)/2 : this.Top;
        }

        private void SearchMacAddress(SwitchModel switchModel, string macAddr)
        {
            this.IsMacAddress = Utils.IsValidMacSequence(this.SearchText);
            this.PortsFound = new ObservableCollection<PortViewModel>();
            this.NbMacAddressesFound = 0;
            this.NbTotalMacAddressesFound = 0;
            foreach (var chas in switchModel.ChassisList)
            {
                foreach (var slot in chas.Slots)
                {
                    foreach (var port in slot.Ports)
                    {
                        if (port.EndPointDevicesList?.Count > 0)
                        {
                            string nameVendor = GetDeviceNameOrVendor(port);
                            if (IsDevicePortFound(nameVendor) && this.PortsFound.FirstOrDefault(p => p.Port == port) == null)
                            {
                                if (port.MacList?.Count == 0) port.MacList.Add(port.EndPointDevicesList[0].MacAddress);
                                if (!this.IsMacAddress)
                                {
                                    if (!string.IsNullOrEmpty(nameVendor)) this.NbMacAddressesFound++;
                                }
                                else
                                {
                                    foreach (EndPointDeviceModel device in port.EndPointDevicesList)
                                    {
                                        if (string.IsNullOrEmpty(device.MacAddress)) continue;
                                        if (!device.MacAddress.Contains(",")) this.NbMacAddressesFound++;
                                        else AddMacFound(new List<string>(device.MacAddress.Split(',')));
                                    }
                                }
                                this.PortsFound.Add(new PortViewModel(port, SearchText));
                                this.NbTotalMacAddressesFound += port.MacList.Count;
                            }
                        }
                        if (port.MacList?.Count == 0) continue;
                        foreach (string mac in port.MacList)
                        {
                            if (IsDevicePortFound(mac) && this.PortsFound.FirstOrDefault(p => p.Port == port) == null)
                            {
                                AddMacFound(port.MacList);
                                this.PortsFound.Add(new PortViewModel(port, SearchText));
                                this.NbTotalMacAddressesFound += port.MacList.Count;
                                port.CreateVirtualDeviceEndpoint();
                                break;
                            }
                        }
                    }
                }
            }
            if (string.IsNullOrEmpty(this.SearchText)) this.NbMacAddressesFound = this.NbTotalMacAddressesFound;
            this.NbPortsFound = this.PortsFound.Count;
        }

        private void AddMacFound(List<string> macList)
        {
            if (string.IsNullOrEmpty(this.SearchText)) return;
            foreach (string macAddr in macList)
            {
                if (IsDevicePortFound(macAddr)) this.NbMacAddressesFound++;
            }
        }

        private bool IsDevicePortFound(string macAddr)
        {
            if (this.IsMacAddress && !string.IsNullOrEmpty(macAddr) && macAddr.StartsWith(this.SearchText)) return true;
            else return Utils.GetVendorName(macAddr).ToLower().Contains(this.SearchText);
        }

        private string GetDeviceNameOrVendor(PortModel port)
        {
            foreach(EndPointDeviceModel device in port.EndPointDevicesList)
            {
                string nameVendor = device.Name.ToLower();
                if (string.IsNullOrEmpty(nameVendor) || !nameVendor.Contains(this.SearchText)) nameVendor = device.Vendor.ToLower();
                if (string.IsNullOrEmpty(nameVendor) || !nameVendor.Contains(this.SearchText)) nameVendor = SearchVendorInMacList(new List<string>(device.MacAddress.Split(',')));
                if (string.IsNullOrEmpty(nameVendor) || !nameVendor.Contains(this.SearchText)) continue;
                return nameVendor;
            }
            return string.Empty;
        }

        private string SearchVendorInMacList(List<string> macList)
        {
            foreach (string mac in macList)
            {
                string vendor = Utils.GetVendorName(mac).ToLower();
                if (!string.IsNullOrEmpty(vendor) && !vendor.Contains(":") && vendor.Contains(this.SearchText)) return vendor;
                else if (mac.StartsWith(this.SearchText)) return mac;
            }
            return string.Empty;
        }

        private void PortSelection_Changed(Object sender, RoutedEventArgs e)
        {
            if (_portsListView.SelectedItem is PortModel port)
            {
                SelectedPort = port;
            }
        }

        private void Mouse_DoubleClick(Object sender, RoutedEventArgs e)
        {
            if (_portsListView.SelectedItem is PortViewModel port)
            {
                SelectedPort = port.Port;
                this.Close();
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}

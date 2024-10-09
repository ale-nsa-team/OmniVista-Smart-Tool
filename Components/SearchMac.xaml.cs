using PoEWizard.Data;
using PoEWizard.Device;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for SearchPort.xaml
    /// </summary>
    public partial class SearchPort : Window
    {

        private string _device_mac = string.Empty;

        public string DeviceMac => $"{(!string.IsNullOrEmpty(this._device_mac) ? $"\"{this._device_mac}\"" : "Any")}";
        public ObservableCollection<PortModel> PortsFound { get; set; }
        public PortModel SelectedPort { get; set; }
        public bool IsMacAddress {  get; set; }
        public int NbMacAddressesFound { get; set; }
        public int NbPortsFound { get; set; }
        public int NbTotalMacAddressesFound { get; set; }

        public SearchPort(SwitchModel device, string macAddress)
        {
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
            this.SelectedPort = null;
            SearchMacAddress(device, macAddress);
            if (this.PortsFound.Count == 1) this.SelectedPort = this.PortsFound[0];
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.MouseDown += delegate { this.DragMove(); };

            this.Height = this._portsListView.ActualHeight + 115;
            this.Top = this.Owner.Height> this.Height ? this.Owner.Top + (this.Owner.Height - this.Height)/2 : this.Top;
        }

        private void SearchMacAddress(SwitchModel device, string macAddr)
        {
            this._device_mac = !string.IsNullOrEmpty(macAddr) ? macAddr.ToLower().Trim() : string.Empty;
            this.IsMacAddress = Utils.IsValidMacSequence(this._device_mac);
            this.PortsFound = new ObservableCollection<PortModel>();
            this.NbMacAddressesFound = 0;
            this.NbTotalMacAddressesFound = 0;
            foreach (var chas in device.ChassisList)
            {
                foreach (var slot in chas.Slots)
                {
                    foreach (var port in slot.Ports)
                    {
                        if (port.EndPointDevicesList?.Count > 0)
                        {
                            if (IsDevicePortFound(GetDeviceNameOrVendor(port)) && !this.PortsFound.Contains(port))
                            {
                                if (port.MacList?.Count == 0) port.MacList.Add(port.EndPointDevicesList[0].MacAddress);
                                AddMacFound(port);
                                this.PortsFound.Add(port);
                                this.NbTotalMacAddressesFound += port.MacList.Count;
                            }
                        }
                        if (port.MacList?.Count == 0) continue;
                        foreach (string mac in port.MacList)
                        {
                            if (IsDevicePortFound(mac) && !this.PortsFound.Contains(port))
                            {
                                AddMacFound(port);
                                this.PortsFound.Add(port);
                                this.NbTotalMacAddressesFound += port.MacList.Count;
                                port.CreateVirtualDeviceEndpoint();
                                break;
                            }
                        }
                    }
                }
            }
            if (string.IsNullOrEmpty(this._device_mac)) this.NbMacAddressesFound = this.NbTotalMacAddressesFound;
            this.NbPortsFound = this.PortsFound.Count;
        }

        private void AddMacFound(PortModel port)
        {
            if (string.IsNullOrEmpty(this._device_mac)) return;
            foreach (string macAddr in port.MacList)
            {
                if (IsDevicePortFound(macAddr)) this.NbMacAddressesFound++;
            }
        }

        private bool IsDevicePortFound(string macAddr)
        {
            if (this.IsMacAddress && macAddr.StartsWith(this._device_mac)) return true;
            else return Utils.GetVendorName(macAddr).ToLower().Contains(this._device_mac);
        }

        private string GetDeviceNameOrVendor(PortModel port)
        {
            foreach(EndPointDeviceModel device in port.EndPointDevicesList)
            {
                string mac = device.Name.ToLower();
                if (string.IsNullOrEmpty(mac) || !mac.Contains(this._device_mac)) mac = device.Vendor.ToLower();
                if (string.IsNullOrEmpty(mac) || !mac.Contains(this._device_mac)) mac = SearchMacList(new List<string>(device.MacAddress.Split(',')));
                if (string.IsNullOrEmpty(mac) || !mac.Contains(this._device_mac)) continue;
                return mac;
            }
            return string.Empty;
        }

        private string SearchMacList(List<string> macList)
        {
            foreach (string mac in macList)
            {
                string vendor = Utils.GetVendorName(mac).ToLower();
                if (!string.IsNullOrEmpty(vendor) && !vendor.Contains(":") && vendor.Contains(this._device_mac)) return vendor;
                else if (mac.StartsWith(this._device_mac)) return mac;
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

    }
}

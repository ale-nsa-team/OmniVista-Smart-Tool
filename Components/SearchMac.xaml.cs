using PoEWizard.Data;
using PoEWizard.Device;
using System;
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

        public ObservableCollection<PortModel> PortsFound { get; set; }
        public PortModel SelectedPort { get; set; }
        public bool IsMacAddress {  get; set; }

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
            SelectedPort = null;
            SearchMacAddress(device, macAddress);
            if (PortsFound.Count == 1) SelectedPort = PortsFound[0];
        }

        private void SearchMacAddress(SwitchModel device, string macAddr)
        {
            this._device_mac = !string.IsNullOrEmpty(macAddr) ? macAddr.ToLower().Trim() : string.Empty;
            this.IsMacAddress = Utils.IsValidMacSequence(this._device_mac);
            this.PortsFound = new ObservableCollection<PortModel>();
            foreach (var chas in device.ChassisList)
            {
                foreach (var slot in chas.Slots)
                {
                    foreach (var port in slot.Ports)
                    {
                        if (port.EndPointDevicesList?.Count > 0)
                        {
                            if (FoundDevice(port))
                            {
                                this.PortsFound.Add(port);
                                if (port.MacList?.Count == 0) port.MacList.Add(port.EndPointDevicesList[0].MacAddress);
                                continue;
                            }
                        }
                        if (port.MacList?.Count == 0) continue;
                        foreach (string mac in port.MacList)
                        {
                            if (FoundDevice(port, mac))
                            {
                                this.PortsFound.Add(port);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private bool FoundDevice(PortModel port, string macAddr = null)
        {
            string mac = string.Empty;
            if (string.IsNullOrEmpty(macAddr))
            {
                if (port.EndPointDevicesList?.Count > 0)
                {
                    if (!this.IsMacAddress)
                    {
                        mac = port.EndPointDevicesList[0].Name.ToLower();
                        if (!string.IsNullOrEmpty(mac) && !mac.Contains(this._device_mac)) mac = port.EndPointDevicesList[0].Vendor.ToLower();
                        if (string.IsNullOrEmpty(mac))
                        {
                            mac = Utils.GetVendorName(port.EndPointDevicesList[0].MacAddress).ToLower();
                            if (mac.Contains(":"))
                            {
                                string[] split = mac.Split(':');
                                if (split.Length > 4) mac = string.Empty;
                            }
                        }
                    }
                    else mac = port.EndPointDevicesList[0].MacAddress;
                }
            }
            else
            {
                mac = this.IsMacAddress ? macAddr : Utils.GetVendorName(macAddr);
            }
            if (this.IsMacAddress) return mac.StartsWith(this._device_mac) && !this.PortsFound.Contains(port);
            return mac.Contains(this._device_mac) && !this.PortsFound.Contains(port);
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

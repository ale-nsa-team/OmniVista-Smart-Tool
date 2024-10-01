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

        public ObservableCollection<PortModel> PortsFound { get; set; }
        public PortModel SelectedPort { get; set; }

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
            PortsFound = SearchMacAddress(device, macAddress);
            if (PortsFound.Count == 1) SelectedPort = PortsFound[0];
        }

        private ObservableCollection<PortModel> SearchMacAddress(SwitchModel device, string macAddress)
        {
            ObservableCollection<PortModel> portsFound = new ObservableCollection<PortModel>();
            foreach (var chas in device.ChassisList)
            {
                foreach (var slot in chas.Slots)
                {
                    foreach (var port in slot.Ports)
                    {
                        if (port.EndPointDevicesList?.Count > 0)
                        {
                            if (port.EndPointDevicesList[0].Type == SWITCH) continue;
                            if (!string.IsNullOrEmpty(port.EndPointDevicesList[0].MacAddress))
                            {
                                if (port.EndPointDevicesList[0].MacAddress.Contains(macAddress) && !portsFound.Contains(port))
                                {
                                    portsFound.Add(port);
                                    if (port.MacList?.Count == 0) port.MacList.Add(port.EndPointDevicesList[0].MacAddress);
                                    continue;
                                }
                            }
                        }
                        if (port.MacList?.Count == 0 || port.EndPointDevicesList[0].Type == SWITCH) continue;
                        foreach (string mac in port.MacList)
                        {
                            if (mac.Contains(macAddress) && !portsFound.Contains(port))
                            {
                                portsFound.Add(port);
                                break;
                            }
                        }
                    }
                }
            }
            return portsFound;
        }

        private void PortSelection_Changed(Object sender, RoutedEventArgs e)
        {
            if (_portsListView.SelectedItem is PortModel port)
            {
                SelectedPort = port;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

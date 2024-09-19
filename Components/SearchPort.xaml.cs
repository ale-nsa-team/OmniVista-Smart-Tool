using PoEWizard.Device;
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
            PortsFound = SearchMacAddress(device, macAddress);
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
                        if (port.MacList.Count < 1) continue;
                        foreach (string mac in port.MacList)
                        {
                            if (mac.Contains(macAddress))
                            {
                                portsFound.Add(port);
                            }
                        }
                    }
                }
            }
            return portsFound;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

using System.Collections.Generic;
using System.Windows;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for DeviceSelection.xaml
    /// </summary>
    public partial class DeviceSelection : Window
    {
        public List<string> DeviceTypes { get;  set; } = new List<string>() { "Access Point", "Camera", "Telephone", "Other" };
        public string Device { get; set; }

        public DeviceSelection(string port)
        {
            InitializeComponent();
            DataContext = this;
            _header.Text = "Please, selecte the type of device conntect to port " + port;
        }
    }
}

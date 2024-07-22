using PoEWizard.Device;
using System.Collections.Generic;
using System.Windows;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for PowerSupply.xaml
    /// </summary>
    public partial class PowerSupply : Window
    {
        public List<PowerSupplyModel> PSList { get; set; }
        
        public PowerSupply(SwitchModel device)
        {
            InitializeComponent();
            PSList = new List<PowerSupplyModel>();
            foreach (var chas in device.ChassisList)
            {
                PSList.AddRange(chas.PowerSupplies);
            }
            DataContext = this;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

using PoEWizard.Device;
using System.Collections.Generic;
using System.Windows;
using static PoEWizard.Data.Constants;

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

            PSList = new List<PowerSupplyModel>();
            foreach (var chas in device.ChassisList)
            {
                PSList.AddRange(chas.PowerSupplies);
            }
            DataContext = this;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.MouseDown += delegate { this.DragMove(); };
            this.Height = this._psView.ActualHeight + 105;
            this.Top = this.Owner.Height > this.Height ? this.Owner.Top + (this.Owner.Height - this.Height) / 2 : this.Top;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

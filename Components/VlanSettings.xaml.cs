using PoEWizard.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for VlanSettings.xaml
    /// </summary>
    public partial class VlanSettings : Window
    {
        public List<VlanModel> VlanList { get; set; }
        public bool Proceed {  get; set; }
        public VlanSettings(List<Dictionary<string, string>> dictList)
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
            this.VlanList = new List<VlanModel>();
            foreach (Dictionary<string, string> dict in dictList)
            {
                this.VlanList.Add(new VlanModel(dict));
            }
            DataContext = this;
        }

        public void SetTitle()
        {
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.MouseDown += delegate { this.DragMove(); };
            this.Height = this._vlanView.ActualHeight + 105;
            this.Top = this.Owner.Height > this.Height ? this.Owner.Top + (this.Owner.Height - this.Height) / 2 : this.Top;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Proceed = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Proceed = false;
            this.Close();
        }
    }
}

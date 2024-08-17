using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
    /// Interaction logic for TrafficAnalysis.xaml
    /// </summary>
    public partial class TrafficAnalysis : Window
    {
        const string MINUTE = "minute";
        const string MINUTES = MINUTE + "s";
        public List<string> TimeDurationList { get; set; }
        public string Duration {  get; set; }
        public int TrafficDurationSec { get; set; }
        public int SampleInterval { get; set; }
        public int NbSamples { get; set; }
        public TrafficAnalysis()
        {
            SampleInterval = 30;
            InitializeComponent();
            if (MainWindow.theme == ThemeType.Dark)
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[0]);
            }
            else
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[1]);
            }
            _header.Text = "Select the traffic analysis duration";
            TimeDurationList = new List<string>() { $"1 {MINUTE}", $"2 {MINUTE}s", $"3 {MINUTE}s", $"5 {MINUTE}s", $"15 {MINUTE}s"};
        }

        public void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;
            Duration = $"1 {MINUTE}";
        }

        public void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            TrafficDurationSec = Utils.StringToInt(Duration) * 60;
            DialogResult = true;
            Close();
        }

        public void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

    }
}

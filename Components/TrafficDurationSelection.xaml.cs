using PoEWizard.Data;
using System.Collections.Generic;
using System.Windows;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for TrafficAnalysis.xaml
    /// </summary>
    public partial class TrafficAnalysis : Window
    {
        private const string DEFAULT_DURATION = "3 minutes";

        public List<string> TimeDurationList { get; set; }
        public string Duration {  get; set; }
        public int TrafficDurationSec { get; set; }
        public TrafficAnalysis()
        {
            InitializeComponent();
            if (MainWindow.theme == ThemeType.Dark)
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[0]);
            }
            else
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[1]);
            }
            _header.Text = "Please, select the traffic analysis duration";
            TimeDurationList = new List<string>() { $"1 {MINUTE}", $"2 {MINUTE}s", $"3 {MINUTE}s", $"5 {MINUTE}s",
                                                    $"10 {MINUTE}s", $"15 {MINUTE}s", $"30 {MINUTE}s", $"1 {HOUR}" };
        }

        public void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;
            Duration = DEFAULT_DURATION;
        }

        public void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (Duration.Contains(MINUTE)) TrafficDurationSec = Utils.StringToInt(Duration) * 60;
            else if (Duration.Contains(HOUR)) TrafficDurationSec = Utils.StringToInt(Duration) * 3600;
            else TrafficDurationSec = Utils.StringToInt(Duration);
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

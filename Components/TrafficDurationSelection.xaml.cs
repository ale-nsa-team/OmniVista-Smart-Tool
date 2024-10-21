using System.Collections.Generic;
using System.Windows;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.Utils;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for TrafficAnalysis.xaml
    /// </summary>
    public partial class TrafficAnalysis : Window
    {

        private readonly string minute = "minute";
        private readonly string hour = "hour";

        public List<string> TimeDurationList { get; set; }
        public string Duration {  get; set; }
        public int TrafficDurationSec { get; set; }
        public TrafficAnalysis()
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
            minute = Translate("i18n_tamin");
            hour = Translate("i18n_tahour");
            TimeDurationList = new List<string>() { $"1 {minute}", $"2 {minute}s", $"3 {minute}s", $"5 {minute}s",
                                                    $"10 {minute}s", $"15 {minute}s", $"30 {minute}s", $"1 {hour}" };
        }

        public void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;
            Duration = $"3 {Translate("i18n_tamin")}s";
        }

        public void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (Duration.Contains(minute)) TrafficDurationSec = StringToInt(Duration) * 60;
            else if (Duration.Contains(hour)) TrafficDurationSec = StringToInt(Duration) * 3600;
            else TrafficDurationSec = StringToInt(Duration);
            DialogResult = true;
            Close();
        }

        public void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public static string Translate(string key)
        {
            return (string)MainWindow.Strings[key] ?? key;
        }

    }
}

using PoEWizard.Device;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for CfgWizPage2.xaml
    /// </summary>
    public partial class CfgWizPage2 : Page
    {
        private readonly ServerModel serverData;

        public CfgWizPage2(ServerModel srvData)
        {
            string tz = srvData.Timezone; //saving before calling InitializeComponent because it's resetting it
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

            serverData = srvData;
            serverData.Timezone = tz;
            DataContext = serverData;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            _defaultGwy.Focus();
        }
    }
}


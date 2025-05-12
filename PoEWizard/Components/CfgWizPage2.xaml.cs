using PoEWizard.Device;
using System.Windows;
using System.Windows.Controls;
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

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && tb.IsKeyboardFocusWithin)
            {
                ConfigWiz.Instance.HasChanges = true;
            }
        }

        private void OnCbUnchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.IsKeyboardFocusWithin)
            {
                ConfigWiz.Instance.HasChanges = true;
            }
        }

        private void OnTZChanged(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox cb && cb.IsKeyboardFocusWithin)
            {
                ConfigWiz.Instance.HasChanges = true;
            }
        }
    }
}


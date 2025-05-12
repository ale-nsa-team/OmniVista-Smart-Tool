using PoEWizard.Device;
using System.Windows;
using System.Windows.Controls;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for CfgWizPage3.xaml
    /// </summary>
    public partial class CfgWizPage3 : Page
    {
        private readonly FeatureModel features;

        public CfgWizPage3(FeatureModel features)
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

            this.features = features;
            DataContext = features;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            _vlans.ItemsSource = features.Vlans;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb && tb.IsKeyboardFocusWithin)
            {
                ConfigWiz.Instance.HasChanges = true;
            }
        }

        private void OnCbCheckChanged(object sender, RoutedEventArgs e)
        {
            if ((sender is CheckBox cb && cb.IsKeyboardFocusWithin) || 
                (sender is DataGridCell dc && dc.IsKeyboardFocusWithin))
            {
                ConfigWiz.Instance.HasChanges = true;
            }
        }
    }
}

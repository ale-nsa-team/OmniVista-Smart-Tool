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
            if (MainWindow.theme == ThemeType.Dark)
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[0]);
            }
            else
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[1]);
            }

            this.features = features;
            DataContext = features;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            _vlans.ItemsSource = features.Vlans;
        }
    }
}

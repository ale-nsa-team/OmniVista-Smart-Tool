using PoEWizard.Device;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

        private void Option_Changed(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb.IsKeyboardFocusWithin)
            {
                BindingExpression b = BindingOperations.GetBindingExpression(cb, CheckBox.IsCheckedProperty);
                //features.Save(b.ResolvedSourcePropertyName, cb.IsChecked == true);
            }
        }

        private void SrvAddr_Changed(object sender, RoutedEventArgs e)
        {
            BindingExpression b = BindingOperations.GetBindingExpression(_srvAddr, TextBox.TextProperty);
            if (!b?.HasValidationError ?? false)
            {
                //features.Save(b.ResolvedSourcePropertyName, _srvAddr.Text);
            }
        }
    }
}

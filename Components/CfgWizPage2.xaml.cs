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
            InitializeComponent();
            if (MainWindow.theme == ThemeType.Dark)
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[0]);
            }
            else
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[1]);
            }

            serverData = srvData;
            DataContext = serverData;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            _defaultGwy.Focus();
        }

        private void Text_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                BindingExpression b = BindingOperations.GetBindingExpression(tb, TextBox.TextProperty);
                if (!b?.HasValidationError ?? false)
                {
                    //serverData.Save(b.ResolvedSourcePropertyName, tb.Text);
                }
            }
        }

        private void Option_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                if (!cb.IsKeyboardFocusWithin) return;
                BindingExpression b = BindingOperations.GetBindingExpression(cb, CheckBox.IsCheckedProperty);
                //serverData.Save(b.ResolvedSourcePropertyName, cb.IsChecked.ToString());
            }
        }
    }
}


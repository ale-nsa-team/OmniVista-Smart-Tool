using PoEWizard.Device;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for CfgWizPage4.xaml
    /// </summary>
    public partial class CfgWizPage4 : Page
    {

        private readonly SnmpModel data;
        private readonly ImageSource eye_open;
        private readonly ImageSource eye_closed;

        public CfgWizPage4(SnmpModel snmpData)
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

            eye_open = (ImageSource)Resources.MergedDictionaries[0]["eye_open"];
            eye_closed = (ImageSource)Resources.MergedDictionaries[0]["eye_closed"];

            data = snmpData;
            DataContext = data;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            //_snmpUser.Focus();
        }

        private void PasswordEnter(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                e.Handled = true;
                tb.Focus();
                tb.SelectAll();
            }
        }

        private void MaskedPwdEnter(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
            {
                e.Handled = true;
                pb.Focus();
                pb.SelectAll();
            }
        }

        private void ShowPassword(object sender, RoutedEventArgs e)
        {
            //Button btn = sender as Button;
            //if (btn?.Content is Image img)
            //{
            //    bool isEnabled = img.Source == eye_closed;
            //    img.Source = isEnabled ? eye_open : eye_closed;
            //    _clearSnmpPwd.Visibility = isEnabled ? Visibility.Hidden : Visibility.Visible;
            //    _maskedSnmpPwd.Visibility = isEnabled ? Visibility.Visible : Visibility.Hidden;
            //}
        }

        private void ShowPrivKey(object sender, RoutedEventArgs e)
        {
            //Button btn = sender as Button;
            //if (btn?.Content is Image img)
            //{
            //    bool isEnabled = img.Source == eye_closed;
            //    img.Source = isEnabled ? eye_open : eye_closed;
            //    _clearPrivKey.Visibility = isEnabled ? Visibility.Hidden : Visibility.Visible;
            //    _maskedPrivKey.Visibility = isEnabled ? Visibility.Visible : Visibility.Hidden;
            //}
        }

        private void ShowAuthKey(object sender, RoutedEventArgs e)
        {
            //Button btn = sender as Button;
            //if (btn?.Content is Image img)
            //{
            //    bool isEnabled = img.Source == eye_closed;
            //    img.Source = isEnabled ? eye_open : eye_closed;
            //    _clearAuthKey.Visibility = isEnabled ? Visibility.Hidden : Visibility.Visible;
            //    _maskedAuthKey.Visibility = isEnabled ? Visibility.Visible : Visibility.Hidden;
            //}
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            if (cb.IsKeyboardFocusWithin)
            {
                BindingExpression be = BindingOperations.GetBindingExpression(cb, ComboBox.SelectedValueProperty);
                //data.Save(be.ResolvedSourcePropertyName, (string)cb.SelectedValue);
            }
        }

        private void TextChanged(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                BindingExpression b = BindingOperations.GetBindingExpression(tb, TextBox.TextProperty);
                if (!b?.HasValidationError ?? false)
                {
                    //data.Save(b.ResolvedSourcePropertyName, tb.Text);
                }
            }
        }

        private void MaskedPasswordChanged(object sender, RoutedEventArgs e)
        {

            //if (sender is PasswordBox pb)
            //{
            //    BindingExpression b = BindingOperations.GetBindingExpression(pb, PasswordBoxAssistant.BoundPassword);
            //    if (!b?.HasValidationError ?? false)
            //    {
            //        //data.Save(b.ResolvedSourcePropertyName, pb.Password);
            //    }
            //    else
            //    {
            //        switch (pb.Name)
            //        {
            //            case "_maskedSnmpPwd":
            //                _clearSnmpPwd.Text = _maskedSnmpPwd.Password;
            //                break;
            //            case "_maskedPrivKey":
            //                _clearPrivKey.Text = _maskedPrivKey.Password;
            //                break;
            //            case "_maskedAuthKey":
            //                _clearAuthKey.Text = _maskedAuthKey.Password;
            //                break;
            //        }
            //    }
            //}

        }

        private void ClearPasswordChanged(object sender, RoutedEventArgs e)
        {
            //if (sender is TextBox tb)
            //{
            //    BindingExpression b = BindingOperations.GetBindingExpression(tb, TextBox.TextProperty);
            //    if (!b?.HasValidationError ?? false)
            //    {
            //        //data.Save(b.ResolvedSourcePropertyName, tb.Text);
            //    }
            //    else
            //    {
            //        switch (tb.Name)
            //        {
            //            case "_clearSnmpPwd":
            //                _maskedSnmpPwd.Password = _clearSnmpPwd.Text;
            //                break;
            //            case "_clearPrivKey":
            //                _maskedPrivKey.Password = _clearPrivKey.Text;
            //                break;
            //            case "_clearAuthKey":
            //                _maskedAuthKey.Password = _clearAuthKey.Text;
            //                break;
            //        }
            //    }
            //}
        }
    }
}

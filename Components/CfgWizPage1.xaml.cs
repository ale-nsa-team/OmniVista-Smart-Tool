using PoEWizard.Device;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for CfgPage1.xaml
    /// </summary>
    public partial class CfgWizPage1 : Page
    {
        private readonly SystemModel sysData;
        private readonly ImageSource eye_open;
        private readonly ImageSource eye_closed;

        public CfgWizPage1(SystemModel systemData)
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

            eye_open = (ImageSource)Resources.MergedDictionaries[0]["eye_open"];
            eye_closed = (ImageSource)Resources.MergedDictionaries[0]["eye_closed"];

            sysData = systemData;
            DataContext = sysData;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            _mgtIpAddress.Focus();
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
            Button btn = sender as Button;
            if (btn?.Content is Image img)
            {
                bool isEnabled = img.Source == eye_closed;
                img.Source = isEnabled ? eye_open : eye_closed;
                _clearAdminPwd.Visibility = isEnabled ? Visibility.Hidden : Visibility.Visible;
                _maskedAdminPwd.Visibility = isEnabled ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private void MaskedPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
            {
                _clearAdminPwd.Text = _maskedAdminPwd.Password;
            }

        }

        private void ClearPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                _maskedAdminPwd.Password = tb.Text;
            }
        }
    }
}

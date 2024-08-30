using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for NewUser.xaml
    /// </summary>
    public partial class NewUser : Window
    {
        private readonly ImageSource eye_open;
        private readonly ImageSource eye_closed;

        public string Username { get; set; }
        public string Password { get; set; }

        public NewUser()
        {
            this.DataContext = this;
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

        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (HasErrors()) return;
            this.DialogResult = true;
            this.Close();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _username.Focus();
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !HasErrors()) BtnOk_Click(sender, e);
        }

        private void MaskedPwdEnter(object sender, RoutedEventArgs e)
        {
            PasswordBox pb = sender as PasswordBox;
            e.Handled = true;
            pb.Focus();
            pb.SelectAll();
        }

        private void ClearPwdEnter(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            e.Handled = true;
            tb.Focus();
            tb.SelectAll();
        }

        private void ShowPwd(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn?.Content is Image img)
            {
                bool isVisible = img.Source == eye_closed;
                img.Source = isVisible ? eye_open : eye_closed;
                _clearPwd.Visibility = isVisible ? Visibility.Hidden : Visibility.Visible;
                _maskedPwd.Visibility = isVisible ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private void MaskedPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
            {
                _clearPwd.Text = pb.Password;
            }

        }

        private void ClearPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                _maskedPwd.Password = tb.Text;
            }
        }

        private bool HasErrors()
        {
            bool nameErr = string.IsNullOrEmpty(_username.Text.Trim());
            bool pwdErr = string.IsNullOrEmpty(_clearPwd.Text.Trim());

            return nameErr || pwdErr;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}

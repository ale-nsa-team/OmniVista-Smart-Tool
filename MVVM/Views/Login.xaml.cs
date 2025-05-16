using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        private readonly ImageSource eye_open;
        private readonly ImageSource eye_closed;
        private readonly LoginViewModel _viewModel;

        public Login(String user)
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

            // Initialize the view model
            _viewModel = new LoginViewModel(user);
            this.DataContext = _viewModel;
            _ipAddress.ItemsSource = _viewModel.IpList;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _ipAddress.Focus();
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !_viewModel.HasValidationErrors())
            {
                ExecuteOkCommand();
            }
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
            _viewModel.TogglePasswordVisibilityCommand.Execute(null);

            // Update UI based on password visibility state
            if (_viewModel.IsPwdVisible)
            {
                _viewPwd.Source = eye_closed;
                _clearPwd.Visibility = Visibility.Visible;
                _maskedPwd.Visibility = Visibility.Hidden;
                _clearPwd.Text = _viewModel.Password;
            }
            else
            {
                _viewPwd.Source = eye_open;
                _clearPwd.Visibility = Visibility.Hidden;
                _maskedPwd.Visibility = Visibility.Visible;
                _maskedPwd.Password = _viewModel.Password;
            }
        }

        private void ClearIpList(object sender, RoutedEventArgs e)
        {
            _viewModel.ClearListCommand.Execute(null);
            _ipAddress.ItemsSource = _viewModel.IpList;
        }

        private void DelSelectedIp(object sender, RoutedEventArgs e)
        {
            _viewModel.DeleteSelectedCommand.Execute(null);
            _ipAddress.ItemsSource = _viewModel.IpList;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            ExecuteOkCommand();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CancelCommand.Execute(null);
            this.DialogResult = _viewModel.DialogResult;
        }

        private void ExecuteOkCommand()
        {
            _viewModel.OkCommand.Execute(null);
            this.DialogResult = _viewModel.DialogResult;
        }
    }
}
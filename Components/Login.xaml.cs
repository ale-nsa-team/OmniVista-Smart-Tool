using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        private string _pwd;
        public string IpAddress { get; set; }
        public string User { get; set; }
        public string Password
        {
            get => _pwd;
            set
            {
                _pwd = value;
                password.Password = value;
            } 
        }

        public Login(String user)
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
            this.DataContext = this;
            User = user;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (HasErrors()) return;
            IpAddress = ipAddress.Text;
            User = username.Text;
            Password = password.Password;
            this.DialogResult = true;
            this.Close();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            ipAddress.Focus();
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !HasErrors()) BtnOk_Click(sender, e);
        }

        private bool HasErrors()
        {
            bool ipErr = string.IsNullOrEmpty(ipAddress.Text) || ipAddress.GetBindingExpression(TextBox.TextProperty).HasError;
            bool nameErr = string.IsNullOrEmpty(username.Text.Trim());
            bool pwdErr = string.IsNullOrEmpty(password.Password.Trim());

            return ipErr || nameErr || pwdErr;
        } 

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}

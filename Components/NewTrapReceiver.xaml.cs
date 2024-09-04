using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for NewTrapReceiver.xaml
    /// </summary>
    public partial class NewTrapReceiver : Window
    {
        public string IpAddress { get; set; }
        public string Version { get; set; } = "v2";
        public List<string> Users { get; set; }
        public List<string> Communities { get; set; }
        public string SelectedUser { get; set; }
        public string SelectedCommunity { get; set; }

        public NewTrapReceiver()
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
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _ipAddress.Focus();
            _users.ItemsSource = Users;
            _communities.ItemsSource = Communities;
            _version.Text = "v2";
        }

        private void OnVersionChanged(object sender, RoutedEventArgs e)
        {
            bool isV2 = Version.Contains("v2");
            _commLabel.Visibility = isV2 ? Visibility.Visible : Visibility.Collapsed;
            _communities.Visibility = isV2 ? Visibility.Visible: Visibility.Collapsed;
            _usrLabel.Visibility = isV2 ? Visibility.Collapsed: Visibility.Visible;
            _users.Visibility = isV2 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (HasErrors()) return;
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private bool HasErrors()
        {
            BindingExpression b = BindingOperations.GetBindingExpression(_ipAddress, TextBox.TextProperty);
            return string.IsNullOrEmpty(IpAddress) || b?.HasValidationError == true;
        }
    }
}

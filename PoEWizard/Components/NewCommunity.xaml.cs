using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for NewCommunity.xaml
    /// </summary>
    public partial class NewCommunity : Window
    {
        public string CommunityName { get; set; }
        public List<string> Users { get; set; }
        public string SelectedUser { get; set; }

        public NewCommunity()
        {
            this.DataContext = this;
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
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _commname.Focus();
            _users.ItemsSource = Users;
        }

        private void TextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnOk_Click(sender, e);
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (HasErrors()) return;
            CommunityName = CommunityName.Replace(" ", "_").Trim();
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
            return string.IsNullOrEmpty(CommunityName) || string.IsNullOrEmpty(SelectedUser);
        }
    }
}

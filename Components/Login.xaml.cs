using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        public string User { get; set; }
        public string Password { get; set; }

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
            username.Text = user;
            User = user;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            User = username.Text;
            Password = passwword.Password;
            this.DialogResult = true;
            this.Close();
        }

        private void Password_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnOk_Click(sender, e);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}

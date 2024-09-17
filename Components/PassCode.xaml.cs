using System.Windows;
using System.Windows.Input;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for PassCode.xaml
    /// </summary>
    public partial class PassCode : Window
    {
        public string Password { get; set; }
        
        public PassCode(Window owner)
        {
            InitializeComponent();
            DataContext = this;
            this.Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _pwd.Focus();
        }

        private void Pwd_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnOk_Click(sender, e);
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}

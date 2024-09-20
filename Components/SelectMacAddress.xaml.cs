using System.Windows;
using System.Windows.Input;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for SelectMacAddress.xaml
    /// </summary>
    public partial class SelectMacAddress : Window
    {
        public string SearchMacAddress { get; set; }
        public SelectMacAddress(Window owner)
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
            DataContext = this;
            this.Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _macAddr.Focus();
            SearchMacAddress = string.Empty;
        }

        private void Select_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnOk_Click(sender, e);
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            SearchMacAddress = null;
            this.Close();
        }
    }
}

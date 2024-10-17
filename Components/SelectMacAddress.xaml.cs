using System.Windows;
using System.Windows.Controls;
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
            if (MainWindow.Theme == ThemeType.Dark)
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

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.MouseDown += delegate { this.DragMove(); };
        }

        private void SelectMac_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !HasErrors()) BtnOk_Click(sender, e);
        }

        private bool HasErrors()
        {
            return _macAddr.GetBindingExpression(TextBox.TextProperty).HasError;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (HasErrors()) return;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            SearchMacAddress = null;
            this.Close();
        }
    }
}

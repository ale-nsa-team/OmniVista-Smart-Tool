using System.Windows;
using System.Windows.Input;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for SelectMacAddress.xaml
    /// </summary>
    public partial class SearchParams : Window
    {
        public string SearchParam { get; set; }
        public SearchParams(Window owner)
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

            DataContext = this;
            this.Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _srcText.Focus();
            SearchParam = string.Empty;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.MouseDown += delegate { this.DragMove(); };
            _btnOk.IsEnabled = SearchParam.Length > 1;
        }

        private void SelectDev_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnOk_Click(sender, e);
            else if (e.Key == Key.Escape) BtnCancel_Click(sender, e);
            else _btnOk.IsEnabled = _srcText.Text.Length > 1;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (_srcText.Text.Length < 2) return;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            SearchParam = null;
            this.Close();
        }
    }
}

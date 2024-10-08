using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for AboutBox.xaml
    /// </summary>
    public partial class AboutBox : Window
    {
        public AboutBox()
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

            int year = DateTime.Now.Year;
            Assembly assembly = Assembly.GetExecutingAssembly();
            string version = assembly.GetName().Version.ToString();
            string ver = (string)Resources.MergedDictionaries[1]["i18n_ver"];
            string cr = (string)Resources.MergedDictionaries[1]["i18n_cpright"];
            string rsrv = (string)Resources.MergedDictionaries[1]["i18n_reserved"];
            _version.Text = $"{ver} {string.Join(".", version.Split('.').ToList().Take(2))}";
            _copyRight.Text = $"{cr} © {year} ALE USA Inc. {rsrv}.";

        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            Assembly assembly = Assembly.GetExecutingAssembly();
            string version = assembly.GetName().Version.ToString();
            _aboutTitle.Text = assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
            _version.Text = "Version " + string.Join(".", version.Split('.').ToList().Take(2));
            _company.Text = assembly.GetCustomAttribute<AssemblyCompanyAttribute>().Company;
            _copyWright.Text = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright; ;
            _aboutDescr.Text = "This application allows you to troubleshoot PoE issues on an Alcatel-Lucent Enterprise OmniSwitch®, equipped with AOS 8 version.\n" +
                "The application communicates with the switch via REST API, to gether information on the power supplies and PoE ports," +
                " and allows the user to perform some configuration changes to mitigate common PoE issues.\n" +
                "In case the wizard is unable to fix the problem, it allows the user to collect relevant information to be sent to TAC.";
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

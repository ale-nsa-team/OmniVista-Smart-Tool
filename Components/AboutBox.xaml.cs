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
        private string description = "This application allows you to apply basic cocnfiguration and troubleshoot PoE issues on an " + 
            "Alcatel-Lucent Enterprise OmniSwitch®, equipped with AOS 8 version.\nThe application communicates with the switch via " + 
            "REST API, to gather information on the power supplies and PoE ports, and allows the user to perform some configuration " + 
            "changes to mitigate common PoE issues. In case the wizard is unable to fix the problem, it allows the user to collect " + 
            "relevant information to be sent to TAC.\r\n";

        private string disclamer = "\r\nPermission to use, copy, modify, and distribute this source code, product, " +
            "and its documentation without a fee and without a signed license agreement is hereby granted, provided that the copyright notice, this " +
            "paragraph, and the following two paragraphs appear in all copies, modifications, and distributions." +
            "\r\n \r\nIN NO EVENT SHALL ALE USA INC. BE LIABLE TO ANY PARTY FOR DIRECT, INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES, INCLUDING " +
            "LOST PROFITS, ARISING OUT OF THE USE OF THIS SOURCE CODE, PRODUCT, AND ITS DOCUMENTATION, EVEN IF ALE USA INC. HAS BEEN ADVISED OF THE " +
            "POSSIBILITY OF SUCH DAMAGE.\r\n \r\nALE USA INC. SPECIFICALLY DISCLAIMS ANY WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTES " +
            "OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. THE SOURCE CODE, PRODUCT, AND ACCOMPANYING DOCUMENTATION, IF ANY, " +
            "PROVIDED HEREUNDER IS PROVIDED “AS IS.” ALE USA INC. HAS NO OBLIGATION TO PROVIDE MAINTENANCE, SUPPORT, UPDATES, ENHANCEMENTS, OR MODIFICATIONS.\r\n";
        public AboutBox()
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

            int year = DateTime.Now.Year;
            Assembly assembly = Assembly.GetExecutingAssembly();
            string version = assembly.GetName().Version.ToString();
            _aboutTitle.Text = "Alcatel-Lucent Enterprise Installer's Toolkit";
            _version.Text = "Version " + string.Join(".", version.Split('.').ToList().Take(2));
            _copyRight.Text = $"Copyright © {year} ALE USA Inc. All Rights Reserved.";
            _aboutDescr.Text = description + disclamer;

        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

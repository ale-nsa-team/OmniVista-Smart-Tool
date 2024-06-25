using PoEWizard.Data;
using PoEWizard.Device;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace PoEWizard
{
    /// <summary>
    /// Interaction logic for Mainwindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        #region Private Variables
        private readonly ResourceDictionary darkDict;
        private readonly ResourceDictionary lightDict;
        private ResourceDictionary currentDict;
        private readonly string appVersion;
        private readonly string appPath;
        private readonly string templatePath;
        private string selectedFunction;
        private string selectedConfig;
        private DeviceModel device;
        private readonly IProgress<ProgressReport> progress;
        private bool checkPort = true;
        #endregion
        #region public variables
        public static Window Instance;
        public static ThemeType theme;
        public static string dataPath;

        #endregion
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}

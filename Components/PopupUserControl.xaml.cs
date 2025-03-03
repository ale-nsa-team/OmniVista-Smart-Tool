using Microsoft.Win32;
using PoEWizard.Data;
using PoEWizard.Device;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using static PoEWizard.Data.Utils;
using static PoEWizard.Data.Constants;
using System.Threading;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for PopupUserControl.xaml
    /// </summary>
    public partial class PopupUserControl : UserControl
    {
        private CancellationTokenSource _cts;

        public IProgress<ProgressReport> Progress { get; set; }
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
        public string KeyHeader { get; set; }
        public string ValueHeader { get; set; }
        public UIElement Target { 
            get => _popup.PlacementTarget;
            set => _popup.PlacementTarget = value;
        }
        public PlacementMode Placement { 
            get => _popup.Placement;  
            set => _popup.Placement = value; 
        }
        public double OffsetX { 
            get => _popup.HorizontalOffset; 
            set => _popup.HorizontalOffset = value; 
        }
        public double OffsetY { 
            get => _popup.VerticalOffset; 
            set => _popup.VerticalOffset = value; 
        }

        public PopupUserControl()
        {
            InitializeComponent();
            this.DataContext = this;
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


        public void Show()
        {
            _dictGrid.ItemsSource = Data;
            _popup.IsOpen = true;
            _cts = new CancellationTokenSource();
            Task.Delay(1000, _cts.Token).ContinueWith(t =>
            {
                try
                {
                    if (!_cts.Token.IsCancellationRequested)
                        _popup.Dispatcher.Invoke(() => _popup.IsOpen = false);
                }
                catch (OperationCanceledException)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            });
        }

        private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _cts.Cancel();
        }

        private async void Value_Click(object sender, RoutedEventArgs e)
        {
            if (sender is TextBlock tb)
            {
                string ipAddr = tb.Text;
                if (string.IsNullOrEmpty(ipAddr)) return;
                Progress?.Report(new ProgressReport(ReportType.Value, Translate("i18n_cnxDev", ipAddr), null));
                Progress?.Report(new ProgressReport(ReportType.Status, null, Translate("i18n_devCnx")));
                int port = await Task.Run(() => IpScan.GetOpenPort(ipAddr));
                ConnectToPort(ipAddr, port);
            }
        }

        private void ConnectToPort(string ipAddr, int port)
        {
            try
            {
                switch (port)
                {
                    case 22:
                    case 23:
                        string putty = MainWindow.Config.Get("putty");
                        if (string.IsNullOrEmpty(putty))
                        {
                            var ofd = new OpenFileDialog()
                            {
                                Filter = $"{Translate("i18n_puttyFile")}|*.exe",
                                Title = Translate("i18n_puttyLoc"),
                                InitialDirectory = Environment.SpecialFolder.ProgramFiles.ToString()
                            };
                            if (ofd.ShowDialog() == false) return;
                            putty = ofd.FileName;
                            MainWindow.Config.Set("putty", putty);
                        }
                        string cnx = port == 22 ? "ssh" : "telnet";
                        Process.Start(putty, $"-{cnx} {ipAddr}");
                        break;
                    case 80:
                        Process.Start("explorer.exe", $"http://{ipAddr}");
                        break;
                    case 443:
                        Process.Start("explorer.exe", $"https://{ipAddr}");
                        break;
                    case 3389:
                        Process.Start("mstsc", $"/v: {ipAddr}");
                        break;
                    default:
                        Progress?.Report(new ProgressReport(ReportType.Warning, null, Translate("i18n_noPtOpen", ipAddr)));
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                if (!string.IsNullOrEmpty(ipAddr))
                {
                    if (port != 0) 
                        Progress?.Report(new ProgressReport(ReportType.Error, null, Translate("i18n_cnxFail", ipAddr, port.ToString())));
                    else
                        Progress?.Report(new ProgressReport(ReportType.Warning, null, Translate("i18n_noPtOpen", ipAddr)));
                }
            }
            finally
            {
                Progress?.Report(new ProgressReport(ReportType.Status, null, null));
                Progress?.Report(new ProgressReport(ReportType.Value, null, "-1"));
            }
        }

        private void HidePopup(object sender, RoutedEventArgs e)
        {
            _popup.IsOpen = false;
        }
    }
}

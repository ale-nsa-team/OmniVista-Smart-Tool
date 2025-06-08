using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using PoEWizard.Data;
using System.Windows.Media;
using static PoEWizard.Data.Utils;
using PoEWizard.Device;
using System.Collections.ObjectModel;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for TrafficReportViewer.xaml
    /// </summary>
    public partial class TrafficReportViewer : Window
    {
        private readonly string _title;
        private readonly string _content;
        public string Filename { get; set; }
        public string SaveFilename { get; set; }
        public string CsvData { get; set; }
        public TrafficReport TrafficReportData { get; set; }
        private ObservableCollection<PortTrafficViewModel> _portTrafficData;
        private ObservableCollection<LldpPortViewModel> _portLldpData;
        private ObservableCollection<TransceiverViewModel> _transceiverData;

        public class PortTrafficViewModel
        {
            public string Port { get; set; }
            public string Alias { get; set; }
            public string Bandwidth { get; set; }
            public string RxRate { get; set; }
            public string TxRate { get; set; }
            public string RxBroadcastFrames { get; set; }
            public string RxUnicastFrames { get; set; }
            public string RxBroadcastUnicastPercent { get; set; }
            public string RxMulticastFrames { get; set; }
            public string RxUnicastMulticastRate { get; set; }
            public string RxLostFrames { get; set; }
            public string RxCrcError { get; set; }
            public string RxAlignmentsError { get; set; }
            public string TxBroadcastFrames { get; set; }
            public string TxUnicastFrames { get; set; }
            public string TxBroadcastUnicastPercent { get; set; }
            public string TxMulticastFrames { get; set; }
            public string TxUnicastMulticastRate { get; set; }
            public string TxLostFrames { get; set; }
            public string TxCollidedFrames { get; set; }
            public string TxCollisions { get; set; }
            public string TxLateCollisions { get; set; }
            public string TxExcessiveCollisions { get; set; }
            public string DeviceType { get; set; }
            public string Vendor { get; set; }
            public List<string> MacAddresses { get; set; }
            public string MacAddressesString { get; set; }
        }

        public class LldpPortViewModel
        {
            public string Port { get; set; }
            public string Alias { get; set; }
            public string Type { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string IpAddress { get; set; }
            public string Vendor { get; set; }
            public string Model { get; set; }
            public string SoftwareVersion { get; set; }
            public string SerialNumber { get; set; }
            public string MacAddress { get; set; }
            public string Bandwidth { get; set; }
        }

        public class TransceiverViewModel
        {
            public string ChassisSlotNumber { get; set; }
            public string TransceiverNumber { get; set; }
            public string AluModelName { get; set; }
            public string AluModelNumber { get; set; }
            public string HardwareRevision { get; set; }
            public string SerialNumber { get; set; }
            public string ManufactureDate { get; set; }
            public string LaserWaveLength { get; set; }
            public string AdminStatus { get; set; }
            public string OperationalStatus { get; set; }
        }

        public TrafficReportViewer(string content = "")
        {
            InitializeComponent();

            if (MainWindow.Theme == Constants.ThemeType.Dark)
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[0]);
            }
            else
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[1]);
            }
            Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[1]);
            Resources.MergedDictionaries.Add(MainWindow.Strings);

            _title = Translate("i18n_taIdle");
            _content = content;
            Title = Translate("i18n_taIdle");
            _portTrafficData = new ObservableCollection<PortTrafficViewModel>();
            _portLldpData = new ObservableCollection<LldpPortViewModel>();
            _transceiverData = new ObservableCollection<TransceiverViewModel>();
        }

        private void OnWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetTitleColor(this);

            double h = Owner.ActualHeight;
            double w = Owner.ActualWidth;
            Width = w - 20;
            Height = h - 100;
            Top = Owner.Top + 50;
            Left = Owner.Left + 10;

            if (Filename != null && File.Exists(Filename))
            {
                _textContent.Document.Blocks.Add(new Paragraph(new Run(Translate("i18n_fload"))));
                Task.Run(() =>
                {
                    Thread.Sleep(200);
                    Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        FileStream fs = new FileStream(Filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        TextRange range = new TextRange(_textContent.Document.ContentStart, _textContent.Document.ContentEnd);
                        range.Load(fs, DataFormats.Text);
                        fs.Close();
                    }));
                });
            }
            else
            {
                _textContent.Document.Blocks.Add(new Paragraph(new Run(_content)));
            }

            PopulatePortTrafficData();
        }

        private void PopulatePortTrafficData()
        {
            if (TrafficReportData != null && TrafficReportData.SwitchTraffic != null)
            {
                _portTrafficData.Clear();
                _portLldpData.Clear();
                _transceiverData.Clear();
                var switchModel = TrafficReportData.SwitchTraffic;

                foreach (var portPair in switchModel.Ports)
                {
                    string portName = portPair.Key;
                    PortTrafficModel portTraffic = portPair.Value;

                    PortModel portModel = null;
                    foreach (var chassis in switchModel.ChassisList)
                    {
                        foreach (var slot in chassis.Slots)
                        {
                            foreach (var port in slot.Ports)
                            {
                                if (port.Name == portName)
                                {
                                    portModel = port;
                                    break;
                                }
                            }
                            if (portModel != null) break;
                        }
                        if (portModel != null) break;
                    }

                    string rxRate = "0";
                    if (portTraffic.RxBytes != null && portTraffic.RxBytes.Count >= 2)
                    {
                        double rxRateValue = Math.Round((portTraffic.RxBytes[portTraffic.RxBytes.Count - 1] - portTraffic.RxBytes[0]) * 8 /
                            TrafficReportData.TrafficDuration / 1024, 2);
                        rxRate = rxRateValue.ToString();
                    }

                    string txRate = "0";
                    if (portTraffic.TxBytes != null && portTraffic.TxBytes.Count >= 2)
                    {
                        double txRateValue = Math.Round((portTraffic.TxBytes[portTraffic.TxBytes.Count - 1] - portTraffic.TxBytes[0]) * 8 /
                            TrafficReportData.TrafficDuration / 1024, 2);
                        txRate = txRateValue.ToString();
                    }

                    double GetDiffValue(List<double> samples)
                    {
                        if (samples != null && samples.Count >= 2)
                            return samples[samples.Count - 1] - samples[0];
                        return 0;
                    }

                    double rxBroadcastFrames = GetDiffValue(portTraffic.RxBroadcastFrames);
                    double rxUnicastFrames = GetDiffValue(portTraffic.RxUnicastFrames);
                    double rxMulticastFrames = GetDiffValue(portTraffic.RxMulticastFrames);
                    double rxLostFrames = GetDiffValue(portTraffic.RxLostFrames);
                    double rxCrcError = GetDiffValue(portTraffic.RxCrcErrorFrames);
                    double rxAlignmentsError = GetDiffValue(portTraffic.RxAlignmentsError);

                    double txBroadcastFrames = GetDiffValue(portTraffic.TxBroadcastFrames);
                    double txUnicastFrames = GetDiffValue(portTraffic.TxUnicastFrames);
                    double txMulticastFrames = GetDiffValue(portTraffic.TxMulticastFrames);
                    double txLostFrames = GetDiffValue(portTraffic.TxLostFrames);
                    double txCollidedFrames = GetDiffValue(portTraffic.TxCollidedFrames);
                    double txCollisions = GetDiffValue(portTraffic.TxCollisions);
                    double txLateCollisions = GetDiffValue(portTraffic.TxLateCollisions);
                    double txExcCollisions = GetDiffValue(portTraffic.TxExcCollisions);

                    string rxBroadcastUnicastPercent = "-";
                    if (rxUnicastFrames > 0)
                    {
                        double percent = Math.Round((rxBroadcastFrames / rxUnicastFrames) * 100, 2);
                        rxBroadcastUnicastPercent = percent.ToString();
                    }

                    string txBroadcastUnicastPercent = "-";
                    if (txUnicastFrames > 0)
                    {
                        double percent = Math.Round((txBroadcastFrames / txUnicastFrames) * 100, 2);
                        txBroadcastUnicastPercent = percent.ToString();
                    }

                    double rxUnicastMulticastRate = Math.Round((rxUnicastFrames + rxMulticastFrames) * 8 / TrafficReportData.TrafficDuration, 2);
                    double txUnicastMulticastRate = Math.Round((txUnicastFrames + txMulticastFrames) * 8 / TrafficReportData.TrafficDuration, 2);

                    string deviceType = "-";
                    string vendor = "-";
                    List<string> macAddresses = new List<string>();

                    if (portModel != null)
                    {
                        if (portModel.EndPointDevice != null)
                        {
                            deviceType = !string.IsNullOrEmpty(portModel.EndPointDevice.Type) ?
                                portModel.EndPointDevice.Type : "-";
                            vendor = !string.IsNullOrEmpty(portModel.EndPointDevice.Vendor) ?
                                portModel.EndPointDevice.Vendor : "-";
                        }

                        if (portModel.MacList != null && portModel.MacList.Count > 0)
                        {
                            macAddresses = portModel.MacList;
                        }
                    }

                    _portTrafficData.Add(new PortTrafficViewModel
                    {
                        Port = portName,
                        Alias = portModel?.Alias ?? "-",
                        Bandwidth = GetNetworkSpeed(portTraffic.BandWidth.ToString()),
                        RxRate = rxRate,
                        TxRate = txRate,
                        RxBroadcastFrames = rxBroadcastFrames.ToString(),
                        RxUnicastFrames = rxUnicastFrames.ToString(),
                        RxBroadcastUnicastPercent = rxBroadcastUnicastPercent,
                        RxMulticastFrames = rxMulticastFrames.ToString(),
                        RxUnicastMulticastRate = rxUnicastMulticastRate.ToString(),
                        RxLostFrames = rxLostFrames.ToString(),
                        RxCrcError = rxCrcError.ToString(),
                        RxAlignmentsError = rxAlignmentsError.ToString(),
                        TxBroadcastFrames = txBroadcastFrames.ToString(),
                        TxUnicastFrames = txUnicastFrames.ToString(),
                        TxBroadcastUnicastPercent = txBroadcastUnicastPercent,
                        TxMulticastFrames = txMulticastFrames.ToString(),
                        TxUnicastMulticastRate = txUnicastMulticastRate.ToString(),
                        TxLostFrames = txLostFrames.ToString(),
                        TxCollidedFrames = txCollidedFrames.ToString(),
                        TxCollisions = txCollisions.ToString(),
                        TxLateCollisions = txLateCollisions.ToString(),
                        TxExcessiveCollisions = txExcCollisions.ToString(),
                        DeviceType = deviceType,
                        Vendor = vendor,
                        MacAddresses = macAddresses,
                        MacAddressesString = macAddresses.Count > 0 ? $"{macAddresses[0]} ..." : "-"
                    });
                }

                PopulateLldpAndTransceiverData(switchModel);

                _portsTrafficGrid.ItemsSource = _portTrafficData;
                _lldpPortsGrid.ItemsSource = _portLldpData;
                _transceiversGrid.ItemsSource = _transceiverData;
            }
        }

        private void PopulateLldpAndTransceiverData(SwitchTrafficModel switchModel)
        {
            if (switchModel?.ChassisList == null) return;

            Dictionary<string, PortModel> portsDictionary = new Dictionary<string, PortModel>();
            Dictionary<string, TransceiverModel> transceiverDictionary = new Dictionary<string, TransceiverModel>();

            foreach (var chassis in switchModel.ChassisList)
            {
                foreach (var slot in chassis.Slots)
                {
                    foreach (var port in slot.Ports)
                    {
                        if (!string.IsNullOrEmpty(port.Name))
                        {
                            portsDictionary[port.Name] = port;
                        }
                    }
                    foreach (var transceiver in slot.Transceivers)
                    {
                        string key = $"{transceiver.ChassisNumber}/{transceiver.SlotNumber}/{transceiver.TransceiverNumber}";
                        transceiverDictionary[key] = transceiver;
                    }
                }
            }

            foreach (var portPair in portsDictionary)
            {
                PortModel port = portPair.Value;
                if (port == null || port.EndPointDevice == null || string.IsNullOrEmpty(port.EndPointDevice.Type))
                    continue;

                EndPointDeviceModel device = port.EndPointDevice;
                if (IsDeviceTypeUnknown(device)) continue;
                string formattedMacAddress = FormatMacAddress(device.MacAddress);

                _portLldpData.Add(new LldpPortViewModel
                {
                    Port = port.Name,
                    Alias = GetDeviceInfo(port.Alias),
                    Type = GetDeviceInfo(device.Type),
                    Name = GetDeviceInfo(device.Name),
                    Description = GetDeviceInfo(device.Description),
                    IpAddress = GetDeviceInfo(device.IpAddress),
                    Vendor = GetDeviceInfo(device.Vendor),
                    Model = GetDeviceInfo(device.Model),
                    SoftwareVersion = GetDeviceInfo(device.SoftwareVersion),
                    SerialNumber = GetDeviceInfo(device.SerialNumber),
                    MacAddress = formattedMacAddress,
                    Bandwidth = port.Bandwidth
                });
            }

            foreach (var transceiverPair in transceiverDictionary)
            {
                TransceiverModel transceiver = transceiverPair.Value;
                _transceiverData.Add(new TransceiverViewModel
                {
                    ChassisSlotNumber = $"{transceiver.ChassisNumber}/{transceiver.SlotNumber}",
                    TransceiverNumber = transceiver.TransceiverNumber.ToString(),
                    AluModelName = transceiver.AluModelName,
                    AluModelNumber = transceiver.AluModelNumber,
                    HardwareRevision = transceiver.HardwareRevision,
                    SerialNumber = transceiver.SerialNumber,
                    ManufactureDate = transceiver.ManufactureDate,
                    LaserWaveLength = transceiver.LaserWaveLength,
                    AdminStatus = transceiver.AdminStatus,
                    OperationalStatus = transceiver.OperationalStatus
                });
            }
        }

        private string FormatMacAddress(string macAddress)
        {
            if (string.IsNullOrEmpty(macAddress))
                return "-";

            if (macAddress.Contains(","))
            {
                string[] macs = macAddress.Split(',');
                if (macs.Length > 0)
                    return macs[0] + (macs.Length > 1 ? " ..." : "");
            }

            return macAddress;
        }

        private string GetDeviceInfo(string info)
        {
            return !string.IsNullOrEmpty(info) ? info : "-";
        }

        private bool IsDeviceTypeUnknown(EndPointDeviceModel device)
        {
            return device == null ||
                   string.IsNullOrEmpty(device.Type) ||
                   device.Type == Constants.NO_LLDP ||
                   device.Type == Constants.MED_UNKNOWN ||
                   device.Type == Constants.MED_UNSPECIFIED;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string file = SaveFilename ?? (Filename != null ? Path.GetFileName(Filename) : "");
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = $"{Translate("i18n_ftxt")}|*.txt",
                Title = $"{Translate("i18n_svBtn")} {_title}",
                InitialDirectory = Filename != null ? Path.GetDirectoryName(Filename) : Environment.SpecialFolder.MyDocuments.ToString(),
                FileName = file
            };
            if (sfd.ShowDialog() == true)
            {
                string txt = new TextRange(_textContent.Document.ContentStart, _textContent.Document.ContentEnd).Text;
                File.WriteAllText(sfd.FileName, txt);
                if (!string.IsNullOrEmpty(this.CsvData))
                {
                    string csvFileName = Path.Combine(Path.GetDirectoryName(sfd.FileName), $"{Path.GetFileNameWithoutExtension(sfd.FileName)}.csv");
                    File.WriteAllText(csvFileName, this.CsvData);
                }
            }
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            if (Filename == Logger.LogPath) Logger.Clear();
            else if (Filename == Activity.FilePath) Activity.Clear();
            _textContent.Document.Blocks.Clear();
            Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(t => Dispatcher.Invoke(() => Close()));
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
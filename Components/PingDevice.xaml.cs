using PoEWizard.Comm;
using PoEWizard.Data;
using PoEWizard.Device;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using static PoEWizard.Data.Utils;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for PingDevice.xaml
    /// </summary>
    public partial class PingDevice : Window, INotifyPropertyChanged
    {
        private ObservableCollection<string> _ipAddresses;
        private string _selectedIpAddress;
        private string _pingResult;
        private bool _isLoading;
        private bool _canPing = true;
        private const int DefaultTimeout = 2000; // 2 seconds timeout
        private const int DefaultTtl = 128;
        private const int DefaultPingCount = 4;

        private AosSshService _sshService;
        private SwitchModel _switchModel;

        public ObservableCollection<string> IpAddresses
        {
            get => _ipAddresses;
            set
            {
                _ipAddresses = value;
                OnPropertyChanged();
            }
        }

        public string SelectedIpAddress
        {
            get => _selectedIpAddress;
            set
            {
                if (_selectedIpAddress != value)
                {
                    _selectedIpAddress = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanPing));
                }
            }
        }

        public string PingResult
        {
            get => _pingResult;
            set
            {
                _pingResult = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                CanPing = !value;
            }
        }

        public bool CanPing
        {
            get => _canPing && !string.IsNullOrWhiteSpace(SelectedIpAddress) && IsValidIpAddress(SelectedIpAddress);
            set
            {
                _canPing = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public PingDevice(List<string> ipAddresses = null)
        {
            InitializeComponent();

            IpAddresses = ipAddresses != null
                ? new ObservableCollection<string>(ipAddresses)
                : new ObservableCollection<string>();

            DataContext = this;

            // If IP address list is empty, show a prompt
            if (IpAddresses.Count == 0)
            {
                PingResult = Translate("i18n_enterIpAddress");
            }
            else
            {
                PingResult = Translate("i18n_selectIpAddress");
            }
        }

        public PingDevice(PortModel portModel)
        {
            InitializeComponent();

            IpAddresses = new ObservableCollection<string>();

            if (portModel?.IpAddrList != null && portModel?.IpAddrList.Count > 0)
            {
                foreach (var ipPair in portModel.IpAddrList)
                {
                    string ipAddress = ipPair.Value;
                    if (!string.IsNullOrEmpty(ipAddress) && !IpAddresses.Contains(ipAddress))
                    {
                        IpAddresses.Add(ipAddress);
                    }
                }
            }

            if (portModel?.EndPointDeviceList != null && portModel?.EndPointDeviceList.Count > 0)
            {
                foreach (var endPoint in portModel.EndPointDeviceList)
                {
                    if (!string.IsNullOrEmpty(endPoint.IpAddress) && !IpAddresses.Contains(endPoint.IpAddress))
                    {
                        IpAddresses.Add(endPoint.IpAddress);
                    }
                }
            }

            _title.Text = $"{Translate("i18n_pingDevice")} - {Translate("i18n_port")} {portModel?.Name}";

            _switchModel = MainWindow.restApiService?.SwitchModel;

            DataContext = this;

            // If IP address list is empty, show a prompt
            if (IpAddresses.Count == 0)
            {
                PingResult = Translate("i18n_enterIpAddress");
            }
            else
            {
                PingResult = Translate("i18n_selectIpAddress");
                if (IpAddresses.Count > 0)
                {
                    SelectedIpAddress = IpAddresses[0];
                }
            }
        }

        public void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (MainWindow.Theme == Data.Constants.ThemeType.Dark)
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[0]);
            }
            else
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[1]);
            }
            Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[1]);
            Resources.MergedDictionaries.Add(MainWindow.Strings);

            IpAddressComboBox.Focus();

            IpAddressComboBox.AddHandler(TextBoxBase.TextChangedEvent,
                new TextChangedEventHandler(IpAddressComboBox_TextChanged));
        }

        private void IpAddressComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Force update of CanPing property when text changes
            OnPropertyChanged(nameof(CanPing));
        }

        private bool IsValidIpAddress(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;

            System.Windows.Data.BindingExpression b = System.Windows.Data.BindingOperations.GetBindingExpression(IpAddressComboBox, ComboBox.TextProperty);
            return IPAddress.TryParse(ipAddress, out _) && (b == null || !b.HasValidationError);
        }

        private async void PingButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsValidIpAddress(SelectedIpAddress))
            {
                PingResult = Translate("i18n_validIpRequired");
                return;
            }

            try
            {
                IsLoading = true;
                PingResult = Translate("i18n_pingInProgress");

                await Task.Run(() => ExecutePing());
            }
            catch (Exception ex)
            {
                PingResult = $"{Translate("i18n_error")}: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecutePing()
        {
            StringBuilder resultBuilder = new StringBuilder();
            resultBuilder.AppendLine($"{Translate("i18n_pinging")} {SelectedIpAddress} {Translate("i18n_withBytes")}:");

            int successCount = 0;
            long totalTime = 0;
            long minTime = long.MaxValue;
            long maxTime = long.MinValue;

            try
            {
                if (_switchModel != null)
                {
                    try
                    {
                        _sshService = new AosSshService(_switchModel);
                        _sshService.ConnectSshClient();

                        LinuxCommand pingCmd = new LinuxCommand($"ping {SelectedIpAddress} count {DefaultPingCount}");
                        Dictionary<string, string> response = _sshService.SendLinuxCommand(pingCmd);

                        if (response != null && response.ContainsKey("output"))
                        {
                            string output = response["output"];
                            Dictionary<string, string> parsedOutput = ParsePingOutput(output);

                            string cleanOutput = CleanPingOutput(output);
                            resultBuilder.Clear();
                            resultBuilder.Append(cleanOutput);

                            if (parsedOutput.ContainsKey("received_packets") &&
                                int.TryParse(parsedOutput["received_packets"], out successCount))
                            {
                                if (parsedOutput.ContainsKey("min_time") &&
                                    double.TryParse(parsedOutput["min_time"], out double min))
                                {
                                    minTime = (long)min;
                                }

                                if (parsedOutput.ContainsKey("max_time") &&
                                    double.TryParse(parsedOutput["max_time"], out double max))
                                {
                                    maxTime = (long)max;
                                }

                                if (parsedOutput.ContainsKey("avg_time") &&
                                    double.TryParse(parsedOutput["avg_time"], out double avg))
                                {
                                    totalTime = (long)(avg * successCount);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // If SSH ping fails, append error message and fallback to local ping
                        StringBuilder fallbackResultBuilder = new StringBuilder();
                        fallbackResultBuilder.AppendLine($"{Translate("i18n_error")}: {ex.Message}");
                        fallbackResultBuilder.AppendLine();
                        fallbackResultBuilder.AppendLine(Translate("i18n_pingFailedFallback"));
                        fallbackResultBuilder.AppendLine();

                        // Reset state for local ping
                        successCount = 0;
                        totalTime = 0;
                        minTime = long.MaxValue;
                        maxTime = long.MinValue;

                        // Create new builder for local ping results
                        StringBuilder localPingBuilder = new StringBuilder();
                        FallbackToLocalPing(localPingBuilder, ref successCount, ref totalTime, ref minTime, ref maxTime);

                        // Combine both results
                        fallbackResultBuilder.AppendLine(localPingBuilder.ToString());
                        resultBuilder = fallbackResultBuilder;
                    }
                    finally
                    {
                        _sshService?.DisconnectSshClient();
                    }
                }
                else
                {
                    FallbackToLocalPing(resultBuilder, ref successCount, ref totalTime, ref minTime, ref maxTime);
                }

                Dispatcher.Invoke(() =>
                {
                    if (!IpAddresses.Contains(SelectedIpAddress))
                    {
                        IpAddresses.Add(SelectedIpAddress);
                    }

                    PingResult = resultBuilder.ToString();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    PingResult = $"{Translate("i18n_pingFailed")}: {ex.Message}";
                });
            }
        }

        private Dictionary<string, string> ParsePingOutput(string output)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            Match statsMatch = Regex.Match(output, @"(\d+) packets transmitted, (\d+) received, (\d+)% packet loss");
            if (statsMatch.Success && statsMatch.Groups.Count >= 4)
            {
                result["transmitted_packets"] = statsMatch.Groups[1].Value;
                result["received_packets"] = statsMatch.Groups[2].Value;
                result["packet_loss_percent"] = statsMatch.Groups[3].Value;
            }

            Match rttMatch = Regex.Match(output, @"rtt min/avg/max/mdev = ([0-9.]+)/([0-9.]+)/([0-9.]+)/([0-9.]+)");
            if (rttMatch.Success && rttMatch.Groups.Count >= 5)
            {
                result["min_time"] = rttMatch.Groups[1].Value;
                result["avg_time"] = rttMatch.Groups[2].Value;
                result["max_time"] = rttMatch.Groups[3].Value;
                result["mdev_time"] = rttMatch.Groups[4].Value;
            }

            return result;
        }

        private string CleanPingOutput(string output)
        {
            // Remove the command line and any terminal prompts
            StringBuilder cleanOutput = new StringBuilder();
            bool foundPingData = false;

            using (System.IO.StringReader reader = new System.IO.StringReader(output))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Skip the command line
                    if (line.StartsWith("ping "))
                    {
                        continue;
                    }

                    // Skip terminal prompts
                    if (line.Contains("->"))
                    {
                        continue;
                    }

                    // Start collecting from "PING" line
                    if (line.StartsWith("PING "))
                    {
                        foundPingData = true;
                    }

                    if (foundPingData)
                    {
                        cleanOutput.AppendLine(line);
                    }
                }
            }

            return cleanOutput.ToString().TrimEnd();
        }

        private void FallbackToLocalPing(StringBuilder resultBuilder, ref int successCount, ref long totalTime, ref long minTime, ref long maxTime)
        {
            Ping pingSender = new Ping();
            byte[] buffer = new byte[32];
            new Random().NextBytes(buffer);
            PingOptions options = new PingOptions(DefaultTtl, true);

            try
            {
                for (int i = 0; i < DefaultPingCount; i++)
                {
                    PingReply reply = pingSender.Send(SelectedIpAddress, DefaultTimeout, buffer, options);

                    if (reply.Status == IPStatus.Success)
                    {
                        successCount++;
                        totalTime += reply.RoundtripTime;
                        minTime = Math.Min(minTime, reply.RoundtripTime);
                        maxTime = Math.Max(maxTime, reply.RoundtripTime);

                        resultBuilder.AppendLine($"{Translate("i18n_replyFrom")} {reply.Address}: {Translate("i18n_bytes")}={reply.Buffer.Length} {Translate("i18n_time")}={reply.RoundtripTime}ms TTL={reply.Options?.Ttl ?? 0}");
                    }
                    else
                    {
                        resultBuilder.AppendLine($"{Translate("i18n_requestTimeout")}. ({reply.Status})");
                    }

                    if (i < DefaultPingCount - 1) Task.Delay(500).Wait();
                }

                resultBuilder.AppendLine();
                resultBuilder.AppendLine($"{Translate("i18n_pingStats")} {SelectedIpAddress}:");
                resultBuilder.AppendLine($"    {Translate("i18n_packets")}: {Translate("i18n_sent")} = {DefaultPingCount}, {Translate("i18n_received")} = {successCount}, {Translate("i18n_lost")} = {DefaultPingCount - successCount} ({(DefaultPingCount - successCount) * 100 / DefaultPingCount}% {Translate("i18n_loss")})");

                if (successCount > 0)
                {
                    resultBuilder.AppendLine($"{Translate("i18n_roundTripTimes")}:");
                    resultBuilder.AppendLine($"    {Translate("i18n_minimum")} = {minTime}ms, {Translate("i18n_maximum")} = {maxTime}ms, {Translate("i18n_average")} = {totalTime / successCount}ms");
                }
            }
            finally
            {
                pingSender.Dispose();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
            _sshService?.Dispose();
        }
    }
}
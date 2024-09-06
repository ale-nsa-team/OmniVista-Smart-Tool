using PoEWizard.Data;
using PoEWizard.Device;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    internal static class Colors
    {
        internal static SolidColorBrush Danger => (SolidColorBrush)new BrushConverter().ConvertFrom("#ff6347");
        internal static SolidColorBrush Clear => MainWindow.theme == ThemeType.Dark ? Brushes.Lime
                        : (SolidColorBrush)new BrushConverter().ConvertFrom("#12b826");
        internal static SolidColorBrush Warn => Brushes.Orange;
        internal static SolidColorBrush Unknown => Brushes.Gray;
        internal static SolidColorBrush Disable => (SolidColorBrush)new BrushConverter().ConvertFrom("#aaa");
        internal static SolidColorBrush Problem => MainWindow.theme == ThemeType.Dark 
            ? (SolidColorBrush)new BrushConverter().ConvertFrom("#C29494") : Brushes.Orchid;
        internal static SolidColorBrush Default => MainWindow.theme == ThemeType.Dark ? Brushes.White : Brushes.Black;
    }

    public class RectangleValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                float val = Utils.ParseFloat(value);
                return val > 45 ? val - 45 : val;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class BoolToVisibilityConvertter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility v = bool.Parse(parameter?.ToString() ?? "true") ? Visibility.Visible : Visibility.Collapsed;
            Visibility notv = v == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            return bool.Parse(value.ToString()) ? v : notv;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class ValueToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            try
            {
                if (Utils.IsInvalid(value)) return Colors.Default;
                string val = value?.ToString() ?? string.Empty;
                string param = parameter?.ToString() ?? string.Empty;
                switch (param)
                {
                    case "ConnectionStatus":
                        return val == "Reachable" ? Colors.Clear : Colors.Danger;
                    case "Temperature":
                        return val == "UnderThreshold" ? Colors.Clear : val == "OverThreshold" ? Colors.Warn : Colors.Danger;
                    case "Power":
                        float percent = Utils.ParseFloat(value);
                        return percent > 10 ? Colors.Clear : (percent > 0 ? Colors.Warn : Colors.Danger);
                    case "Poe":
                        switch (val)
                        {
                            case "On":
                                return Colors.Clear;
                            case "Fault":
                            case "Deny":
                            case "Conflict":
                                return Colors.Danger;
                            case "Searching":
                                return Colors.Problem;
                            case "Off":
                                return Colors.Warn;
                            default:
                                return Colors.Disable;
                        }
                    case "PoeStatus":
                        return val == "UnderThreshold" ? Colors.Clear : val == "NearThreshold" ? Colors.Warn : Colors.Danger;
                    case "PortStatus":
                        return val == "Up" ? Colors.Clear : val == "Down" ? Colors.Danger : Colors.Unknown;
                    case "PowerSupply":
                        return val == "Up" ? Colors.Clear : Colors.Danger;
                    case "RunningDir":
                        return val == CERTIFIED_DIR ? Colors.Danger : Colors.Default;
                    case "Boolean":
                        return val.ToLower() == "true" ? Colors.Clear : Colors.Danger;
                    case "AosVersion":
                        return Utils.IsOldAosVersion(val) ? Colors.Warn : Colors.Default;
                    case "SyncStatus":
                        return val == SyncStatusType.Synchronized.ToString() ? Colors.Clear :
                               val == SyncStatusType.NotSynchronized.ToString() ? Colors.Danger :
                               val == SyncStatusType.Unknown.ToString() ? Colors.Unknown : Colors.Warn;
                    case "LpCmmDebugLevel":
                    case "LpNiDebugLevel":
                        SwitchDebugLogLevel level = Utils.StringToSwitchDebugLevel(val);
                        return level == SwitchDebugLogLevel.Info ? Colors.Clear :
                               level == SwitchDebugLogLevel.Off ? Colors.Danger :
                               level > SwitchDebugLogLevel.Off && level < SwitchDebugLogLevel.Warn ? Colors.Problem :
                               level >= SwitchDebugLogLevel.Warn && level < SwitchDebugLogLevel.Info ? Colors.Warn :
                               level >= SwitchDebugLogLevel.Debug1 ? Colors.Danger : Colors.Problem;
                    default:
                        return Colors.Unknown;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return Colors.Unknown;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class FpgaToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (Utils.IsInvalid(values)) return Colors.Default;
                string versionType = parameter?.ToString() ?? FPGA;
                string model = values[0].ToString();
                string versions = values[1].ToString();
                if (string.IsNullOrEmpty(versions)) return Colors.Unknown;
                int[] minversion = Utils.GetMinimunVersion(model, versionType);
                if (minversion == null) return Colors.Default;
                string[] s = versions.Split('.');
                int[] fpgas = Array.ConvertAll(s, int.Parse);
                return ((fpgas[0] < minversion[0]) || (fpgas[0] == minversion[0] && fpgas[1] < minversion[1])) ? Colors.Warn : Colors.Default;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return Colors.Default;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string[] splitValues = (value?.ToString() ?? string.Empty).Split(' ');
            return splitValues;
        }
    }

    public class BadVersionToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush val = new FpgaToColorConverter().Convert(values, targetType, parameter, culture) as SolidColorBrush;
            return val == Colors.Warn ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string[] splitValues = (value?.ToString() ?? string.Empty).Split(' ');
            return splitValues;
        }
    }

    public class ConfigTypeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (Utils.IsInvalid(value)) return false;
                string val = value?.ToString() ?? string.Empty;
                string par = parameter?.ToString() ?? string.Empty;
                ConfigType ct = Enum.TryParse(val, true, out ConfigType c) ? c : ConfigType.Unavailable;
                return par == "Available" ? ct != ConfigType.Unavailable : ct == ConfigType.Enable;
            } catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class ConfigTypeToSimbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (Utils.IsInvalid(value)) return string.Empty;
                string val = value?.ToString() ?? string.Empty;
                ConfigType ct = Enum.TryParse(val, true, out ConfigType c) ? c : ConfigType.Unavailable;
                switch (ct)
                {
                    case ConfigType.Enable: return "✓";
                    case ConfigType.Disable: return "✗";
                    default: return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class AosVersionToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (Utils.IsInvalid(value)) return Visibility.Collapsed;
                return Utils.IsOldAosVersion(value) ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return Visibility.Collapsed;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class TempStatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (Utils.IsInvalid(value)) return Visibility.Collapsed;
                return value.ToString() == "OverThreshold" || value.ToString() == "Danger" ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class BoolToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (Utils.IsInvalid(value)) return string.Empty;
                bool val = bool.TryParse(value.ToString(), out bool b) && b;
                return val ? "✓" : "✗";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class BoolToPoEModeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (Utils.IsInvalid(values)) return string.Empty;
                string val = values[0].ToString();
                PoeStatus poeType = string.IsNullOrEmpty(val) || val.Contains("UnsetValue") ? PoeStatus.NoPoe : (PoeStatus)Enum.Parse(typeof(PoeStatus), val);
                bool is4pair = !bool.TryParse(values[1].ToString(), out bool b) || b;
                return poeType != PoeStatus.NoPoe ? (is4pair ? "4-pair" : "2-pair") : string.Empty;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return string.Empty;
            }

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string[] splitValues = (value?.ToString() ?? string.Empty).Split(' ');
            return splitValues;
        }
    }

    public class PoeToPriorityLevelConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (Utils.IsInvalid(values)) return PriorityLevelType.Low;
                string val = values[0].ToString();
                PoeStatus poeType = string.IsNullOrEmpty(val) || val.Contains("UnsetValue") ? PoeStatus.NoPoe : (PoeStatus)Enum.Parse(typeof(PoeStatus), val);
                string priorityLevel = values[1].ToString();
                return poeType != PoeStatus.NoPoe ? Enum.Parse(typeof(PriorityLevelType), priorityLevel) : PriorityLevelType.Low;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return PriorityLevelType.Low;
            }

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string[] splitValues = (value?.ToString() ?? string.Empty).Split(' ');
            return splitValues;
        }
    }

    public class CpuUsageToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (Utils.IsInvalid(values)) return Colors.Default;
                double pct = Utils.GetThresholdPercentage(values);
                return pct > 0.1 ? Colors.Clear : pct < 0 ? Colors.Danger : Colors.Warn;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return Colors.Unknown;
            }

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string[] splitValues = (value?.ToString() ?? string.Empty).Split(' ');
            return splitValues;
        }
    }

    public class CpuUsageToToolTipConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (Utils.IsInvalid(values)) return null;
                int thrshld = int.TryParse(values[1].ToString(), out int i) ? i : 0;
                double pct = Utils.GetThresholdPercentage(values);
                return pct > 0.1 ? string.Empty : pct < 0 ? $"CPU usage over threshold ({thrshld}%)" : $"CPU usage near threshold ({thrshld}%)";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;    
            }

        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string[] splitValues = (value?.ToString() ?? string.Empty).Split(' ');
            return splitValues;
        }
    }

    public class CpuUsageToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (Utils.IsInvalid(values)) return Visibility.Collapsed;
                double pct = Utils.GetThresholdPercentage(values);
                return pct > 0.1 ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return Visibility.Collapsed;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string[] splitValues = (value?.ToString() ?? string.Empty).Split(' ');
            return splitValues;
        }
    }

    public class SnmpVersionToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Utils.IsInvalid(value) || Utils.IsInvalid(parameter)) return Visibility.Collapsed;
            string item = value as string;
            string par = parameter as string;
            return item.Contains(par) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class DeviceToTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            const int MAX_NB_DEVICES = 5;
            try
            {
                if (Utils.IsInvalid(value)) return null;
                if (value is List<EndPointDeviceModel> edmList)
                {
                    if (edmList.Count < 1) return null;
                    bool hasmore = edmList.Count > MAX_NB_DEVICES;
                    List<EndPointDeviceModel> displayList = hasmore ? edmList.GetRange(0, MAX_NB_DEVICES) : edmList;
                    List<string> tooltip = displayList.Select(x => x.ToTooltip()).ToList();
                    int maxlen = tooltip.Max(t => MaxLineLen(t));
                    if (hasmore) tooltip.Add($"{new string(' ', maxlen/2 -6)}({edmList.Count - MAX_NB_DEVICES} more...)");
                    string separator = $"\n{new string('-', maxlen)}\n";
                    return string.Join(separator, tooltip);
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

        private int MaxLineLen(string s)
        {
            return s.Split('\n').Max(l => l.Length);
        }
    }

    public class PoeToTooltipConverter : IValueConverter
    {

        static readonly Dictionary<PoeStatus, string> toolTip = new Dictionary<PoeStatus, string>
        {
            [PoeStatus.On] = "PoE power activation is complete, and the attached device is receiving power.",
            [PoeStatus.Off] = "PoE has been disabled for this port.",
            [PoeStatus.Searching] = "PoE is enabled, and the switch is sending PoE probes looking for a device, but activation or class detection is incomplete.",
            [PoeStatus.Fault] = "PoE activation or class detection has failed.",
            [PoeStatus.Deny] = "PoE power management has denied power to the port due to priority disconnect or over subscription.",
            [PoeStatus.Test] = "Port has been forced on and will remain on until it is forced off by RTP functions.",
            [PoeStatus.Delayed] = "Delayed start is enabled.",
            [PoeStatus.Conflict] = "Two PoE ports have been connected and failed to disable PoE on one port.",
            [PoeStatus.NoPoe] = "Port doesn't support PoE."
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                PoeStatus poe = (PoeStatus)value;
                if (Utils.IsInvalid(value) || !toolTip.ContainsKey(poe)) return DependencyProperty.UnsetValue;
                return toolTip[poe];
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return DependencyProperty.UnsetValue;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

    }

}

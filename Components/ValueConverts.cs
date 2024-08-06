using PoEWizard.Data;
using PoEWizard.Device;
using System;
using System.Collections.Generic;
using System.Globalization;
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
                        return val == "Normal" ? Colors.Clear : val == "NearThreshold" ? Colors.Warn : Colors.Danger;
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

    public class DeviceToTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string header = new string('-', 50);
                if (Utils.IsInvalid(value)) return null;
                if (value is List<EndPointDeviceModel> edmList)
                {
                    string toolTip = "";
                    foreach (var edm in edmList)
                    {
                        if (!string.IsNullOrEmpty(toolTip)) toolTip += "\n";
                        toolTip += $"{header}\n{edm.ToTooltip()}";
                    }
                    toolTip += $"\n{header}";
                    return toolTip;
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
    }
}

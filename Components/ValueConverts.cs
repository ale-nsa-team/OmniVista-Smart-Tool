using PoEWizard.Data;
using PoEWizard.Device;
using System;
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
        internal static SolidColorBrush Red => (SolidColorBrush)new BrushConverter().ConvertFrom("#ff6347");
        internal static SolidColorBrush Green => MainWindow.theme == ThemeType.Dark ? Brushes.Lime
                        : (SolidColorBrush)new BrushConverter().ConvertFrom("#12b826");
        internal static SolidColorBrush Orange => Brushes.Orange;
        internal static SolidColorBrush Gray => Brushes.Gray;
        internal static SolidColorBrush LightGray => (SolidColorBrush)new BrushConverter().ConvertFrom("#aaa");
        internal static SolidColorBrush Default => MainWindow.theme == ThemeType.Dark ? Brushes.White : Brushes.Black;
    }

    public class RectangleValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                float val = GetFloat(value);
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

        internal static float GetFloat(object input)
        {
            if (input == null) return 0;
            string num = new string(input.ToString().Where(c => char.IsDigit(c) || c == '.').ToArray());
            return float.TryParse(num, out float res) ? res : 0;
        }
    }

    public class ValueToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Utils.IsInvalid(value)) return Colors.Default;
            string val = value?.ToString() ?? string.Empty;
            string param = parameter?.ToString() ?? string.Empty;
            switch (param)
            {
                case "ConnectionStatus":
                    return val == "Reachable" ? Colors.Green : Colors.Red;
                case "Temperature":
                    return val == "UnderThreshold" ? Colors.Green : val == "OverThreshold" ? Colors.Orange : Colors.Red;
                case "Power":
                    float percent = RectangleValueConverter.GetFloat(value);
                    return percent > 10 ? Colors.Green : (percent > 0 ? Colors.Orange : Colors.Red);
                case "Poe":
                    switch (val)
                    {
                        case "On":
                            return Colors.Green;
                        case "Fault":
                        case "Deny":
                        case "Conflict":
                            return Colors.Red;
                        case "Off":
                            return Colors.Orange;
                        default:
                            return Colors.LightGray;
                    }
                case "PoeStatus":
                    return val == "Normal" ? Colors.Green : val == "NearThreshold" ? Colors.Orange : Colors.Red;
                case "PortStatus":
                    return val == "Up" ? Colors.Green : val == "Down" ? Colors.Red : Colors.Gray;
                case "PowerSupply":
                    return val == "Up" ? Colors.Green : Colors.Red;
                case "RunningDir":
                    return val == CERTIFIED_DIR ? Colors.Red : Colors.Default;
                case "Boolean":
                    return val.ToLower() == "true" ? Colors.Green : Colors.Red;
                case "AosVersion":
                    return Utils.IsOldAosVersion(val) ? Colors.Orange : Colors.Default;
                default:
                    return Colors.Red;
            }
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
            if (Utils.IsInvalid(values)) return Colors.Default;
            string versionType = parameter?.ToString() ?? FPGA;
            try
            {
                string model = values[0].ToString();
                string versions = values[1].ToString();
                int[] minversion = Utils.GetMinimunVersion(model, versionType);
                if (minversion == null) return Colors.Default;
                string[] s = versions.Split('.');
                int[] fpgas = Array.ConvertAll(s, int.Parse);
                return ((fpgas[0] < minversion[0]) || (fpgas[0] == minversion[0] && fpgas[1] < minversion[1])) ? Colors.Orange : Colors.Default;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return Colors.Default;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class BadVersionToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush val = new FpgaToColorConverter().Convert(values, targetType, parameter, culture) as SolidColorBrush;
            return val == Colors.Orange ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ConfigTypeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Utils.IsInvalid(value)) return false;
            string val = value?.ToString() ?? string.Empty;
            string par = parameter?.ToString() ?? string.Empty;
            ConfigType ct = Enum.TryParse(val, true, out ConfigType c) ? c : ConfigType.Unavailable;
            return par == "Available" ? ct != ConfigType.Unavailable : ct == ConfigType.Enable;
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class AosVersionToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Utils.IsInvalid(value)) return Visibility.Collapsed;
            return Utils.IsOldAosVersion(value) ? Visibility.Visible : Visibility.Collapsed;
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
            if (Utils.IsInvalid(value)) return Visibility.Collapsed;
            return value.ToString() == "OverThreshold" || value.ToString() == "Danger" ? Visibility.Visible : Visibility.Collapsed;
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
            if (Utils.IsInvalid(value)) return string.Empty;
            bool val = bool.TryParse(value.ToString(), out bool b) && b;
            return val ? "✓" : "✗";
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
            if (Utils.IsInvalid(values)) return string.Empty;
            string val = values[0].ToString();
            PoeStatus poeType = string.IsNullOrEmpty(val) || val.Contains("UnsetValue") ? PoeStatus.NoPoe : (PoeStatus)Enum.Parse(typeof(PoeStatus), val);
            bool is4pair = !bool.TryParse(values[1].ToString(), out bool b) || b;
            return poeType != PoeStatus.NoPoe ? (is4pair ? "4-pair" : "2-pair") : string.Empty;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string[] splitValues = (value.ToString()).Split(' ');
            return splitValues;
        }
    }

    public class PoeToPriorityLevelConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (Utils.IsInvalid(values)) return PriorityLevelType.Low;
            string val = values[0].ToString();
            PoeStatus poeType = string.IsNullOrEmpty(val) || val.Contains("UnsetValue") ? PoeStatus.NoPoe : (PoeStatus)Enum.Parse(typeof(PoeStatus), val);
            string priorityLevel = values[1].ToString();
            return poeType != PoeStatus.NoPoe ? Enum.Parse(typeof(PriorityLevelType), priorityLevel) : PriorityLevelType.Low;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string[] splitValues = (value.ToString()).Split(' ');
            return splitValues;
        }
    }

    public class CpuUsageToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (Utils.IsInvalid(values)) return Colors.Default;
            int cpu = int.TryParse(values[0].ToString(), out int i) ? i : 0;
            int thrshld = int.TryParse(values[1].ToString(), out i) ? i : 0;
            if (thrshld == 0) return Colors.Default;
            double pct = 1 - cpu / thrshld;
            return pct > 0.1 ? Colors.Green : pct < 0 ? Colors.Red : Colors.Orange;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string[] splitValues = (value.ToString()).Split(' ');
            return splitValues;
        }
    }

    public class CpuUsageToToolTipConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (Utils.IsInvalid(values)) return string.Empty;
            int cpu = int.TryParse(values[0].ToString(), out int i) ? i : 0;
            int thrshld = int.TryParse(values[1].ToString(), out i) ? i : 0;
            if (thrshld == 0) return string.Empty;
            double pct = 1 - cpu / thrshld;
            return pct > 0.1 ? string.Empty : pct < 0 ? $"CPU usage over threshold {thrshld}%)" : $"CPU usage near threshold {thrshld}%)";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string[] splitValues = (value.ToString()).Split(' ');
            return splitValues;
        }
    }

    public class CpuUsageToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (Utils.IsInvalid(values)) return Visibility.Collapsed;
            int cpu = int.TryParse(values[0].ToString(), out int i) ? i : 0;
            int thrshld = int.TryParse(values[1].ToString(), out i) ? i : 0;
            if (thrshld == 0) return Visibility.Collapsed;
            double pct = 1 - cpu / thrshld;
            return pct > 0.1 ? Visibility.Collapsed : Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string[] splitValues = (value.ToString()).Split(' ');
            return splitValues;
        }
    }

    public class DeviceToTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Utils.IsInvalid(value)) return null;
            if (value is EndPointDeviceModel edm)
            {
                return edm.ToTooltip();
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}

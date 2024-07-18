using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using PoEWizard.Data;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    internal static class Colors
    {
        internal static SolidColorBrush Red = (SolidColorBrush)new BrushConverter().ConvertFrom("#f00736");
        internal static SolidColorBrush Green = MainWindow.theme == Constants.ThemeType.Dark ? Brushes.Lime : Brushes.Green;
        internal static SolidColorBrush Orange = Brushes.Orange;
        internal static SolidColorBrush Gray = Brushes.Gray;
        internal static SolidColorBrush LightGray = (SolidColorBrush)new BrushConverter().ConvertFrom("#aaa");
        internal static SolidColorBrush Def = MainWindow.theme == Constants.ThemeType.Dark ? Brushes.White : Brushes.Black;
    }

    public class RectangleValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            float val = GetFloat(value);
            return val > 45 ? val - 45 : val;
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
            if (value == null) return DependencyProperty.UnsetValue;
            string val = value.ToString();
            string param = parameter.ToString();
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
                    return val == Constants.CERTIFIED_DIR ? Colors.Red : Colors.Def;
                case "Boolean":
                    return val.ToLower() == "true" ? Colors.Green : Colors.Red;
                case "AosVersion":
                    return IsOldAosVersion(val) ? Colors.Orange : Colors.Def;
                default:
                    return Colors.Red;
            }
        }

        public static bool IsOldAosVersion(string aos)
        {
            Match version = Regex.Match(aos, Constants.MATCH_AOS_VERSION);
            if (version.Success)
            {
                int v1 = int.Parse(version.Groups[1].ToString());
                int v2 = int.Parse(version.Groups[2].ToString());
                int r = int.Parse(version.Groups[5].ToString());
                string[] minver = Constants.MIN_AOS_VERSION.Split(' ');
                int minv1 = int.Parse(minver[0].Split('.')[0]);
                int minv2 = int.Parse(minver[0].Split('.')[1]);
                int minr = int.Parse(minver[1].Replace("R", ""));
                return (v1 < minv1) || (v1 == minv1 && v2 < minv2) || (v1 == minv1 && v2 == minv2 && r < minr);
            }
            return false;
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
            if (values == null || values[0] == null) return DependencyProperty.UnsetValue;
            string model = values[0].ToString();
            string fpga = values[1].ToString();
            int[] minfpga = GetMinFpga(model);
            if (minfpga == null) return Colors.Def;
            string[] s = fpga.Split('.');
            int[] fpgas = Array.ConvertAll(s, int.Parse);
            return ((fpgas[0] < minfpga[0]) || (fpgas[0] == minfpga[0] && fpgas[1] < minfpga[1])) ? Colors.Orange : Colors.Def;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private int[] GetMinFpga(string model)
        {
            string m = model;
            while (m.Length > 2) {
                if (Constants.fpga.TryGetValue(m, out string val))
                {
                    string[] vals = val.Split('.');
                    return Array.ConvertAll(vals, int.Parse);
                }
                m = m.Substring(0, m.Length - 1);
            }
            return null;
        }
    }

    public class ConfigTypeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string val = value.ToString();
            string par = parameter.ToString();
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
            string val = value.ToString();
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
            if (value == null) return Visibility.Collapsed;
            return ValueToColorConverter.IsOldAosVersion(value.ToString()) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class FpgaToVisibilityConverter : IMultiValueConverter
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

    public class TempStatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Visibility.Collapsed;
            return value.ToString() == "OvererThreshold" || value.ToString() == "Danger"? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool val = bool.Parse(value.ToString());
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
            string val = values[0].ToString();
            PoeStatus poeType = string.IsNullOrEmpty(val) || val.Contains("UnsetValue") ? PoeStatus.NoPoe : (PoeStatus)Enum.Parse(typeof(Constants.PoeStatus), val);
            bool is4pair = bool.TryParse(values[1].ToString(), out bool b) ? b : true;
            return poeType != PoeStatus.NoPoe ? (is4pair ? "4-pair" : "2-pair") : "";
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
            string val = values[0].ToString();
            PoeStatus poeType = string.IsNullOrEmpty(val) || val.Contains("UnsetValue") ? PoeStatus.NoPoe : (PoeStatus)Enum.Parse(typeof(Constants.PoeStatus), val);
            string priorityLevel = values[1].ToString();
            return poeType != PoeStatus.NoPoe ? Enum.Parse(typeof(Constants.PriorityLevelType), priorityLevel) : null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string[] splitValues = (value.ToString()).Split(' ');
            return splitValues;
        }
    }
}

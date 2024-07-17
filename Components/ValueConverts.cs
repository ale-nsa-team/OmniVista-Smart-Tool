using System;
using System.Collections.Generic;
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
            var red = new BrushConverter().ConvertFrom("#f00736");
            var green = MainWindow.theme == Data.Constants.ThemeType.Dark ? Brushes.Lime : Brushes.Green;
            var def = MainWindow.theme == Data.Constants.ThemeType.Dark ? Brushes.White : Brushes.Black;
            string val = value.ToString();
            string param = parameter.ToString();
            switch (param)
            {
                case "ConnectionStatus":
                    return val == "Reachable" ? green : red;
                case "Temperature":
                    return val == "UnderThreshold" ? green : red;
                case "Power":
                    float percent = RectangleValueConverter.GetFloat(value);
                    return percent > 10 ? green : (percent > 0 ? Brushes.Orange : red);
                case "Poe":
                    switch (val)
                    {
                        case "On":
                            return green;
                        case "Fault":
                        case "Deny":
                        case "Conflict":
                            return red;
                        case "Off":
                            return Brushes.Orange;
                        default:
                            return new BrushConverter().ConvertFrom("#aaa");
                    }
                case "PoeStatus":
                    return val == "Normal" ? green : val == "NearThreshold" ? Brushes.Orange : red;
                case "PortStatus":
                    return val == "Up" ? green : val == "Down" ? red : Brushes.Gray;
                case "CPUStatus":
                    return val == "UnderThreshold" ? green : red;
                case "UplinkPort":
                    var brush = new SolidColorBrush(Color.FromArgb(255, (byte)163, (byte)101, (byte)209));
                    return val == "False" ? Brushes.Transparent : brush;
                case "MacList":
                    return (value as List<string>)?.Count == 0 ? Brushes.White : red;
                case "PowerSupply":
                    return val == "Up" ? green : red;
                case "RunningDir":
                    return val == Data.Constants.CERTIFIED_DIR ? red : def;
                case "Boolean":
                    return val.ToLower() == "true" ? green : red;
                case "AosVersion":
                    return IsOldVersion(val) ? Brushes.Orange : def;
                default:
                    return red;
            }
        }

        public static bool IsOldVersion(string aos)
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

    public class ConfigTypeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string val = value.ToString();
            string par = parameter.ToString();
            ConfigType ct = Enum.TryParse(val, true, out ConfigType c) ? c : ConfigType.Unavailable;
            return par == "Enable" ? ct == ConfigType.Enable : ct != ConfigType.Unavailable;
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
            return ValueToColorConverter.IsOldVersion(value.ToString()) ? Visibility.Visible : Visibility.Collapsed;
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

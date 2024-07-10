using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

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
            string param = parameter.ToString();
            switch (param)
            {
                case "ConnectionStatus":
                    return value.ToString() == "Reachable" ? Brushes.Lime : red;
                case "Temperature":
                    return value.ToString() == "UnderThreshold" ? Brushes.Lime : red;
                case "Power":
                    float percent = RectangleValueConverter.GetFloat(value);
                    return percent > 10 ? Brushes.Lime : (percent > 0 ? Brushes.Orange : red);
                case "Poe":
                    switch (value.ToString())
                    {
                        case "On":
                            return Brushes.Lime;
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
                    return value.ToString() == "Normal" ? Brushes.Lime : value.ToString() == "NearThreshold" ? Brushes.Orange : red;
                case "PortStatus":
                    return value.ToString() == "Up" ? Brushes.Lime : value.ToString() == "Down" ? red : Brushes.Gray;
                case "CPUStatus":
                    return value.ToString() == "UnderThreshold" ? Brushes.Lime : red;
                case "UplinkPort":
                    var brush = new SolidColorBrush(Color.FromArgb(255, (byte)163, (byte)101, (byte)209));
                    return value.ToString() == "False" ? Brushes.Transparent : brush;
                case "MacList":
                    return (value as List<string>)?.Count == 0 ? Brushes.White : red;
                case "PowerSupply":
                    return value.ToString() == "Up" ? Brushes.Lime : red;
                case "Boolean":
                    return value.ToString().ToLower() == "true" ? Brushes.Lime : red;
                default:
                    return red;
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
            bool val = bool.Parse(value.ToString());
            return val ? "  ✓" : "  ✗";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class PoeTypeToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string poeType = (string)value.ToString();
            switch (poeType.ToLower())
            {
                case "on":
                    return "  ✓";

                case "off":
                    return "  ✗";

                default:
                    return "  -";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class PoeToPriorityLevelConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasPoe = (bool)values[0];
            string priorityLevel = values[1].ToString();

            return hasPoe ? $"  {priorityLevel}" : "  -";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            string[] splitValues = ((string)value).Split(' ');
            return splitValues;
        }
    }
}

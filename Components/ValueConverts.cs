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
            string param = parameter.ToString();
            switch (param)
            {
                case "ConnectionStatus":
                    return value.ToString() == "Reachable" ? Brushes.Lime : Brushes.IndianRed;
                case "Temperature":
                    return value.ToString() == "UnderThreshold" ? Brushes.Lime : Brushes.IndianRed;
                case "Power":
                    float percent = RectangleValueConverter.GetFloat(value);
                    return percent > 10 ? Brushes.Lime : (percent > 0 ? Brushes.Orange : Brushes.IndianRed);
                case "PoeStatus":
                    return value.ToString() == "Normal" ? Brushes.Lime : value.ToString() == "NearThreshold" ? Brushes.Orange : Brushes.Red;
                case "CPUStatus":
                    return value.ToString() == "UnderThreshold" ? Brushes.Lime : Brushes.IndianRed;
                case "UplinkPort":
                    var brush = new SolidColorBrush(Color.FromArgb(255, (byte)163, (byte)101, (byte)209));
                    return value.ToString() == "False" ? Brushes.Transparent : brush;
                case "MacList":
                    return (value as List<string>)?.Count == 0 ? Brushes.White : Brushes.IndianRed;
                case "PowerSupply":
                    return value.ToString() == "Up" ? Brushes.Lime : Brushes.IndianRed;
                default:
                    return Brushes.IndianRed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}

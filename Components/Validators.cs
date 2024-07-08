using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace PoEWizard.Components
{
    public class IpAddressRule : ValidationRule
    {
        private const string pattern = @"^((([0-1]?[0-9]{1,2}|2[0-4][0-9]|25[0-5])\.){3}([0-1]?[0-9]{1,2}|2[0-4][0-9]|25[0-5]))$";

        public IpAddressRule() { }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return Regex.IsMatch((string)value, pattern)
                ? ValidationResult.ValidResult
                : new ValidationResult(false, "Invalid IP Address");

        }
    }
}

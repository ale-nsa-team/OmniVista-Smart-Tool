using PoEWizard.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using static PoEWizard.Data.Utils;

namespace PoEWizard.Components
{
    public class IpAddressRule : ValidationRule
    {
        private const string pattern = @"^((([0-1]?[0-9]{1,2}|2[0-4][0-9]|25[0-5])\.){3}([0-1]?[0-9]{1,2}|2[0-4][0-9]|25[0-5]))$";

        public IpAddressRule() { }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (IsInvalid(value)) return new ValidationResult(false, "Invalid IP Address");
            return Regex.IsMatch((string)value, pattern)
                ? ValidationResult.ValidResult
                : new ValidationResult(false, "Invalid IP Address");

        }
    }

    public class IpAddressNullRule : ValidationRule
    {
        public IpAddressNullRule() { }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (string.IsNullOrEmpty((string)value)) return ValidationResult.ValidResult;
            return new IpAddressRule().Validate(value, cultureInfo);
        }
    }

    public class SubnetMaskRule : ValidationRule
    {
        private const string leadingOnes = "(255|254|252|248|240|224|192|128|0+)";
        private const string allOnes = @"(255\.)";
        private readonly string pattern = $@"^(({allOnes}{{3}}{leadingOnes})|({allOnes}{{2}}{leadingOnes}\.0+)|({allOnes}{leadingOnes}(\.0+){{2}})|({leadingOnes}(\.0+){{3}}))$";

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return Regex.IsMatch((string)value, pattern)
                ? ValidationResult.ValidResult
                : new ValidationResult(false, "Invalid subnet mask");
        }
    }

    public class NameRule : ValidationRule
    {
        public string PropertyName { get; set; }
        private readonly string allowedChars = "_-";

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string stringValue = value as string;

            if (stringValue.Contains(" "))
            {
                return new ValidationResult(false, $"{PropertyName} cannot contain spaces");
            }

            if (stringValue.Length > 32)
            {
                return new ValidationResult(false, $"{PropertyName} cannot have more than 32 characters");
            }
            if (stringValue.Any(ch => !char.IsLetterOrDigit(ch) && !allowedChars.Contains(ch)))
            {
                return new ValidationResult(false, $"{PropertyName} cannot have special characters other than '_' or '-'");
            }
            return ValidationResult.ValidResult;
        }
    }

    public class PasswordRule : ValidationRule
    {
        public string PropertyName { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string pwd = value as string;

            if (PropertyName == "AdminPwd" && pwd == Constants.DEFAULT_PASSWORD) return ValidationResult.ValidResult;

            if (pwd.Contains("!")) return new ValidationResult(false, "password cannot contain exclamation point (!)");

            if (pwd.Length < 8) return new ValidationResult(false, "password must be at least 8 characters long");

            if (pwd.Length > 30) return new ValidationResult(false, "password must not be more than 30 characters long");

            if (!pwd.Any(char.IsDigit)) return new ValidationResult(false, "password must contain at least one numeric character");

            if (!pwd.Any(char.IsUpper)) return new ValidationResult(false, "password must contain at least one uppercase letter");

            if (!pwd.Any(ch => !char.IsLetterOrDigit(ch))) return new ValidationResult(false, "passord must contain at least one special character");

            return ValidationResult.ValidResult;
        }
    }

    public class MaxLengthRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return ((string)value).Length < 255
                ? ValidationResult.ValidResult
                : new ValidationResult(false, "Must be less then 255 characters in length");
        }
    }

    public class DomainNameRule : ValidationRule
    {
        private const string pattern = @"(?=^.{1,254}$)(^(?:(?!\d+\.)[a-zA-Z0-9_\-]{1,63}\.?)+(?:[a-zA-Z]{1,})$)";

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (string.IsNullOrEmpty((string)value)) return ValidationResult.ValidResult;
            return Regex.IsMatch((string)value, pattern)
                ? ValidationResult.ValidResult
                : new ValidationResult(false, "Invalid domain name");
        }
    }

    public class HostnameRule : ValidationRule
    {
        private const string ipAddrPattern = @"^((([0-1]?[0-9]{1,2}|2[0-4][0-9]|25[0-5])\.){3}([0-1]?[0-9]{1,2}|2[0-4][0-9]|25[0-5]))$";
        private const string fqdnPattern = @"^(([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])\.)*([A-Za-z0-9]|[A-Za-z0-9][A-Za-z0-9\-]*[A-Za-z0-9])$";

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string hostname = (string)value;
            if (string.IsNullOrEmpty(hostname)) return ValidationResult.ValidResult;
            bool isIp = Regex.IsMatch(hostname, ipAddrPattern);
            bool isFqdn = Regex.IsMatch(hostname, fqdnPattern);
            return (isIp || isFqdn) ? ValidationResult.ValidResult : new ValidationResult(false, "Invalid hostname");
        }
    }
}

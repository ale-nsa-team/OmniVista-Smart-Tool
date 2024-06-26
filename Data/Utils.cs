using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace PoEWizard.Data
{
    public static class Utils
    {
        private const string ENCRYPT_KEY = "a9cd76210f6e0bb4fdbd23a9cda9831a";

        public static string EncryptString(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext)) return plaintext;
            byte[] key = Encoding.UTF8.GetBytes(ENCRYPT_KEY);

            using (Aes aes = Aes.Create())
            {
                ICryptoTransform encryptor = aes.CreateEncryptor(key, aes.IV);
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(plaintext);
                        }
                    }
                    byte[] cyphertextBytes = memoryStream.ToArray();

                    return Convert.ToBase64String(aes.IV.Concat(cyphertextBytes).ToArray());
                }
            }
        }

        public static string DecryptString(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;
            byte[] dataBytes = Convert.FromBase64String(cipherText);
            byte[] iv = dataBytes.Take(16).ToArray();
            byte[] cipherBytes = dataBytes.Skip(16).ToArray();
            byte[] key = Encoding.UTF8.GetBytes(ENCRYPT_KEY);

            using (Aes aes = Aes.Create())
            {
                ICryptoTransform decryptor = aes.CreateDecryptor(key, iv);
                using (MemoryStream memoryStream = new MemoryStream(cipherBytes))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the first ancester of specified type
        /// </summary>
        public static T FindAncestor<T>(DependencyObject current)
        where T : DependencyObject
        {
            current = VisualTreeHelper.GetParent(current);

            while (current != null)
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            };
            return null;
        }

        /// <summary>
        /// Returns a specific ancester of an object
        /// </summary>
        public static T FindAncestor<T>(DependencyObject current, T lookupItem)
        where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T && current == lookupItem)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            };
            return null;
        }

        /// <summary>
        /// Finds an ancestor object by name and type
        /// </summary>
        public static T FindAncestor<T>(DependencyObject current, string parentName)
        where T : DependencyObject
        {
            while (current != null)
            {
                if (!string.IsNullOrEmpty(parentName))
                {
                    var frameworkElement = current as FrameworkElement;
                    if (current is T && frameworkElement != null && frameworkElement.Name == parentName)
                    {
                        return (T)current;
                    }
                }
                else if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            };

            return null;

        }

        /// <summary>
        /// Looks for a child control within a parent by name
        /// </summary>
        public static T FindChild<T>(DependencyObject parent, string childName)
        where T : DependencyObject
        {
            // Confirm parent and childName are valid.
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child.
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                    else
                    {
                        // recursively drill down the tree
                        foundChild = FindChild<T>(child, childName);

                        // If the child is found, break so we do not overwrite the found child.
                        if (foundChild != null) break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        /// <summary>
        /// Looks for a child control within a parent by type
        /// </summary>
        public static T FindChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            // Confirm parent is valid.
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child);

                    // If the child is found, break so we do not overwrite the found child.
                    if (foundChild != null) break;
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }
            return foundChild;
        }

        public static string CalcStringDuration(DateTime? startTime)
        {
            TimeSpan dur = DateTime.Now.Subtract((DateTime)startTime);
            int hour = dur.Hours;
            int sec = dur.Seconds;
            int min = dur.Minutes;
            int milliSec = dur.Milliseconds;
            string duration = "";
            if (hour > 0)
            {
                duration = hour.ToString() + " hour";
                if (duration.Length > 1) duration += "s";
            }
            if (min > 0)
            {
                if (duration.Length > 0) duration += " ";
                duration += min.ToString() + " min";
            }
            if (sec > 0)
            {
                if (duration.Length > 0) duration += " ";
                duration += sec.ToString() + " sec";
            }
            if (milliSec > 0)
            {
                if (duration.Length > 0) duration += " ";
                duration += milliSec.ToString() + " ms";
            }
            if (duration.Length < 1) duration = "< 1 ms";
            return duration;
        }

        public static bool IsTimeExpired(DateTime startTime, double period)
        {
            return GetTimeDuration(startTime) >= period;
        }

        public static double GetTimeDuration(DateTime startTime)
        {
            try
            {
                return DateTime.Now.Subtract(startTime).TotalSeconds;
            }
            catch { }
            return -1;
        }

        public static double GetTimeDurationMs(DateTime startTime)
        {
            try
            {
                return DateTime.Now.Subtract(startTime).TotalMilliseconds;
            }
            catch { }
            return -1;
        }

        public static int StringToInt(string strNumber)
        {
            if (string.IsNullOrEmpty(strNumber)) return -1;
            try
            {
                string number = ExtractNumber(strNumber);
                if (!string.IsNullOrEmpty(number))
                {
                    bool isNumeric = int.TryParse(number.Trim(), out int intVal);
                    if (isNumeric && (intVal >= 0)) return intVal;
                }
            }
            catch { }
            return -1;
        }

        public static double StringToDouble(string strNumber)
        {
            if (strNumber == null) return 0;
            try
            {
                string number = ExtractNumber(strNumber);
                if (!string.IsNullOrEmpty(number))
                {
                    bool isNumeric = double.TryParse(strNumber.Trim(), out double dVal);
                    if (isNumeric && (dVal > 0)) return dVal;
                }
            }
            catch { }
            return 0;
        }

        private static string ExtractNumber(string strNumber)
        {
            try
            {
                return new string(strNumber.Where(char.IsDigit).ToArray());
            }
            catch { }
            return null;
        }

        public static string PrintEnum(Enum enumVar)
        {
            try
            {
                return $"\"{ParseEnumToString(enumVar)}\"";
            }
            catch { }
            return "";
        }

        public static string ParseEnumToString(Enum enumVar)
        {
            try
            {
                return Enum.GetName(enumVar.GetType(), enumVar);
            }
            catch { }
            return "";
        }

    }
}


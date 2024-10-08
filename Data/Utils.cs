using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Xml;
using System.Xml.Linq;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Data
{
    public static class Utils
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const string ENCRYPT_KEY = "a9cd76210f6e0bb4fdbd23a9cda9831a";

        public static void SetTitleColor(Window window)
        {
            IntPtr handle = new WindowInteropHelper(window).Handle;
            int bckgndColor = MainWindow.theme == ThemeType.Dark ? 0x333333 : 0xF0F0F0;
            int textColor = MainWindow.theme == ThemeType.Dark ? 0xFFFFFF : 0x000000;
            DwmSetWindowAttribute(handle, 35, ref bckgndColor, Marshal.SizeOf(bckgndColor));
            DwmSetWindowAttribute(handle, 36, ref textColor, Marshal.SizeOf(textColor));
        }

        public static string CalcStringDuration(DateTime? startTime, bool skipMs = false)
        {
            TimeSpan dur = DateTime.Now.Subtract((DateTime)startTime);
            int hour = dur.Hours;
            int sec = dur.Seconds;
            int min = dur.Minutes;
            int milliSec = dur.Milliseconds;
            string duration = string.Empty;
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
            if (skipMs) return duration;
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

        public static int StringToInt(string strNumber)
        {
            if (string.IsNullOrEmpty(strNumber)) return -1;
            try
            {
                string number = ExtractNumber(strNumber);
                if (!string.IsNullOrEmpty(number))
                {
                    if (number.Contains("."))
                    {
                        string[] split = number.Split('.');
                        if (split.Length == 2) number = split[0]; else number = number.Replace(".", string.Empty);
                    }
                    bool isNumeric = int.TryParse(number.Trim(), out int intVal);
                    if (isNumeric && (intVal >= 0)) return intVal;
                }
            }
            catch { }
            return -1;
        }

        public static int ParseNumber(string chasSlotPort, int index)
        {
            string[] parts = chasSlotPort.Split('/');
            return parts.Length > index ? (int.TryParse(parts[index], out int n) ? n : 0) : 0;
        }

        public static double StringToDouble(string strNumber)
        {
            if (string.IsNullOrEmpty(strNumber)) return 0;
            try
            {
                string number = ExtractNumber(strNumber);
                if (!string.IsNullOrEmpty(number)) return ParseToDouble(number);
            }
            catch { }
            return 0;
        }

        public static double ParseToDouble(string strNumber)
        {
            if (string.IsNullOrEmpty(strNumber)) return 0;
            try
            {
                bool isNumeric = double.TryParse(strNumber.Trim(), out double dVal);
                if (isNumeric && (dVal > 0)) return dVal;
            }
            catch { }
            return 0;
        }

        public static long StringToLong(string strNumber)
        {
            if (string.IsNullOrEmpty(strNumber)) return -1;
            try
            {
                string number = ExtractNumber(strNumber);
                if (!string.IsNullOrEmpty(number))
                {
                    bool isNumeric = long.TryParse(number.Trim(), out long longVal);
                    if (isNumeric && (longVal >= 0)) return longVal;
                }
            }
            catch { }
            return -1;
        }

        public static string ExtractNumber(string strNumber)
        {
            try
            {
                if (strNumber.Contains("."))
                {
                    string[] split = strNumber.Split('.');
                    if (split.Length == 2) return new string(strNumber.Where(c => char.IsDigit(c) || c == '.').ToArray());
                    else strNumber = strNumber.Replace(".", string.Empty);
                }
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
            return string.Empty;
        }

        public static string ParseEnumToString(Enum enumVar)
        {
            try
            {
                return Enum.GetName(enumVar.GetType(), enumVar);
            }
            catch { }
            return string.Empty;
        }

        public static HttpStatusCode ConvertToHttpStatusCode(Dictionary<string, string> errorList)
        {
            try
            {
                return (HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), errorList[RestUrl.HTTP_RESPONSE]);
            }
            catch
            {
                return HttpStatusCode.Unused;
            }
        }

        public static string PrintMethodClass(int skipFrames = 1)
        {
            var method = new StackFrame(skipFrames).GetMethod();
            string fname = method.DeclaringType.FullName;
            if (fname.Contains("<"))
            {
                string[] parts = fname.Split(new char[] { '.', '<', '>' });
                if (parts.Length > 2) return $"{parts[1].Replace("+", string.Empty)}: {parts[2]}";
            }
            return $"Method {method.Name} of {method.DeclaringType.Name} class";
        }

        public static string PrintXMLDoc(string xmlDoc)
        {
            try
            {
                var stringBuilder = new StringBuilder();
                var element = XElement.Parse(xmlDoc);
                var settings = new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true, NewLineOnAttributes = true };
                using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
                {
                    element.Save(xmlWriter);
                }
                return stringBuilder.ToString();
            }
            catch { }
            return xmlDoc;
        }

        public static string ToPascalCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            string[] parts = Regex.Split(text, @"\s|_");
            StringBuilder sb = new StringBuilder();
            foreach (string p in parts)
            {
                sb.Append(FirstChToUpper(p));
            }
            return sb.ToString();
        }

        public static string FirstChToUpper(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return $"{char.ToUpper(text[0])}{text.Substring(1).ToLower()}";
        }

        public static bool IsNumber(string strNumber)
        {
            if (strNumber == null) return false;
            try
            {
                return int.TryParse(strNumber.Trim(), out int intVal);
            }
            catch { }
            return false;
        }

        public static float ParseFloat(object value)
        {
            if (value == null) return 0f;
            string num = new string(value.ToString().Where(c => char.IsDigit(c) || c == '.').ToArray());
            return float.TryParse(num, out float res) ? res : 0f;
        }

        public static string PrintTimeDurationSec(DateTime startTime)
        {
            return $"{RoundUp(GetTimeDuration(startTime))} sec";
        }

        public static double RoundUp(double input, int dec = 0)
        {
            double multiplier = Math.Pow(10, dec);
            double multiplication = input * multiplier;
            double result = Math.Round(multiplication) / multiplier;
            return result;
        }

        public static string ExtractSubString(string inputStr, string startStr, string endStr = null)
        {
            if (string.IsNullOrEmpty(inputStr) || string.IsNullOrEmpty(startStr)) return string.Empty;
            int startPos = inputStr.IndexOf(startStr) + startStr.Length;
            int length = !string.IsNullOrEmpty(endStr) ? inputStr.IndexOf(endStr, inputStr.IndexOf(startStr) + 1) - startPos : inputStr.Length - startPos;
            if (length < 1 || length > inputStr.Length) return string.Empty;
            return inputStr.Substring(startPos, length);
        }

        public static string RemoveSpaces(string inputStr)
        {
            return new Regex(" {2,}").Replace(inputStr, " ");
        }

        public static bool IsValidIP(string ipAddr)
        {
            try
            {
                if (!string.IsNullOrEmpty(ipAddr))
                {
                    return Regex.IsMatch(ipAddr, "^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$");
                }
            }
            catch { }
            return false;
        }

        public static bool IsValidMacAddress(string mac)
        {
            try
            {
                if (!string.IsNullOrEmpty(mac))
                {
                    return Regex.IsMatch(mac, "^[0-9a-fA-F]{2}(((:[0-9a-fA-F]{2}){5})|((:[0-9a-fA-F]{2}){5}))$");
                }
            }
            catch { }
            return false;
        }

        public static bool IsValidMacSequence(string mac)
        {
            string[] splitMac = mac.Split(':');
            if (splitMac.Length > 6) return false;
            foreach (string hex in splitMac)
            {
                if (string.IsNullOrEmpty(hex) || !IsValidHex(hex)) return false;
            }
            return true;
        }

        public static bool IsValidHex(string hex)
        {
            return hex.Length == 2 && Regex.IsMatch(hex, "^[0-9a-fA-F]{1,2}$");
        }

        public static bool IsReachable(string ipAddress)
        {
            if (!IsValidIP(ipAddress)) return false;
            Ping pinger = null;
            try
            {
                pinger = new Ping();
                int success = 0;
                for (int i = 0; i < 5; i++)
                {
                    PingReply reply = pinger.Send(ipAddress, 1000);
                    if (reply.Status == IPStatus.Success) success++;
                }
                if (success >= 4) return true;
            }
            catch
            {
            }
            finally
            {
                pinger?.Dispose();
            }
            return false;
        }

        public static string GetDictValue(Dictionary<string, string> dict, string param)
        {
            return !dict.TryGetValue(param, out string val) || string.IsNullOrEmpty(val) ? string.Empty : val.Trim();
        }

        public static bool IsInvalid(object[] values)
        {
            bool res = values == null || values.Length < 2
                || values[0] == null || values[0] == DependencyProperty.UnsetValue
                || values[1] == null || values[1] == DependencyProperty.UnsetValue;
            return res;
        }

        public static bool IsInvalid(object value)
        {
            bool res = value == null || value == DependencyProperty.UnsetValue;
            return res;
        }

        public static bool IsOldAosVersion(object aos)
        {
            if (aos == null) return false;
            Match version = Regex.Match(aos.ToString(), Constants.MATCH_AOS_VERSION);
            if (version.Success && version.Groups.Count > 5)
            {
                int v1 = int.TryParse(version.Groups[1].ToString(), out int i) ? i : 9;
                int v2 = int.TryParse(version.Groups[2].ToString(), out i) ? i : 9;
                int r = int.TryParse(version.Groups[5].ToString(), out i) ? i : 9;
                string[] minver = Constants.MIN_AOS_VERSION.Split(' ');
                int minv1 = int.Parse(minver[0].Split('.')[0]);
                int minv2 = int.Parse(minver[0].Split('.')[1]);
                int minr = int.Parse(minver[1].Replace("R", ""));
                return (v1 < minv1) || (v1 == minv1 && v2 < minv2) || (v1 == minv1 && v2 == minv2 && r < minr);
            }
            return false;
        }

        public static int[] GetMinimunVersion(string model, string versionType)
        {
            Dictionary<string, string> dict = (versionType == FPGA) ? fpgaVersions : cpldVersions;

            string m = model;
            while (m.Length > 2)
            {
                if (dict.TryGetValue(m, out string val))
                {
                    string[] vals = val.Split('.');
                    return Array.ConvertAll(vals, int.Parse);
                }
                m = m.Substring(0, m.Length - 1);
            }
            return null;
        }

        public static double GetThresholdPercentage(object[] values)
        {
            double cpu = StringToDouble(values[0].ToString());
            double thrshld = StringToDouble(values[1].ToString());
            if (thrshld == 0) return 1;
            return 1 - cpu / thrshld;
        }

        public static string ParseIfIndex(string value)
        {
            try
            {
                int ifIndex = StringToInt(value);
                int chassisDiv = ifIndex / 100000;
                int chassisNr = chassisDiv + 1;
                int slotNr = (ifIndex % 10000) / 1000;
                int portNr = (ifIndex % 1000);
                return $"{chassisNr}/{slotNr}/{portNr}";
            }
            catch { }
            return "";
        }

        public static string GetEnumDescription(Enum enaumValue)
        {
            try
            {
                FieldInfo fi = enaumValue.GetType().GetField(enaumValue.ToString());
                return fi.GetCustomAttributes(typeof(DescriptionAttribute), false) is DescriptionAttribute[] attributes && attributes.Any() ? attributes.First().Description : enaumValue.ToString();
            }
            catch { }
            return enaumValue.ToString();
        }

        public static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0) return text;
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length).Trim();
        }

        public static string PrintNumberBytes(long sizeBytes)
        {
            string[] sizeLabels = { "Bytes", "KB", "MB", "GB", "TB" };
            double size = sizeBytes;
            int labelIndex = 0;
            while (size >= 1024 && labelIndex < sizeLabels.Length - 1)
            {
                size /= 1024;
                labelIndex++;
            }
            size = Math.Round(size, 2);
            return $"{size} {sizeLabels[labelIndex]}";
        }

        public static void CreateTextFile(string filePath, StringBuilder txt)
        {
            if (File.Exists(filePath)) File.Delete(filePath);
            File.WriteAllText(filePath, txt.ToString());
        }

        public static SwitchDebugLogLevel IntToSwitchDebugLevel(int logLevel)
        {
            if (Enum.TryParse(logLevel.ToString(), true, out SwitchDebugLogLevel parsedLogLevel)) return parsedLogLevel;
            return SwitchDebugLogLevel.Unknown;
        }

        public static SwitchDebugLogLevel StringToSwitchDebugLevel(string logLevel)
        {
            if (!string.IsNullOrEmpty(logLevel) && Enum.TryParse(logLevel, true, out SwitchDebugLogLevel parsedLogLevel)) return parsedLogLevel;
            return SwitchDebugLogLevel.Unknown;
        }

        public static double CalcPercent(double val1, double val2, int dec)
        {
            try
            {
                if (val2 > 0) return RoundUp((val1 / val2) * 100, dec);
            }
            catch { }
            return 0;
        }

        public static string GetVendorName(string mac)
        {
            string vendorName = mac.Trim();
            string[] macAddr = vendorName.Split(':');
            string macMask = macAddr.Length == 6 ? $"{macAddr[0]}{macAddr[1]}{macAddr[2]}" : "-";
            if (MainWindow.ouiTable.ContainsKey(macMask)) vendorName = MainWindow.ouiTable[macMask];
            return vendorName;
        }


        public static void StartProgressBar(IProgress<ProgressReport> progress, string barText)
        {
            progress.Report(new ProgressReport(barText));
            progress.Report(new ProgressReport(ReportType.Value, barText, "0"));
        }

        public static void UpdateProgressBar(IProgress<ProgressReport> progress, double currVal, double totalVal)
        {
            double ratio = totalVal > 0 ? 100 * currVal / totalVal : 0;
            progress.Report(new ProgressReport(ReportType.Value, null, $"{ratio}"));
        }

        public static void CloseProgressBar(IProgress<ProgressReport> progress)
        {
            progress.Report(new ProgressReport { Type = ReportType.Value, Message = "100" });
            Thread.Sleep(1000);
            progress.Report(new ProgressReport { Type = ReportType.Value, Message = "-1" });
        }

        public static double GetEstimateCollectLogDuration(bool restartPoE, string port)
        {
            if (port == null && restartPoE) return MAX_COLLECT_LOGS_RESET_POE_DURATION;
            else if (port == null && !restartPoE) return MAX_COLLECT_LOGS_DURATION;
            else return MAX_COLLECT_LOGS_WIZARD_DURATION;
        }

        public static string PurgeFiles(string folder, int nbMaxFiles)
        {
            StringBuilder txtDelete = new StringBuilder("\n\t- List of files deleted:");
            if (Directory.Exists(folder))
            {
                string[] filesList = new DirectoryInfo(folder).GetFiles().OrderByDescending(f => f.LastWriteTime).Select(f => f.Name).ToArray();
                int nbFiles = filesList.Length;
                int nbFilesDeleted = 0;
                for (int idx = filesList.Length - 1; idx >= 0; idx--)
                {
                    string fPath = Path.Combine(folder, filesList[idx]);
                    if (File.Exists(fPath))
                    {
                        DateTime lastWriteTime = File.GetLastWriteTime(fPath);
                        if (nbFiles > nbMaxFiles || lastWriteTime < DateTime.Now.AddDays(-MAX_NB_SNAPSHOT_DAYS))
                        {
                            File.Delete(fPath);
                            nbFiles--;
                            if (nbFilesDeleted % 3 == 0) txtDelete.Append("\n\t  "); else if (nbFilesDeleted > 0) txtDelete.Append(", ");
                            txtDelete.Append(filesList[idx]).Append(" (").Append(lastWriteTime.ToString("MM/dd/yyyy hh:mm:ss tt")).Append(")");
                            nbFilesDeleted++;
                        }
                    }
                }
                if (nbFilesDeleted > 0) return $"\n\t- Number of snapshot configuration files deleted: {nbFilesDeleted}\n\t- Folder: {folder}{txtDelete}";
            }
            return string.Empty;
        }

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

        public static ConfigType ConvertToConfigType(Dictionary<string, string> dict, string key)
        {
            return StringToConfigType(GetDictValue(dict, key));
        }

        public static ConfigType StringToConfigType(string sVal)
        {
            if (!string.IsNullOrEmpty(sVal))
            {
                if (sVal.Contains("enable")) return ConfigType.Enable;
                else if (sVal.Contains("disable")) return ConfigType.Disable;
            }
            return ConfigType.Unavailable;
        }
    }
}


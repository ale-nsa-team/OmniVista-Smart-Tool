using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;
using System.Xml.Linq;

namespace PoEWizard.Data
{
    public static class Utils
    {

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
                if (!string.IsNullOrEmpty(number))
                {
                    bool isNumeric = double.TryParse(strNumber.Trim(), out double dVal);
                    if (isNumeric && (dVal > 0)) return dVal;
                }
            }
            catch { }
            return 0;
        }

        public static string ExtractNumber(string strNumber)
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
            return $" by Method {method.Name} of {method.DeclaringType.Name}";
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

        public static double RoundUp(double input)
        {
            double multiplier = Math.Pow(10, 0);
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
            if (!res) Logger.Debug($"Invalid value in converter: {(values == null ? "null" : string.Join(",", values))}");
            return res;
        }

        public static bool IsInvalid(object value)
        {
            bool res = value == null || value == DependencyProperty.UnsetValue;
            if (!res) Logger.Debug($"Invalid value in converter: {(value ?? "null")}");
            return res;
        }

        public static bool IsOldAosVersion(object aos)
        {
            if (aos == null) return false;
            Match version = Regex.Match(aos.ToString(), Constants.MATCH_AOS_VERSION);
            if (version.Success && version.Groups.Count > 5)
            {
                int v1 = int.TryParse(version.Groups[1].ToString(),out int i) ? i : 9;
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

        public static int[] GetMinFpga(string model)
        {
            string m = model;
            while (m.Length > 2)
            {
                if (Constants.fpgaVersions.TryGetValue(m, out string val))
                {
                    string[] vals = val.Split('.');
                    return Array.ConvertAll(vals, int.Parse);
                }
                m = m.Substring(0, m.Length - 1);
            }
            return null;
        }

        public static int[] GetMinimunVersion(string model, string versionType)
        {
            Dictionary<string, string> dict = (versionType == Constants.FPGA) ? Constants.fpgaVersions : Constants.cpldVersions;

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
            double cpu = double.TryParse(values[0].ToString(), out double d) ? d : 0;
            double thrshld = double.TryParse(values[1].ToString(), out d) ? d : 0;
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

    }
}


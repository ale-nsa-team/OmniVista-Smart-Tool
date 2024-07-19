using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
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
                if (pinger != null) pinger.Dispose();
            }
            return false;
        }

        public static string GetDictValue(Dictionary<string, string> dict, string param)
        {
            return (dict.TryGetValue(param, out string val) ? val.Trim() : string.Empty);
        }

    }
}


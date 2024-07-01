using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace PoEWizard.Data
{
    public static class Utils
    {

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
                if (parts.Length > 2) return $"{parts[1].Replace("+", "")}: {parts[2]}";
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

        public static string GetSlotNumberFromPort(string slotPortNr)
        {
            Dictionary<string, object> slotPort = Utils.GetChassisSlotPort(slotPortNr);
            return $"{slotPort[Constants.P_CHASSIS]}/{slotPort[Constants.P_SLOT]}";
        }

        public static Dictionary<string, object> GetChassisSlotPort(string slotPortNr)
        {
            try
            {
                string[] valuesList = slotPortNr.Split('/');
                if (valuesList != null)
                {
                    Dictionary<string, object> slotPort = new Dictionary<string, object>();
                    if (valuesList.Length > 2)
                    {
                        slotPort[Constants.P_CHASSIS] = StringToInt(valuesList[0].Trim());
                        slotPort[Constants.P_SLOT] = StringToInt(valuesList[1].Trim());
                        slotPort[Constants.P_PORT] = valuesList[2].Trim();
                    }
                    else
                    {
                        if (valuesList.Length > 1)
                        {
                            slotPort[Constants.P_CHASSIS] = StringToInt(valuesList[0].Trim());
                            slotPort[Constants.P_SLOT] = StringToInt(valuesList[1].Trim());
                            slotPort[Constants.P_PORT] = "";
                        }
                        else
                        {
                            slotPort[Constants.P_CHASSIS] = StringToInt(valuesList[0].Trim());
                            slotPort[Constants.P_SLOT] = 1;
                            slotPort[Constants.P_PORT] = "";
                        }
                    }
                    return slotPort;
                }
            }
            catch { }
            return null;
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

    }

}


using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Data
{
    public static class CliParseUtils
    {
        private static readonly Regex vtableRegex = new Regex(MATCH_COLON);
        private static readonly Regex etableRegex = new Regex(MATCH_EQUALS);
        private static readonly Regex htableRegex = new Regex(MATCH_TABLE_SEP);
        private static readonly Regex chassisRegex = new Regex(MATCH_CHASSIS);

        public static Dictionary<string, string> ParseXmlToDictionary(string xml, string xpath)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            XmlNodeList nodes = xmlDoc.SelectNodes(xpath);
            foreach (XmlNode node in nodes)
            {
                string key = node.Name;
                //string value = node.InnerText.Trim(new char[] { ':', ' ', '\n' }); ;
                string value = node.InnerText.Trim(new char[] { ':', '\n' }); ;
                dictionary[key] = value;
            }
            return dictionary;
        }

        public static Dictionary<string, string> ParseVTable(string data)
        {
            return ParseTable(data, vtableRegex);
        }

        public static Dictionary<string, string> ParseETable(string data)
        {
            return ParseTable(data, etableRegex);
        }

        public static List<Dictionary<string, string>> ParseHTable(string data, int nbHeaders = 1)
        {
            List<Dictionary<string, string>> table = new List<Dictionary<string, string>>();
            string[] lines = Regex.Split(data, @"\r\n\r|\n");
            Match match = htableRegex.Match(data);
            if (match.Success)
            {
                int line = LineNumberFromPosition(data, match.Index);
                string[] header = new string[match.Value.Count(c => c == '+') + 1];
                for (int i = 1; i <= nbHeaders; i++)
                {
                    string[] hd = GetValues(lines[line], lines[line - i]);
                    header = header.Zip(hd, (a, b) => b.EndsWith("/") ? $"{b}{a}" : $"{b} {a}").ToArray();
                }

                for (int i = line + 1; i < lines.Length; i++)
                {
                    if (lines[i] == string.Empty) break;
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    string[] values = GetValues(lines[line], lines[i]);
                    if (values.Length != header.Length) continue;
                    for (int j = 0; j < header.Length; j++)
                    {
                        dict.Add(header[j].Trim(), values?.Skip(j).FirstOrDefault());
                    }
                    table.Add(dict);
                }
            }

            return table;
        }

        public static List<Dictionary<string, string>> ParseChassisTable(string data)
        {
            List<Dictionary<string, string>> table = new List<Dictionary<string, string>>();
            using (StringReader reader = new StringReader(data))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Trim().Length == 0) continue;
                    Match match = chassisRegex.Match(line);
                    if (match.Success)
                    {
                        Dictionary<string, string> dict = new Dictionary<string, string>();
                        dict["ID"] = match.Groups[2].Value;
                        dict["Role"] = match.Groups[3].Value;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if ((match = vtableRegex.Match(line)) != null && match.Success) {
                                string key = match.Groups[1].Value.Trim();
                                string value = match.Groups[2].Value.Trim();
                                value = value.EndsWith(",") ? value.Substring(0, value.Length - 1) : value;
                                dict[key] = value;
                            }
                            else
                            {
                                break;
                            }
                        }
                        table.Add((dict));
                    }
                }
            }
            return table;
        }

        private static Dictionary<string, string> ParseTable(string data, Regex regex)
        {
            Dictionary<string, string> table = new Dictionary<string, string>();
            using (StringReader reader = new StringReader(data))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Match match = regex.Match(line);
                    if (match.Success)
                    {
                        string key = match.Groups[1].Value.Trim();
                        string value = match.Groups[2].Value.Trim();
                        value = value.EndsWith(",") ? value.Substring(0, value.Length - 1) : value;
                        if (!table.ContainsKey(key))
                            table.Add(key, value);
                    }
                }
            }
            return table;
        }

        private static int LineNumberFromPosition(string text, int matchIndex)
        {
            int line = 0;
            for (int i = 0; i < matchIndex; i++)
            {
                if (text[i] == '\n') line++;
            }
            return line;
        }

        private static string[] GetValues(string sep, string line)
        {
            int idx = 0;
            List<string> vals = new List<string>();
            string[] split = sep.Split('+');
            foreach (string s in split)
            {
                int len = idx + s.Length < line.Length ? s.Length + 1 : line.Length - idx;
                if (len > 0)
                {
                    vals.Add(line.Substring(idx, len).Trim());
                    idx = sep.IndexOf('+', idx + 1);
                }
                else
                {
                    vals.Add("");
                }
            }

            return vals.ToArray();
        }
    }
}

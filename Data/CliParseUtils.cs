using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Data
{
    public static class CliParseUtils
    {
        private static readonly Regex vtableRegex = new Regex(MATCH_COLON);
        private static readonly Regex etableRegex = new Regex(MATCH_EQUALS);
        private static readonly Regex htableRegex = new Regex(MATCH_TABLE_SEP);
        private static readonly Regex chassisRegex = new Regex(MATCH_CHASSIS);

        public static List<Dictionary<string, string>> ParseListFromDictionary(Dictionary<string, string> inputDict, string match = null)
        {
            List<Dictionary<string, string>> dictList = new List<Dictionary<string, string>>();
            int idx = 0;
            string lastIdx = "";
            dictList.Add(new Dictionary<string, string>());
            foreach (var keyVal in inputDict)
            {
                string key = keyVal.Key;
                if (key.Contains($"_"))
                {
                    string id = Utils.ExtractNumber(key);
                    if (lastIdx != $"_{id}")
                    {
                        dictList.Add(new Dictionary<string, string>());
                        lastIdx = $"_{id}";
                        idx++;
                    }
                    if (!string.IsNullOrEmpty(id)) key = key.Replace($"_{id}", "");
                }
                if (!string.IsNullOrEmpty(match) && !key.Contains(match)) continue;
                dictList[idx][key] = keyVal.Value;
            }
            return dictList;
        }

        public static Dictionary<string, string> ParseXmlToDictionary(string xml)
        {
            XDocument doc = XDocument.Parse(xml);
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (XElement element in doc.Descendants().Where(p => p.HasElements == false))
            {
                int keyInt = 0;
                string keyName = element.Name.LocalName;
                while (dict.ContainsKey(keyName))
                {
                    keyName = element.Name.LocalName + "_" + keyInt++;
                }
                dict.Add(keyName, element.Value);
            }
            return dict;
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
                    for (int j = 0; j < header.Length; j++)
                    {
                        dict.Add(header[j].Replace("|", "").Trim(), values?.Skip(j).FirstOrDefault());
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
                        Dictionary<string, string> dict = new Dictionary<string, string>
                        {
                            ["ID"] = match.Groups[2].Value,
                            ["Role"] = match.Groups[3].Value
                        };
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

        public static Dictionary<string, List<Dictionary<string, string>>> ParseLldpRemoteTable(string data)
        {
            Dictionary<string, List<Dictionary<string, string>>> dictList = new Dictionary<string, List<Dictionary<string, string>>> ();
            string[] split;
            string currPort = string.Empty;
            using (StringReader reader = new StringReader(data))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Trim().Length == 0) continue;
                    if (line.Contains("Local Port "))
                    {
                        currPort = Utils.ExtractSubString(line.Trim(), "Local Port ", ":");
                        dictList[currPort] = new List<Dictionary<string, string>>();
                    }
                    else if (line.Contains("Chassis ") && !line.Contains("Subtype") && line.Contains(","))
                    {
                        split = line.Trim().Split(',');
                        if (split.Length == 2)
                        {
                            dictList[currPort].Add(new Dictionary<string, string>());
                            dictList[currPort][dictList[currPort].Count - 1] = new Dictionary<string, string> {
                                ["Local Port"] = currPort,
                                [CHASSIS_MAC_ADDRESS] = Utils.ExtractSubString(split[0].Trim(), "Chassis "),
                                [REMOTE_PORT] = Utils.ExtractSubString(split[1].Trim(), "Port ")
                            };
                        }
                    }
                    else
                    {
                        split = line.Trim().Split('=');
                        if (split.Length == 2)
                        {
                            dictList[currPort][dictList[currPort].Count - 1][split[0].Trim()] = split[1].Replace(",", "").Replace("\"", "").Trim();
                        }
                        else if (line.Contains(REMOTE_ID))
                        {
                            split = line.Trim().Split(' ');
                            if (split.Length > 3) dictList[currPort][dictList[currPort].Count - 1][REMOTE_ID] = split[3].Trim().Replace(":", "");
                        }
                    }
                }
            }
            return dictList;
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
                        if (key.StartsWith(FPGA)) key = FPGA;
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
            for (int i = 0; i < split.Length - 1; i++)
            {
                string s = split[i];
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
            if (idx < line.Length) vals.Add(line.Substring(idx).Trim());
            else vals.Add("");

            return vals.ToArray();
        }
    }
}

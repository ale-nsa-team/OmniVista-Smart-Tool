using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Data
{
    public static class CliParseUtils
    {
        private static readonly Regex vtableRegex = new Regex("([^:]+):(.+)");
        private static readonly Regex etableRegex = new Regex("([^:]+)=(.+)");
        private static readonly Regex htableRegex = new Regex(@"(-+\++)+");
        private static readonly Regex chassisRegex = new Regex(@"([Local|Remote] Chassis ID )(\d+) \((.+)\)");

        public static Dictionary<string, string> ParseXmlToDictionary(string xml, string xpath)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            XmlNodeList nodes = xmlDoc.SelectNodes(xpath);
            foreach (XmlNode node in nodes)
            {
                string key = node.Name;
                string value = node.InnerText.Trim(new char[] { ':', ' ', '\n' }); ;
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
                string[] header;
                if (nbHeaders >= 2)
                {
                    string h1 = (line >= nbHeaders) ? lines[line - nbHeaders] : "";
                    string h2 = (line >= nbHeaders - 1) ? lines[line - (nbHeaders - 1)] : "";
                    string h3 = (line >= nbHeaders - 2) ? lines[line - (nbHeaders - 2)] : "";
                    if (h3.Contains("+")) h3 = "";
                    string[] hd1 = GetValues(lines[line], h1);
                    string[] hd2 = GetValues(lines[line], h2);
                    string[] hd3 = GetValues(lines[line], h3);
                    header = hd1.Zip(hd2, (a, b) => $"{a} {b}").ToArray();
                    if (!string.IsNullOrEmpty(h3))
                    {
                        header = header.Zip(hd3, (a, b) => $"{a} {b}").ToArray();
                    }
                }
                else
                {
                    string head = lines[line - 1];
                    header = GetValues(lines[line], head);
                }

                for (int i = line + 1; i < lines.Length; i++)
                {
                    if (lines[i] == string.Empty) break;
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    string[] values = GetValues(lines[line], lines[i]);
                    for (int j = 0; j < header.Length; j++)
                    {
                        dict.Add(header[j], values?.Skip(j).FirstOrDefault());
                    }
                    table.Add(dict);
                }
            }

            return table;
        }

        public static Dictionary<string, string> ParseSingleHTable(string data, int nbHeaders = 1)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            string[] lines = Regex.Split(data, @"\r\n\r|\n");
            Match match = htableRegex.Match(data);
            if (match.Success)
            {
                int line = LineNumberFromPosition(data, match.Index);
                string[] header;
                if (nbHeaders == 2)
                {
                    string h1 = lines[line - 2];
                    string h2 = lines[line - 1];
                    string[] hd1 = GetValues(lines[line], h1);
                    string[] hd2 = GetValues(lines[line], h2);
                    header = hd1.Zip(hd2, (a, b) => $"{a} {b}").ToArray();
                }
                else
                {
                    string head = lines[line - 1];
                    header = GetValues(lines[line], head);
                }

                for (int i = line + 1; i < lines.Length; i++)
                {
                    if (lines[i] == string.Empty) break;
                    string[] values = GetValues(lines[line], lines[i]);
                    for (int j = 0; j < header.Length; j++)
                    {
                        dict.Add(header[j], values?.Skip(j).FirstOrDefault());
                    }
                }
            }
            return dict;
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

        public static Dictionary<string, List<string>> ParseRowInfoHasKeyTable(string data)
        {
            Dictionary<string, List<string>> table = new Dictionary<string, List<string>>();
            using (StringReader reader = new StringReader(data))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    //don't run if line has "->" or "-+-"
                    if (line.Trim() == string.Empty) continue;
                    List<string> listInfo = new List<string>();
                    string[] splits = Regex.Split(line, SPACE_REGEX_S_2);
                    //first item is the key of row.
                    //Ex: CPU 1 2 3 4 => first item is CPU is the key
                    int count = 0;
                    while (string.IsNullOrEmpty(splits[count]) && !string.IsNullOrEmpty(line)) count++;
                    string key = splits[count].Trim().ToUpper();
                    //don't add key to list
                    for (int i = count + 1; i < splits.Length; i++)
                    {
                        listInfo.Add(splits[i].Trim());
                    }
                    if (!table.ContainsKey(key)) table.Add(key, listInfo);
                }
            }
            return table;
        }

        public static List<List<string>> ParseRowInfoNoKeyTable(string data, string cmdSplit, string sessionPrompt, bool isSkipDivider)
        {
            List<List<string>> listInfo = new List<List<string>>();
            int stIdx = data.IndexOf(cmdSplit);
            if (stIdx == -1) return listInfo;
            int endIdx = data.IndexOf(sessionPrompt, stIdx + 1);
            int len = endIdx > 0 ? endIdx - stIdx : data.Length - stIdx;
            data = data.Substring(stIdx, len);
            if (isSkipDivider)
            {
                stIdx = data.LastIndexOf("---");
                if (stIdx > 0) data = data.Substring(data.LastIndexOf("---")).Replace("---", "\n");
            }
            using (StringReader reader = new StringReader(data))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    //don't run line having "->" or "-+-"
                    //split space regex
                    string[] splits = Regex.Split(line, SPACE_REGEX_S_2);
                    if (splits.Length != 1)
                    {
                        //trim value (remove space for each value)
                        List<string> splitList = new List<string>();
                        foreach (string split in splits)
                        {
                            if (!string.IsNullOrEmpty(split)) splitList.Add(split.Trim());
                        }
                        listInfo.Add(splitList);
                    }
                }
            }
            return listInfo;
        }

        public static List<ConfigKeyObject> ParseKeyObjectTable(string data, string sessionPrompt, string cmdSplit)
        {
            List<ConfigKeyObject> dataList = new List<ConfigKeyObject>();
            int stIdx = data.IndexOf(cmdSplit);
            int endIdx = data.IndexOf(sessionPrompt, stIdx + 1);
            int len = endIdx > 0 ? endIdx - stIdx : data.Length - stIdx;
            data = data.Substring(stIdx, len);
            string key = "";
            Dictionary<string, object> map = new Dictionary<string, object>();
            using (StringReader reader = new StringReader(data))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains(COMMA))
                    {
                        if (line.Contains("="))
                        {
                            line = line.Trim();
                            //get key with map, if null, create new objectList
                            List<List<string>> objList = new List<List<string>>();
                            if (map.ContainsKey(key))
                            {
                                objList = (List<List<string>>)map[key];
                            }
                            //splits ","
                            string[] commas = line.Split(',');
                            foreach (string s in commas)
                            {
                                if (s != string.Empty)
                                {
                                    string[] valueList = s.Split('=');
                                    objList.Add(valueList.ToList());
                                }
                            }
                            if (!map.ContainsKey(key)) map.Add(key, objList);
                            else map[key] = objList;
                        }
                        else
                        {
                            key = line.Replace(",", "").Trim();
                        }
                    }
                }
                foreach (string keyMap in map.Keys)
                {
                    dataList.Add(new ConfigKeyObject(keyMap, map[keyMap]));
                }
            }
            return dataList;
        }

        public static string GetErrors(string data)
        {
            StringBuilder errors = new StringBuilder();
            if (data != null && data.Length > 0)
            {
                bool startError = false;
                using (StringReader reader = new StringReader(data))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line.Length > 1)
                        {
                            if (line.StartsWith(ERROR))
                            {
                                startError = true;
                                errors.Append(Regex.Replace(line, ERROR, string.Empty)).Append(LINE_FEED);
                            }
                            else if (line.ToLower().EndsWith("password:") || line.ToLower().EndsWith("password :"))
                            {
                                errors.Append("Session expired, please disconnect and log back in to the switch.");
                            }
                            else if (startError)
                            {
                                errors.Append(line).Append(LINE_FEED);
                            }
                        }
                    }
                }
            }
            return errors.ToString();
        }

        public static List<string> GetExtendedTdr(string data, bool isState)
        {
            int stIdx, len;
            if (isState)
            {
                stIdx = data.IndexOf("Pair Polarity");
                len = data.IndexOf("Pair Skew") - stIdx;
            }
            else
            {
                stIdx = data.IndexOf("Cable Length");
                len = data.IndexOf("Cable Downshift") - stIdx;
            }
            data = data.Substring(stIdx, len);
            List<string> listData = new List<string>();
            using (StringReader reader = new StringReader(data))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains(":"))
                    {
                        string[] valueSplit = line.Split(':');
                        string value = valueSplit[1].Trim();
                        value = value.Substring(0, value.IndexOf(COMMA));
                        listData.Add(value);
                    }
                }
            }
            return listData;
        }

        public static string GetTdrStatus(string data)
        {
            int idx = data.IndexOf("Status");
            if (idx != -1)
            {
                data = data.Substring(idx);
                if (data.Contains(":"))
                {
                    if (data.Split(':')[1].ToLower().Contains("success"))
                        return "Success";
                    else
                        return "Fail";
                }
            }
            return "";
        }

        public static Dictionary<int, List<string>> GetFanData(string data, string sessionPrompt, string cmdSplit)
        {
            int stIdx = data.IndexOf(cmdSplit);
            int len = data.IndexOf(sessionPrompt, stIdx + 3) - stIdx;
            data = data.Substring(stIdx, len);
            Dictionary<int, List<string>> map = new Dictionary<int, List<string>>();
            using (StringReader reader = new StringReader(data))
            {
                string line;
                int count = 0;
                while ((line = reader.ReadLine()) != null)
                {

                    //first element contains labels
                    string[] splits = line.Split('|');
                    if (splits.Length > 2)
                    {
                        map.Add(count++, splits.ToList());
                    }
                    else
                    {
                        //other elements contain values
                        splits = Regex.Split(line, SPACE_REGEX_S_2);
                        if (splits.Length > 2)
                        {
                            map.Add(count++, splits.Where(val => val != string.Empty && val != "-----").ToList());
                        }

                    }
                }
            }
            return map;
        }

        public static List<string> GetWattsAndPower(string data, string cmd)
        {
            List<string> listInfo = new List<string>();
            data = data.Substring(data.IndexOf(cmd));
            data = data.Substring(data.LastIndexOf("---")).Replace("---", "\n");
            using (StringReader reader = new StringReader(data))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains("Watts") && line.Contains("Power"))
                    {
                        listInfo.Add(line.Substring(0, line.IndexOf("Watts")));
                    }
                }
            }
            return listInfo;
        }

        public static int GetNumberOfMacs(string data)
        {
            int sol = data.IndexOf(VALID_MACS);
            if (sol == -1) return 0;
            int eol = data.IndexOf('\n', sol);
            string line = data.Substring(sol, eol - sol);
            string n = line.Split('=')[1];
            return int.TryParse(n, out int i) ? i : 0;
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

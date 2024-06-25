using System.Text;
using System.Text.RegularExpressions;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Comm
{
    public class CmdRequest
    {
        public DataMatch DataMatch { get; set; }
        public string Cmd { get; set; }
        public byte[] Data { get; set; }
        public int Wait { get; set; } = -1;

        public CmdRequest() { }

        public bool Match(string result)
        {
            if (DataMatch == null) return true;
            result = result.Trim();
            string data = DataMatch.Data.Trim();
            switch (DataMatch.Match)
            {
                case MatchOperation.Equals:
                    return result.Equals(data);
                case MatchOperation.StartsWith:
                    return result.StartsWith(data);
                case MatchOperation.EndsWith:
                    return result.EndsWith(data);
                case MatchOperation.Contains:
                    return result.Contains(data);
                case MatchOperation.Regex:
                    Regex rgx = new Regex(DataMatch.Data, RegexOptions.Compiled);
                    return rgx.IsMatch(result);
            }
            return true;
        }

        public override string ToString()
        {
            string cmd = $"cmd: {(Cmd != null ? $"{Cmd.Replace("\n", "\\n")}" : "null")}";
            string data = $"data: {(Data != null ? $"{Encoding.Default.GetString(Data)}" : "null")}";
            string match = $"match: {(DataMatch != null ? $"{DataMatch}" : "null")}";
            return $"{cmd} {data} {match}";
        }
    }
}

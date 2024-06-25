using static PoEWizard.Data.Constants;

namespace PoEWizard.Comm
{
    public class DataMatch
    {
        public DataMatch() { }

        public string Data { get; set; }
        public MatchOperation Match { get; set; }

        public override string ToString()
        {
            return $"oper: {Match}, data: {Data}";
        }
    }
}

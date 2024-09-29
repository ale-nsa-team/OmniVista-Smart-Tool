using static PoEWizard.Data.Constants;

namespace PoEWizard.Data
{
    public class SwitchDebugApp
    {

        public string Name { get; set; }
        public string ID { get; set; }
        public string Index { get; set; }
        public string NbSubApp { get; set; }
        public int DebugLevel { get; set; }

        public SwitchDebugApp(string name) : this(name, string.Empty, string.Empty, string.Empty)
        {
            DebugLevel = (int)SwitchDebugLogLevel.Unknown;
        }

        public SwitchDebugApp(string name, string iD, string index, string nbSubApp)
        {
            Name = name;
            ID = iD;
            Index = index;
            NbSubApp = nbSubApp;
            DebugLevel = (int)SwitchDebugLogLevel.Unknown;
        }

    }
}

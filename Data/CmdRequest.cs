using static PoEWizard.Data.Constants;
using static PoEWizard.Data.RestUrl;

namespace PoEWizard.Data
{
    public class CmdRequest
    {
        public CmdRequest(Command command) : this(command, ParseType.Text, null, 1) { }
        public CmdRequest(Command command, ParseType type) : this(command, type, null, 1) { }
        public CmdRequest(Command command, ParseType type, int nbHeaders) : this(command, type, null, nbHeaders) { }
        public CmdRequest(Command command, ParseType type, string[] data, int nbHeaders)
        {
            Command = command;
            ParseType = type;
            Data = data;
            NbHeaders = nbHeaders;
        }
        public Command Command { get; }
        public ParseType ParseType { get; }
        public string[] Data { get; }
        public int NbHeaders { get; }
    }
}

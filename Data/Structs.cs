using static PoEWizard.Data.Constants;
using static PoEWizard.Data.RestUrl;

namespace PoEWizard.Data
{
    public struct CmdRequest
    {
        public CmdRequest(Command command, ParseType type, string[] data, int nbHeaders)
        {
            Command = command;
            Type = type;
            Data = data;
            NbHeaders = nbHeaders;
        }
        public Command Command { get; }
        public ParseType Type { get; }
        public string[] Data { get; }
        public int NbHeaders { get; }
    }
}

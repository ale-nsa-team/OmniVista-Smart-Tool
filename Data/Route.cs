using System.Collections.Generic;

namespace PoEWizard.Data
{
    public class Route
    {
        public string Destination { get; set; }
        public string Gateway { get; set; }
        public string Age { get; set; }
        public string Protocol { get; set; }

        public Route(Dictionary<string, string> route)
        {
            Destination = route["Dest Address"] ?? string.Empty;
            Gateway = route["Gateway Addr"] ?? string.Empty;
            Age = route["Age"] ?? string.Empty;
            Protocol = route["Protocol"] ?? string.Empty;
        }
    }
}
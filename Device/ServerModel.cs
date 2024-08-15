using PoEWizard.Data;
using System.Collections.Generic;
using System.Reflection;

namespace PoEWizard.Device
{
    public class ServerModel
    {
        private readonly Props config;

        public string Gateway { get; set; }
        public bool IsDns { get; set; } = false;
        public string Dns1 { get; set; }
        public string Dns2 { get; set; }
        public string Dns3 { get; set; }
        public string DnsDomain { get; set; }
        public bool IsNtp { get; set; } = false;
        public string Ntp1 { get; set; }
        public string Ntp2 { get; set; }
        public string Ntp3 { get; set; }

        public ServerModel() { }

        public ServerModel(Props cfg)
        {
            PropertyInfo[] props = GetType().GetProperties();
            foreach (PropertyInfo p in props)
            {
                if (p.PropertyType == typeof(bool))
                {
                    p.SetValue(this, cfg.GetBool(p.Name, false));
                }
                else
                {
                    p.SetValue(this, cfg.Get(p.Name), null);
                }
            }
            config = cfg;
        }

        public List<CmdRequest> ToCommandList()
        {
            List<CmdRequest> cmdList = new List<CmdRequest>();
            if (IsDns)
            {
                string dns = $"{Dns1} {Dns2} {Dns3}".Trim();
                if (!string.IsNullOrEmpty(dns))
                {
                    cmdList.Add(new CmdRequest(Command.DNS_LOOKUP));
                    cmdList.Add(new CmdRequest(Command.DNS_SERVER, dns));
                    if (!string.IsNullOrEmpty(DnsDomain)) cmdList.Add(new CmdRequest(Command.DNS_DOMAIN, DnsDomain));
                }
                if (IsNtp)
                {
                    if (!string.IsNullOrEmpty(Ntp1)) cmdList.Add(new CmdRequest(Command.NTP_SERVER, Ntp1));
                    if (!string.IsNullOrEmpty(Ntp2)) cmdList.Add(new CmdRequest(Command.NTP_SERVER, Ntp2));
                    if (!string.IsNullOrEmpty(Ntp3)) cmdList.Add(new CmdRequest(Command.NTP_SERVER, Ntp3));
                    cmdList.Add(new CmdRequest(Command.ENABLE_NTP));

                }
            }
            return cmdList;
        }
    }
}

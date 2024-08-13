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

        public List<string> ToCommandList()
        {
            List<string> cmdList = new List<string>();
            if (IsDns)
            {
                string dns = $"{Dns1} {Dns2} {Dns3}".Trim();
                if (!string.IsNullOrEmpty(dns))
                {
                    cmdList.Add(Commands.DnsLookup);
                    cmdList.Add(Commands.DnsServer(dns));
                    if (!string.IsNullOrEmpty(DnsDomain)) cmdList.Add(Commands.DnsDomain(DnsDomain));
                }
                if (IsNtp)
                {
                    if (!string.IsNullOrEmpty(Ntp1)) cmdList.Add(Commands.NtpServer(Ntp1));
                    if (!string.IsNullOrEmpty(Ntp2)) cmdList.Add(Commands.NtpServer(Ntp2));
                    if (!string.IsNullOrEmpty(Ntp3)) cmdList.Add(Commands.NtpServer(Ntp3));
                    cmdList.Add(Commands.NtpEnable);
                }
            }
            return cmdList;
        }
    }
}

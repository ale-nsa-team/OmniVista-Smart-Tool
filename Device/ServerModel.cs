using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PoEWizard.Device
{
    public class ServerModel : ICloneable
    {
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

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public List<CmdRequest> ToCommandList(ServerModel orig)
        {
            List<PropertyInfo> changes = GetChanges(orig);
            List<CmdRequest> cmdList = new List<CmdRequest>();
            List<string> dns = new List<string>();
            foreach (var prop in changes)
            {
                if (Regex.IsMatch(prop.Name, "Dns\\d") && IsDns) dns.Add((string)prop.GetValue(this, null));
                if (prop.Name == "DnsDomain" && IsDns) cmdList.Add(new CmdRequest(Command.DNS_DOMAIN, DnsDomain));
                if (prop.Name.StartsWith("Ntp") && IsNtp) cmdList.Add(new CmdRequest(Command.NTP_SERVER, (string)prop.GetValue(this, null)));
                if (prop.Name == "IsDns")
                {
                    if (IsDns) cmdList.Add(new CmdRequest(Command.DNS_LOOKUP));
                    else cmdList.Add(new CmdRequest(Command.NO_DNS_LOOKUP));
                }
                if (prop.Name == "IsNtp")
                {
                    if (IsNtp) cmdList.Add(new CmdRequest(Command.ENABLE_NTP));
                    else cmdList.Add(new CmdRequest(Command.DISABLE_NTP));
                }
            }
            if (dns.Count > 0 && IsDns)
            {
                cmdList.Add(new CmdRequest(Command.DNS_SERVER, string.Join(" ", dns)));
            }
            return cmdList;
        }

        private List<PropertyInfo> GetChanges(ServerModel orig)
        {
            List<PropertyInfo> changes = new List<PropertyInfo>();
            var props = this.GetType().GetProperties();
            foreach ( var prop in props )
            {
                object val = prop.GetValue(this, null);
                if (val?.GetType() == typeof(Boolean))
                {
                    if ((bool)val != (bool)prop.GetValue(orig, null)) changes.Add(prop);
                }
                else if (val != prop.GetValue(orig, null)) changes.Add(prop);
            }
            return changes;
        }
    }
}

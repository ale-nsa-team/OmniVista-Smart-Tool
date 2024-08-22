using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Linq;
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
            List<PropertyInfo> props = this.GetType().GetProperties().Where(p => Regex.IsMatch(p.Name, "Dns\\d")).ToList();
            List<CmdRequest> cmdList = new List<CmdRequest>();
            List<string> dnsAdd = new List<string>();
            HashSet<string> dnsRemove = new HashSet<string>();
            if (changes.FindIndex(p => Regex.IsMatch(p.Name, "Dns\\d")) != -1)
            {
                foreach (var prop in props)
                {
                    string newD = (string)prop.GetValue(this, null);
                    string origD = (string)prop.GetValue(orig, null);
                    if (origD != newD)
                    {
                        if (!string.IsNullOrEmpty(origD)) dnsRemove.Add(origD);
                        if (!string.IsNullOrEmpty(newD))
                        {
                            dnsRemove.Add(newD);
                            dnsAdd.Add(newD);
                        }
                    }
                    else if (!string.IsNullOrEmpty(newD)) dnsAdd.Add(newD);
                }
            }
            foreach (var prop in changes)
            {
                if (prop.Name == "DnsDomain" && IsDns) cmdList.Add(new CmdRequest(Command.DNS_DOMAIN, DnsDomain));
                if (prop.Name.StartsWith("Ntp") && IsNtp)
                {
                    string ntp = (string)prop.GetValue(this, null);
                    if (string.IsNullOrEmpty(ntp))
                        cmdList.Add(new CmdRequest(Command.DELETE_NTP_SERVER, (string)prop.GetValue(orig, null)));
                    else
                        cmdList.Add(new CmdRequest(Command.SET_NTP_SERVER, (string)prop.GetValue(this, null)));
                }
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
            foreach (string d in dnsRemove)
            {
                cmdList.Add(new CmdRequest(Command.DELETE_DNS_SERVER, d));
            }

            if (dnsAdd.Count > 0)
            {
                cmdList.Add(new CmdRequest(Command.SET_DNS_SERVER, string.Join(" ", dnsAdd)));
            }
            return cmdList;
        }

        private List<PropertyInfo> GetChanges(ServerModel orig)
        {
            List<PropertyInfo> changes = new List<PropertyInfo>();
            var props = this.GetType().GetProperties();
            foreach (var prop in props)
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

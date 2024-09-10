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

        private List<PropertyInfo> changes;

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
        public List<string> Timezones { get; } = Constants.timezones;
        public string Timezone { get; set; } = Constants.timezones[0];

        public ServerModel() { }

        public ServerModel(string gateway)
        {
            Gateway = gateway;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public List<CmdRequest> ToCommandList(ServerModel orig)
        {
            changes = GetChanges(orig);
            List<CmdRequest> cmdList = new List<CmdRequest>();
            List<string> dnsAdd = new List<string>();
            List<string> dnsRemove = new List<string>();
            List<string> ntpAdd = new List<string>();
            List<string> ntpRemove = new List<string>();
            UpdateServerList(dnsAdd, dnsRemove, orig, "Dns\\d");
            UpdateServerList(ntpAdd, ntpRemove, orig, "Ntp\\d");

            foreach (var prop in changes)
            {
                if (prop.Name == "Gateway") cmdList.Add(new CmdRequest(Command.SET_DEFAULT_GATEWAY, Gateway));
                if (prop.Name == "DnsDomain" && IsDns) cmdList.Add(new CmdRequest(Command.DNS_DOMAIN, DnsDomain));
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
                if (prop.Name == "Timezone")
                {
                    cmdList.Add(new CmdRequest(Command.SYSTEM_TIMEZONE, Timezone));
                }
            }
            foreach (string dns in dnsRemove)
            {
                cmdList.Add(new CmdRequest(Command.DELETE_DNS_SERVER, dns));
            }

            if (dnsAdd.Count > 0) cmdList.Add(new CmdRequest(Command.SET_DNS_SERVER, string.Join(" ", dnsAdd)));

            foreach (string ntp in ntpRemove)
            {
                cmdList.Add(new CmdRequest(Command.DELETE_NTP_SERVER, ntp));
            }

            foreach (string ntp in ntpAdd)
            {
                cmdList.Add(new CmdRequest(Command.SET_NTP_SERVER, ntp));
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

        private void UpdateServerList(List<string> add, List<string> remove, ServerModel orig, string pattern)
        {
            if (changes.FindIndex(p => Regex.IsMatch(p.Name, pattern)) != -1)
            {
                List<PropertyInfo> srvProps = this.GetType().GetProperties().Where(p => Regex.IsMatch(p.Name, pattern)).ToList();
                foreach (var prop in srvProps)
                {
                    string newD = (string)prop.GetValue(this, null);
                    string origD = (string)prop.GetValue(orig, null);
                    if (!string.IsNullOrEmpty(origD)) remove.Add(origD);
                    if (!string.IsNullOrEmpty(newD)) add.Add(newD);
                }
            }
        }
    }
}

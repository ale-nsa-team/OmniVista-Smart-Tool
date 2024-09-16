using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PoEWizard.Device
{
    public class FeatureModel : ICloneable
    {
        private readonly SwitchModel device;

        public bool NoInsecureProtos { get; set; } = true;
        public bool IsSsh { get; set; } = true;
        public bool IsMulticast { get; set; } = true;
        public bool IsDhcpRelay { get; set; } = false;
        public string DhcpSrv { get; set; }
        public List<EnableObj> Vlans { get; set; }
        public List<EnableObj> Slots { get; set; }

        public FeatureModel() { }

        public FeatureModel(SwitchModel device)
        {
            this.device = device;
            Vlans = new List<EnableObj>();
            Slots = new List<EnableObj>();
            foreach (var chas in device.ChassisList)
            {
                if (chas != null)
                {
                    Slots.AddRange(chas.Slots.Select(s => new EnableObj(s.Name, s.IsInitialized)));
                }
            }
        }

        public object Clone()
        {
            return new FeatureModel
            {
                IsMulticast = this.IsMulticast,
                IsSsh = this.IsSsh,
                NoInsecureProtos = this.NoInsecureProtos,
                IsDhcpRelay = this.IsDhcpRelay,
                DhcpSrv = this.DhcpSrv,
                Vlans = this.Vlans.Select(v => (EnableObj)v.Clone()).ToList(),
                Slots = this.Slots.Select(s => (EnableObj)s.Clone()).ToList()
            };
        }

        public List<CmdRequest> ToCommandList(FeatureModel orig)
        {
            List<PropertyInfo> changes = GetChanges(orig);
            List<CmdRequest> cmdList = new List<CmdRequest>();
            foreach (var prop in changes)
            {
                switch (prop.Name)
                {
                    case "NoInsecureProtos":
                        if (NoInsecureProtos)
                        {
                            cmdList.Add(new CmdRequest(Command.DISABLE_TELNET));
                            cmdList.Add(new CmdRequest(Command.DISABLE_FTP));
                        }
                        else
                        {
                            cmdList.Add(new CmdRequest(Command.ENABLE_TELNET));
                            cmdList.Add(new CmdRequest(Command.ENABLE_FTP));
                            cmdList.Add(new CmdRequest(Command.TELNET_AUTH_LOCAL));
                            cmdList.Add(new CmdRequest(Command.FTP_AUTH_LOCAL));
                        }
                        break;
                    case "IsSsh":
                        if (IsSsh)
                        {
                            cmdList.Add(new CmdRequest(Command.ENABLE_SSH));
                            cmdList.Add(new CmdRequest(Command.SSH_AUTH_LOCAL));
                        }
                        else
                        {
                            cmdList.Add(new CmdRequest(Command.DISABLE_SSH));
                        }
                        break;
                    case "IsMulticast":
                        if (IsMulticast)
                        {
                            cmdList.Add(new CmdRequest(Command.ENABLE_MULTICAST));
                            cmdList.Add(new CmdRequest(Command.ENABLE_QUERYING));
                        }
                        else
                        {
                            cmdList.Add(new CmdRequest(Command.DISABLE_MULTICAST));
                            cmdList.Add(new CmdRequest(Command.DISABLE_QUERYING));
                        }
                        break;
                    case "IsDhcpRelay":
                        if (IsDhcpRelay)
                        {
                            if (!string.IsNullOrEmpty(DhcpSrv)) cmdList.Add(new CmdRequest(Command.DHCP_RELAY_DEST, DhcpSrv));
                            cmdList.Add(new CmdRequest(Command.ENABLE_DHCP_RELAY));
                        }
                        else
                        {
                            cmdList.Add(new CmdRequest(Command.DISABLE_DHCP_RELAY));
                        }
                        break;
                    case "Vlans":
                        if (!IsMulticast) break;
                        List<EnableObj> vdiff = (List<EnableObj>)this.Vlans.Except(orig.Vlans).ToList();
                        if (vdiff?.Count > 0)
                        {
                            foreach (var v in vdiff)
                            {
                                cmdList.Add(new CmdRequest(Command.MULTICAST_VLAN, v.Number, v.Enable ? "enable" : "disable"));
                            }
                        }
                        break;
                    case "Slots":
                        List<EnableObj> sdiff = (List<EnableObj>)this.Slots.Except(orig.Slots).ToList();
                        if (sdiff?.Count > 0)
                        {
                            foreach (var s in sdiff)
                            {
                                cmdList.Add(new CmdRequest(Command.START_STOP_POE, s.Number, s.Enable ? "start" : "stop"));
                            }
                        }
                        break;
                }
            }
            return cmdList;
        }

        private List<PropertyInfo> GetChanges(FeatureModel orig)
        {
            List<PropertyInfo> changes = new List<PropertyInfo>();
            if (orig != null)
            {
                var props = this.GetType().GetProperties();
                foreach (var prop in props)
                {
                    object val = prop.GetValue(this, null);
                    if (val?.GetType() == typeof(Boolean))
                    {
                        if ((bool)val != (bool)prop.GetValue(orig, null)) changes.Add(prop);
                    }
                    else if (prop.Name == "Vlans")
                    {
                        List<EnableObj> curr = val as List<EnableObj>;
                        List<EnableObj> old = prop.GetValue(orig, null) as List<EnableObj>;
                        if (CompareVlans(curr, old)) changes.Add(prop);
                    }
                    else if (val != prop.GetValue(orig, null)) changes.Add(prop);
                }
            }
            return changes;
        }

        private bool CompareVlans(List<EnableObj> curr, List<EnableObj> orig)
        {
            if (curr.Count != orig.Count) return false;
            foreach (var v in curr)
            {
                if (orig.FirstOrDefault(o => o.Equals(v)) == null) return true;
            }
            return false;
        }
    }

    public class EnableObj : ICloneable
    {
        public string Number { get; set; }
        public bool Enable { get; set; }

        public EnableObj() { }

        public EnableObj(string number, bool enable)
        {
            Number = number;
            Enable = enable;
        }

        public bool Equals(EnableObj other)
        {
            return (this.Number == other.Number && this.Enable == other.Enable);
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}

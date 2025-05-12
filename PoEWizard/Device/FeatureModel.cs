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
        public List<VlanMC> Vlans { get; set; }

        public FeatureModel() { }

        public FeatureModel(SwitchModel device)
        {
            this.device = device;
            Vlans = new List<VlanMC>();
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
                Vlans = this.Vlans.Select(v => (VlanMC)v.Clone()).ToList()
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
                        List<VlanMC> vdiff = (List<VlanMC>)this.Vlans.Except(orig.Vlans).ToList();
                        if (vdiff?.Count > 0)
                        {
                            foreach (var v in vdiff)
                            {
                                cmdList.Add(new CmdRequest(Command.MULTICAST_VLAN, v.Number, v.Enable ? "enable" : "disable"));
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
                        List<VlanMC> curr = val as List<VlanMC>;
                        List<VlanMC> old = prop.GetValue(orig, null) as List<VlanMC>;
                        if (CompareVlans(curr, old)) changes.Add(prop);
                    }
                    else if (val != prop.GetValue(orig, null)) changes.Add(prop);
                }
            }
            return changes;
        }

        private bool CompareVlans(List<VlanMC> curr, List<VlanMC> orig)
        {
            if (curr.Count != orig.Count) return false;
            foreach (var v in curr)
            {
                if (orig.FirstOrDefault(o => o.Equals(v)) == null) return true;
            }
            return false;
        }
    }

    public class VlanMC : ICloneable
    {
        public string Number { get; set; }
        public bool Enable { get; set; }

        public VlanMC() { }

        public VlanMC(string number, bool enable)
        {
            Number = number;
            Enable = enable;
        }

        public bool Equals(VlanMC other)
        {
            return (this.Number == other.Number && this.Enable == other.Enable);
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}

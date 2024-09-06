using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PoEWizard.Device
{
    public class FeatureModel : ICloneable
    {
        private readonly SwitchModel device;
        
        public bool IsPoe { get; set; } = false;
        public bool NoInsecureProtos { get; set; } = true;
        public bool IsSsh { get; set; } = true;
        public bool IsMulticast { get; set; } = true;
        public bool IsDhcpRelay { get; set; } = false;
        public string DhcpSrv { get; set; }

        public FeatureModel() { }

        public FeatureModel(SwitchModel device)
        {
            this.device = device;
            foreach (var chas in device.ChassisList)
            {
                if (chas != null)
                {
                    foreach (var slot in chas.Slots)
                    {
                        if (slot?.IsInitialized == true)
                        {
                            IsPoe = true;
                            return;
                        }
                    }
                }
            }
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public List<CmdRequest> ToCommandList(FeatureModel orig)
        {
            List<PropertyInfo> changes = GetChanges(orig);
            List<CmdRequest> cmdList = new List<CmdRequest>();
            foreach (var prop in changes)
            {
                switch (prop.Name)
                {
                    case "IsPoe":
                        var cmd = IsPoe ? Command.START_POE : Command.STOP_POE;
                        foreach (var chas in device.ChassisList)
                        {
                            if (chas != null)
                            {
                                cmdList.Add(new CmdRequest(cmd, chas.Number.ToString()));
                            }
                        }
                        break;
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
                            cmdList.Add(new CmdRequest(Command.ENABLE_MULTICAST_VLAN, ""));
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
                }
            }
            return cmdList;
        }

        private List<PropertyInfo> GetChanges(FeatureModel orig)
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

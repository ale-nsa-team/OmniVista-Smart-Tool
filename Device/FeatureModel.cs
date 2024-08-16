using PoEWizard.Data;
using System;
using System.Collections.Generic;

namespace PoEWizard.Device
{
    public class FeatureModel : ICloneable
    {
        private readonly SwitchModel device;
        
        public bool IsPoe { get; set; } = false;
        public bool IsLldp { get; set; } = false;
        public bool IsInsecureProtos { get; set; } = false;
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

        public List<CmdRequest> ToCommandList()
        {
            List<CmdRequest> cmdList = new List<CmdRequest>();
            if (IsPoe)
            {
                foreach (var chas in device.ChassisList)
                {
                    if (chas != null)
                    {
                        cmdList.Add(new CmdRequest(Command.START_POE, chas.Number.ToString()));
                    }
                }
            }

            if (IsLldp) cmdList.Add(new CmdRequest(Command.LLDP_SYSTEM_DESCRIPTION_ENABLE));
            if (IsInsecureProtos)
            {
                cmdList.Add(new CmdRequest(Command.DISABLE_TELNET));
                cmdList.Add(new CmdRequest(Command.DISABLE_FTP));
            }
            if (IsSsh)
            {
                cmdList.Add(new CmdRequest(Command.ENABLE_SSH));
                cmdList.Add(new CmdRequest(Command.SSH_AUTH_LOCAL));
            }
            if (IsMulticast)
            {
                cmdList.Add(new CmdRequest(Command.ENABLE_MULTICAST));
                cmdList.Add(new CmdRequest(Command.ENABLE_QUERYING));
                cmdList.Add(new CmdRequest(Command.ENABLE_MULTICAST_VLAN1));
            }
            if (IsDhcpRelay)
            {
                if (!string.IsNullOrEmpty(DhcpSrv)) cmdList.Add(new CmdRequest(Command.DHCP_RELAY_DEST, DhcpSrv));
                cmdList.Add(new CmdRequest(Command.ENABLE_DHCP_RELAY));
            }
            return cmdList;
        }
    }
}

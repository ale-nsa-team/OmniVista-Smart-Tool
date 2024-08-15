using PoEWizard.Data;
using System.Collections.Generic;

namespace PoEWizard.Device
{
    public class FeatureModel
    {
        private readonly SwitchModel device;
        
        public bool IsPoe { get; set; } = false;
        public bool IsLldp { get; set; } = false;
        public bool IsInsecureProtos { get; set; } = false;
        public bool IsSsh { get; set; } = true;
        public bool IsMulticast { get; set; } = true;
        public bool IsDhcpRelay { get; set; } = false;
        public string DhcpSrv { get; set; }

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

        public List<CmdRequest> ToCommandList()
        {
            List<CmdRequest> cmdList = new List<CmdRequest>();
            if (IsPoe)
            {
                foreach (var chas in device.ChassisList)
                {
                    if (chas != null)
                    {
                        cmdList.Add(new CmdRequest(Command.START_POE, new string[] { chas.Number.ToString() }));
                    }
                }
            }

            if (IsLldp) cmdList.Add(new CmdRequest(Command.LLDP_SYSTEM_DESCRIPTION_ENABLE));
            if (IsInsecureProtos)
            {
                cmdList.Add(Commands.DisableTelnet);
                cmdList.Add(Commands.DisableFtp);
            }
            if (IsSsh)
            {
                cmdList.Add(Commands.SshEnable);
                cmdList.Add(Commands.SshAuthenticationLocal);
            }
            if (IsMulticast) cmdList.AddRange(Commands.EnableMulticast);
            if (IsDhcpRelay)
            {
                if (!string.IsNullOrEmpty(DhcpSrv)) cmdList.Add(Commands.DhcpRelayDest(DhcpSrv));
                cmdList.Add(Commands.DhcpRelayEnable);
            }
            return cmdList;
        }
    }
}

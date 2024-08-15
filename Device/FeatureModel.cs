using PoEWizard.Data;
using System.Collections.Generic;

namespace PoEWizard.Device
{
    public class FeatureModel
    {
        public bool IsPoe { get; set; } = true;
        public bool IsLldp { get; set; } = false;
        public bool IsInsecureProtos { get; set; } = false;
        public bool IsSsh { get; set; } = true;
        public bool IsMulticast { get; set; } = true;
        public bool IsDhcpRelay { get; set; } = false;
        public string DhcpSrv { get; set; }

        public FeatureModel(SwitchModel device)
        {
            
        }

        public List<string> ToCommandList()
        {
            List<string> cmdList = new List<string>();
            if (IsPoe)
            {

            }

            if (IsLldp) cmdList.Add(Commands.EnhanceLldp);
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

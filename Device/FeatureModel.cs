using PoEWizard.Data;
using System.Collections.Generic;
using System.Reflection;

namespace PoEWizard.Device
{
    public class FeatureModel
    {
        //public bool IsPoe { get; set; } = true;
        //public bool IsFastPoe { get; set; } = false;
        public bool IsLldp { get; set; } = false;
        public bool IsInsecureProtos { get; set; } = false;
        public bool IsSsh { get; set; } = true;
        public bool IsMulticast { get; set; } = true;
        public bool IsDhcpRelay { get; set; } = false;
        public string DhcpSrv { get; set; }

        public FeatureModel()
        {
            PropertyInfo[] props = GetType().GetProperties();
        }

        public List<string> ToCommandList()
        {
            List<string> cmdList = new List<string>();
            //if (IsPoe) cmdList.Add(Commands.StartPoE);
            //if (IsFastPoe) cmdList.Add(Commands.EnableFastPoe);
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

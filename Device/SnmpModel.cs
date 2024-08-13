using PoEWizard.Data;
using System.Collections.Generic;
using System.Reflection;

namespace PoEWizard.Device
{
    public class SnmpModel
    {
        private readonly Props config;

        public string Version { get; set; }
        public string User { get; set; }
        public string Community { get; set; }
        public string Password { get; set; }
        public string CfrmPwd { get; set; }
        public string PrivateKey { get; set; }
        public string AuthKey { get; set; }
        public string Protocols { get; set; }
        public string TrapReceiver { get; set; }

        public SnmpModel()
        {
            Version = "v2";
            Protocols = "MD5 + DES";
        }

        public SnmpModel(Props cfg)
        {
            Version = "v2";
            Protocols = "MD5 + DES";

            PropertyInfo[] props = GetType().GetProperties();
            foreach (PropertyInfo p in props)
            {
                string val = cfg.Get(p.Name);
                if (string.IsNullOrEmpty(val)) continue;
                if (p.Name == "Password" || p.Name == "PrivateKey" || p.Name == "AuthKey")
                {
                    p.SetValue(this, Utils.DecryptString(val));
                }
                else
                {
                    p.SetValue(this, val);
                }
            }
            config = cfg;

        }

        public List<string> ToCommandList()
        {
            List<string> cmdList = new List<string>();

            string protos = Protocols.Replace(" ", "").ToLower();
            switch (Version)
            {
                case "v2":
                    if (NotEmpty(User)) cmdList.Add(Commands.SnmpV2User(User, Password, protos));
                    if (NotEmpty(Community))
                    {
                        cmdList.Add(Commands.SnmpCommunityMode);
                        cmdList.Add(Commands.SnmpCommunityMap(Community, User));
                    }
                    break;
                case "v3":
                    if (NotEmpty(User))
                    {
                        cmdList.Add(Commands.SnmpV3User(User, Password, PrivateKey, protos));
                    }
                    break;
            }
            if (NotEmpty(TrapReceiver) && NotEmpty(User))
            {
                cmdList.Add(Commands.SnmpTrapAuth);
                cmdList.Add(Commands.SnmpStation(TrapReceiver, "162", User, Version));
            }
            if (cmdList.Count > 0)
            {
                cmdList.Insert(0, Commands.SnmpAuthLocal);
                cmdList.Insert(1, Commands.SnmpNoSecurity);
            }
            return cmdList;
        }

        private bool NotEmpty(string val)
        {
            return !string.IsNullOrEmpty(val);
        }
    }
}

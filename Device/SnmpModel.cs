using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PoEWizard.Device
{
    public class SnmpModel : ICloneable
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

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public List<CmdRequest> ToCommandList()
        {
            List<CmdRequest> cmdList = new List<CmdRequest>();

            string protos = Protocols.Replace(" ", "").ToLower();
            switch (Version)
            {
                case "v2":
                    if (NotEmpty(User)) cmdList.Add(new CmdRequest(Command.SNMP_V2_USER, User, Password, protos));
                    if (NotEmpty(Community))
                    {
                        cmdList.Add(new CmdRequest(Command.SNMP_COMMUNITY_MODE));
                        cmdList.Add(new CmdRequest(Command.SNMP_COMMUNITY_MAP, Community, User));
                    }
                    break;
                case "v3":
                    if (NotEmpty(User))
                    {
                        cmdList.Add(new CmdRequest(Command.SNMP_V3_USER, User, Password, PrivateKey, protos));
                    }
                    break;
            }
            if (NotEmpty(TrapReceiver) && NotEmpty(User))
            {
                cmdList.Add(new CmdRequest(Command.SNMP_TRAP_AUTH));
                cmdList.Add(new CmdRequest(Command.SNMP_STATION, TrapReceiver, "162", User, Version));
            }
            if (cmdList.Count > 0)
            {
                cmdList.Insert(0, new CmdRequest(Command.SNMP_AUTH_LOCAL));
                cmdList.Insert(1, new CmdRequest(Command.SNMP_NO_SECURITY));
            }
            return cmdList;
        }

        private bool NotEmpty(string val)
        {
            return !string.IsNullOrEmpty(val);
        }
    }
}

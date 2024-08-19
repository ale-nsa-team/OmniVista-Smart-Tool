using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace PoEWizard.Device
{
    public class SnmpModel : ICloneable
    {
        public ObservableCollection<SnmpUser> Users { get; set; }
        public ObservableCollection<SnmpStation> Stations { get; set; }

        public SnmpModel() { }

        public object Clone()
        {
            SnmpModel clone = new SnmpModel();
            clone.Users = new ObservableCollection<SnmpUser>();
            clone.Stations = new ObservableCollection<SnmpStation>();
            foreach (SnmpUser user in Users) clone.Users.Add(user.Clone() as SnmpUser);
            foreach (SnmpStation station in Stations) clone.Stations.Add(station.Clone() as SnmpStation);
            return clone;
        }

        public List<CmdRequest> ToCommandList(SnmpModel orig)
        {
            //List<PropertyInfo> changes = GetChanges(orig);
            //List<CmdRequest> cmdList = new List<CmdRequest>();
            //if (changes.Count == 0) return cmdList;

            //string protos = Protocols.Replace(" ", "").ToLower();
            //foreach (var prop in changes)
            //{
            //    switch (prop.Name)
            //    {
            //        case "User":
            //        case "Password":
            //        case "AuthKey":
            //        case "PrivateKey":
            //            if (NotEmpty(User))
            //            {
            //                if (Version == "v2") cmdList.Add(new CmdRequest(Command.SNMP_V2_USER, User, Password));
            //                else cmdList.Add(new CmdRequest(Command.SNMP_V3_USER, User, Password, PrivateKey, protos));
            //            }
            //            break;
            //        case "Community":
            //            if (NotEmpty(Community) && Version == "v2")
            //            {
            //                cmdList.Add(new CmdRequest(Command.SNMP_COMMUNITY_MODE));
            //                cmdList.Add(new CmdRequest(Command.SNMP_COMMUNITY_MAP, Community, User));
            //            }
            //            break;
            //        case "TrapReceiver":
            //            if (NotEmpty(TrapReceiver) && NotEmpty(User))
            //            {
            //                cmdList.Add(new CmdRequest(Command.SNMP_TRAP_AUTH));
            //                cmdList.Add(new CmdRequest(Command.SNMP_STATION, TrapReceiver, "162", User, Version));
            //            }
            //            break;
            //    }
            //}

            //if (NotEmpty(TrapReceiver) && NotEmpty(User))
            //{
            //    cmdList.Add(new CmdRequest(Command.SNMP_TRAP_AUTH));
            //    cmdList.Add(new CmdRequest(Command.SNMP_STATION, TrapReceiver, "162", User, Version));
            //}
            //if (cmdList.Count > 0)
            //{
            //    cmdList.Insert(0, new CmdRequest(Command.SNMP_AUTH_LOCAL));
            //    cmdList.Insert(1, new CmdRequest(Command.SNMP_NO_SECURITY));
            //}
            //return cmdList;
        }

        private bool NotEmpty(string val)
        {
            return !string.IsNullOrEmpty(val);
        }

        private List<PropertyInfo> GetChanges(SnmpModel orig)
        {
            List<PropertyInfo> changes = new List<PropertyInfo>();
            var props = this.GetType().GetProperties();
            foreach (var prop in props)
            {
                if (prop.GetValue(this, null) != prop.GetValue(orig, null))
                    changes.Add(prop);
            }
            return changes;
        }
    }
}

using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PoEWizard.Device
{
    public class SystemModel : ICloneable
    {
        private readonly SwitchModel device;

        public string MgtIpAddr { get; set; }
        public string NetMask { get; set; }
        public string AdminPwd { get; set; }
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Location { get; set; }

        public SystemModel() { }

        public SystemModel(SwitchModel device)
        {
            MgtIpAddr = device.IpAddress;
            NetMask = device.NetMask;
            AdminPwd = device.Password;
            Name = device.Name;
            Contact = device.Contact;
            Location = device.Location;
            this.device = device;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public List<CmdRequest> ToCommandList(SystemModel orig)
        {
            List<PropertyInfo> changes = GetChanges(orig);
            List<CmdRequest> cmdList = new List<CmdRequest>();

            foreach (var prop in changes)
            {
                if (prop.Name != "IsDateAndTime" && string.IsNullOrEmpty((string)prop.GetValue(this, null))) continue;
                switch (prop.Name)
                {
                    case "MgtIpAddr":
                        cmdList.Add(new CmdRequest(Command.SET_MGT_INTERFACE, MgtIpAddr, NetMask));
                        device.IpAddress = MgtIpAddr;
                        break;
                    case "AdminPwd":
                        cmdList.Add(new CmdRequest(Command.SET_PASSWORD, "admin", AdminPwd));
                        device.Password = AdminPwd;
                        break;
                    case "Name":
                        cmdList.Add(new CmdRequest(Command.SET_SYSTEM_NAME, Name));
                        device.Name = Name;
                        break;
                    case "Contact":
                        cmdList.Add(new CmdRequest(Command.SET_CONTACT, Contact));
                        device.Contact = Contact;
                        break;
                    case "Location":
                        cmdList.Add(new CmdRequest(Command.SET_LOCATION, Location));
                        device.Location = Location;
                        break;
                }
            }
            return cmdList;
        }

        private List<PropertyInfo> GetChanges(SystemModel orig)
        {
            List<PropertyInfo> changes = new List<PropertyInfo>();
            if (orig != null)
            {
                var props = this.GetType().GetProperties();
                foreach (var prop in props)
                {
                    if ((string)prop.GetValue(this, null) != (string)prop.GetValue(orig, null))
                        changes.Add(prop);
                }
            }
            return changes;
        }
    }
}

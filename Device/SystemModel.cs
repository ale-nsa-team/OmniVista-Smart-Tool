using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

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
            NetMask = "255.255.255.0";
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
            string tz = TimeZoneInfo.Local.StandardName;
            string tzabv = Regex.Replace(tz, "[^A-Z]", "");
            string date = DateTime.Now.ToString("MM/dd/yyy");
            string time = DateTime.Now.ToString("HH:mm:ss");

            cmdList.Add(new CmdRequest(Command.DISABLE_AUTO_FABRIC));
            cmdList.Add(new CmdRequest(Command.SET_SYSTEM_TIMEZONE, tzabv));
            cmdList.Add(new CmdRequest(Command.SET_SYSTEM_DATE, date));
            cmdList.Add(new CmdRequest(Command.SET_SYSTEM_TIME, time));
            cmdList.Add(new CmdRequest(Command.ENABLE_DDM));
            foreach (var prop in changes)
            {
                if (string.IsNullOrEmpty((string)prop.GetValue(this, null))) continue;
                switch (prop.Name)
                {
                    case "MtgIpAddr":
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

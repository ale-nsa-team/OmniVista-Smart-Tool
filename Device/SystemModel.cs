using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PoEWizard.Device
{
    public class SystemModel
    {
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
        }

        public List<CmdRequest> ToCommandList()
        {
            List<CmdRequest> cmdList = new List<CmdRequest>();
            string tz = TimeZoneInfo.Local.StandardName;
            string tzabv = Regex.Replace(tz, "[^A-Z]", "");
            string date = DateTime.Now.ToString("MM/dd/yyy");
            string time = DateTime.Now.ToString("HH:mm:ss");

            cmdList.Add(new CmdRequest(Command.DISABLE_AUTO_FABRIC));
            cmdList.Add(new CmdRequest(Command.SET_SYSTEM_TIMEZONE, tzabv));
            cmdList.Add(new CmdRequest(Command.SET_SYSTEM_DATE,date));
            cmdList.Add(new CmdRequest(Command.SET_SYSTEM_TIME, time));
            cmdList.Add(new CmdRequest(Command.ENABLE_DDM));
            if (!string.IsNullOrEmpty(MgtIpAddr)) cmdList.Add(new CmdRequest(Command.SET_MNGT_INTERFACE, MgtIpAddr, NetMask));
            if (!string.IsNullOrEmpty(AdminPwd) && AdminPwd != Constants.DEFAULT_PASSWORD) cmdList.Add(new CmdRequest(Command.SET_PASSWORD, "admin", AdminPwd));
            if (!string.IsNullOrEmpty(Name)) cmdList.Add(new CmdRequest(Command.SET_SYSTEM_NAME));
            if (!string.IsNullOrEmpty(Contact)) cmdList.Add(new CmdRequest(Command.SET_CONTACT, Contact));
            if (!string.IsNullOrEmpty(Location)) cmdList.Add(new CmdRequest(Command.SET_LOCATION,Location));
            return cmdList;
        }
    }
}

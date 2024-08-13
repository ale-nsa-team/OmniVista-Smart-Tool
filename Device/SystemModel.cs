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

        public List<string> ToCommandList()
        {
            List<string> cmdList = new List<string>();
            string tz = TimeZoneInfo.Local.StandardName;
            string tzabv = Regex.Replace(tz, "[^A-Z]", "");
            string date = DateTime.Now.ToString("MM/dd/yyy");
            string time = DateTime.Now.ToString("HH:mm:ss");

            cmdList.Add(Commands.DisableAutoFabric);
            cmdList.Add(Commands.SetSystemTimezone(tzabv));
            cmdList.Add(Commands.SetSystemDate(date));
            cmdList.Add(Commands.SetSystemTime(time));
            cmdList.Add(Commands.DdmEnable);
            if (!string.IsNullOrEmpty(MgtIpAddr)) cmdList.Add(Commands.SetMgtInterface(MgtIpAddr, NetMask));
            if (!string.IsNullOrEmpty(AdminPwd) && AdminPwd != Constants.DEFAULT_PASSWORD) cmdList.Add(Commands.SetPassword("admin", AdminPwd));
            if (!string.IsNullOrEmpty(Name)) cmdList.Add(Commands.SystemName(Name));
            if (!string.IsNullOrEmpty(Contact)) cmdList.Add(Commands.SystemContact(Contact));
            if (!string.IsNullOrEmpty(Location)) cmdList.Add(Commands.SystemLocation(Location));
            return cmdList;
        }
    }
}

using System;

namespace PoEWizard.Device
{
    public class SnmpStation : ICloneable
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public string Status { get; set; }
        public string Version { get; set; }
        public string User {  get; set; }
        public string Community { get;set; }
        
        public SnmpStation() { }

        public SnmpStation(string ip_port, string status, string version, string user)
        {
            string[] parts = ip_port.Split('/');
            if (parts.Length == 2 )
            {
                IpAddress = parts[0];
                Port = int.Parse(parts[1]);
            }
            Status = status;
            Version = version;
            User = user;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}

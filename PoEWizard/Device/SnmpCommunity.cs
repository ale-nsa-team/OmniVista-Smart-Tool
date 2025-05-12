using System;

namespace PoEWizard.Device
{
    public class SnmpCommunity : ICloneable
    {
        public string Name { get; set; }
        public string User {  get; set; }
        public string Status { get; set; }

        public SnmpCommunity() { }

        public SnmpCommunity(string name, string user, string status)
        {
            Name = name;
            User = user;
            Status = status;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}

using System;

namespace PoEWizard.Device
{
    public class SnmpUser : ICloneable
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public string PrivateKey { get; set; }
        public string Version { get; set; }
        public string Protocol { get; set; }
        public string Encryption { get; set; }

        public SnmpUser() { }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}

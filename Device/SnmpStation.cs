using System;

namespace PoEWizard.Device
{
    public class SnmpStation : ICloneable
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }
        public string Status { get; set; }
        public string Version { get; set; }
        public string User { get;set; }
        
        public SnmpStation() { }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}

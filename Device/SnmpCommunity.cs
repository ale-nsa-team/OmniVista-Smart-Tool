using System;

namespace PoEWizard.Device
{
    public class SnmpCommunity : ICloneable
    {
        public string Name { get; set; }
        public string User {  get; set; }
        public string Status { get; set; }

        public SnmpCommunity() { }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}

using System;
using System.Collections.Generic;

namespace DatabaseHelper.Entities
{
    public class PortEntity
    {
        public Guid Id { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }
        public string PortIndex { get; set; }
        public double Poe { get; set; }
        public double Power { get; set; }
        public double MaxPower { get; set; }
        public string Status { get; set; }
        public bool IsPoeON { get; set; }
        public string PriorityLevel { get; set; }
        public bool IsUplink { get; set; }
        public bool IsLldpMdi { get; set; }
        public bool IsLldpExtMdi { get; set; }
        public bool IsVfLink { get; set; }
        public bool Is4Pair { get; set; }
        public bool IsPowerOverHdmi { get; set; }
        public bool IsCapacitorDetection { get; set; }
        public string Protocol8023bt { get; set; }
        public bool IsEnabled { get; set; }
        public string Class { get; set; }
        public string IpAddress { get; set; }
        public string Alias { get; set; }
        public string Violation { get; set; }
        public string Type { get; set; }
        public string InterfaceType { get; set; }
        public string Bandwidth { get; set; }
        public string Duplex { get; set; }
        public string AutoNegotiation { get; set; }
        public string Transceiver { get; set; }
        public string EPP { get; set; }
        public string LinkQuality { get; set; }
        public virtual ICollection<EndpointDeviceEntity> EndPointDevices { get; set; }

        // Foreign key to SlotEntity
        public Guid SlotId { get; set; }
    }
}
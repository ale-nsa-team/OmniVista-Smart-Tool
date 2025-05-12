using System;
using System.Collections.Generic;

namespace DatabaseHelper.Entities
{
    public class EndpointDeviceEntity
    {
        public Guid Id { get; set; }
        public string RemoteId { get; set; }
        public string Vendor { get; set; }
        public string Model { get; set; }
        public string SoftwareVersion { get; set; }
        public string HardwareVersion { get; set; }
        public string SerialNumber { get; set; }
        public string PowerClass { get; set; }
        public string LocalPort { get; set; }
        public string PortSubType { get; set; }
        public string MacAddress { get; set; }
        public string Type { get; set; }
        public string IpAddress { get; set; }
        public string EthernetType { get; set; }
        public string RemotePort { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string PortDescription { get; set; }
        public string MEDPowerType { get; set; }
        public string MEDPowerSource { get; set; }
        public string MEDPowerPriority { get; set; }
        public string MEDPowerValue { get; set; }
        public bool IsMacName { get; set; }
        public string Label { get; set; }
        public ICollection<CapabilityEntity> Capabilities { get; set; }

        // Foreign key to PortEntity
        public Guid PortId { get; set; }
    }
}
using System;

namespace DatabaseHelper.Entities
{
    public class PowerSupplyEntity
    {
        public Guid Id { get; set; }

        public string Name { get; set; }
        public string Model { get; set; }
        public string Type { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string PowerProvision { get; set; }
        public string Status { get; set; }
        public string PartNumber { get; set; }
        public string HardwareRevision { get; set; }
        public string SerialNumber { get; set; }

        // Foreign key to ChassisEntity
        public Guid ChassisId { get; set; }
    }
}
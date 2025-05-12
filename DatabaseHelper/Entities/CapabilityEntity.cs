using System;

namespace DatabaseHelper.Entities
{
    public class CapabilityEntity
    {
        public Guid Id { get; set; }

        public string Value { get; set; }

        // Foreign key to EndPointDeviceEntity
        public Guid EndPointDeviceId { get; set; }
    }
}
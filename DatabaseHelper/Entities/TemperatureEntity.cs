using System;

namespace DatabaseHelper.Entities
{
    public class TemperatureEntity
    {
        public Guid Id { get; set; }

        public string Device { get; set; }
        public int Current { get; set; }
        public string Range { get; set; }
        public int Threshold { get; set; }
        public int Danger { get; set; }
        public string Status { get; set; }

        // Foreign key to ChassisEntity
        public Guid ChassisId { get; set; }
    }
}
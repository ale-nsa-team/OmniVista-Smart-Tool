using System;
using System.Collections.Generic;

namespace DatabaseHelper.Entities
{
    public class SlotEntity
    {
        public Guid Id { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        public int NbPorts { get; set; }
        public int NbPoePorts { get; set; }
        public string PoeStatus { get; set; }
        public double Power { get; set; }
        public double Budget { get; set; }
        public double Threshold { get; set; }
        public bool Is8023btSupport { get; set; }
        public bool IsPoeModeEnable { get; set; }
        public bool IsPriorityDisconnect { get; set; }
        public string FPoE { get; set; }
        public string PPoE { get; set; }
        public string PowerClassDetection { get; set; }
        public bool IsHiResDetection { get; set; }
        public bool IsInitialized { get; set; }
        public bool SupportsPoE { get; set; }
        public bool IsMaster { get; set; }
        public ICollection<PortEntity> Ports { get; set; }

        // Foreign key to ChassisEntity
        public Guid ChassisId { get; set; }
    }
}
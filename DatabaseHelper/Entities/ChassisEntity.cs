using System;
using System.Collections.Generic;

namespace DatabaseHelper.Entities
{
    public class ChassisEntity
    {
        public Guid Id { get; set; }
        public int Number { get; set; }
        public string Model { get; set; }
        public string Type { get; set; }
        public bool IsMaster { get; set; }
        public string AdminStatus { get; set; }
        public string OperationalStatus { get; set; }
        public string Status { get; set; }
        public double PowerBudget { get; set; }
        public double PowerConsumed { get; set; }
        public double PowerRemaining { get; set; }
        public string SerialNumber { get; set; }
        public string PartNumber { get; set; }
        public string HardwareRevision { get; set; }
        public string MacAddress { get; set; }
        public string SwitchTemperature { get; set; }
        public bool SupportsPoE { get; set; }
        public string Fpga { get; set; }
        public string Cpld { get; set; }
        public string Uboot { get; set; }
        public string Onie { get; set; }
        public int Cpu { get; set; }
        public string FlashSize { get; set; }
        public string FlashUsage { get; set; }
        public string FlashSizeUsed { get; set; }
        public string FlashSizeFree { get; set; }
        public string FreeFlash { get; set; }
        public TemperatureEntity Temperature { get; set; }
        public ICollection<PowerSupplyEntity> PowerSupplies { get; set; }
        public ICollection<SlotEntity> Slots { get; set; }

        // Foreign key to SwitchEntity
        public Guid SwitchId { get; set; }
    }
}
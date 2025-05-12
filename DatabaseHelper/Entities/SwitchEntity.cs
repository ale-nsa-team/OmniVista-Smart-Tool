using System;
using System.Collections.Generic;

namespace DatabaseHelper.Entities
{
    public class SwitchEntity
    {
        public Guid Id { get; set; }
        public string IpAddress { get; set; }
        public string Name { get; set; }
        public string NetMask { get; set; }
        public string DefaultGateway { get; set; }
        public string MacAddress { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public int CnxTimeout { get; set; }
        public string Status { get; set; }
        public string Version { get; set; }
        public string SerialNumber { get; set; }
        public string Model { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string Contact { get; set; }
        public string UpTime { get; set; }
        public string RunningDirectory { get; set; }
        public string ConfigSnapshot { get; set; }
        public double Power { get; set; }
        public double Budget { get; set; }
        public string SyncStatus { get; set; }
        public bool SupportsPoE { get; set; }
        public IDictionary<string, SwitchDebugAppEntity> DebugApp { get; set; }
        public ICollection<ChassisEntity> ChassisList { get; set; }
    }
}
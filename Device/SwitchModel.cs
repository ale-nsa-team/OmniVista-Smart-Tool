using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    [Serializable]
    public class SwitchModel
    {
        public string Name { get; set; }

        public string IpAddr { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public SwitchStatus Status { get; set; }
        public int CnxTimeout { get; set; }
        public string Model { get; set; }
        public string Version { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string Contact { get; set; }
        public string Power { get; set; } = "0";
        public string Budget { get; set; } = "0";
        public string UpTime { get; set; }

        public List<ChassisInfo> ChassisList { get; set; }

        public string ReleaseNumber { get; set; }
        public bool IsConnected { get; set; }

        public PowerSupplyState PowerSupplyState => GetPowerSupplyState();

        public SwitchModel()
        {
        }
        public SwitchModel(string ipAddr, string username, string password, int cnxTimeout)
        {
            IpAddr = ipAddr;
            Login = username;
            Password = password;
            CnxTimeout = cnxTimeout;
            IsConnected = false;
        }

        public ChassisInfo GetChassis(int chassisNumber)
        {
            return ChassisList.FirstOrDefault(c => c.Number == chassisNumber);
        }

        private PowerSupplyState GetPowerSupplyState()
        {
            if (ChassisList != null && ChassisList.Count > 0)
            {
                foreach (ChassisInfo chassis in ChassisList)
                {
                    foreach (PowerSupplyInfo ps in chassis.PowerSupplies)
                    {
                        if (ps.Status == PowerSupplyState.Down)
                        {
                            return PowerSupplyState.Down;
                        }
                    }
                }
                return PowerSupplyState.Up;
            }
            return PowerSupplyState.Unknown;
        }

        public void UpdateSwitchUplinks()
        {
            if (this.ChassisList?.Count == 0) return;
            foreach (ChassisInfo chassis in this.ChassisList)
            {
                foreach (SlotInfo slot in chassis.Slots)
                {
                    slot.SwitchPorts?.ToList().ForEach(port =>
                    {
                        port.IsUplink = port.IsLldp || port.IsVfLink;
                    });
                }
            }
        }

        public void UpdateUplink(string portNr, bool isUplink)
        {
            this.ChassisList?.ForEach(c =>
            {
                c.Slots?.ForEach(s =>
                {
                    s.SwitchPorts.ForEach(p =>
                    {
                        string name = $"{c.Number}/{s.Number}/{p.Number}";
                        if (name.Equals(portNr))
                        {
                            p.IsUplink = isUplink;
                            return;
                        }
                    });
                });
            });
        }
    }

}

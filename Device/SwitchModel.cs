using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    [Serializable]
    public class SwitchModel
    {
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string MacAddress { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public SwitchStatus Status { get; set; }
        public int CnxTimeout { get; set; }
        public string Model { get; set; }
        public string Version { get; set; }
        public string RunningDir { get; set; }
        public string SyncStatus { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string SerialNumber { get; set; }
        public string Contact { get; set; }
        public double Power { get; set; } = 0;
        public double Budget { get; set; } = 0;
        public string UpTime { get; set; }
        public List<ChassisModel> ChassisList { get; set; }
        public bool IsConnected { get; set; }
        public PowerSupplyState PowerSupplyState => GetPowerSupplyState();
        public string ConfigSnapshot { get; set; }
        public bool ConfigChanged { get; set; } = false;

        public SwitchModel() : this("", DEFAULT_USERNAME, DEFAULT_PASSWORD, 5) { }

        public SwitchModel(string ipAddr, string username, string password, int cnxTimeout)
        {
            IpAddress = ipAddr;
            Login = username;
            Password = password;
            CnxTimeout = cnxTimeout;
            IsConnected = false;
        }

        public void LoadFromDictionary(Dictionary<string, string> dict, DictionaryType dt)
        {
            string s;
            switch (dt)
            {
                case DictionaryType.System:
                    Name = dict.TryGetValue(NAME, out s) ? s : "";
                    Description = dict.TryGetValue(DESCRIPTION, out s) ? s : "";
                    Location = dict.TryGetValue(LOCATION, out s) ? s : "";
                    Contact = dict.TryGetValue(CONTACT, out s) ? s : "";
                    UpTime = dict.TryGetValue(UP_TIME, out s) ? s : "";
                    break;
                case DictionaryType.RunningDir:
                    RunningDir = Utils.FirstChToUpper(dict.TryGetValue(RUNNING_CONFIGURATION, out s) ? s : "");
                    SyncStatus = Utils.FirstChToUpper(dict.TryGetValue(SYNCHRONIZATION_STATUS, out s) ? s : "");
                    break;
                case DictionaryType.MicroCode:
                    Version = dict.TryGetValue(RELEASE, out s) ? s : "";
                    break;
            }
;
        }

        public void LoadFromList(List<Dictionary<string, string>> list, DictionaryType dt)
        {
            switch (dt)
            {
                case DictionaryType.Chassis:
                    ChassisList = new List<ChassisModel>();
                    foreach (Dictionary<string, string> dict in list)
                    {
                        ChassisModel ci = new ChassisModel(dict);
                        ChassisList.Add(ci);
                        if (ci.IsMaster)
                        {
                            this.Model = ci.Model;
                            this.MacAddress = ci.MacAddress;
                            this.SerialNumber = ci.SerialNumber;
                        }
                    }
                    break;
                case DictionaryType.PortsList:
                    int nchas = list.GroupBy(d => GetChassisId(d)).Count();
                    for (int i = 1; i <= nchas; i++)
                    {
                        List<Dictionary<string, string>> chasList = list.Where(d => GetChassisId(d) == i).ToList();
                        if (chasList?.Count == 0) continue;
                        ChassisModel chas = this.GetChassis(GetChassisId(chasList[0]));
                        int nslots = chasList.GroupBy(c => GetSlotId(c)).Count();
                        for (int j = 1; j <= nslots; j++)
                        {
                            var slot = new SlotModel(chasList[j][CHAS_SLOT_PORT]);
                            List<Dictionary<string, string>> slotList = chasList.Where(c => GetSlotId(c) == j).ToList();
                            slot.NbPorts = slotList.Count;
                            foreach (var dict in slotList)
                            {
                                slot.Ports.Add(new PortModel(dict));
                            }
                            chas.Slots.Add(slot);
                        }
                    }
                    break;
                case DictionaryType.PowerSupply:
                    foreach (var dic in list)
                    {
                        var chas = GetChassis(ParseId(dic[CHAS_PS], 0));
                        if (chas == null) continue;
                        chas.PowerSupplies.Add(new PowerSupplyModel(GetPsId(dic[CHAS_PS]), dic[LOCATION]));
                    }
                    break;
                case DictionaryType.LanPower:
                    break;
                case DictionaryType.LldpRemoteList:
                    foreach (Dictionary<string, string> dict in list)
                    {
                        ChassisSlotPort slotPort = new ChassisSlotPort(dict[LOCAL_PORT]);
                        ChassisModel chassis = GetChassis(slotPort.ChassisNr);
                        if (chassis == null) continue;
                        SlotModel slot = chassis.GetSlot(slotPort.SlotNr);
                        if (slot == null) continue;
                        PortModel port = slot.GetPort(slotPort.PortNr);
                        if (port == null) continue;
                        port.LoadLldpRemoteTable(dict);
                    }
                    break;
                case DictionaryType.MacAddressList:
                    string prevPort = "";
                    foreach (Dictionary<string, string> dict in list)
                    {
                        string currPort = dict.TryGetValue(INTERFACE, out string s) ? s : "";
                        ChassisSlotPort slotPort = new ChassisSlotPort(currPort);
                        ChassisModel chassis = GetChassis(slotPort.ChassisNr);
                        if (chassis == null) continue;
                        SlotModel slot = chassis.GetSlot(slotPort.SlotNr);
                        if (slot == null) continue;
                        PortModel port = slot.GetPort(slotPort.PortNr);
                        if (port == null) continue;
                        if (currPort != prevPort)
                        {
                            port.MacList.Clear();
                            prevPort = currPort;
                        }
                        if (port.AddMacToList(dict) >= 10) break;
                    }
                    break;
            }
        }

        public ChassisModel GetChassis(int chassisNumber)
        {
            return ChassisList.FirstOrDefault(c => c.Number == chassisNumber);
        }

        public ChassisModel GetChassis(string slotName)
        {
            return ChassisList.FirstOrDefault(c => c.Number == ParseId(slotName, 0));
        }

        public SlotModel GetSlot(string slotName)
        {
            ChassisModel cm = GetChassis(slotName);
            return cm?.Slots.FirstOrDefault(s => s.Name == slotName);
        }

        private PowerSupplyState GetPowerSupplyState()
        {
            if (ChassisList != null && ChassisList.Count > 0)
            {
                foreach (ChassisModel chassis in ChassisList)
                {
                    foreach (PowerSupplyModel ps in chassis.PowerSupplies)
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
            foreach (ChassisModel chassis in this.ChassisList)
            {
                foreach (SlotModel slot in chassis.Slots)
                {
                    slot.Ports?.ToList().ForEach(port =>
                    {
                        port.IsUplink = port.IsLldpMdi || port.IsLldpExtMdi || port.IsVfLink;
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
                    s.Ports.ForEach(p =>
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

        public PortModel GetPort(string slotPortNr)
        {
            ChassisSlotPort slotPort = new ChassisSlotPort(slotPortNr);
            ChassisModel chassisModel = ChassisList.FirstOrDefault(c => c.Number == slotPort.ChassisNr);
            if (chassisModel == null) return null;
            SlotModel slotModel = chassisModel.Slots.FirstOrDefault(c => c.Number == slotPort.SlotNr);
            if (slotModel == null) return null;
            return slotModel.Ports.FirstOrDefault(c => c.Number == slotPort.PortNr);
        }

        private int GetChassisId(Dictionary<string, string> chas)
        {
            string[] parts = chas[CHAS_SLOT_PORT].Split('/');
            if (parts.Length < 3) return 1;
            return ParseId(chas[CHAS_SLOT_PORT], 0);
        }

        private int GetSlotId(Dictionary<string, string> chas)
        {
            string[] parts = chas[CHAS_SLOT_PORT].Split('/');
            int idx = (parts.Length > 2) ? 1 : 0;
            return ParseId(chas[CHAS_SLOT_PORT], idx);
        }

        private string GetPortId(string chSlotPort)
        {
            string[] parts = chSlotPort.Split('/');
            return parts.Length > 2 ? parts[2] : "0";
        }

        private int GetPsId(string chId)
        {
            return ParseId(chId, 1);
        }

        private int ParseId(string chSlotPort, int index)
        {
            string[] parts = chSlotPort.Split('/');
            return parts.Length > index ? (int.TryParse(parts[index], out int i) ? i : 0) : 0;
        }
    }
}

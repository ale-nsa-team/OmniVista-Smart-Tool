using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.Utils;

namespace PoEWizard.Device
{
    [Serializable]
    public class SlotModel
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        public int NbPorts { get; set; }
        public int NbPoePorts { get; set; }
        public SlotPoeStatus PoeStatus { get; set; }
        public string PoeStatusLabel => GetLabelPoeStatus();
        public double Power { get; set; }
        public double Budget { get; set; }
        public List<PortModel> Ports { get; set; }
        public double Threshold { get; set; }
        public bool Is8023btSupport {get;set;}
        public bool IsPoeModeEnable { get; set; }
        public bool IsPriorityDisconnect { get; set; }
        public ConfigType FPoE { get; set; }
        public ConfigType PPoE { get; set; }
        public ConfigType PowerClassDetection { get; set; }
        public bool IsHiResDetection { get; set; }
        public bool IsInitialized { get; set; }
        public bool SupportsPoE { get; set; }
        public bool IsMaster { get; set; }
        public string MasterSlave => ConvertMasterSlaveToString();
        public List<TransceiverModel> Transceivers { get; set; }

        public SlotModel() { }

        public SlotModel(ChassisSlotPort chassisSlot, string model)
        {
            this.Number = chassisSlot.SlotNr;
            this.Name = $"{chassisSlot.ChassisNr}/{chassisSlot.SlotNr}";
            this.Ports = new List<PortModel>();
            this.SupportsPoE = true;
            this.IsMaster = true;
            this.Model = model;
        }

        public void LoadFromDictionary(Dictionary<string, string> dict)
        {
            if (dict.ContainsKey(MAX_POWER)) this.Budget = ParseDouble(GetDictValue(dict, MAX_POWER));
            if (dict.ContainsKey(INIT_STATUS)) this.IsInitialized = GetDictValue(dict, INIT_STATUS).ToLower() == "initialized";
            if (MODEL_WITH_8023BT.Contains(this.Model)) this.Is8023btSupport = true;
            else if (dict.ContainsKey(BT_SUPPORT)) this.Is8023btSupport = GetDictValue(dict, BT_SUPPORT) == "Yes";
            this.IsPoeModeEnable = !this.Is8023btSupport;
            if (dict.ContainsKey(CLASS_DETECTION)) this.PowerClassDetection = ConvertToConfigType(dict, CLASS_DETECTION);
            if (dict.ContainsKey(HI_RES_DETECTION)) this.IsHiResDetection = ConvertToConfigType(dict, HI_RES_DETECTION) == ConfigType.Enable;
            if (dict.ContainsKey(PPOE)) this.PPoE = ConvertToConfigType(dict, PPOE);
            if (dict.ContainsKey(FPOE)) this.FPoE = ConvertToConfigType(dict, FPOE);
            if (dict.ContainsKey(USAGE_THRESHOLD)) this.Threshold = ParseDouble(GetDictValue(dict, USAGE_THRESHOLD));
        }

        public void LoadFromList(List<Dictionary<string, string>> list, DictionaryType dt)
        {
            if (dt == DictionaryType.LanPower) this.NbPoePorts = 0;
            foreach (var dict in list)
            {
                string[] split = (dict.TryGetValue((dt == DictionaryType.LanPower) ? PORT : CHAS_SLOT_PORT, out string s) ? s : "0").Split('/');
                string sport = null;
                if (split.Length == 1)
                {
                    sport = $"{this.Name}/{split[0]}";
                }
                else if (split.Length == 3)
                {
                    sport = $"{this.Name}/{split[2]}";
                }
                else continue;
                if (sport == null) continue;
                PortModel port = GetPort(sport);
                if (port == null) continue;
                if (dt == DictionaryType.LanPower)
                {
                    port.LoadPoEData(dict);
                    if (port.Poe != Constants.PoeStatus.NoPoe) this.NbPoePorts++;
                }
                else
                {
                    port.LoadPoEConfig(dict);
                }
            }
            if (!this.IsInitialized)
            {
                this.PoeStatus = SlotPoeStatus.Off;
                return;
            }
            this.Power = Ports.Sum(p => p.Power);
            double powerConsumedMetric = 100 * this.Power / this.Budget;
            double nearThreshold = 0.9 * this.Threshold;
            if (powerConsumedMetric < nearThreshold) this.PoeStatus = SlotPoeStatus.UnderThreshold;
            else if (powerConsumedMetric >= nearThreshold && powerConsumedMetric < this.Threshold) this.PoeStatus = SlotPoeStatus.NearThreshold;
            else this.PoeStatus = SlotPoeStatus.Critical;
        }

        public PortModel GetPort(string slotPortNr)
        {
            return Ports.FirstOrDefault(p => p.Name == slotPortNr);
        }

        private double ParseDouble(string val)
        {
            return double.TryParse(val, out double d) ? d : 0;
        }

        private string GetLabelPoeStatus()
        {
            return GetEnumDescription(this.PoeStatus);
        }

        private string ConvertMasterSlaveToString()
        {
            return this.IsMaster ? "Master" : "Slave";
        }
    }
}

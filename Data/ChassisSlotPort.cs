using System;

namespace PoEWizard.Data
{
    public class ChassisSlotPort
    {
        public int ChassisNr { get; set; } = 0;
        public int SlotNr { get; set; } = 0;
        public string PortNr { get; set; } = string.Empty;

        public ChassisSlotPort(string slotPortNr)
        {
            if (string.IsNullOrEmpty(slotPortNr)) return;
            string[] valuesList = slotPortNr?.Trim().Split('/');
            if (string.IsNullOrEmpty(valuesList[0])) return;
            switch (valuesList.Length)
            {
                case 3:
                    this.ChassisNr = ParseToInt(valuesList[0]);
                    this.SlotNr = ParseToInt(valuesList[1]);
                    this.PortNr = valuesList[2];
                    break;

                case 2:
                    this.ChassisNr = 1;
                    this.SlotNr = ParseToInt(valuesList[0]);
                    PortNr = valuesList[1];
                    break;

                case 1:
                    this.ChassisNr = 1;
                    this.SlotNr = 1;
                    this.PortNr = valuesList[0];
                    break;
            }
        }

        private int ParseToInt(string strVal)
        {
            try
            {
                int.TryParse(strVal, out int intVal);
                return intVal;
            }
            catch { }
            return 0;
        }
    }
}

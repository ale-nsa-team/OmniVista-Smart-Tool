namespace PoEWizard.Device
{
    public class ChassisInfo
    {
        public string SerialNumber { get; set; }
        public string MacAddres { get; set; }
        public string ModelName { get; set; }
        public ChassisInfo(string sn, string mac, string model)
        {
            SerialNumber = sn;
            MacAddres = mac;
            ModelName = model;
        }
        public bool IsOS6x
        {
            get
            {
                return ModelName.StartsWith("OS6350") || ModelName.StartsWith("OS6450");
            }
        }
    }
}

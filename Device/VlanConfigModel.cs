using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.Utils;

namespace PoEWizard.Device
{
    public class VlanModel
    {
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string SubnetMask { get; set; }
        public string Status { get; set; }
        public bool IsForward { get; set; }
        public string Device { get; set; }

        public VlanModel(Dictionary<string, string> dict)
        {
            this.Name = GetDictValue(dict, VLAN_NAME);
            this.IpAddress = GetDictValue(dict, VLAN_IP);
            this.SubnetMask = GetDictValue(dict, VLAN_MASK);
            this.Status = GetDictValue(dict, VLAN_STATUS);
            string fwd = GetDictValue(dict, VLAN_FWD);
            this.IsForward = !string.IsNullOrEmpty(fwd) && fwd.ToUpper() == "YES";
            this.Device = GetDictValue(dict, VLAN_DEVICE);
        }
    }

    public class VlanConfigModel
    {
        public List<VlanModel> VlanList { get; set; }
        public VlanConfigModel(List<Dictionary<string, string>> dictList)
        {
            this.VlanList = new List<VlanModel>();
            foreach (Dictionary<string, string> dict in dictList)
            {
                this.VlanList.Add(new VlanModel(dict));
            }
        }
    }
}

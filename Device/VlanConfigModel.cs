using System.Collections.Generic;

namespace PoEWizard.Device
{

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

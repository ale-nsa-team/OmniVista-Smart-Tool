using System.Collections.Generic;

namespace PoEWizard.Device
{
    public class SlotView
    {
        public List<SlotModel> Slots { get; }

        public SlotView(SwitchModel device) 
        {
            Slots = new List<SlotModel>();
            foreach (var chas in device.ChassisList)
            {
                foreach (var slot in chas.Slots)
                {
                    Slots.Add(slot);
                }
            }
        
        }
    }
}

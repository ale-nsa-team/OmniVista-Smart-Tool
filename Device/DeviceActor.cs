using PoEWizard.Comm;
using System;

namespace PoEWizard.Device
{
    public class DeviceActor : CmdActor
    {
        public Func<CmdActor, string, CmdActor> GetNext { get; set; }

        public DeviceActor(Func<CmdActor, string, CmdActor> getNext)
        {
            GetNext = getNext;
        }
        public override CmdActor DoNext(CmdActor actor, string data)
        {
            return GetNext(actor, data);
        }
    }
}

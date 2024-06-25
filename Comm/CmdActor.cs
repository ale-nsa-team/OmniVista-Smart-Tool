using System.Threading;

namespace PoEWizard.Comm
{
    public class CmdActor
    {
        public CmdRequest Request { get; set; } = new CmdRequest();
        public CmdActor Previous { get; set; }
        public virtual CmdActor Next { get; set; }
        private int loopAvoidance = 0;

        public virtual CmdActor DoNext(CmdActor activeActor, string data)
        {
            if (Request.Match(data))
            {
                loopAvoidance = 0;
                return Next;
            }
            if (loopAvoidance++ < 100)
            {
                Thread.Sleep(10);
                return activeActor;
            }
            else
            {
                loopAvoidance = 0;
                return null;
            }
        }

        public override string ToString()
        {
            return GetType().Name + "=" + Request.ToString();
        }
    }
}

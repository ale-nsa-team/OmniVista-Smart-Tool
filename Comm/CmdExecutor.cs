namespace PoEWizard.Comm
{
    public class CmdExecutor : CmdActor
    {
        protected CmdConsumer consumer;
        public const byte CTRL_C = 0x3;
        public override CmdActor Next { get; set; }

        public CmdConsumer Consumer
        {
            get
            {
                if (consumer == null)
                {
                    consumer = new CmdConsumer(this);
                }
                return consumer;
            }
        }
        public CmdConsumer Send(string data)
        {
            Request.Cmd = data;
            Next = Consumer;
            return Consumer;
        }
        public CmdConsumer Send(byte[] data)
        {
            Request.Data = data;
            Next = Consumer;
            return Consumer;
        }
        public CmdConsumer Send(string data, int timeout)
        {
            Request.Cmd = data;
            Request.Wait = timeout;
            Next = Consumer;
            return Consumer;
        }
        public CmdConsumer Send(byte[] data, int timeout)
        {
            Request.Data = data;
            Request.Wait = timeout;
            Next = Consumer;
            return Consumer;
        }
        public CmdConsumer Wait(int timeout)
        {
            Request.Wait = timeout;
            Next = Consumer;
            return Consumer;
        }
        public CmdConsumer CtrlBreak()
        {
            Request.Data = new byte[] { CTRL_C };
            Next = Consumer;
            return Consumer;
        }
        public CmdConsumer Enter()
        {
            Request.Cmd = "\n";
            Next = Consumer;
            return Consumer;
        }
    }
}

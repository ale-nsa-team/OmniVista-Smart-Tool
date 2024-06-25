namespace PoEWizard.Comm
{
    public class CmdConsumer : CmdExecutor
    {
        private CmdActor aBranch;
        private CmdResponder responder;
        private static readonly ResultCallback DUMMY_CALLBACK = new ResultCallback(result => { }, error => { });
        public override CmdActor Next { get; set; }
        public CmdResponder Responder
        {
            get
            {
                if (responder == null)
                {
                    responder = new CmdResponder(this);
                }
                return responder;
            }
        }
        public CmdConsumer(CmdActor previous) : base()
        {
            Previous = previous;
        }

        public void Consume(ResultCallback callback)
        {
            Next = null;
            CmdActor root = FindRoot(Previous);
            ComCommander.Execute(root, callback);
        }

        public void Consume()
        {
            Next = null;
            CmdActor root = FindRoot(Previous);
            ComCommander.Execute(root, DUMMY_CALLBACK);
        }

        public CmdConsumer Custom(CmdActor aBranch)
        {
            this.aBranch = aBranch;
            Next = Consumer;
            return Consumer;
        }

        public override CmdActor DoNext(CmdActor activeActor, string data)
        {
            CmdActor aNext = null;
            if (aBranch != null)
            {
                aNext = aBranch.DoNext(activeActor, data);
            }
            return aNext ?? base.DoNext(activeActor, data);
        }

        public CmdResponder Response()
        {
            CmdResponder theNext = Responder;
            Previous.Next = theNext;
            theNext.Previous = Previous;
            Next = Responder;
            return Responder;
        }

        private CmdActor FindRoot(CmdActor cmdActor)
        {
            CmdActor rootActor = cmdActor.Previous;
            if (rootActor == null)
            {
                return cmdActor;
            }
            while (rootActor.Previous != null)
            {
                rootActor = rootActor.Previous;
            }
            return rootActor;
        }
    }
}

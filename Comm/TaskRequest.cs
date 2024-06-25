namespace PoEWizard.Comm
{
    public class TaskRequest
    {
        public CmdActor Actor { get; set; }
        public ResultCallback Callback { get; set; }

        public TaskRequest(CmdActor cmdActor, ResultCallback callBack)
        {
            Actor = cmdActor;
            Callback = callBack;
        }
        public DataMatch GetMatch(CmdActor actor)
        {
            //find the macthing criteria somewhere inside the command chain
            CmdActor act = actor;
            while (act != null)
            {
                DataMatch dm = act.Request.DataMatch;
                if (!string.IsNullOrEmpty(dm?.Data)) return dm;
                act = act.Next;
            }
            return null;
        }
    }
}

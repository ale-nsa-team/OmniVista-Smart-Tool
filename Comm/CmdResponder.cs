using static PoEWizard.Data.Constants;

namespace PoEWizard.Comm
{
    public class CmdResponder : CmdActor
    {
        private CmdConsumer consumer;

        public CmdConsumer Consumer
        {
            get
            {
                if (consumer == null)
                {
                    consumer = new CmdConsumer(this);
                    Next = consumer;
                }
                return consumer;
            }
        }

        public CmdResponder(CmdActor previous)
        {
            Previous = previous;
        }

        public CmdConsumer Contains(string data)
        {
            SetConstraint(data, MatchOperation.Contains);
            return Consumer;
        }

        public CmdConsumer StartsWith(string data)
        {
            SetConstraint(data, MatchOperation.StartsWith);
            return Consumer;
        }

        public CmdConsumer EndsWith(string data)
        {
            SetConstraint(data, MatchOperation.EndsWith);
            return Consumer;
        }

        public CmdConsumer Equals(string data)
        {
            SetConstraint(data, MatchOperation.Equals);
            return Consumer;
        }

        public CmdConsumer Regex(string data)
        {
            SetConstraint(data, MatchOperation.Regex);
            return Consumer;
        }
        private void SetConstraint(string data, MatchOperation constraintOperation)
        {
            Request.DataMatch = new DataMatch
            {
                Match = constraintOperation,
                Data = data
            };
        }
    }
}

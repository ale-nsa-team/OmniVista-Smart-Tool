using System;

namespace PoEWizard.Comm
{
    public class ResultCallback
    {
        public Action<string> OnData { get; set; }

        public Action<string> OnError { get; set; }

        public ResultCallback(Action<string> onData, Action<string> onError)
        {
            OnError = onError ?? throw new ArgumentException(nameof(onError));
            OnData = onData ?? throw new ArgumentException(nameof(onData));
        }
    }
}

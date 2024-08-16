using System;
using System.Runtime.Serialization;

namespace PoEWizard.Exceptions
{
    [Serializable]
    public class SwitchConnectionFailure : Exception
    {
        public SwitchConnectionFailure(string message)
            : base(message)
        { }

        protected SwitchConnectionFailure(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        { }
    }

    [Serializable]
    public class SwitchConnectionDropped : Exception
    {
        public SwitchConnectionDropped(string message)
            : base(message)
        { }

        protected SwitchConnectionDropped(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        { }
    }

    [Serializable]
    public class SwitchAuthenticationFailure : Exception
    {
        public SwitchAuthenticationFailure(string message)
            : base(message)
        { }

        protected SwitchAuthenticationFailure(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        { }
    }

    [Serializable]
    public class SwitchLoginFailure : Exception
    {
        public SwitchLoginFailure(string message)
            : base(message)
        { }

        protected SwitchLoginFailure(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        { }
    }

    [Serializable]
    public class SwitchRejectConnection : Exception
    {
        public SwitchRejectConnection(string message)
            : base(message)
        { }

        protected SwitchRejectConnection(SerializationInfo info, StreamingContext ctxt)
            : base(info, ctxt)
        { }
    }

    [Serializable]
    public class SwitchCommandError : Exception
    {
        public SwitchCommandError(string message)
            : base(message)
        { }
    }

    [Serializable]
    public class SwitchCommandNotSupported : Exception
    {
        public SwitchCommandNotSupported(string message)
            : base(message)
        { }
    }

    [Serializable]
    public class InvalidSwitchCommandResult : Exception
    {
        public InvalidSwitchCommandResult(string message)
            : base(message)
        { }
    }

    [Serializable]
    public class TaskCanceled : Exception
    {
        public TaskCanceled(string message)
            : base(message)
        { }
    }

}

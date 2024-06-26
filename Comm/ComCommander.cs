using PoEWizard.Data;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Comm
{
    public static class ComCommander
    {
        private const int BUFFER_LENGTH = 16384;
        public static int Timeout { get; set; } = 15000;
        //public static int Timeout { get; set; } = 600000;
        private static CmdTask executeTask;
        private static readonly LockingStringBuilder results = new LockingStringBuilder();
        private static readonly LockingStringBuilder errors = new LockingStringBuilder();
        private static readonly LockingStringBuilder buffer = new LockingStringBuilder(BUFFER_LENGTH);
        public static RestApiService ComService { get; set; }

        public static void Quit()
        {
            ComService.Close();
        }

        public static void Execute(CmdActor cmdActor, ResultCallback callBack)
        {
            if (ComService?.Callback == null) //in case callback is hijacked by the terminal
            {
                ComService.Callback = new ResultCallback(
                result =>
                {
                    buffer.Append(result);
                },
                error =>
                {
                    if (!errors.ToString().Contains(error)) errors.AppendLine(error);
                }
            );
            }

            executeTask = new CmdTask();
            executeTask.Execute(new TaskRequest(cmdActor, callBack));
        }

        class LockingStringBuilder
        {
            private readonly StringBuilder sb;
            private readonly object dataLock = new object();
            public int Length
            {
                get
                {
                    lock (dataLock) { return sb.Length; }
                }
            }
            public LockingStringBuilder()
            {
                sb = new StringBuilder();
            }
            public LockingStringBuilder(int capacity)
            {
                sb = new StringBuilder(capacity);
            }
            public void Append(string value)
            {
                lock (dataLock) { sb.Append(value); }
            }
            public void AppendLine(string value)
            {
                lock (dataLock) { sb.AppendLine(value); }
            }
            public void Clear()
            {
                lock (dataLock) { sb.Clear(); }
            }
            public override string ToString()
            {
                lock (dataLock) { return sb.ToString(); }
            }

        }
        class CmdTask
        {
            public ReturnData Execute(TaskRequest request)
            {
                CmdActor activeActor = request.Actor;
                results.Clear();
                errors.Clear();
                buffer.Clear();
                bool dataReceived = false;

                while (activeActor != null)
                {
                    CmdRequest nextCmd = activeActor.Request;

                    if (ExecuteCmd(activeActor))
                    {
                        int timeout = nextCmd.Wait > 0 ? Timeout + nextCmd.Wait : Timeout;
                        long before = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        DataMatch criteria = request.GetMatch(activeActor);

                        while (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - before < timeout)
                        {
                            if (IsExpectedResponse(buffer.ToString().Trim(), criteria) || buffer.Length > BUFFER_LENGTH)
                            {
                                results.Append(buffer.ToString());
                                Logger.Debug($"Response: {buffer}");
                                buffer.Clear();
                                dataReceived = true;
                                break;
                            }
                            Thread.Sleep(50);
                        }
                        if (!dataReceived)
                        { //timedout
                            Logger.Debug($"Failed to match \"{buffer.ToString().Trim()}\"\nwith {criteria.Match}({criteria.Data.Replace("\n", "\\n")})");
                            if (errors.Length == 0) errors.AppendLine("Timeout"); //not mixing timeout with other errors.
                            ComService.Write(new byte[] { CmdExecutor.CTRL_C });
                            break;
                        }
                        dataReceived = false;
                    }
                    activeActor = activeActor.DoNext(activeActor, results.ToString());
                }

                if (errors.Length > 0) request.Callback.OnError(errors.ToString());
                else request.Callback.OnData(results.ToString());
                return new ReturnData(results.ToString(), errors.ToString());
            }
            private bool ExecuteCmd(CmdActor actor)
            {
                string cmd = actor.Request.Cmd;
                byte[] data = actor.Request.Data;
                if (cmd != null)
                {
                    if (!cmd.Equals("y", StringComparison.OrdinalIgnoreCase)
                        && !cmd.Equals("n", StringComparison.OrdinalIgnoreCase)
                        && !cmd.Equals(" ")
                        && !cmd.EndsWith("\n"))
                    {
                        cmd += "\n";
                    }
                    data = Encoding.ASCII.GetBytes(cmd);
                }
                if (data != null)
                {
                    ComService.Write(data);
                    //check if there is a wait after the command
                    //next cannot be another command, only a standalone Wait
                    //because a command + wait (when calling .Send(cmd, timeout))
                    //is only used to increase the receive timeout, not to sleep.
                    CmdRequest next = actor.Next.Request;
                    if (next.Data == null && next.Cmd == null && next.Wait > 0)
                    {
                        Thread.Sleep(next.Wait);
                    }
                    return true;
                }
                return false;
            }
            private bool IsExpectedResponse(string text, DataMatch criteria)
            {
                if (string.IsNullOrEmpty(text)) return false;
                if (criteria == null) return true;
                string pattern = criteria.Data.Trim();
                switch (criteria.Match)
                {
                    case MatchOperation.Equals:
                        return text.Equals(pattern);
                    case MatchOperation.EndsWith:
                        return text.EndsWith(pattern);
                    case MatchOperation.StartsWith:
                        return text.StartsWith(pattern);
                    case MatchOperation.Contains:
                        return text.Contains(pattern);
                    case MatchOperation.Regex:
                        return Regex.IsMatch(text, pattern);
                }
                return false;
            }
        }
    }
}

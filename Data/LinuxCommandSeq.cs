using System;
using System.Collections.Generic;
using System.Linq;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.RestUrl;

namespace PoEWizard.Data
{
    public class LinuxCommand
    {
        private const int DEFAULT_WAIT_TIME_SEC = 10;
        public string Command { get; set; }
        public int DelaySec { get; set; }
        public int MaxWaitSec { get; set; }
        public string Expected {  get; set; }
        public Dictionary<string, string> Response { get; set; }

        public LinuxCommand(string cmd) : this(cmd, null, DEFAULT_WAIT_TIME_SEC, 0) { }
        public LinuxCommand(string cmd, string expected) : this(cmd, expected, DEFAULT_WAIT_TIME_SEC, 0) { }
        public LinuxCommand(string cmd, string expected, int maxWaitSec) : this(cmd, expected, maxWaitSec, 0) { }
        public LinuxCommand(string cmd, string expected, int maxWaitSec, int delaySec)
        {
            this.Command = cmd;
            this.DelaySec = delaySec;
            this.MaxWaitSec = maxWaitSec;
            this.Expected = expected;
            this.Response = new Dictionary<string, string> { [REST_URL] = "Run linux command", [ERROR] = string.Empty, [DURATION] = string.Empty };
        }
    }

    public class LinuxCommandSeq
    {
        public List<LinuxCommand> CommandSeq { get; set; }
        public DateTime StartTime { get; set; }
        public string Duration { get; set; }

        public LinuxCommandSeq() : this(new List<LinuxCommand>()) { }
        public LinuxCommandSeq(List<LinuxCommand> cmdSeq) 
        {
            this.StartTime = DateTime.Now;
            this.CommandSeq = cmdSeq;
        }

        public LinuxCommandSeq(LinuxCommand linuxCmd)
        {
            this.CommandSeq = new List<LinuxCommand> { linuxCmd };
        }

        public void AddCommandSeq(List<LinuxCommand> cmdSeq)
        {
            if (this.CommandSeq == null) this.CommandSeq = new List<LinuxCommand>();
            this.CommandSeq.AddRange(cmdSeq);
        }

        public Dictionary<string, string> GetResponse(string cmd)
        {
            LinuxCommand linuxCmd = this.CommandSeq?.FirstOrDefault(d => d.Command == cmd);
            return linuxCmd?.Response;
        }
    }
}

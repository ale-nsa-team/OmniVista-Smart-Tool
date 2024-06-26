
using PoEWizard.Comm;
using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    public class PoE
    {
        private readonly IProgress<ProgressReport> progress;
        private readonly string prompt;

        public List<string> PortList { get; private set; }
        public string PortRange { get; private set; }
        public List<PoeModel> Status { get; private set; }

        public PoE(DeviceModel device, IProgress<ProgressReport> progress)
        {
            prompt = DEFAULT_PROMPT;
            this.progress = progress;
            PortList = new List<string>();
        }

        public void GetStatus()
        {
            try
            {
                string result = RunCommand(Commands.ShowPoeStatus);
                List<Dictionary<string, string>> ports = CliParseUtils.ParseHTable(result);
                PortList = ports.Select(p => p["Port"]).ToList();
                if (PortList[0].Contains("/"))
                {
                    string[] lasPortParts = PortList[PortList.Count - 1].Split('/');
                    PortRange = PortList[0] + "-" + lasPortParts[lasPortParts.Length - 1];
                }
                else
                {
                    PortRange = "1"; //aos 6 crap
                }
                Status = ports.Select(p => new PoeModel(p)).ToList();
            }
            catch (Exception ex)
            {
                progress.Report(new ProgressReport(ReportType.Error, "PoE", ex.Message));
            }

        }

        public bool Enable(int index, bool? isEnable)
        {
            try
            {
                string port = PortList[index];
                if (!port.Contains("/")) port = $"1/{port}"; // aos 6 crap
                string cmd = isEnable == true ? Commands.EnablePoe(port) : Commands.DisablePoe(port);
                string result = RunCommand(cmd);
                return true;
            }
            catch (Exception ex)
            {
                progress.Report(new ProgressReport(ReportType.Error, "PoE", ex.Message));
                return false;
            }
        }

        public bool EnableAll(bool? isEnable)
        {
            try
            {
                string cmd = isEnable == true ? Commands.EnablePoe(PortRange) : Commands.DisablePoe(PortRange);
                _ = RunCommand(cmd);
                return true;
            }
            catch (Exception ex)
            {
                progress.Report(new ProgressReport(ReportType.Error, "PoE", ex.Message));
                return false;
            }
        }

        public bool SetPriority(int index, string priority)
        {
            try
            {
                string port = PortList[index];
                if (!port.Contains("/")) port = $"1/{port}"; // aos 6 crap
                string cmd = Commands.SetPoePriority(port, priority);
                _ = RunCommand(cmd);
                return true;
            }
            catch (Exception ex)
            {
                progress.Report(new ProgressReport(ReportType.Error, "PoE", ex.Message));
                return false;
            }
        }

        public bool HasChanges(List<PoeModel> orig)
        {
            for (int i = 0; i < Status.Count; i++)
            {
                if (!Status[i].Equals(orig[i])) return true;
            }
            return false;
        }

        public void WriteMemory()
        {
            try
            {
                _ = RunCommand(Commands.WriteMemoryFlashSync, 30000);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to write memory: {ex.Message}");
                progress.Report(new ProgressReport(ReportType.Error, "Write Memory", $"Operation failed: {ex.Message}"));
            }
        }

        private string RunCommand(string command, int timeout = 5000)
        {
            bool hasError = false;
            string response = null;
            CmdExecutor executor = new CmdExecutor();
            CmdConsumer sender = executor.CtrlBreak().Response().EndsWith(prompt);
            sender.Send(command, timeout).Response().EndsWith(prompt)
            .Consume(new ResultCallback(result =>
            {
                string resultErrors = CliParseUtils.GetErrors(result);
                if (string.IsNullOrEmpty(resultErrors))
                {
                    response = result;
                }
                else
                {
                    response = resultErrors;
                    hasError = true;
                }
            }, error =>
            {
                hasError = true;
                response = error;
            }));
            return !hasError ? response : throw new Exception(response);
        }
    }
}

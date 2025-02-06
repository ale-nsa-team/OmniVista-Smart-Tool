using PoEWizard.Comm;
using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace PoEWizard.Device
{
    public static class IpScan
    {
        private const string PING_SCRIPT = "PoEWizard.Resources.ping.py";
        private const string REM_PATH = Constants.PYTHON_DIR + "ping.py";
        private const int SCAN_TIMEOUT = 4*60*1000;
        private static SwitchModel model;
        private static SftpService sftpSrv;

        public async static Task LaunchScan(SwitchModel swModel)
        {
            try
            {
                model = swModel;
                string err = OpenScp();
                if (err != null) throw new Exception(err);
                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(PING_SCRIPT))
                {
                    if (sftpSrv.IsConnected && sftpSrv.UploadStream(resource, REM_PATH, true))
                    {
                        sftpSrv.Disconnect();
                        await RunScript();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error running IP address scan", ex);
            }
        }

        private static string OpenScp()
        {
            if (sftpSrv == null)
            {
                sftpSrv = new SftpService(model.IpAddress, model.Login, model.Password);
            }
            return sftpSrv.Connect();
        }

        private async static Task RunScript()
        {
            AosSshService ssh = new AosSshService(model);
            try
            {
                Task<List<Dictionary<string, string>>> task = Task<List<Dictionary<string, string>>>.Factory.StartNew(() =>
                {
                    ssh.ConnectSshClient();
                    Dictionary<string, string> resp = ssh.SendCommand(new RestUrlEntry(Command.RUN_PYTHON_SCRIPT, new string[] { REM_PATH }), SCAN_TIMEOUT);
                    resp = ssh.SendCommand(new RestUrlEntry(Command.SHOW_ARP), null);
                    ssh.DisconnectSshClient();
                    ssh.Dispose();
                    return CliParseUtils.ParseHTable(resp["output"]);
                });
                if (await Task.WhenAny(task, Task.Delay(SCAN_TIMEOUT)) == task)
                {
                    List<Dictionary<string, string>> arp = task.Result;
                    model.LoadIPAdrressFromList(arp);
                }
                else
                {
                    Logger.Error($"Timeout waiting for ip address scan on switch {model.Name}");
                }
            }
            catch (Exception ex) 
            {
                Logger.Error($"Could not scan IP addresses on switch {model.Name}", ex);
            }
        }
    }
}

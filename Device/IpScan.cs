using PoEWizard.Comm;
using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace PoEWizard.Device
{
    public static class IpScan
    {
        private const string PING_SCRIPT = "PoEWizard.Resources.installers_toolkit_helper.py";
        private const string REM_PATH = Constants.PYTHON_DIR + "installers_toolkit_helper.py";
        private const int SCAN_TIMEOUT = 4 * 60 * 1000;
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
                    if (sftpSrv.IsConnected)
                    {
                        long filesize = 0L;
                        int count = 0;
                        sftpSrv.UploadStream(resource, REM_PATH, true);
                        while (filesize < resource.Length && count < 3)
                        {                          
                            Thread.Sleep(2000);
                            filesize = sftpSrv.GetFileSize(REM_PATH);
                            count++;
                        }

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
                    if (resp["output"].Contains("Err")) Logger.Error($"Failed to run ip scan on switch {model.Name}: {resp["output"]}");
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

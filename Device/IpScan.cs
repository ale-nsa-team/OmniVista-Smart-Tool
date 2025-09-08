using PoEWizard.Comm;
using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PoEWizard.Device
{
    public static class IpScan
    {
        private const string PING_SCRIPT = "PoEWizard.Resources." + Constants.HELPER_SCRIPT_FILE_NAME;
        private const string REM_PATH = Constants.PYTHON_DIR + Constants.HELPER_SCRIPT_FILE_NAME;
        private const int SCAN_TIMEOUT = 290 * 1000;
        private const int PORT_TIMEOUT = 2500;
        private static SwitchModel model;
        private static SftpService sftpService;
        private static AosSshService sshService;

        public static void Init(SwitchModel model)
        {
            IpScan.model = model;
            sshService = new AosSshService(model);
            sftpService = new SftpService(model.IpAddress, model.Login, model.Password);
            sshService.ConnectSshClient();
            sftpService.Connect();
        }

        public static void Disconnect()
        {
            sshService.DisconnectSshClient();
            sftpService.Disconnect();
        }

        public async static Task LaunchScan()
        {
            try
            {
                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(PING_SCRIPT))
                {
                    if (sftpService.IsConnected)
                    {
                        long filesize = 0L;
                        int count = 0;
                        Logger.Activity($"Uploading python script to {REM_PATH} on {model.Name}");
                        sftpService.UploadStream(resource, REM_PATH, true);
                        while (filesize < resource.Length && count < 3)
                        {
                            Thread.Sleep(2000);
                            filesize = sftpService.GetFileSize(REM_PATH);
                            count++;
                        }
                        if (filesize == resource.Length)
                            Logger.Activity($"Uploading complete, filesize: {filesize / 1024} KB");
                        else
                            Logger.Error("Failed to upload python script to switch: timeout");
                        await RunScript();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error running IP address scan", ex);
            }
        }

        public static async Task<bool> IsIpScanRunning()
        {
            LinuxCommand suCmd = new LinuxCommand("su", "Entering maintenance shell");
            LinuxCommand psCmd = new LinuxCommand("ps -e | grep python", "#->");
            LinuxCommand exitCmd = new LinuxCommand("exit");
            Task<bool> task = Task<bool>.Factory.StartNew(() =>
            {
                try
                {
                    sshService.SendLinuxCommand(suCmd);
                    psCmd.Response = sshService.SendLinuxCommand(psCmd);
                    sshService.SendLinuxCommand(exitCmd);
                    string output = psCmd?.Response["output"] ?? "";
                    return output.Contains(REM_PATH);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error checking if IP address scan is running", ex);
                    sshService.SendLinuxCommand(exitCmd);
                    return false;
                }
            });
            return await task;
        }

        public static int GetOpenPort(string host)
        {

            foreach (int port in Constants.portsToScan)
            {
                Logger.Trace($"Begin checking host {host} port {port}");

                if (IsPortOpen(host, port))
                {
                    Logger.Trace($"{host}:{port} is open");
                    return port;
                }
                else
                {
                    Logger.Trace($"{host}:{port} is not open");
                }
            }
            return 0;
        }

        public static bool IsPortOpen(string host, int port)
        {
            if (string.IsNullOrEmpty(host)) return false;
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { Blocking = true })
                {
                    var result = socket.BeginConnect(host, port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(PORT_TIMEOUT, false);
                    if (success) socket.EndConnect(result);
                    socket.Close();
                    return success;
                }
            }
            catch (SocketException)
            {
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Error checking port open", ex);
                return false;
            }
        }

        private async static Task RunScript()
        {
            try
            {
                Logger.Activity($"Launching python script on {model.Name}");
                Task<List<Dictionary<string, string>>> task = Task<List<Dictionary<string, string>>>.Factory.StartNew(() =>
                {
                    Dictionary<string, string> resp = sshService.SendCommand(new RestUrlEntry(Command.RUN_PYTHON_SCRIPT, new string[] { REM_PATH }), SCAN_TIMEOUT);
                    if (resp["output"].Contains("Err")) Logger.Error($"Failed to run ip scan on switch {model.Name}: {resp["output"]}");
                    resp = sshService.SendCommand(new RestUrlEntry(Command.SHOW_ARP), null);
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

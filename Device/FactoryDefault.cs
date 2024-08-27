using PoEWizard.Comm;
using PoEWizard.Data;
using System;
using System.Collections.Generic;

namespace PoEWizard.Device
{
    public static class FactoryDefault
    {
        private static RestApiService restSvc;
        public static IProgress<ProgressReport> Progress { get; set; }

        private static readonly List<string> files = new List<string>()
        {
            "/flash/working/vcboot.cfg",
            "/flash/certified/vcboot.cfg",
            "/flash/working/vcboot.cfg.sav",
            "/flash/certified/vcboot.cfg.sav",
            "/flash/working/*.txt",
            "/flash/*.err",
            "/flash/*.cfg",
            "/flash/system/userTable*",
            "/flash/working/*.cfg-ft",
            "/flash/working/Udiag.img",
            "/flash/working/Urescue.img",
            "/flash/working/*.img-2sec",
            "/flash/switch/cloud/*",
            "/flash/working/cloudagent.cfg",
            "/flash/certified/cloudagent.cfg",
            "/flash/switch/dhcpd.conf",
            "/flash/switch/dhcpd.conf.lastgood",
            "/flash/switch/dhcpd.pcy",
            "/flash/libcurl_log",
            "/flash/libcurl_log.1"
        };

        public static void Reset(SwitchModel device)
        {
            Progress.Report(new ProgressReport());
            SftpService sftp = new SftpService(device.IpAddress, "admin", device.Password);
            sftp.Connect();
            foreach (string file in files)
            {
                sftp.DeleteFile(file);
            }
            //setup mgt vlan
            restSvc = MainWindow.restApiService;
            restSvc.RunSwitchCommand(new CmdRequest(Command.DISABLE_AUTO_FABRIC));
            restSvc.RunSwitchCommand(new CmdRequest(Command.ENABLE_DDM));
            restSvc.RunSwitchCommand(new CmdRequest(Command.ENABLE_MGT_VLAN));
            restSvc.RunSwitchCommand(new CmdRequest(Command.SET_MGT_INTERFACE, device.IpAddress));
            restSvc.RunSwitchCommand(new CmdRequest(Command.ENABLE_SPAN_TREE));
            restSvc.RunSwitchCommand(new CmdRequest(Command.SET_SYSTEM_NAME, device.IpAddress));
            restSvc.RunSwitchCommand(new CmdRequest(Command.SET_LOOPBACK_DET));
            restSvc.RebootSwitch(600);
        }
    }
}

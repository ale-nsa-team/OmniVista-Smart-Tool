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
            "/flash/working/*.cfg",
            "/flash/certified/*.cfg",
            "/flash/working/*.conf",
            "/flash/certified/*.conf",
            "/flash/working/*.sav",
            "/flash/certified/*.sav",
            "/flash/working/*.md5",
            "/flash/certified/*.md5",
            "/flash/working/*.txt",
            "/flash/certified/*.txt",
            "/flash/working/*.cfg-ft",
            "/flash/certified/*.cfg-ft",
            "/flash/working/Udiag.img",
            "/flash/working/Urescue.img",
            "/flash/certified/Udiag.img",
            "/flash/certified/Urescue.img",
            "/flash/*.err",
            "/flash/*.cfg",
            "/flash/tech*",
            "/flash/swlog_archive/*",
            "/flash/system/user*",
            "/flash/switch/cloud/*",
            "/flash/switch/dhcpd*",
            "/flash/switch/*.txt",
            "/flash/libcurl*",
            "/flash/agcmm*",
            "/flash/working/pkg/*",
            "/flash/certified/pkg/*",
            "/flash/pmd",
            "/flash/serial.txt",
            "/flash/rcl.log"
        };

        public static void Reset(SwitchModel device)
        {
            Progress.Report(new ProgressReport("Removing config files..."));
            SftpService sftp = new SftpService(device.IpAddress, "admin", device.Password);
            sftp.Connect();
            foreach (string file in files)
            {
                sftp.DeleteFile(file);
            }
            restSvc = MainWindow.restApiService;
            restSvc.RunSwitchCommand(new CmdRequest(Command.CLEAR_SWLOG));
            sftp.DeleteFile("/flash/.bash_history");
            //Progress.Report(new ProgressReport("setting up management interface..."));
            //setup mgt vlan
            //restSvc.RunSwitchCommand(new CmdRequest(Command.DISABLE_AUTO_FABRIC));
            //restSvc.RunSwitchCommand(new CmdRequest(Command.ENABLE_DDM));
            //restSvc.RunSwitchCommand(new CmdRequest(Command.ENABLE_MGT_VLAN));
            //restSvc.RunSwitchCommand(new CmdRequest(Command.SET_MGT_INTERFACE, device.IpAddress, device.NetMask));
            //restSvc.RunSwitchCommand(new CmdRequest(Command.ENABLE_SPAN_TREE));
            //restSvc.RunSwitchCommand(new CmdRequest(Command.SET_SYSTEM_NAME, device.IpAddress));
            //restSvc.RunSwitchCommand(new CmdRequest(Command.SET_LOOPBACK_DET));
            sftp.Disconnect();
            restSvc.RebootSwitch(600);
        }
    }
}

using System.Collections.Generic;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    public static class Commands
    {
        private static bool isAos8 = false;

        public static AosVersion Version
        {
            get => isAos8 ? AosVersion.V8 : AosVersion.V6;
            set => isAos8 = value == AosVersion.V8;
        }

        public static string ApplyConfig => $"configuration apply {HAS_BEEN_APPLIED_CONFIG_FLAG}";
        public static string CatStartEoF => "cat <<EOF >";
        public static string CdToSwitchDir => "cd /flash/switch";
        public static string CdToWorkingDir => "cd /flash/working";
        public static string CopyWorkingCertified => isAos8 ? $"copy running certified {FLASH_SYNCHRO}" : $"copy working certified {FLASH_SYNCHRO}";
        public static string DdmEnable => "interfaces ddm enable";
        public static string DefaultGateway(string gateway) => $"ip static-route 0.0.0.0/0 gateway {gateway}";
        public static string DhcpRelayDest(string dest) => $"ip dhcp relay destination {dest}";
        public static string DhcpRelayEnable => "ip dhcp relay admin-state enable";
        public static string DhcpServerDisable => "dhcp-server disable";
        public static string DhcpServerEnable => "dhcp-server enable";
        public static string DhcpServerRestart => "dhcp-server restart";
        public static string DisableAutoFabric => "auto-fabric admin-state disable";
        public static string DisableIpServices => isAos8 ? "ip service all admin-state disable" : "no ip service all";
        public static string DisableFtp => "ip service ftp admin-state disable";
        public static string DisableQos => "qos disable";
        public static string DisableTelnet => "ip service telnet admin-state disable";
        public static string DnsLookup => "ip domain-lookup";
        public static string DnsDomain(string domain) => $"ip domain-name {domain}";
        public static string DnsServer(string server) => $"ip name-server {server}";
        public static string EnablePoe(string port) => isAos8 ? $"lanpower port {port} admin-state enable" : $"lanpower start {port}";
        public static string DisablePoe(string port) => isAos8 ? $"lanpower port {port} admin-state disable" : $"lanpower stop {port}";
        public static string EnableFastPoe => "lanpower slot 1/1 fpoe enable";
        public static string SetPoePriority(string port, string priority) => isAos8 ? $"lanpower port {port} priority {priority}" : $"lanpower {port} priority {priority}";
        public static List<string> EnableMulticast => new List<string>()
        {
            "ip multicast admin-state enable",
            "ip multicast querying enable",
            "ip multicast querier-forwarding enable",
            "ip multicast vlan 1 admin-state enable"
        };
        public static string EnableQos => "qos enable";
        public static string EnableTdr(string port, bool extended)
        {
            return $"interfaces {port} {(extended ? " tdr-extended-test-start" : (isAos8 ? " tdr enable" : " tdr-test-start"))}";
        }
        public static string EnhanceLldp => "lldp nearest-bridge chassis tlv management port-description enable system-name enable system-description enable";
        public static string Exit => "exit";
        public static string Interfaces => "interfaces ";
        public static string LogEnable => "command-log enable";
        public static string LsApplyConfigFlag => isAos8 ? "ls -la " + FLASH_WORKING_DIR + " | grep " + HAS_BEEN_APPLIED_CONFIG_FLAG_NAME : "ls " + FLASH_WORKING_DIR;
        public static string LsErrorFlag => isAos8 ? "ls -la /flash | grep " + HAS_BEEN_APPLIED_CONFIG_FLAG_NAME : "ls /flash";
        public static string NoPortSecurity(string port) => isAos8 ? $"no port-security port {port}" : $"no port-security {port}";
        public static string NoPortSecurityRange => isAos8 ? "no port-security port 1/1/1-" : "no port-security 1/1-";
        public static string NoSshAuthentication => "no aaa authentication ssh";
        public static string NtpServer(string server) => $"ntp server {server}";
        public static string NtpEnable => "ntp client admin-state enable";
        public static string PolicyPortGroupUserPorts => isAos8 ? "policy port group UserPorts 1/1/1-" : "policy port group UserPorts 1/1-";
        public static string PortSecurityChassisConvertToStatic = "port-security chassis convert-to-static";
        public static string PortSecurityRange => isAos8 ? "port-security port 1/1/1-" : "port-security 1/1-";
        public static string PortSecurityEnable(string port)
        {
            return isAos8 ? $"port-security port {port} admin-state enable" : $"port-security {port} admin-status enable";
        }
        public static string PortSecurityLock(string port)
        {
            return isAos8 ? $"port-security port {port} admin-state locked" : $"port-security {port} admin-status locked";
        }
        public static string PortSecurityConvertToStatic(string port)
        {
            return isAos8 ? $"port-security port {port} convert-to-static" : $"port-security {port} convert-to-static";
        }
        public static string QosApply => "qos apply";
        public static string QosUserPortShutdownSpoofBpduDhcpServer = "qos user-port shutdown bpdu vrrp dhcp-server pim dvmrp";
        public static string ReloadFromWorking => isAos8 ? "reload from working no rollback-timeout" : "reload working no rollback-timeout";
        public static string RemoveDhcpCfg => isAos8 ? "rm -f " + FLASH_SWITCH_DIR + DHCPD_CONF : "rm " + FLASH_SWITCH_DIR + DHCPD_CONF;
        public static string RemoveDhcpPcy => isAos8 ? "rm -f " + FLASH_SWITCH_DIR + DHCPD_PCY : "rm " + FLASH_SWITCH_DIR + DHCPD_PCY;
        public static string RemoveConfigFlag => isAos8 ? "rm -f " + HAS_BEEN_APPLIED_CONFIG_FLAG : "rm " + HAS_BEEN_APPLIED_CONFIG_FLAG;
        public static string SessionTimeout => isAos8 ? "session cli timeout " : "session timeout cli ";
        public static string SetPassword(string user, string pwd) => $"user {user} password \"{pwd}\"";
        public static string SetMgtInterface(string ipAddr, string mask) => $"ip interface \"MGT\" address {ipAddr} mask {mask} vlan 1";
        public static string SetSystemDate(string date) => $"system date {date}";
        public static string SetSystemTime(string time) => $"system time {time}";
        public static string SetSystemTimezone(string tz) => $"system timezone {tz}";
        public static string ShowAaaAuthentication => "show aaa authentication";
        public static string ShowChassis => "show chassis";
        public static string ShowCmm => "show cmm";
        public static string ShowFan => "show fan";
        public static string ShowHealth => "show health ";
        public static string ShowHealthConfig => isAos8 ? "show health configuration" : "show health threshold";
        public static string ShowHealthThreshold => "show health threshold";
        public static string ShowInterfaces => "show interfaces ";
        public static string ShowInterfaceStatus => "show interfaces status";
        public static string ShowIpService => "show ip service";
        public static string ShowMacLearning => isAos8 ? "show mac-learning" : "show mac-address-table";
        public static string ShowMicrocode => "show microcode";
        public static string ShowPoeStatus => isAos8 ? "show lanpower slot 1/1" : "show lanpower 1";
        public static string ShowPortHealth => isAos8 ? "show health port " : "show health ";
        public static string ShowPortMacLearning(string port) => isAos8 ? $"show mac-learning port {port}" : $"show mac-address-table {port}";
        public static string ShowPortSecurity(string port) => isAos8 ? $"show port-security port {port}" : $"show port-security {port}";
        public static string ShowSlotSecurity => isAos8 ? "show port-security slot 1/1" : "show port-security 1";
        public static string ShowRunningDir => "show running-directory";
        public static string ShowSessionConfig => "show session config";
        public static string ShowSnapshot => "show configuration snapshot";
        public static string ShowSnapShotQos => "show configuration snapshot qos";
        public static string ShowStackTopology => isAos8 ? "debug show virtual-chassis topology" : "show stack topology";
        public static string ShowSystem => "show system";
        public static string SnmpAuthLocal => "aaa authentication snmp local";
        public static string SnmpCommunityMode => "snmp community mode enable";
        public static string SnmpCommunityMap(string community, string user) => $"snmp community-map {community} user {user} enable";
        public static string SnmpNoSecurity => "snmp security no-security";
        public static string SnmpStation(string ip, string port, string user, string version) => $"snmp station {ip} {port} {user} {version} enable";
        public static string SnmpTrapAuth => "snmp authentication-trap enable";
        public static string SnmpV2User(string user, string pwd, string protocols) => $"user {user} password {pwd} read-write all {protocols}";
        public static string SnmpV3User(string user, string pwd, string privPwd, string protocols) => $"user {user} password {pwd} read-write all {protocols} priv-password {privPwd}";
        public static string SshAuthenticationLocal => "aaa authentication ssh local";
        public static string SshEnable => isAos8 ? "ip service ssh admin-state enable" : "ip service ssh";
        public static string StartPoE => isAos8 ? "lanpower slot 1/1 service start" : "lanpower start 1";
        public static string SystemName(string name) => $"system name \"{name}\"";
        public static string SystemContact(string contact) => $"system contact \"{contact}\"";
        public static string SystemLocation(string location) => $"system location \"{location}\"";
        public static string WriteMemoryFlashSync => $"write memory {FLASH_SYNCHRO}";
        public static string WriteMemory => "write memory";
    }
}

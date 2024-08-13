using System.Collections.Generic;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    public static class Commands
    {
        public static string DdmEnable => "interfaces ddm enable";
        public static string DefaultGateway(string gateway) => $"ip static-route 0.0.0.0/0 gateway {gateway}";
        public static string DhcpRelayDest(string dest) => $"ip dhcp relay destination {dest}";
        public static string DhcpRelayEnable => "ip dhcp relay admin-state enable";
        public static string DhcpServerDisable => "dhcp-server disable";
        public static string DhcpServerEnable => "dhcp-server enable";
        public static string DhcpServerRestart => "dhcp-server restart";
        public static string DisableAutoFabric => "auto-fabric admin-state disable";
        public static string DisableIpServices => "ip service all admin-state disable";
        public static string DisableFtp => "ip service ftp admin-state disable";
        public static string DisableQos => "qos disable";
        public static string DisableTelnet => "ip service telnet admin-state disable";
        public static string DnsLookup => "ip domain-lookup";
        public static string DnsDomain(string domain) => $"ip domain-name {domain}";
        public static string DnsServer(string server) => $"ip name-server {server}";
        public static List<string> EnableMulticast => new List<string>()
        {
            "ip multicast admin-state enable",
            "ip multicast querying enable",
            "ip multicast querier-forwarding enable",
            "ip multicast vlan 1 admin-state enable"
        };
        public static string EnableQos => "qos enable";
        public static string EnhanceLldp => "lldp nearest-bridge chassis tlv management port-description enable system-name enable system-description enable";
        public static string LogEnable => "command-log enable";
        public static string NoSshAuthentication => "no aaa authentication ssh";
        public static string NtpServer(string server) => $"ntp server {server}";
        public static string NtpEnable => "ntp client admin-state enable";
        public static string PolicyPortGroupUserPorts => "policy port group UserPorts 1/1/1-";
        public static string QosApply => "qos apply";
        public static string QosUserPortShutdownSpoofBpduDhcpServer = "qos user-port shutdown bpdu vrrp dhcp-server pim dvmrp";
        public static string SetPassword(string user, string pwd) => $"user {user} password \"{pwd}\"";
        public static string SetMgtInterface(string ipAddr, string mask) => $"ip interface \"MGT\" address {ipAddr} mask {mask} vlan 1";
        public static string SetSystemDate(string date) => $"system date {date}";
        public static string SetSystemTime(string time) => $"system time {time}";
        public static string SetSystemTimezone(string tz) => $"system timezone {tz}";
        public static string SnmpAuthLocal => "aaa authentication snmp local";
        public static string SnmpCommunityMode => "snmp community mode enable";
        public static string SnmpCommunityMap(string community, string user) => $"snmp community-map {community} user {user} enable";
        public static string SnmpNoSecurity => "snmp security no-security";
        public static string SnmpStation(string ip, string port, string user, string version) => $"snmp station {ip} {port} {user} {version} enable";
        public static string SnmpTrapAuth => "snmp authentication-trap enable";
        public static string SnmpV2User(string user, string pwd, string protocols) => $"user {user} password {pwd} read-write all {protocols}";
        public static string SnmpV3User(string user, string pwd, string privPwd, string protocols) => $"user {user} password {pwd} read-write all {protocols} priv-password {privPwd}";
        public static string SshAuthenticationLocal => "aaa authentication ssh local";
        public static string SshEnable =>"ip service ssh admin-state enable";
        public static string SystemName(string name) => $"system name \"{name}\"";
        public static string SystemContact(string contact) => $"system contact \"{contact}\"";
        public static string SystemLocation(string location) => $"system location \"{location}\"";
        public static string WriteMemoryFlashSync => $"write memory {FLASH_SYNCHRO}";
    }
}

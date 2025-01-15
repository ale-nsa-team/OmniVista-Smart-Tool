using PoEWizard.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.Utils;

namespace PoEWizard.Data
{
    public class RestUrl
    {

        #region Public Constants
        public const string REST_URL = "REQUEST_URL";
        public const string RESULT = "RESULT";
        public const string RESPONSE = "RESPONSE";
        public const string DURATION = "DURATION";
        public const string API_ERROR = "error";
        public const string API_NODE_ERROR = "node";
        public const string STRING = "string";
        public const string OUTPUT = "output";
        public const string DATA = "data";
        public const string NODE = "node";
        public const string HTTP_RESPONSE = "diag";
        #endregion

        #region Private Constants
        private const string DAT = "%_DATA_%";
        #endregion

        #region Command Table
        public readonly static Dictionary<Command, string> CMD_TBL = new Dictionary<Command, string>
        {
            #region Basic Connect Switch commands
            [Command.SHOW_MICROCODE] = "show microcode",
            [Command.SHOW_CMM] = "show cmm",
            [Command.DEBUG_SHOW_APP_LIST] = "urn=systemSwitchLoggingApplicationTable&mibObject0=systemSwitchLoggingApplicationAppId&mibObject2=systemSwitchLoggingApplicationAppName&fllterDuplicateIndex=systemSwitchLoggingApplicationAppId&litmit=100&limit=200&ignoreError=true", // 130
            #endregion

            #region Refresh Switch commands
            [Command.SHOW_SYSTEM] = "show system",
            [Command.SHOW_RUNNING_DIR] = "show running-directory",
            [Command.SHOW_CHASSIS] = "show chassis",
            [Command.SHOW_PORTS_LIST] = "show interfaces alias",
            [Command.SHOW_POWER_SUPPLIES] = $"show powersupply",
            [Command.SHOW_POWER_SUPPLY] = $"show powersupply chassis-id {DAT}",
            [Command.SHOW_LAN_POWER] = $"show lanpower slot {DAT}",
            [Command.SHOW_CHASSIS_LAN_POWER_STATUS] = $"show lanpower chassis {DAT} status",
            [Command.SHOW_SLOT] = $"show slot {DAT}",
            [Command.SHOW_MAC_LEARNING] = $"show mac-learning domain vlan",
            [Command.SHOW_TEMPERATURE] = $"show temperature",
            [Command.SHOW_HEALTH] = $"show health all cpu",
            [Command.SHOW_LAN_POWER_CONFIG] = $"show lanpower slot {DAT} port-config",
            [Command.SHOW_LLDP_LOCAL] = "show lldp local-port",
            [Command.SHOW_LLDP_REMOTE] = "show lldp remote-system",
            [Command.POWER_CLASS_DETECTION_ENABLE] = $"lanpower slot {DAT} class-detection enable",
            [Command.SHOW_SLOT_LAN_POWER_STATUS] = $"show lanpower slot {DAT} status",
            [Command.LLDP_SYSTEM_DESCRIPTION_ENABLE] = "lldp nearest-bridge chassis tlv management port-description enable system-name enable system-description enable",
            [Command.LLDP_ADDRESS_ENABLE] = "lldp nearest-bridge chassis tlv management management-address enable",
            [Command.SHOW_HEALTH_CONFIG] = "show health configuration",
            [Command.SHOW_LLDP_INVENTORY] = "show lldp remote-system med inventory",
            [Command.SHOW_SYSTEM_RUNNING_DIR] = "urn=chasControlModuleTable&mibObject0=sysName&mibObject1=sysLocation&mibObject2=sysContact&mibObject3=sysUpTime&mibObject4=sysDescr&mibObject5=configChangeStatus&mibObject6=chasControlCurrentRunningVersion&mibObject7=chasControlCertifyStatus",
            [Command.SHOW_FREE_SPACE] = $"freespace /{FLASH_DIR}",
            [Command.SHOW_LAN_POWER_FEATURE] = $"show lanpower slot {DAT} {DAT}",
            #endregion

            #region PoE Wizard commands
            [Command.POWER_DOWN_PORT] = $"lanpower port {DAT} admin-state disable",
            [Command.POWER_UP_PORT] = $"lanpower port {DAT} admin-state enable",
            [Command.POWER_PRIORITY_PORT] = $"lanpower port {DAT} priority {DAT}",
            [Command.POWER_4PAIR_PORT] = $"lanpower port {DAT} 4pair enable",
            [Command.POWER_2PAIR_PORT] = $"lanpower port {DAT} 4pair disable",
            [Command.POWER_DOWN_SLOT] = $"lanpower slot {DAT} service stop",
            [Command.POWER_UP_SLOT] = $"lanpower slot {DAT} service start",
            [Command.POWER_823BT_ENABLE] = $"lanpower slot {DAT} 8023bt enable",
            [Command.POWER_823BT_DISABLE] = $"lanpower slot {DAT} 8023bt disable",
            [Command.POWER_HDMI_ENABLE] = $"lanpower port {DAT} power-over-hdmi enable",
            [Command.POWER_HDMI_DISABLE] = $"lanpower port {DAT} power-over-hdmi disable",
            [Command.LLDP_POWER_MDI_ENABLE] = $"lldp nearest-bridge port {DAT} tlv dot3 power-via-mdi enable",
            [Command.LLDP_POWER_MDI_DISABLE] = $"lldp nearest-bridge port {DAT} tlv dot3 power-via-mdi disable",
            [Command.LLDP_EXT_POWER_MDI_ENABLE] = $"lldp nearest-bridge port {DAT} tlv med ext-power-via-mdi enable",
            [Command.LLDP_EXT_POWER_MDI_DISABLE] = $"lldp nearest-bridge port {DAT} tlv med ext-power-via-mdi disable",
            [Command.POE_FAST_ENABLE] = $"lanpower slot {DAT} fpoe enable",
            [Command.POE_PERPETUAL_ENABLE] = $"lanpower slot {DAT} ppoe enable",
            [Command.SHOW_PORT_MAC_ADDRESS] = $"show mac-learning port {DAT}",
            [Command.SHOW_PORT_STATUS] = $"show interfaces port {DAT} alias",
            [Command.SHOW_PORT_POWER] = $"show lanpower slot {DAT}|grep {DAT}",
            [Command.SHOW_PORT_LLDP_REMOTE] = $"show lldp port {DAT} remote-system",
            [Command.POE_FAST_DISABLE] = $"lanpower slot {DAT} fpoe disable",
            [Command.POE_PERPETUAL_DISABLE] = $"lanpower slot {DAT} ppoe disable",
            [Command.SET_MAX_POWER_PORT] = $"lanpower port {DAT} power {DAT}",
            [Command.CAPACITOR_DETECTION_ENABLE] = $"lanpower port {DAT} capacitor-detection enable",
            [Command.CAPACITOR_DETECTION_DISABLE] = $"lanpower port {DAT} capacitor-detection disable",
            [Command.ETHERNET_ENABLE] = $"interfaces {DAT} admin-state enable",
            [Command.ETHERNET_DISABLE] = $"interfaces {DAT} admin-state disable",
            [Command.SHOW_INTERFACE_PORT] = $"show interfaces port {DAT}",
            #endregion

            #region General Switch commands
            [Command.WRITE_MEMORY] = "write memory flash-synchro",
            [Command.SHOW_CONFIGURATION] = "show configuration snapshot",
            [Command.SHOW_HW_INFO] = "show hardware-info",
            [Command.REBOOT_SWITCH] = "reload from working no rollback-timeout",
            [Command.UPDATE_UBOOT] = $"update uboot cmm all file {DAT}",
            #endregion

            #region Traffic Analysis commands
            [Command.SHOW_INTERFACES] = "show interfaces",
            [Command.SHOW_DDM_INTERFACES] = "show interfaces ddm",
            #endregion

            #region TDR commands
            [Command.ENABLE_TDR] = $"interfaces port {DAT} tdr enable",
            [Command.SHOW_TDR_STATISTICS] = $"show interfaces port {DAT} tdr-statistics",
            [Command.CLEAR_TDR_STATISTICS] = $"clear interfaces {DATA} tdr-statistics",
            #endregion

            #region Switch Debug Log commands
            [Command.DEBUG_SHOW_LAN_POWER_STATUS] = $"debug show lanpower slot {DAT} status ni",
            [Command.DEBUG_CREATE_LOG] = "show tech-support eng complete",
            [Command.DEBUG_SHOW_LEVEL] = $"urn=systemSwitchLoggingApplicationTable&startIndex={DAT}&limit={DAT}&mibObject0=systemSwitchLoggingApplicationAppId&mibObject1=systemSwitchLoggingApplicationSubAppId&mibObject2=systemSwitchLoggingApplicationSubAppVrfLevelIndex&mibObject3=systemSwitchLoggingApplicationAppName&mibObject4=systemSwitchLoggingApplicationSubAppName&mibObject6=systemSwitchLoggingApplicationSubAppLevel&ignoreError=true",
            [Command.DEBUG_SHOW_LPNI_LEVEL] = $"urn=systemSwitchLoggingApplicationTable&startIndex={DAT}&limit=3&mibObject0=systemSwitchLoggingApplicationAppId&mibObject1=systemSwitchLoggingApplicationSubAppId&mibObject2=systemSwitchLoggingApplicationSubAppVrfLevelIndex&mibObject3=systemSwitchLoggingApplicationAppName&mibObject4=systemSwitchLoggingApplicationSubAppName&mibObject6=systemSwitchLoggingApplicationSubAppLevel&ignoreError=true",
            [Command.DEBUG_SHOW_LPCMM_LEVEL] = $"urn=systemSwitchLoggingApplicationTable&startIndex={DAT}&limit=4&mibObject0=systemSwitchLoggingApplicationAppId&mibObject1=systemSwitchLoggingApplicationSubAppId&mibObject2=systemSwitchLoggingApplicationSubAppVrfLevelIndex&mibObject3=systemSwitchLoggingApplicationAppName&mibObject4=systemSwitchLoggingApplicationSubAppName&mibObject6=systemSwitchLoggingApplicationSubAppLevel&ignoreError=true",
            [Command.DEBUG_UPDATE_LPNI_LEVEL] = "urn=systemSwitchLogging&setIndexForScalar=true",
            [Command.DEBUG_UPDATE_LPCMM_LEVEL] = "urn=systemSwitchLogging&setIndexForScalar=true",
            [Command.DEBUG_UPDATE_LLDPNI_LEVEL] = "urn=systemSwitchLogging&setIndexForScalar=true",
            [Command.DEBUG_CLI_UPDATE_LPNI_LEVEL] = $"swlog appid lpni subapp all level {DAT}",
            [Command.DEBUG_CLI_UPDATE_LPCMM_LEVEL] = $"swlog appid lpcmm subapp all level {DAT}",
            [Command.DEBUG_CLI_SHOW_LPNI_LEVEL] = "show swlog appid lpni",
            [Command.DEBUG_CLI_SHOW_LPCMM_LEVEL] = "show swlog appid lpcmm",
            #endregion

            #region Config Wizard commands
            [Command.DISABLE_AUTO_FABRIC] = "auto-fabric admin-state disable",
            [Command.ENABLE_DDM] = "interfaces ddm enable",
            [Command.ENABLE_MULTICAST] = "ip multicast admin-state enable",
            [Command.ENABLE_QUERYING] = "ip multicast querying enable",
            [Command.DISABLE_MULTICAST] = "ip multicast admin-state disable",
            [Command.DISABLE_QUERYING] = "ip multicast querying disable",
            [Command.ENABLE_QUERIER_FWD] = "ip multicast querier-forwarding enable",
            [Command.MULTICAST_VLAN] = $"ip multicast vlan {DAT} admin-state {DAT}",
            [Command.ENABLE_DHCP_RELAY] = "ip dhcp relay admin-state enable",
            [Command.DISABLE_DHCP_RELAY] = "ip dhcp relay admin-state disable",
            [Command.DHCP_RELAY_DEST] = $"ip dhcp relay destination {DAT}",
            [Command.DISABLE_FTP] = "ip service ftp admin-state disable",
            [Command.DISABLE_TELNET] = "ip service telnet admin-state disable",
            [Command.ENABLE_TELNET] = "ip service telnet admin-state enable",
            [Command.ENABLE_FTP] = "ip service ftp admin-state enable",
            [Command.TELNET_AUTH_LOCAL] = "aaa authentication telnet local",
            [Command.FTP_AUTH_LOCAL] = "aaa authentication ftp local",
            [Command.ENABLE_SSH] = "ip service ssh admin-state enable",
            [Command.DISABLE_SSH] = "ip service ssh admin-state disable",
            [Command.SSH_AUTH_LOCAL] = "aaa authentication ssh local",
            [Command.SYSTEM_TIMEZONE] = $"system timezone {DAT}",
            [Command.SET_SYSTEM_NAME] = $"system name {DAT}",
            [Command.SET_LOOPBACK_DET] = "loopback-detection enable",
            [Command.SET_PASSWORD] = $"user {DAT} password {DAT}",
            [Command.SET_CONTACT] = $"system contact {DAT}",
            [Command.SET_LOCATION] = $"system location \"{DAT}\"",
            [Command.SET_DEFAULT_GATEWAY] = $"ip static-route 0.0.0.0/0 gateway {DAT}",
            [Command.SET_PORT_ALIAS] = "urn=ifXTable",
            [Command.SHOW_IP_SERVICE] = "show ip service",
            [Command.SHOW_DNS_CONFIG] = "urn=systemDNS&mibObject0=systemDNSEnableDnsResolver&mibObject1=systemDNSDomainName&mibObject2=systemDNSNsAddr1&mibObject3=systemDNSNsAddr2&mibObject4=systemDNSNsAddr3",
            [Command.SHOW_DHCP_CONFIG] = "urn=alaDhcpRelayGlobalConfig&mibObject0=alaDhcpRelayAdminStatus&mibObject1=alaDhcpRelayForwardDelay&mibObject2=alaDhcpRelayMaximumHops&mibObject3=alaDhcpRelayPxeSupport&mibObject4=alaDhcpRelayInsertAgentInformation&mibObject5=alaDhcpRelayInsertAgentInformationPolicy&mibObject6=alaDhcpRelayPerInterfaceMode",
            [Command.SHOW_DHCP_RELAY] = "urn=alaDhcpRelayServerDestinationTable&mibObject0=alaDhcpRelayServerDestinationAddressType&mibObject1=alaDhcpRelayServerDestinationAddress&limit=200&ignoreError=true",
            [Command.SHOW_NTP_CONFIG] = "urn=alaNtpPeerTable&mibObject0=alaNtpPeerAddressType&mibObject1=alaNtpPeerAddress&mibObject2=alaNtpPeerInetAddress&mibObject3=alaNtpPeerInetAddressType&mibObject4=alaNtpPeerType&mibObject5=alaNtpPeerAuth&mibObject6=alaNtpPeerVersion&mibObject7=alaNtpPeerMinpoll&mibObject8=alaNtpPeerMaxpoll&mibObject9=alaNtpPeerPrefer&mibObject10=alaNtpPeerName&mibObject11=alaNtpPeerPreempt&mibObject12=alaNtpPeerBurst&mibObject13=alaNtpPeerIBurst&mibObject14=alaNtpPeerAdmin&limit=200&ignoreError=true",
            [Command.SHOW_NTP_STATUS] = "show ntp status",
            [Command.SHOW_IP_INTERFACE] = "show ip interface",
            [Command.SHOW_IP_ROUTES] = "show ip routes",
            [Command.SHOW_MULTICAST_GLOBAL] = "show ip multicast",
            [Command.SHOW_MULTICAST_VLAN] = $"show ip multicast vlan {DAT}",
            [Command.DNS_LOOKUP] = "ip domain-lookup",
            [Command.NO_DNS_LOOKUP] = "no ip domain-lookup",
            [Command.DNS_DOMAIN] = $"ip domain-name {DAT}",
            [Command.SET_DNS_SERVER] = $"ip name-server {DAT}",
            [Command.DELETE_DNS_SERVER] = $"no ip name-server {DAT}",
            [Command.ENABLE_NTP] = "ntp client admin-state enable",
            [Command.DISABLE_NTP] = "ntp client admin-state disable",
            [Command.SET_NTP_SERVER] = $"ntp server {DAT}",
            [Command.DELETE_NTP_SERVER] = $"no ntp server {DAT}",
            [Command.SNMP_AUTH_LOCAL] = "aaa authentication snmp local",
            [Command.SNMP_COMMUNITY_MODE] = "snmp community-map mode enable",
            [Command.SNMP_COMMUNITY_MAP] = $"snmp community-map \"{DAT}\" user \"{DAT}\" enable",
            [Command.SNMP_NO_SECURITY] = "snmp security no-security",
            [Command.SNMP_STATION] = $"snmp station {DAT} 162 \"{DAT}\" {DAT} enable",
            [Command.SNMP_TRAP_AUTH] = "snmp authentication-trap enable",
            [Command.SHOW_AAA_AUTH] = "show aaa authentication",
            [Command.SHOW_USER] = "show user",
            [Command.SHOW_SNMP_SECURITY] = "show snmp security",
            [Command.SHOW_SNMP_STATION] = "show snmp station",
            [Command.SHOW_SNMP_COMMUNITY] = "show snmp community-map",
            [Command.SHOW_VLAN] = "show vlan",
            [Command.SHOW_VLAN_MEMBERS] = $"show vlan {DAT} members",
            [Command.SHOW_LINKAGG] = $"show linkagg agg {DAT}",
            [Command.ENABLE_MGT_VLAN] = "vlan 1 admin-state enable",
            [Command.SET_MGT_INTERFACE] = $"ip interface \"IBMGT-1\" address {DAT} mask {DAT} vlan 1",
            [Command.SET_IP_INTERFACE] = $"ip interface \"{DAT}\" address {DAT} mask {DAT} {DAT}",
            [Command.ENABLE_SPAN_TREE] = "spantree vlan 1 admin-state enable",
            [Command.CLEAR_SWLOG] = "swlog clear all",
            [Command.SNMP_USER] = $"user {DAT} password {DAT} read-only all {DAT} priv-password {DAT}",
            [Command.DELETE_USER] = $"no user {DAT}",
            [Command.DELETE_COMMUNITY] = $"no snmp community-map {DAT}",
            [Command.DELETE_STATION] = $"no snmp station {DAT}"
            #endregion

        };
        #endregion

        #region MIB Request Table

        public static Dictionary<Command, string> MIB_REQ_TBL = new Dictionary<Command, string>
        {
            [Command.SHOW_DNS_CONFIG] = MIB_SWITCH_CFG_DNS,
            [Command.SHOW_DHCP_CONFIG] = MIB_SWITCH_CFG_DHCP,
            [Command.SHOW_DHCP_RELAY] = MIB_SWITCH_CFG_DHCP,
            [Command.SHOW_NTP_CONFIG] = MIB_SWITCH_CFG_NTP,
            [Command.DEBUG_SHOW_LEVEL] = DEBUG_SWITCH_LOG
        };
        #endregion

        #region Public Methods
        public static string ParseUrl(RestUrlEntry entry)
        {
            string req = GetReqFromCmdTbl(entry.RestUrl, entry.Data).Trim();
            if (string.IsNullOrEmpty(req)) throw new SwitchCommandError($"Couldn't find command in table!");
            if (req.Contains("urn=")) return $"?domain=mib&{req}";
            return $"cli/aos?cmd={WebUtility.UrlEncode(req)}";
        }

        public static string GetReqFromCmdTbl(Command cmd, string[] data)
        {
            if (CMD_TBL.ContainsKey(cmd))
            {
                string url = CMD_TBL[cmd];
                if (!url.Contains(DAT)) return url;
                string[] lines = Regex.Split(url, @"" + DAT);
                if (lines.Length == 1) return url;
                if (data.Length < lines.Length - 1) throw new SwitchCommandError($"Invalid url {PrintEnum(cmd)}!");
                string cmd_url = lines[0];
                int idx = 1;
                foreach(string val in data)
                {
                    cmd_url += $"{val}{lines[idx]}";
                    idx++;
                }
                return cmd_url;
            }
            else
            {
                throw new SwitchCommandError($"Invalid url {PrintEnum(cmd)}!");
            }
        }

        public static Dictionary<string, string> GetContent(Command cmd, string[] data)
        {
            if (data != null && data.Length > 0 && CONTENT_TABLE.ContainsKey(cmd))
            {
                Dictionary<string, string> content = CONTENT_TABLE[cmd];
                var dict = new Dictionary<string, string>(content);
                switch (cmd)
                {
                    // 130 - 149: Switch debug MIB commands
                    case Command.DEBUG_UPDATE_LPNI_LEVEL:       // 134
                    case Command.DEBUG_UPDATE_LPCMM_LEVEL:      // 135
                    case Command.DEBUG_UPDATE_LLDPNI_LEVEL:     // 136
                    case Command.SET_PORT_ALIAS:
                        for (int i = 0; i < dict.Keys.Count; i++)
                        {
                            var elem = dict.ElementAt(i);
                            dict[elem.Key] = elem.Value.Replace(DAT, data[i] ?? string.Empty); 
                        }
                        return dict;

                    default:
                        throw new SwitchCommandError($"Invalid command {PrintEnum(cmd)}!");
                }
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region Private Methods
        private static readonly Dictionary<Command, Dictionary<string, string>> CONTENT_TABLE = new Dictionary<Command, Dictionary<string, string>>
        {
            [Command.DEBUG_UPDATE_LLDPNI_LEVEL] = new Dictionary<string, string> {
                { "mibObject0-T1", "systemSwitchLoggingIndex:|-1" },
                { "mibObject1-T1", "systemSwitchLoggingAppName:lldpNi" },
                { "mibObject2-T1", $"systemSwitchLoggingLevel:{DAT}" },
                { "mibObject3-T1", "systemSwitchLoggingVrf:" }
            },
            [Command.DEBUG_UPDATE_LPNI_LEVEL] = new Dictionary<string, string> {
                { "mibObject0-T1", "systemSwitchLoggingIndex:|-1" },
                { "mibObject1-T1", "systemSwitchLoggingAppName:lpNi" },
                { "mibObject2-T1", $"systemSwitchLoggingLevel:{DAT}" },
                { "mibObject3-T1", "systemSwitchLoggingVrf:" }
            },
            [Command.DEBUG_UPDATE_LPCMM_LEVEL] = new Dictionary<string, string> {
                { "mibObject0-T1", "systemSwitchLoggingIndex:|-1" },
                { "mibObject1-T1", "systemSwitchLoggingAppName:lpCmm" },
                { "mibObject2-T1", $"systemSwitchLoggingLevel:{DAT}" },
                { "mibObject3-T1", "systemSwitchLoggingVrf:" }
            },
            [Command.SET_PORT_ALIAS] = new Dictionary<string, string>
            {
                {"mibObject0", $"ifIndex:{DAT}"},
                {"mibObject1", $"ifAlias:{DAT}"}
            }
        };
        #endregion

    }
}

using PoEWizard.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace PoEWizard.Data
{
    public class RestUrl
    {

        public const string REST_URL = "REQUEST_URL";
        public const string RESULT = "RESULT";
        public const string RESPONSE = "RESPONSE";
        public const string DURATION = "DURATION";
        public const string API_ERROR = "error";
        public const string STRING = "string";
        public const string OUTPUT = "output";
        public const string DATA = "data";
        public const string NODE = "node";
        public const string HTTP_RESPONSE = "diag";

        public const string DATA_0 = "%1_DATA_1%";
        public const string DATA_1 = "%2_DATA_2%";
        public const string DATA_2 = "%3_DATA_3%";
        public const string DATA_3 = "%4_DATA_4%";

        public readonly static Dictionary<Command, string> CMD_TBL = new Dictionary<Command, string>
        {
            // 0 - 29: Basic commands to gather switch data
            [Command.SHOW_SYSTEM] = "show system",                                                          //   0
            [Command.SHOW_MICROCODE] = "show microcode",                                                    //   1
            [Command.SHOW_RUNNING_DIR] = "show running-directory",                                          //   2
            [Command.SHOW_CHASSIS] = "show chassis",                                                        //   3
            [Command.SHOW_PORTS_LIST] = "show interfaces alias",                                            //   4
            [Command.SHOW_POWER_SUPPLIES] = $"show powersupply",                                            //   5
            [Command.SHOW_POWER_SUPPLY] = $"show powersupply {DATA_0}",                                     //   6
            [Command.SHOW_LAN_POWER] = $"show lanpower slot {DATA_0}",                                      //   7
            [Command.SHOW_CHASSIS_LAN_POWER_STATUS] = $"show lanpower chassis {DATA_0} status",             //   8
            [Command.SHOW_SLOT] = $"show slot {DATA_0}",                                                    //   9
            [Command.SHOW_MAC_LEARNING] = $"show mac-learning domain vlan",                                 //  10
            [Command.SHOW_TEMPERATURE] = $"show temperature",                                               //  11
            [Command.SHOW_HEALTH] = $"show health all cpu",                                                 //  12
            [Command.SHOW_LAN_POWER_CONFIG] = $"show lanpower slot {DATA_0} port-config",                   //  13
            [Command.SHOW_LLDP_REMOTE] = "show lldp remote-system",                                         //  14
            [Command.SHOW_CMM] = "show cmm",                                                                //  15
            [Command.POWER_CLASS_DETECTION_ENABLE] = $"lanpower slot {DATA_0} class-detection enable",      //  16
            [Command.SHOW_SLOT_LAN_POWER_STATUS] = $"show lanpower slot {DATA_0} status",                   //  17
            [Command.LLDP_SYSTEM_DESCRIPTION_ENABLE] = "lldp nearest-bridge chassis tlv management port-description enable system-name enable system-description enable", //  18
            [Command.SHOW_HEALTH_CONFIG] = "show health configuration",                                     //  19
            [Command.SHOW_LLDP_INVENTORY] = "show lldp remote-system med inventory",                        //  20
            [Command.SHOW_SYSTEM_RUNNING_DIR] = "urn=chasControlModuleTable&mibObject0=sysName&mibObject1=sysLocation&mibObject2=sysContact&mibObject3=sysUpTime&mibObject4=sysDescr&mibObject5=configChangeStatus&mibObject6=chasControlCurrentRunningVersion&mibObject7=chasControlCertifyStatus", //  21
            [Command.SHOW_INTERFACES] = "show interfaces",                                                  //  22
            // 30 - 69: Commands related to actions on port
            [Command.POWER_DOWN_PORT] = $"lanpower port {DATA_0} admin-state disable",                      //  30
            [Command.POWER_UP_PORT] = $"lanpower port {DATA_0} admin-state enable",                         //  31
            [Command.POWER_PRIORITY_PORT] = $"lanpower port {DATA_0} priority {DATA_1}",                    //  32
            [Command.POWER_4PAIR_PORT] = $"lanpower port {DATA_0} 4pair enable",                            //  33
            [Command.POWER_2PAIR_PORT] = $"lanpower port {DATA_0} 4pair disable",                           //  34
            [Command.POWER_DOWN_SLOT] = $"lanpower slot {DATA_0} service stop",                             //  35
            [Command.POWER_UP_SLOT] = $"lanpower slot {DATA_0} service start",                              //  36
            [Command.POWER_823BT_ENABLE] = $"lanpower slot {DATA_0} 8023bt enable",                         //  37
            [Command.POWER_823BT_DISABLE] = $"lanpower slot {DATA_0} 8023bt disable",                       //  38
            [Command.POWER_HDMI_ENABLE] = $"lanpower port {DATA_0} power-over-hdmi enable",                 //  39
            [Command.POWER_HDMI_DISABLE] = $"lanpower port {DATA_0} power-over-hdmi disable",               //  40
            [Command.LLDP_POWER_MDI_ENABLE] = $"lldp nearest-bridge port {DATA_0} tlv dot3 power-via-mdi enable",          //  41
            [Command.LLDP_POWER_MDI_DISABLE] = $"lldp nearest-bridge port {DATA_0} tlv dot3 power-via-mdi disable",        //  42
            [Command.LLDP_EXT_POWER_MDI_ENABLE] = $"lldp nearest-bridge port {DATA_0} tlv med ext-power-via-mdi enable",   //  43
            [Command.LLDP_EXT_POWER_MDI_DISABLE] = $"lldp nearest-bridge port {DATA_0} tlv med ext-power-via-mdi disable", //  44
            [Command.POE_FAST_ENABLE] = $"lanpower slot {DATA_0} fpoe enable",                              //  45
            [Command.POE_PERPETUAL_ENABLE] = $"lanpower slot {DATA_0} ppoe enable",                         //  46
            [Command.SHOW_PORT_MAC_ADDRESS] = $"show mac-learning port {DATA_0}",                           //  47
            [Command.SHOW_PORT_STATUS] = $"show interfaces port {DATA_0} alias",                            //  48
            [Command.SHOW_PORT_POWER] = $"show lanpower slot {DATA_0}|grep {DATA_1}",                       //  49
            [Command.SHOW_PORT_LLDP_REMOTE] = $"show lldp port {DATA_0} remote-system",                     //  50
            [Command.POE_FAST_DISABLE] = $"lanpower slot {DATA_0} fpoe disable",                            //  51
            [Command.POE_PERPETUAL_DISABLE] = $"lanpower slot {DATA_0} ppoe disable",                       //  52
            [Command.SET_MAX_POWER_PORT] = $"lanpower port {DATA_0} power {DATA_1}",                        //  53
            [Command.CAPACITOR_DETECTION_ENABLE] = $"lanpower port {DATA_0} capacitor-detection enable",    //  54
            [Command.CAPACITOR_DETECTION_DISABLE] = $"lanpower port {DATA_0} capacitor-detection disable",  //  55
            // 70 - 99: Special switch commands
            [Command.WRITE_MEMORY] = "write memory flash-synchro",                                          //  70
            [Command.SHOW_CONFIGURATION] = "show configuration snapshot",                                   //  71
            [Command.REBOOT_SWITCH] = "reload from working no rollback-timeout",                            //  72
            // 120 - 129: Switch debug commands
            [Command.DEBUG_SHOW_LAN_POWER_STATUS] = $"debug show lanpower slot {DATA_0} status ni",         // 120
            [Command.DEBUG_CREATE_LOG] = "show tech-support eng complete",                                  // 121
            // 130 - 149: Switch debug MIB commands
            [Command.DEBUG_SHOW_APP_LIST] = "urn=systemSwitchLoggingApplicationTable&mibObject0=systemSwitchLoggingApplicationAppId&mibObject2=systemSwitchLoggingApplicationAppName&fllterDuplicateIndex=systemSwitchLoggingApplicationAppId&litmit=100&limit=200&ignoreError=true", // 130
            [Command.DEBUG_SHOW_LEVEL] = $"urn=systemSwitchLoggingApplicationTable&startIndex={DATA_0}&limit={DATA_1}&mibObject0=systemSwitchLoggingApplicationAppId&mibObject1=systemSwitchLoggingApplicationSubAppId&mibObject2=systemSwitchLoggingApplicationSubAppVrfLevelIndex&mibObject3=systemSwitchLoggingApplicationAppName&mibObject4=systemSwitchLoggingApplicationSubAppName&mibObject6=systemSwitchLoggingApplicationSubAppLevel&ignoreError=true", // 131
            [Command.DEBUG_SHOW_LPNI_LEVEL] = $"urn=systemSwitchLoggingApplicationTable&startIndex={DATA_0}&limit=3&mibObject0=systemSwitchLoggingApplicationAppId&mibObject1=systemSwitchLoggingApplicationSubAppId&mibObject2=systemSwitchLoggingApplicationSubAppVrfLevelIndex&mibObject3=systemSwitchLoggingApplicationAppName&mibObject4=systemSwitchLoggingApplicationSubAppName&mibObject6=systemSwitchLoggingApplicationSubAppLevel&ignoreError=true",   // 132
            [Command.DEBUG_SHOW_LPCMM_LEVEL] = $"urn=systemSwitchLoggingApplicationTable&startIndex={DATA_0}&limit=4&mibObject0=systemSwitchLoggingApplicationAppId&mibObject1=systemSwitchLoggingApplicationSubAppId&mibObject2=systemSwitchLoggingApplicationSubAppVrfLevelIndex&mibObject3=systemSwitchLoggingApplicationAppName&mibObject4=systemSwitchLoggingApplicationSubAppName&mibObject6=systemSwitchLoggingApplicationSubAppLevel&ignoreError=true",  // 133
            [Command.DEBUG_UPDATE_LPNI_LEVEL] = "urn=systemSwitchLogging&setIndexForScalar=true",           // 134
            [Command.DEBUG_UPDATE_LPCMM_LEVEL] = "urn=systemSwitchLogging&setIndexForScalar=true",          // 135
            [Command.DEBUG_UPDATE_LLDPNI_LEVEL] = "urn=systemSwitchLogging&setIndexForScalar=true",         // 136
            // 150 - 189: Switch debug CLI commands
            [Command.DEBUG_CLI_UPDATE_LPNI_LEVEL] = $"swlog appid lpni subapp all level {DATA_0}",          // 150
            [Command.DEBUG_CLI_UPDATE_LPCMM_LEVEL] = $"swlog appid lpcmm subapp all level {DATA_0}",        // 151
            [Command.DEBUG_CLI_SHOW_LPNI_LEVEL] = "show swlog appid lpni",                                  // 152
            [Command.DEBUG_CLI_SHOW_LPCMM_LEVEL] = "show swlog appid lpcmm",                                // 153
            // 190 - 229 Config Wizard commands
            [Command.DISABLE_AUTO_FABRIC] = "auto-fabric admin-state disable",
            [Command.ENABLE_DDM] = "interfaces ddm enable",
            [Command.ENABLE_MULTICAST] = "ip multicast admin-state enable",
            [Command.ENABLE_QUERYING] = "ip multicast querying enable",
            [Command.ENABLE_QUERIER_FWD] = "ip multicast querier-forwarding enable",
            [Command.ENABLE_MULTICAST_VLAN1] = "ip multicast vlan 1 admin-state enable",
            [Command.ENABLE_DHCP_RELAY] = "ip dhcp relay admin-state enable",
            [Command.DHCP_RELAY_DEST] = $"ip dhcp relay destination {DATA_0}",
            [Command.DISABLE_FTP] = "ip service ftp admin-state disable",
            [Command.DISABLE_TELNET] = "ip service telnet admin-state disable",
            [Command.ENABLE_SSH] = "ip service ssh admin-state enable",
            [Command.SSH_AUTH_LOCAL] = "aaa authentication ssh local",
            [Command.SET_SYSTEM_TIMEZONE] = $"system timezone {DATA_0}",
            [Command.SET_SYSTEM_DATE] = $"system date {DATA_0}",
            [Command.SET_SYSTEM_TIME] = $"set system time {DATA_0}",
            [Command.SET_SYSTEM_NAME] = $"system name \"{DATA_0}\"",
            [Command.SET_MNGT_INTERFACE] = $"ip interface \"MGT\" address {DATA_0} mask {DATA_1} vlan 1",
            [Command.SET_PASSWORD] = $"user {DATA_0} password \"{DATA_1}\"",
            [Command.SET_CONTACT] = $"system contact \"{DATA_0}\"",
            [Command.SET_LOCATION] = $"system location \"{DATA_0}\"",
            [Command.SHOW_IP_SERVICE] = "show ip service",
            [Command.SHOW_DNS_CONFIG] = "urn=systemDNS&mibObject0=systemDNSEnableDnsResolver&mibObject1=systemDNSDomainName&mibObject2=systemDNSNsAddr1&mibObject3=systemDNSNsAddr2&mibObject4=systemDNSNsAddr3",   // 207
            [Command.SHOW_DHCP_CONFIG] = "urn=alaDhcpRelayGlobalConfig&mibObject0=alaDhcpRelayAdminStatus&mibObject1=alaDhcpRelayForwardDelay&mibObject2=alaDhcpRelayMaximumHops&mibObject3=alaDhcpRelayPxeSupport&mibObject4=alaDhcpRelayInsertAgentInformation&mibObject5=alaDhcpRelayInsertAgentInformationPolicy&mibObject6=alaDhcpRelayPerInterfaceMode", // 208
            [Command.SHOW_DHCP_RELAY] = "urn=alaDhcpRelayServerDestinationTable&mibObject0=alaDhcpRelayServerDestinationAddressType&mibObject1=alaDhcpRelayServerDestinationAddress&limit=200&ignoreError=true",    // 209
            [Command.SHOW_NTP_CONFIG] = "urn=alaNtpPeerTable&mibObject0=alaNtpPeerAddressType&mibObject1=alaNtpPeerAddress&mibObject2=alaNtpPeerInetAddress&mibObject3=alaNtpPeerInetAddressType&mibObject4=alaNtpPeerType&mibObject5=alaNtpPeerAuth&mibObject6=alaNtpPeerVersion&mibObject7=alaNtpPeerMinpoll&mibObject8=alaNtpPeerMaxpoll&mibObject9=alaNtpPeerPrefer&mibObject10=alaNtpPeerName&mibObject11=alaNtpPeerPreempt&mibObject12=alaNtpPeerBurst&mibObject13=alaNtpPeerIBurst&mibObject14=alaNtpPeerAdmin&limit=200&ignoreError=true",  // 210
            [Command.SHOW_NTP_STATUS] = "show ntp status",
            [Command.SHOW_IP_INTERFACE] = "show ip interface",
            [Command.SHOW_IP_ROUTES] = "show ip routes",
            [Command.SHOW_MULTICAST] = "show ip multicast",
            [Command.DNS_LOOKUP] = "ip domain-lookup",
            [Command.NO_DNS_LOOKUP]= "no ip domain-lookup",
            [Command.DNS_DOMAIN] = $"ip domain-name {DATA_0}",
            [Command.DNS_SERVER] = $"ip name-server {DATA_0}",
            [Command.ENABLE_NTP] = "ntp client admin-state enable",
            [Command.DISABLE_NTP] = "ntp client admin-state disable",
            [Command.NTP_SERVER] = $"ntp server {DATA_0}",
            [Command.START_POE] = $"lanpower chassis {DATA_0} service start",
            [Command.STOP_POE] = $"lanpower chassis {DATA_0} service stop",
            [Command.SNMP_AUTH_LOCAL] = "aaa authentication snmp local",
            [Command.SNMP_COMMUNITY_MODE] = "snmp community mode enable",
            [Command.SNMP_COMMUNITY_MAP] = $"snmp community-map {DATA_0} user {DATA_1} enable",
            [Command.SNMP_NO_SECURITY] = "snmp security no-security",
            [Command.SNMP_STATION] = $"snmp station {DATA_0} {DATA_1} {DATA_2} {DATA_3} enable",
            [Command.SNMP_TRAP_AUTH] = "snmp authentication-trap enable",
            [Command.SNMP_V2_USER] = $"user {DATA_0} password {DATA_1} read-write all {DATA_2}",
            [Command.SNMP_V3_USER] = $"user {DATA_0} password {DATA_1} read-write all {DATA_2} priv-password {DATA_3}",
            [Command.SHOW_AAA_AUTH] = "show aaa authentication",
            [Command.SHOW_USER] =  "show user",
            [Command.SHOW_SNMP_SECURITY] = "show snmp security",
            [Command.SHOW_SNMP_STATION] = "show snmp station",
            [Command.SHOW_SNMP_COMMUNITY] = "show snmp community_map"
        };

        public static Dictionary<Command, Dictionary<string, string>> CONTENT_TABLE = new Dictionary<Command, Dictionary<string, string>>
        {
            [Command.DEBUG_UPDATE_LLDPNI_LEVEL] = new Dictionary<string, string> {
                { "mibObject0-T1", "systemSwitchLoggingIndex:|-1" },
                { "mibObject1-T1", "systemSwitchLoggingAppName:lldpNi" },
                { "mibObject2-T1", $"systemSwitchLoggingLevel:{DATA_0}" },
                { "mibObject3-T1", "systemSwitchLoggingVrf:" }
            },
            [Command.DEBUG_UPDATE_LPNI_LEVEL] = new Dictionary<string, string> {
                { "mibObject0-T1", "systemSwitchLoggingIndex:|-1" },
                { "mibObject1-T1", "systemSwitchLoggingAppName:lpNi" },
                { "mibObject2-T1", $"systemSwitchLoggingLevel:{DATA_0}" },
                { "mibObject3-T1", "systemSwitchLoggingVrf:" }
            },
            [Command.DEBUG_UPDATE_LPCMM_LEVEL] = new Dictionary<string, string> {
                { "mibObject0-T1", "systemSwitchLoggingIndex:|-1" },
                { "mibObject1-T1", "systemSwitchLoggingAppName:lpCmm" },
                { "mibObject2-T1", $"systemSwitchLoggingLevel:{DATA_0}" },
                { "mibObject3-T1", "systemSwitchLoggingVrf:" }
            }
        };

        public static string ParseUrl(RestUrlEntry entry)
        {
            string req = GetReqFromCmdTbl(entry.RestUrl, entry.Data).Trim();
            if (string.IsNullOrEmpty(req)) return null;
            switch (entry.RestUrl)
            {
                // 190 - 229 Config Wizard commands
                case Command.SHOW_DNS_CONFIG:               // 207
                case Command.SHOW_DHCP_CONFIG:              // 208
                case Command.SHOW_DHCP_RELAY:               // 209
                case Command.SHOW_NTP_CONFIG:               // 210
                // 130 - 149: Switch debug MIB commands
                case Command.DEBUG_SHOW_APP_LIST:           // 130
                case Command.DEBUG_SHOW_LEVEL:              // 131
                case Command.DEBUG_SHOW_LPNI_LEVEL:         // 132
                case Command.DEBUG_SHOW_LPCMM_LEVEL:        // 133
                case Command.DEBUG_UPDATE_LPNI_LEVEL:       // 134
                case Command.DEBUG_UPDATE_LPCMM_LEVEL:      // 135
                case Command.DEBUG_UPDATE_LLDPNI_LEVEL:     // 136
                // 0 - 29: Basic commands to gather switch data
                case Command.SHOW_SYSTEM_RUNNING_DIR:       //  21
                    return $"?domain=mib&{req}";

                default:
                    return $"cli/aos?cmd={WebUtility.UrlEncode(req)}";
            }
        }

        public static string GetReqFromCmdTbl(Command cmd, string[] data)
        {
            if (CMD_TBL.ContainsKey(cmd))
            {
                string url = CMD_TBL[cmd];
                switch (cmd)
                {

                    // 0 - 29: Basic commands to gather switch data
                    case Command.SHOW_POWER_SUPPLY:             //   6
                    case Command.SHOW_LAN_POWER:                //   7
                    case Command.SHOW_CHASSIS_LAN_POWER_STATUS: //   8
                    case Command.SHOW_LAN_POWER_CONFIG:         //  13
                    case Command.POWER_CLASS_DETECTION_ENABLE:  //  16
                    case Command.SHOW_SLOT_LAN_POWER_STATUS:    //  17
                    // 30 - 69: Commands related to actions on port
                    case Command.POWER_DOWN_PORT:               //  30
                    case Command.POWER_UP_PORT:                 //  31
                    case Command.POWER_4PAIR_PORT:              //  33
                    case Command.POWER_2PAIR_PORT:              //  34
                    case Command.POWER_DOWN_SLOT:               //  35
                    case Command.POWER_UP_SLOT:                 //  36
                    case Command.POWER_823BT_ENABLE:            //  37
                    case Command.POWER_823BT_DISABLE:           //  38
                    case Command.POWER_HDMI_ENABLE:             //  39
                    case Command.POWER_HDMI_DISABLE:            //  40
                    case Command.LLDP_POWER_MDI_ENABLE:         //  41
                    case Command.LLDP_POWER_MDI_DISABLE:        //  42
                    case Command.LLDP_EXT_POWER_MDI_ENABLE:     //  43
                    case Command.LLDP_EXT_POWER_MDI_DISABLE:    //  44
                    case Command.POE_FAST_ENABLE:               //  45
                    case Command.POE_PERPETUAL_ENABLE:          //  46
                    case Command.SHOW_PORT_MAC_ADDRESS:         //  47
                    case Command.SHOW_PORT_STATUS:              //  48
                    case Command.SHOW_PORT_LLDP_REMOTE:         //  50
                    case Command.POE_FAST_DISABLE:              //  51
                    case Command.POE_PERPETUAL_DISABLE:         //  52
                    case Command.CAPACITOR_DETECTION_ENABLE:    //  54
                    case Command.CAPACITOR_DETECTION_DISABLE:   //  55
                    // 120 - 139: Switch debug commands
                    case Command.DEBUG_SHOW_LAN_POWER_STATUS:   // 120
                    // 150 - 189: Switch debug CLI commands
                    case Command.DEBUG_CLI_UPDATE_LPNI_LEVEL:   // 150
                    case Command.DEBUG_CLI_UPDATE_LPCMM_LEVEL:  // 151
                        if (data == null || data.Length < 1) throw new SwitchCommandError($"Invalid url {Utils.PrintEnum(cmd)}!");
                        return url.Replace(DATA_0, (data == null || data.Length < 1) ? "" : data[0]);

                    // 30 - 69: Commands related to actions on port
                    case Command.POWER_PRIORITY_PORT:           //  32
                    case Command.SHOW_PORT_POWER:               //  49
                    case Command.SET_MAX_POWER_PORT:            //  53
                    // 130 - 149: Switch debug MIB commands
                    case Command.DEBUG_SHOW_LEVEL:              // 131
                        if (data == null || data.Length < 2) throw new SwitchCommandError($"Invalid url {Utils.PrintEnum(cmd)}!");
                        return url.Replace(DATA_0, data[0]).Replace(DATA_1, data[1]);

                    // 100 - 119: Virtual commands
                    case Command.CHECK_POWER_PRIORITY:          //  100
                    case Command.RESET_POWER_PORT:              //  101
                    case Command.NO_COMMAND:                    //  -1
                        return null;

                    default:
                        return url;
                }
            }
            else
            {
                throw new SwitchCommandError($"Invalid url {Utils.PrintEnum(cmd)}!");
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
                        foreach (string key in dict.Keys.ToList())
                        {
                            if (data.Length > 0) dict[key] = dict[key].Replace(DATA_0, data[0] ?? string.Empty);
                            if (data.Length > 1) dict[key] = dict[key].Replace(DATA_1, data[1] ?? string.Empty);
                            if (data.Length > 2) dict[key] = dict[key].Replace(DATA_2, data[2] ?? string.Empty);
                            if (data.Length > 3) dict[key] = dict[key].Replace(DATA_3, data[3] ?? string.Empty);
                        }
                        return dict;

                    default:
                        throw new SwitchCommandError($"Invalid command {Utils.PrintEnum(cmd)}!");
                }
            }
            else
            {
                return null;
            }
        }

    }
}

using PoEWizard.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace PoEWizard.Data
{
    public class RestUrl
    {

        public const string RELEASE_UNKNOWN = "UNKNOWN";
        public const string RELEASE_6 = "6";
        public const string RELEASE_8 = "8";

        public const string REST_URL = "REQUEST_URL";
        public const string RESULT = "RESULT";
        public const string RESPONSE = "RESPONSE";
        public const string DURATION = "DURATION";
        public const string API_ERROR = "error";
        public const string OUTPUT = "output";
        public const string NODE = "node";
        public const string HTTP_RESPONSE = "diag";

        public const string DATA_0 = "%1_DATA_1%";
        public const string DATA_1 = "%2_DATA_2%";
        public const string DATA_2 = "%3_DATA_3%";
        public const string DATA_3 = "%4_DATA_4%";

        public const string DEFAULT_PROMPT = "->";
        public const int DEF_CMD_LONG_WAIT = 90;
        public const string CMD = "COMMAND";
        public const string PROMPT = "SESSION_PROMPT";

        public enum RestUrlId
        {
            SHOW_SYSTEM = 0,
            SHOW_CHASSIS = 1,
            SHOW_PORTS_LIST = 2,
            SHOW_POWER_SUPPLY = 3,
            SHOW_LAN_POWER = 4,
            SHOW_SLOT_POWER = 5,
            SHOW_MAC_LEARNING = 6,
            SHOW_TEMPERATURE = 7,
            SHOW_HEALTH = 8,
            SHOW_TRAFFIC = 9,
            SHOW_PORT_SECURITY_MAC_LEARNING = 10,
            SHOW_SNMP_ENGINE_ID = 11,

            WRITE_MEMORY = 20,
            REBOOT_SWITCH = 21,
            COPY_FLASH_SYNCHRO = 22,

            POWER_DOWN_PORT = 30,
            POWER_UP_PORT = 31,
            POWER_PRIORITY_PORT = 32,
            POWER_4PAIR_PORT = 33,
            POWER_2PAIR_PORT = 34,
            POWER_DOWN_SLOT = 35,
            POWER_UP_SLOT = 36,

            PORT_SECURITY_ENABLE = 50,
            LOCK_PORT_SECURITY = 51,
            UNLOCK_PORT_SECURITY = 52,
            PORT_SECURIRY_CONVERT_STATIC = 53,
            PORT_SECURITY_RELEASE_VIOLATION = 54,

            SHOW_SNMP_COMMUNITY = 70,
            SHOW_SNMP_AGENT_CONFIG = 71,
            SHOW_SNMP_TRAP_STATION = 72,
            SHOW_SNMP_TRAP_FILTER = 73,
            SHOW_SNMP_USER = 74,
            SNMP_ENABLE_COMMUNITY_MODE = 75,
            SNMP_CREATE_COMMUNITY_MAP = 76,
            SNMP_CREATE_STATION = 77,
            SNMP_ENABLE_TRAP_AUTHENTICATION = 78,
            SNMP_DELETE_STATION = 79,
            SNMP_DELETE_COMMUNITY_MAP = 80,
            SNMP_CREATE_USER = 81,
            SNMP_DELETE_USER = 82,
            SHOW_AAA_AUTHENTICATION = 83,
            SNMP_UPDATE_LOCAL_AUTHENTICATION = 84,
            SNMP_SET_TRAP_FILTER = 85,

            SHOW_LLDP_REMOTE_SYSTEM = 90,
            SHOW_VFL_MEMBER_PORT = 91,
            INTERFACES_TDR_ENABLE = 92,
            SHOW_INTERFACES_TDR_STATISTICS = 93,
            CLEAR_INTERFACES_TDR_STATISTICS = 94,
            SHOW_PORT_LAN_POWER = 95,
            SHOW_PORT_SECURITY = 96,
            SHOW_PORT_MAC_LEARNING = 97
        }

        public static Dictionary<string, Dictionary<RestUrlId, string>> REST_URL_TABLE = new Dictionary<string, Dictionary<RestUrlId, string>>
        {
            [RELEASE_8] = new Dictionary<RestUrlId, string>
            {
                [RestUrlId.SHOW_SYSTEM] = "cli/aos?cmd=show system",
                [RestUrlId.SHOW_CHASSIS] = "cli/aos?cmd=show chassis",
                [RestUrlId.SHOW_PORTS_LIST] = "cli/aos?cmd=show interfaces status",
                [RestUrlId.SHOW_POWER_SUPPLY] = $"cli/aos?cmd=show powersupply {DATA_0}",
                [RestUrlId.SHOW_LAN_POWER] = $"cli/aos?cmd=show lanpower slot {DATA_0}",

                [RestUrlId.POWER_DOWN_PORT] = $"cli/aos?cmd=lanpower port {DATA_0} admin-state disable",
                [RestUrlId.POWER_UP_PORT] = $"cli/aos?cmd=lanpower port {DATA_0} admin-state enable",
                [RestUrlId.POWER_4PAIR_PORT] = $"cli/aos?cmd=lanpower port {DATA_0} 4pair enable",
                [RestUrlId.POWER_2PAIR_PORT] = $"cli/aos?cmd=lanpower port {DATA_0} 4pair disable",
                [RestUrlId.POWER_DOWN_SLOT] = $"cli/aos?cmd=lanpower slot {DATA_0} service stop",
                [RestUrlId.POWER_UP_SLOT] = $"cli/aos?cmd=lanpower slot {DATA_0} service start",

                [RestUrlId.WRITE_MEMORY] = "cli/aos?cmd=write memory flash-synchro",

                [RestUrlId.SHOW_SLOT_POWER] = "domain=mib&urn=alaPethMainPseTable&mibObject0=pethMainPseGroupIndex&mibObject1=alaPethMainPseMaxPower&mibObject2=pethMainPseUsageThreshold&mibObject3=pethMainPseConsumptionPower&function=chassisSlot_vcSlotNum&object=pethMainPseGroupIndex&ignoreError=true",
                [RestUrlId.SHOW_SNMP_ENGINE_ID] = "domain=mib&urn=snmpEngine&&mibObject0=snmpEngineID",

                [RestUrlId.POWER_DOWN_PORT] = "domain=mib&urn=pethPsePortTable",
                [RestUrlId.POWER_UP_PORT] = "domain=mib&urn=pethPsePortTable",
                [RestUrlId.POWER_PRIORITY_PORT] = "domain=mib&urn=pethPsePortTable",

                [RestUrlId.PORT_SECURITY_ENABLE] = "domain=mib&urn=learnedPortSecurityTable%7ClearnedPortSecurityTable",
                [RestUrlId.PORT_SECURIRY_CONVERT_STATIC] = "domain=mib&urn=learnedPortSecurityGlobalGroup",
                [RestUrlId.LOCK_PORT_SECURITY] = "domain=mib&urn=learnedPortSecurityTable",
                [RestUrlId.UNLOCK_PORT_SECURITY] = "domain=mib&urn=learnedPortSecurityTable",
                [RestUrlId.PORT_SECURITY_RELEASE_VIOLATION] = "domain=mib&urn=learnedPortSecurityTable",

                [RestUrlId.INTERFACES_TDR_ENABLE] = "domain=mib&urn=esmTdrPortTable",
                [RestUrlId.SHOW_INTERFACES_TDR_STATISTICS] = "domain=mib&urn=esmTdrPortTable&mibObject0=ifIndex&mibObject1=esmTdrPortValidPairs&mibObject2=esmTdrPortCableState&mibObject3=esmTdrPortFuzzLength&mibObject4=esmTdrPortPair1State&mibObject5=esmTdrPortPair1Length&mibObject6=esmTdrPortPair2State&mibObject7=esmTdrPortPair2Length&mibObject8=esmTdrPortPair3State&mibObject9=esmTdrPortPair3Length&mibObject10=esmTdrPortPair4State&mibObject11=esmTdrPortPair4Length&mibObject12=esmTdrPortResult&mibObject13=esmTdrPortTest&function=slotPort_ifindex&object=ifIndex&limit=200&ignoreError=true",
                [RestUrlId.CLEAR_INTERFACES_TDR_STATISTICS] = "domain=mib&urn=esmTdrPortTable",

                [RestUrlId.SHOW_SNMP_COMMUNITY] = "domain=mib&urn=snmpCommunityTable&mibObject0=snmpCommunityIndex&mibObject1=snmpCommunitySecurityName&mibObject2=snmpCommunityStorageType&mibObject3=snmpCommunityStatus",
                [RestUrlId.SHOW_SNMP_AGENT_CONFIG] = "domain=mib&urn=snmpAgtConfig&mibObject0=snmpAgtCommunityMode&mibObject1=snmpAgtSecurityLevel&mibObject2=snmpAgtTsmAdminState&mibObject3=snmpAgtTlsAdminState",
                [RestUrlId.SHOW_SNMP_TRAP_STATION] = "domain=mib&urn=alaTrapInetStationTable&mibObject0=alaTrapInetStationIPType&mibObject1=alaTrapInetStationIP&mibObject2=alaTrapInetStationUser&mibObject3=alaTrapInetStationPort&mibObject4=alaTrapInetStationProtocol&mibObject5=alaTrapInetStationReplay&mibObject6=alaTrapInetStationNextSeq&mibObject7=alaTrapInetStationRowStatus&mibObject8=alaTrapInetStationSecurityModel&mibObject9=alaTrapInetStationLocalIdentity&mibObject10=alaTrapInetStationRemoteIdentity",
                [RestUrlId.SHOW_SNMP_TRAP_FILTER] = "domain=mib&urn=alaTrapInetFilterTable&mibObject0=alaTrapInetStationIPType&mibObject1=alaTrapInetStationIP&mibObject2=trapIndex&mibObject3=alaTrapInetFilterStatus&trapConfigTable-trapIndex-0=trapName&trapConfigTable-trapIndex-1=trapFamily",
                [RestUrlId.SHOW_SNMP_USER] = "domain=mib&urn=aaaUserTable&mibObject0=aaauUserName&mibObject1=aaauSnmpLevel&mibObject2=aaauRowStatus",

                //using domain cli
                [RestUrlId.SNMP_SET_TRAP_FILTER] = $"domain=cli&cmd=snmp-trap filter-ip {DATA_0} {DATA_1}",

                [RestUrlId.SNMP_ENABLE_COMMUNITY_MODE] = "domain=mib&urn=snmpAgtConfig",
                [RestUrlId.SNMP_CREATE_COMMUNITY_MAP] = "domain=mib&urn=snmpCommunityTable",
                [RestUrlId.SNMP_CREATE_STATION] = "domain=mib&urn=alaTrapInetStationTable",
                [RestUrlId.SNMP_ENABLE_TRAP_AUTHENTICATION] = "domain=mib&urn=snmpAgtConfig",
                [RestUrlId.SNMP_DELETE_STATION] = "domain=mib&urn=alaTrapInetStationTable",
                [RestUrlId.SNMP_DELETE_COMMUNITY_MAP] = "domain=mib&urn=snmpCommunityTable",
                [RestUrlId.SNMP_CREATE_USER] = "domain=mib&urn=aaaUserTable",
                [RestUrlId.SNMP_DELETE_USER] = "domain=mib&urn=aaaUserTable",

                [RestUrlId.SHOW_AAA_AUTHENTICATION] = "domain=mib&urn=aaaAuthSATable&mibObject0=aaatsInterface&mibObject1=aaatsName1&mibObject2=aaatsName2&mibObject3=aaatsName3&mibObject4=aaatsName4",
                [RestUrlId.SNMP_UPDATE_LOCAL_AUTHENTICATION] = "domain=mib&urn=aaaAuthSATable",

                [RestUrlId.COPY_FLASH_SYNCHRO] = "domain=mib&urn=chasControlModuleTable",
                [RestUrlId.REBOOT_SWITCH] = "domain=mib&urn=chasControlModuleTable",

                [RestUrlId.SHOW_LLDP_REMOTE_SYSTEM] = "domain=mib&urn=lldpV2RemTable&mibObject1=lldpV2RemLocalIfIndex&mibObject2=lldpV2RemSysCapEnabled&function=slotPort_ifindex&object=lldpV2RemLocalIfIndex",
                [RestUrlId.SHOW_VFL_MEMBER_PORT] = "domain=mib&urn=virtualChassisVflMemberPortTable&mibObject0=virtualChassisVflMemberPortIfindex&function=slotPort_ifindex&object=virtualChassisVflMemberPortIfindex",

                [RestUrlId.SHOW_PORT_LAN_POWER] = "domain=mib&urn=pethPsePortTable&mibObject0=pethPsePortGroupIndex&mibObject1=pethPsePortIndex&mibObject2=pethPsePortAdminEnable&mibObject3=pethPsePortPowerPriority&mibObject4=pethPsePortType&mibObject5=alaPethPsePortPowerMaximum&mibObject6=alaPethPsePortPowerActual&mibObject7=alaPethPsePort4PairStatus&mibObject8=alaPethPsePortPowerStatus&mibObject9=alaPethPsePortPowerClass&mibObject10=alaPethPsePortPowerClassA&mibObject11=alaPethPsePortPowerClassB&mibObject12=alaPethPsePortPowerOverHdmi&mibObject13=alaPethPsePortTrusted&mibObject14=alaPethPsePortCapacitorDetection&function=slotPort_vcSlotPort&object=pethPsePortGroupIndex%2CpethPsePortIndex",
                [RestUrlId.SHOW_PORT_SECURITY] = "domain=mib&urn=learnedPortSecurityTable&mibObject0=ifIndex&mibObject1=lpsMaxMacNum&mibObject2=lpsViolationOption&mibObject3=lpsAdminStatus&mibObject4=lpsOperStatus&mibObject5=lpsMaxFilteredMacNum&mibObject6=lpsLearnTrapThreshold&mibObject7=lpsViolatingMac&mibObject8=lpsPacketRelay&function=slotPort_ifindex&object=ifIndex",

                //[RestUrlId.SHOW_PORT_MAC_LEARNING] = "domain=mib&urn=alaSlMacAddressGlobalTable&mibObject0=slMacDomain&mibObject1=slLocaleType&mibObject2=slOriginId&mibObject3=slServiceId&mibObject4=slSubId&mibObject5=slMacAddressGbl&mibObject6=slMacAddressGblManagement&mibObject7=slMacAddressGblDisposition&mibObject8=slMacAddressGblProtocol&mibObject9=slMacAddressGblGroupField&mibObject10=slSvcISID&mibObject11=slVxLanVnID&mibObject12=slL2GreVpnID&function=slotPort_ifindex&object=slOriginId",
                //using domain cli
                [RestUrlId.SHOW_PORT_MAC_LEARNING] = $"domain=cli&cmd=debug show json-mac-learning limit 4096 {DATA_0}",
            }
        };

        public static string ParseUrl(string version, RestUrlEntry entry)
        {
            string url = GetUrlFromTable(version, entry.RestUrl, entry.Data).Trim();
            string[] urlSplit = url.Split('=');
            url = $"{urlSplit[0]}={urlSplit[1].Replace(" ", "%20").Replace("/", "%2F")}";
            return url;
        }

        private static string GetUrlFromTable(string version, RestUrlId restUrlId, string[] data)
        {
            if (version == null || version == RELEASE_UNKNOWN)
            {
                throw new SwitchCommandError("Switch version invalid to get the URL for the Rest Api!");
            }
            if (REST_URL_TABLE.ContainsKey(version) && REST_URL_TABLE[version].ContainsKey(restUrlId))
            {
                string url = REST_URL_TABLE[version][restUrlId];
                switch (restUrlId)
                {
                    case RestUrlId.SHOW_SYSTEM:
                    case RestUrlId.SHOW_CHASSIS:
                    case RestUrlId.SHOW_PORTS_LIST:
                    case RestUrlId.SHOW_TRAFFIC:
                    case RestUrlId.SHOW_TEMPERATURE:
                    case RestUrlId.SHOW_HEALTH:
                    case RestUrlId.SHOW_SLOT_POWER:
                    case RestUrlId.SHOW_SNMP_ENGINE_ID:


                    case RestUrlId.SHOW_SNMP_COMMUNITY:
                    case RestUrlId.SHOW_SNMP_AGENT_CONFIG:
                    case RestUrlId.SHOW_SNMP_TRAP_STATION:
                    case RestUrlId.SHOW_SNMP_TRAP_FILTER:
                    case RestUrlId.SHOW_SNMP_USER:
                    case RestUrlId.SNMP_ENABLE_COMMUNITY_MODE:
                    case RestUrlId.SNMP_CREATE_COMMUNITY_MAP:
                    case RestUrlId.SNMP_CREATE_STATION:
                    case RestUrlId.SNMP_ENABLE_TRAP_AUTHENTICATION:
                    case RestUrlId.SNMP_DELETE_STATION:
                    case RestUrlId.SNMP_DELETE_COMMUNITY_MAP:
                    case RestUrlId.SNMP_CREATE_USER:
                    case RestUrlId.SNMP_DELETE_USER:

                    case RestUrlId.SHOW_AAA_AUTHENTICATION:
                    case RestUrlId.SNMP_UPDATE_LOCAL_AUTHENTICATION:

                    case RestUrlId.WRITE_MEMORY:
                    case RestUrlId.COPY_FLASH_SYNCHRO:
                    case RestUrlId.REBOOT_SWITCH:
                    case RestUrlId.SHOW_LLDP_REMOTE_SYSTEM:
                    case RestUrlId.SHOW_VFL_MEMBER_PORT:

                    case RestUrlId.SHOW_PORT_LAN_POWER:
                    case RestUrlId.SHOW_PORT_SECURITY:
                    case RestUrlId.SHOW_PORT_SECURITY_MAC_LEARNING:
                        return url;

                    //using domain cli
                    case RestUrlId.SHOW_MAC_LEARNING:
                    case RestUrlId.SHOW_PORT_MAC_LEARNING:
                    case RestUrlId.SHOW_POWER_SUPPLY:
                    case RestUrlId.SHOW_LAN_POWER:
                        if (data == null || data.Length < 1) throw new SwitchCommandError($"Invalid url {Utils.PrintEnum(restUrlId)}!");
                        return url.Replace(DATA_0, (data == null || data.Length < 1) ? "" : data[0]);

                    case RestUrlId.POWER_DOWN_PORT:
                    case RestUrlId.POWER_UP_PORT:
                    case RestUrlId.POWER_PRIORITY_PORT:
                    case RestUrlId.PORT_SECURITY_ENABLE:
                    case RestUrlId.PORT_SECURIRY_CONVERT_STATIC:
                    case RestUrlId.LOCK_PORT_SECURITY:
                    case RestUrlId.UNLOCK_PORT_SECURITY:
                    case RestUrlId.PORT_SECURITY_RELEASE_VIOLATION:
                    case RestUrlId.INTERFACES_TDR_ENABLE:
                    case RestUrlId.SHOW_INTERFACES_TDR_STATISTICS:
                    case RestUrlId.CLEAR_INTERFACES_TDR_STATISTICS:
                    case RestUrlId.SNMP_SET_TRAP_FILTER:
                        if (data == null || data.Length < 2) throw new SwitchCommandError($"Invalid url {Utils.PrintEnum(restUrlId)}!");
                        return url.Replace(DATA_0, (data.Length < 1) ? "" : data[0]).Replace(DATA_1, (data.Length < 2) ? "" : data[1]);

                    default:
                        throw new SwitchCommandError($"Invalid url {Utils.PrintEnum(restUrlId)}!");
                }
            }
            else
            {
                throw new SwitchCommandError($"Invalid url {Utils.PrintEnum(restUrlId)}!");
            }
        }

        public static HttpMethod GetHttpMethod(string version, RestUrlId restUrlId)
        {
            if (version == null || version == RELEASE_UNKNOWN)
            {
                throw new SwitchCommandError("Switch version invalid to get the HTTP Method for the Rest Api!");
            }
            if (REST_URL_TABLE.ContainsKey(version) && REST_URL_TABLE[version].ContainsKey(restUrlId))
            {
                switch (restUrlId)
                {
                    case RestUrlId.SHOW_SYSTEM:
                    case RestUrlId.SHOW_CHASSIS:
                    case RestUrlId.SHOW_PORTS_LIST:
                    case RestUrlId.SHOW_TRAFFIC:
                    case RestUrlId.SHOW_MAC_LEARNING:
                    case RestUrlId.SHOW_TEMPERATURE:
                    case RestUrlId.SHOW_HEALTH:
                    case RestUrlId.SHOW_PORT_SECURITY_MAC_LEARNING:
                    case RestUrlId.SHOW_LAN_POWER:
                    case RestUrlId.SHOW_PORT_LAN_POWER:
                    case RestUrlId.SHOW_SLOT_POWER:
                    case RestUrlId.SHOW_SNMP_ENGINE_ID:

                    case RestUrlId.SHOW_INTERFACES_TDR_STATISTICS:

                    case RestUrlId.SHOW_SNMP_COMMUNITY:
                    case RestUrlId.SHOW_SNMP_AGENT_CONFIG:
                    case RestUrlId.SHOW_SNMP_TRAP_STATION:
                    case RestUrlId.SHOW_SNMP_TRAP_FILTER:
                    case RestUrlId.SHOW_SNMP_USER:

                    case RestUrlId.SHOW_AAA_AUTHENTICATION:
                    case RestUrlId.SHOW_LLDP_REMOTE_SYSTEM:
                    case RestUrlId.SHOW_VFL_MEMBER_PORT:
                    case RestUrlId.SHOW_POWER_SUPPLY:
                        return HttpMethod.Get;

                    case RestUrlId.POWER_DOWN_PORT:
                    case RestUrlId.POWER_UP_PORT:
                    case RestUrlId.POWER_PRIORITY_PORT:
                    case RestUrlId.PORT_SECURITY_ENABLE:
                    case RestUrlId.PORT_SECURIRY_CONVERT_STATIC:
                    case RestUrlId.LOCK_PORT_SECURITY:
                    case RestUrlId.UNLOCK_PORT_SECURITY:
                    case RestUrlId.PORT_SECURITY_RELEASE_VIOLATION:

                    case RestUrlId.INTERFACES_TDR_ENABLE:
                    case RestUrlId.CLEAR_INTERFACES_TDR_STATISTICS:

                    case RestUrlId.SNMP_ENABLE_COMMUNITY_MODE:
                    case RestUrlId.SNMP_CREATE_COMMUNITY_MAP:
                    case RestUrlId.SNMP_CREATE_STATION:
                    case RestUrlId.SNMP_ENABLE_TRAP_AUTHENTICATION:
                    case RestUrlId.SNMP_DELETE_STATION:
                    case RestUrlId.SNMP_DELETE_COMMUNITY_MAP:
                    case RestUrlId.SNMP_CREATE_USER:
                    case RestUrlId.SNMP_DELETE_USER:
                    case RestUrlId.SNMP_SET_TRAP_FILTER:

                    case RestUrlId.SNMP_UPDATE_LOCAL_AUTHENTICATION:
                    case RestUrlId.WRITE_MEMORY:
                    case RestUrlId.COPY_FLASH_SYNCHRO:
                    case RestUrlId.REBOOT_SWITCH:
                        return HttpMethod.Post;

                    default:
                        throw new SwitchCommandError($"Invalid command {Utils.PrintEnum(restUrlId)}!");
                }
            }
            else
            {
                throw new SwitchCommandError($"Invalid command {Utils.PrintEnum(restUrlId)}!");
            }
        }

        public static Dictionary<string, Dictionary<RestUrlId, Dictionary<string, string>>> CONTENT_TABLE = new Dictionary<string, Dictionary<RestUrlId, Dictionary<string, string>>>
        {
            [RELEASE_8] = new Dictionary<RestUrlId, Dictionary<string, string>>
            {
                [RestUrlId.POWER_DOWN_PORT] = new Dictionary<string, string>
                {
                    { "mibObject0-T1", $"pethPsePortGroupIndex:|{DATA_0}" },
                    { "mibObject1-T1", $"pethPsePortIndex:|{DATA_1}" },
                    { "mibObject2-T1", "pethPsePortAdminEnable:2" },
                },
                [RestUrlId.POWER_UP_PORT] = new Dictionary<string, string>
                {
                    { "mibObject0-T1", $"pethPsePortGroupIndex:|{DATA_0}" },
                    { "mibObject1-T1", $"pethPsePortIndex:|{DATA_1}" },
                    { "mibObject2-T1", "pethPsePortAdminEnable:1" },
                },
                [RestUrlId.POWER_PRIORITY_PORT] = new Dictionary<string, string>
                {
                    { "mibObject0-T1", $"pethPsePortGroupIndex:|{DATA_0}" },
                    { "mibObject1-T1", $"pethPsePortIndex:|{DATA_1}" },
                    { "mibObject2-T1", $"pethPsePortPowerPriority:{DATA_2}" },
                },
                [RestUrlId.PORT_SECURITY_ENABLE] = new Dictionary<string, string>
                {
                    { "mibObject0-T1", $"ifIndex:|{DATA_0}" },
                    { "mibObject1-T1", "lpsRowStatus:4" },
                    { "mibObject2-T1", "lpsMaxMacNum:1" },
                    { "mibObject3-T1", "lpsViolationOption:1" },
                    { "mibObject4-T1", "lpsAdminStatus:1" },
                    { "mibObject5-T1", "lpsMaxFilteredMacNum:0" },
                    { "mibObject0-T2", $"ifIndex:|{DATA_0}" },
                    { "mibObject1-T2", "lpsLearnTrapThreshold:undefined" },
                    { "mibObject6-T1", "lpsPacketRelay:2" }
                },
                [RestUrlId.PORT_SECURIRY_CONVERT_STATIC] = new Dictionary<string, string>
                {
                    { "mibObject0-T1", $"lpsConvertToStatic:{DATA_0}" }
                },
                [RestUrlId.LOCK_PORT_SECURITY] = new Dictionary<string, string>
                {
                    { "mibObject0", $"ifIndex:|{DATA_0}" },
                    { "mibObject1", "lpsAdminStatus:3" }
                },
                [RestUrlId.UNLOCK_PORT_SECURITY] = new Dictionary<string, string>
                {
                    { "mibObject0", $"ifIndex:|{DATA_0}" },
                    { "mibObject1", "lpsRowStatus:6" }
                },
                [RestUrlId.PORT_SECURITY_RELEASE_VIOLATION] = new Dictionary<string, string>
                {
                    { "mibObject0", $"ifIndex:|{DATA_0}" },
                    { "mibObject1", "lpsRelease:1" }
                },
                [RestUrlId.INTERFACES_TDR_ENABLE] = new Dictionary<string, string>
                {
                    { "mibObject0", $"ifIndex:|{DATA_0}" },
                    { "mibObject1", "esmTdrPortTest:2" }
                },
                [RestUrlId.CLEAR_INTERFACES_TDR_STATISTICS] = new Dictionary<string, string>
                {
                    { "mibObject0", $"ifIndex:|{DATA_0}" },
                    { "mibObject1", $"ifIndex:|{DATA_0}" },
                    { "mibObject2", "esmTdrPortClearStats:2" }
                },
                [RestUrlId.SNMP_ENABLE_COMMUNITY_MODE] = new Dictionary<string, string>
                {
                    { "mibObject0-T1", "snmpAgtCommunityMode:1" }
                },
                [RestUrlId.SNMP_CREATE_COMMUNITY_MAP] = new Dictionary<string, string>
                {
                    { "mibObject0", $"snmpCommunityIndex:|{DATA_0}" },
                    { "mibObject1", $"snmpCommunitySecurityName:{DATA_1}" },
                    { "mibObject2", "snmpCommunityStatus:100" }
                },
                [RestUrlId.SNMP_CREATE_STATION] = new Dictionary<string, string>
                {
                    { "mibObject0", "alaTrapInetStationIPType:|1" },
                    { "mibObject1", $"alaTrapInetStationIP:|{DATA_0}" },
                    { "mibObject2", $"alaTrapInetStationPort:{DATA_1}" },
                    { "mibObject3", $"alaTrapInetStationProtocol:{DATA_3}" },
                    { "mibObject4", $"alaTrapInetStationUser:{DATA_2}" },
                    { "mibObject5", "alaTrapInetStationRowStatus:4" },
                    { "mibObject6", "alaTrapInetStationReplay:0" }
                },
                [RestUrlId.SNMP_ENABLE_TRAP_AUTHENTICATION] = new Dictionary<string, string>
                {
                    { "mibObject0-T1", "snmpEnableAuthenTraps:1" }
                },
                [RestUrlId.SNMP_DELETE_STATION] = new Dictionary<string, string>
                {
                    { "mibObject0", "alaTrapInetStationRowStatus:6" },
                    { "mibObject1", "alaTrapInetStationIPType:|1" },
                    { "mibObject3", $"alaTrapInetStationIP:|{DATA_0}" }
                },
                [RestUrlId.SNMP_DELETE_COMMUNITY_MAP] = new Dictionary<string, string>
                {
                    { "mibObject0", $"snmpCommunityIndex:|{DATA_0}" },
                    { "mibObject1", "snmpCommunityStatus:6" }
                },
                [RestUrlId.SNMP_CREATE_USER] = new Dictionary<string, string>
                {
                    { "mibObject0", $"aaauUserName:|{DATA_0}" },
                    { "mibObject1", $"aaauPassword:{DATA_1}" },
                    { "mibObject2", $"aaauSnmpPrivPassword:{DATA_2}" },
                    { "mibObject3", $"aaauSnmpLevel:{DATA_3}" },
                    { "mibObject4", "aaauPasswordExpirationInMinute:-1" },
                    { "mibObject5", "aaauRowStatus:4" },
                    { "mibObject6", "aaauReadRight1:4294967295" },
                    { "mibObject7", "aaauReadRight2:4294967295" },
                    { "mibObject8", "aaauReadRight3:4294967295" },
                    { "mibObject9", "aaauReadRight4:0" },
                    { "encryptField", "aaauPassword" }
                },
                [RestUrlId.SNMP_DELETE_USER] = new Dictionary<string, string>
                {
                    { "mibObject0", $"aaauUserName:|{DATA_0}" },
                    { "mibObject1", "aaauRowStatus:6" }
                },
                [RestUrlId.SNMP_UPDATE_LOCAL_AUTHENTICATION] = new Dictionary<string, string>
                {
                    { "mibObject0", "aaatsInterface:6"},
                    { "mibObject1", "aaatsName1:local"}
                },
                [RestUrlId.WRITE_MEMORY] = new Dictionary<string, string>
                {
                    { "mibObject0", "configWriteMemory:1"}
                },
                [RestUrlId.COPY_FLASH_SYNCHRO] = new Dictionary<string, string>
                {
                    { "mibObject0", "entPhysicalIndex:65" },
                    { "mibObject1", "chasControlVersionMngt:2" }
                },
                [RestUrlId.REBOOT_SWITCH] = new Dictionary<string, string>
                {
                    { "mibObject0", "entPhysicalIndex:65" },
                    { "mibObject1", "chasControlActivateTimeout:0" },
                    { "mibObject2", "chasControlVersionMngt:6" },
                    { "mibObject3", "chasControlWorkingVersion:working" },
                    { "mibObject4", "chasControlRedundancyTime: 0" },
                    { "mibObject5", "chasControlDelayedActivateTimer:0" },
                    { "mibObject6", "chasControlChassisId:0" }
                }
            }
        };

        public static Dictionary<string, string> GetContent(string version, RestUrlId restUrlId, string[] data)
        {
            if (data == null || data.Length < 1)
            {
                throw new SwitchCommandError("Missing data to get the content for the Rest Api!");
            }
            if (version == null || version == RELEASE_UNKNOWN)
            {
                throw new SwitchCommandError("Switch version invalid to get the content for the Rest Api!");
            }
            if (CONTENT_TABLE.ContainsKey(version) && CONTENT_TABLE[version].ContainsKey(restUrlId))
            {
                Dictionary<string, string> content = CONTENT_TABLE[version][restUrlId];
                var dict = new Dictionary<string, string>(content);
                switch (restUrlId)
                {
                    // Ports actions
                    case RestUrlId.POWER_DOWN_PORT:
                    case RestUrlId.POWER_UP_PORT:
                    case RestUrlId.POWER_PRIORITY_PORT:
                    case RestUrlId.PORT_SECURITY_ENABLE:
                    case RestUrlId.PORT_SECURIRY_CONVERT_STATIC:
                    case RestUrlId.LOCK_PORT_SECURITY:
                    case RestUrlId.UNLOCK_PORT_SECURITY:
                    case RestUrlId.PORT_SECURITY_RELEASE_VIOLATION:
                    case RestUrlId.INTERFACES_TDR_ENABLE:
                    case RestUrlId.CLEAR_INTERFACES_TDR_STATISTICS:
                    // SNMP actions with data
                    case RestUrlId.SNMP_CREATE_COMMUNITY_MAP:
                    case RestUrlId.SNMP_DELETE_STATION:
                    case RestUrlId.SNMP_DELETE_COMMUNITY_MAP:
                    case RestUrlId.SNMP_DELETE_USER:
                    case RestUrlId.SNMP_CREATE_STATION:
                    case RestUrlId.SNMP_CREATE_USER:
                    // Switch actions
                    case RestUrlId.WRITE_MEMORY:
                    case RestUrlId.COPY_FLASH_SYNCHRO:
                    case RestUrlId.REBOOT_SWITCH:
                        foreach (string key in dict.Keys.ToList())
                        {
                            if (data.Length > 0 && !string.IsNullOrEmpty(data[0]))
                            {
                                dict[key] = dict[key].Replace(DATA_0, data[0]);
                            }
                            if (data.Length > 1 && !string.IsNullOrEmpty(data[1]))
                            {
                                dict[key] = dict[key].Replace(DATA_1, data[1]);
                            }
                            if (data.Length > 2 && !string.IsNullOrEmpty(data[2]))
                            {
                                dict[key] = dict[key].Replace(DATA_2, data[2]);
                            }
                            if (data.Length > 3 && !string.IsNullOrEmpty(data[3]))
                            {
                                dict[key] = dict[key].Replace(DATA_3, data[3]);
                            }
                        }
                        return dict;

                    // SNMP actions with no data
                    case RestUrlId.SNMP_ENABLE_COMMUNITY_MODE:
                    case RestUrlId.SNMP_ENABLE_TRAP_AUTHENTICATION:
                    case RestUrlId.SNMP_UPDATE_LOCAL_AUTHENTICATION:
                        return content;

                    default:
                        throw new SwitchCommandError($"Invalid command {Utils.PrintEnum(restUrlId)}!");
                }
            }
            else
            {
                return null;
            }
        }
    }


}

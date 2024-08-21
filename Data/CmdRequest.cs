using System.Linq;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Data
{
    public enum Command
    {
        NO_COMMAND = -1,
        // 0 - 29: Basic commands to gather switch data
        SHOW_SYSTEM = 0,
        SHOW_MICROCODE = 1,
        SHOW_RUNNING_DIR = 2,
        SHOW_CHASSIS = 3,
        SHOW_PORTS_LIST = 4,
        SHOW_POWER_SUPPLIES = 5,
        SHOW_POWER_SUPPLY = 6,
        SHOW_LAN_POWER = 7,
        SHOW_CHASSIS_LAN_POWER_STATUS = 8,
        SHOW_SLOT = 9,
        SHOW_MAC_LEARNING = 10,
        SHOW_TEMPERATURE = 11,
        SHOW_HEALTH = 12,
        SHOW_LAN_POWER_CONFIG = 13,
        SHOW_LLDP_REMOTE = 14,
        SHOW_CMM = 15,
        POWER_CLASS_DETECTION_ENABLE = 16,
        SHOW_SLOT_LAN_POWER_STATUS = 17,
        LLDP_SYSTEM_DESCRIPTION_ENABLE = 18,
        SHOW_HEALTH_CONFIG = 19,
        SHOW_LLDP_INVENTORY = 20,
        SHOW_SYSTEM_RUNNING_DIR = 21,
        SHOW_INTERFACES = 22,
        // 30 - 69: Commands related to actions on port
        POWER_DOWN_PORT = 30,
        POWER_UP_PORT = 31,
        POWER_PRIORITY_PORT = 32,
        POWER_4PAIR_PORT = 33,
        POWER_2PAIR_PORT = 34,
        POWER_DOWN_SLOT = 35,
        POWER_UP_SLOT = 36,
        POWER_823BT_ENABLE = 37,
        POWER_823BT_DISABLE = 38,
        POWER_HDMI_ENABLE = 39,
        POWER_HDMI_DISABLE = 40,
        LLDP_POWER_MDI_ENABLE = 41,
        LLDP_POWER_MDI_DISABLE = 42,
        LLDP_EXT_POWER_MDI_ENABLE = 43,
        LLDP_EXT_POWER_MDI_DISABLE = 44,
        POE_FAST_ENABLE = 45,
        POE_PERPETUAL_ENABLE = 46,
        SHOW_PORT_MAC_ADDRESS = 47,
        SHOW_PORT_STATUS = 48,
        SHOW_PORT_POWER = 49,
        SHOW_PORT_LLDP_REMOTE = 50,
        POE_FAST_DISABLE = 51,
        POE_PERPETUAL_DISABLE = 52,
        SET_MAX_POWER_PORT = 53,
        CAPACITOR_DETECTION_ENABLE = 54,
        CAPACITOR_DETECTION_DISABLE = 55,
        // 70 - 99: Special switch commands
        WRITE_MEMORY = 70,
        SHOW_CONFIGURATION = 71,
        REBOOT_SWITCH = 72,
        // 100 - 119: Virtual commands
        CHECK_POWER_PRIORITY = 100,
        RESET_POWER_PORT = 101,
        CHECK_823BT = 102,
        CHECK_MAX_POWER = 103,
        CHANGE_MAX_POWER = 104,
        CHECK_CAPACITOR_DETECTION = 105,
        // 120 - 129: Switch debug commands
        DEBUG_SHOW_LAN_POWER_STATUS = 120,
        DEBUG_CREATE_LOG = 121,
        // 130 - 149: Switch debug MIB commands
        DEBUG_SHOW_APP_LIST = 130,
        DEBUG_SHOW_LEVEL = 131,
        DEBUG_SHOW_LPNI_LEVEL = 132,
        DEBUG_SHOW_LPCMM_LEVEL = 133,
        DEBUG_UPDATE_LPNI_LEVEL = 134,
        DEBUG_UPDATE_LPCMM_LEVEL = 135,
        DEBUG_UPDATE_LLDPNI_LEVEL = 136,
        // 150 - 189: Switch debug CLI commands
        DEBUG_CLI_UPDATE_LPNI_LEVEL = 150,
        DEBUG_CLI_UPDATE_LPCMM_LEVEL = 151,
        DEBUG_CLI_SHOW_LPNI_LEVEL = 152,
        DEBUG_CLI_SHOW_LPCMM_LEVEL = 153,
        // 190 - 249 Config Wizard commands
        DISABLE_AUTO_FABRIC = 190,
        ENABLE_DDM = 191,
        ENABLE_MULTICAST = 192,
        ENABLE_QUERYING = 193,
        ENABLE_QUERIER_FWD = 194,
        ENABLE_MULTICAST_VLAN1 = 195,
        ENABLE_DHCP_RELAY = 196,
        DHCP_RELAY_DEST = 197,
        DISABLE_FTP = 198,
        DISABLE_TELNET = 199,
        ENABLE_SSH = 200,
        SSH_AUTH_LOCAL = 201,
        SET_SYSTEM_TIMEZONE = 202,
        SET_SYSTEM_DATE = 203,
        SET_SYSTEM_TIME = 204,
        SET_SYSTEM_NAME = 205,
        SET_LOCATION = 206,
        SET_MNGT_INTERFACE = 207,
        SET_PASSWORD = 208,
        SET_CONTACT = 209,
        SHOW_IP_SERVICE = 210,
        SHOW_DNS_CONFIG = 211,
        SHOW_DHCP_CONFIG = 212,
        SHOW_DHCP_RELAY = 213,
        SHOW_NTP_STATUS = 214,
        SHOW_NTP_CONFIG = 215,
        SHOW_IP_ROUTES = 216,
        DNS_LOOKUP = 217,
        NO_DNS_LOOKUP = 218,
        DNS_SERVER = 219,
        DNS_DOMAIN = 220,
        ENABLE_NTP = 221,
        DISABLE_NTP = 222,
        NTP_SERVER = 223,
        START_POE = 224,
        STOP_POE = 225,
        SNMP_AUTH_LOCAL = 226,
        SNMP_COMMUNITY_MODE = 227,
        SNMP_COMMUNITY_MAP = 228,
        SNMP_NO_SECURITY = 229,
        SNMP_STATION = 230,
        SNMP_TRAP_AUTH = 231,
        SNMP_V2_USER = 232,
        SNMP_V3_USER = 233,
        SHOW_IP_INTERFACE = 234,
        SHOW_MULTICAST = 235,
        SHOW_SNMP_SECURITY = 236,
        SHOW_SNMP_STATION = 237,
        SHOW_SNMP_COMMUNITY = 238,
        SHOW_AAA_AUTH = 239,
        SHOW_USER = 240,
        ENABLE_TELNET = 241,
        ENABLE_FTP = 242,
        TELNET_AUTH_LOCAL = 243,
        FTP_AUTH_LOCAL = 244,
        DISABLE_SSH = 245,
        DISABLE_DHCP_RELAY = 246
    }

    public class CmdRequest
    {
        public Command Command { get; }
        public ParseType ParseType { get; }
        public DictionaryType DictionaryType { get; }
        public string[] Data { get; }

        public CmdRequest(Command command) : this(command, ParseType.Text, DictionaryType.None, null) { }
        public CmdRequest(Command command, params string[] data) : this(command, ParseType.Text, DictionaryType.None, data) { }
        public CmdRequest(Command command, ParseType type) : this(command, type, DictionaryType.None, null) { }
        public CmdRequest(Command command, ParseType type, DictionaryType dtype, params string[] data)
        {
            Command = command;
            ParseType = type;
            DictionaryType = dtype;
            Data = data?.ToArray();
        }
    }
}

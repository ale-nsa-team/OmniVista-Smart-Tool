using System.Linq;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Data
{
    public enum Command
    {
        NO_COMMAND,

        #region Basic Connect Switch commands
        SHOW_MICROCODE,
        SHOW_CMM,
        DEBUG_SHOW_APP_LIST,
        #endregion

        #region Refresh Switch commands
        SHOW_SYSTEM,
        SHOW_RUNNING_DIR,
        SHOW_CHASSIS,
        SHOW_PORTS_LIST,
        SHOW_POWER_SUPPLIES,
        SHOW_POWER_SUPPLY,
        SHOW_LAN_POWER,
        SHOW_CHASSIS_LAN_POWER_STATUS,
        SHOW_SLOT,
        SHOW_MAC_LEARNING,
        SHOW_TEMPERATURE,
        SHOW_HEALTH,
        SHOW_LAN_POWER_CONFIG,
        SHOW_LLDP_REMOTE,
        POWER_CLASS_DETECTION_ENABLE,
        SHOW_SLOT_LAN_POWER_STATUS,
        LLDP_SYSTEM_DESCRIPTION_ENABLE,
        SHOW_HEALTH_CONFIG,
        SHOW_LLDP_INVENTORY,
        SHOW_SYSTEM_RUNNING_DIR,
        #endregion

        #region PoE Wizard commands
        POWER_DOWN_PORT,
        POWER_UP_PORT,
        POWER_PRIORITY_PORT,
        POWER_4PAIR_PORT,
        POWER_2PAIR_PORT,
        POWER_DOWN_SLOT,
        POWER_UP_SLOT,
        POWER_823BT_ENABLE,
        POWER_823BT_DISABLE,
        POWER_HDMI_ENABLE,
        POWER_HDMI_DISABLE,
        LLDP_POWER_MDI_ENABLE,
        LLDP_POWER_MDI_DISABLE,
        LLDP_EXT_POWER_MDI_ENABLE,
        LLDP_EXT_POWER_MDI_DISABLE,
        POE_FAST_ENABLE,
        POE_PERPETUAL_ENABLE,
        SHOW_PORT_MAC_ADDRESS,
        SHOW_PORT_STATUS,
        SHOW_PORT_POWER,
        SHOW_PORT_LLDP_REMOTE,
        POE_FAST_DISABLE,
        POE_PERPETUAL_DISABLE,
        SET_MAX_POWER_PORT,
        CAPACITOR_DETECTION_ENABLE,
        CAPACITOR_DETECTION_DISABLE,
        ETHERNET_ENABLE,
        ETHERNET_DISABLE,
        SHOW_INTERFACE_PORT,
        #endregion

        #region General Switch commands
        SHOW_INTERFACES,
        WRITE_MEMORY,
        SHOW_CONFIGURATION,
        REBOOT_SWITCH,
        #endregion

        #region Virtual Meta commands
        CHECK_POWER_PRIORITY,
        RESET_POWER_PORT,
        CHECK_823BT,
        CHECK_MAX_POWER,
        CHANGE_MAX_POWER,
        CHECK_CAPACITOR_DETECTION,
        #endregion

        #region Switch Debug Log commands
        DEBUG_SHOW_LAN_POWER_STATUS,
        DEBUG_CREATE_LOG,
        DEBUG_SHOW_LEVEL,
        DEBUG_SHOW_LPNI_LEVEL,
        DEBUG_SHOW_LPCMM_LEVEL,
        DEBUG_UPDATE_LPNI_LEVEL,
        DEBUG_UPDATE_LPCMM_LEVEL,
        DEBUG_UPDATE_LLDPNI_LEVEL,
        DEBUG_CLI_UPDATE_LPNI_LEVEL,
        DEBUG_CLI_UPDATE_LPCMM_LEVEL,
        DEBUG_CLI_SHOW_LPNI_LEVEL,
        DEBUG_CLI_SHOW_LPCMM_LEVEL,
        #endregion

        #region Config Wizard commands
        DISABLE_AUTO_FABRIC,
        ENABLE_DDM,
        ENABLE_MULTICAST,
        ENABLE_QUERYING,
        ENABLE_QUERIER_FWD,
        MULTICAST_VLAN,
        DISABLE_MULTICAST,
        DISABLE_QUERYING,
        ENABLE_DHCP_RELAY,
        DHCP_RELAY_DEST,
        DISABLE_FTP,
        DISABLE_TELNET,
        ENABLE_SSH,
        SSH_AUTH_LOCAL,
        SYSTEM_TIMEZONE,
        SET_SYSTEM_NAME,
        SET_LOCATION,
        SET_PASSWORD,
        SET_CONTACT,
        SET_DEFAULT_GATEWAY,
        ENABLE_MGT_VLAN,
        SET_MGT_INTERFACE,
        SET_IP_INTERFACE,
        ENABLE_SPAN_TREE,
        SET_LOOPBACK_DET,
        SHOW_IP_SERVICE,
        SHOW_DNS_CONFIG,
        SHOW_DHCP_CONFIG,
        SHOW_DHCP_RELAY,
        SHOW_NTP_STATUS,
        SHOW_NTP_CONFIG,
        SHOW_IP_ROUTES,
        DNS_LOOKUP,
        NO_DNS_LOOKUP,
        SET_DNS_SERVER,
        DELETE_DNS_SERVER,
        DNS_DOMAIN,
        ENABLE_NTP,
        DISABLE_NTP,
        SET_NTP_SERVER,
        DELETE_NTP_SERVER,
        START_POE,
        STOP_POE,
        SNMP_AUTH_LOCAL,
        SNMP_COMMUNITY_MODE,
        SNMP_COMMUNITY_MAP,
        SNMP_NO_SECURITY,
        SNMP_STATION,
        SNMP_TRAP_AUTH,
        SNMP_USER,
        SHOW_IP_INTERFACE,
        SHOW_MULTICAST_GLOBAL,
        SHOW_MULTICAST_VLAN,
        SHOW_SNMP_SECURITY,
        SHOW_SNMP_STATION,
        SHOW_SNMP_COMMUNITY,
        SHOW_AAA_AUTH,
        SHOW_USER,
        SHOW_VLAN,
        DELETE_USER,
        DELETE_COMMUNITY,
        DELETE_STATION,
        ENABLE_TELNET,
        ENABLE_FTP,
        TELNET_AUTH_LOCAL,
        FTP_AUTH_LOCAL,
        DISABLE_SSH,
        DISABLE_DHCP_RELAY,
        CLEAR_SWLOG
        #endregion

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
        public CmdRequest(Command command, ParseType type, params string[] data) : this(command, type, DictionaryType.None, data) { }
        public CmdRequest(Command command, ParseType type, DictionaryType dtype, params string[] data)
        {
            Command = command;
            ParseType = type;
            DictionaryType = dtype;
            Data = data?.ToArray();
        }
    }
}

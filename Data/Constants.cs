using System.Collections.Generic;
using System.ComponentModel;

namespace PoEWizard.Data
{
    public static class Constants
    {
        #region enums
        public enum ThemeType { Dark, Light }
        public enum LogLevel { Error, Warn, Activity, Info, Debug, Trace }
        public enum ReportType { Error, Warning, Info, Status }
        public enum MatchOperation { EndsWith, Equals, StartsWith, Contains, Regex }
        public enum DeviceFunction { Edge, Core };
        public enum MsgBoxButtons { Ok, Cancel, OkCancel, None };
        public enum MsgBoxIcons { Info, Warning, Error, Question, None };
        public enum GridBorderions { Dhcp, Edge, Core, Lps, Lldp, Security, MaxConfigs };
        public enum AosVersion { V6, V8 };
        public enum SwitchStatus { Unknown, Reachable, Unreachable, LoginFail }
        public enum PortStatus { Unknown, Up, Down }
        public enum PoeStatus { On, Off, Searching, Fault, Deny, Conflict, PoweredOff, NoPoe }
        public enum SlotPoeStatus {
            [Description("Under Threshold")]
            UnderThreshold,
            [Description("Near Threshold")]
            NearThreshold,
            [Description("Critical")]
            Critical,
            [Description("Off")]
            Off
        }
        public enum ParseType { Htable, Htable2, Vtable, Etable, Text }
        public enum EType { Fiber, Copper, Unknown }
        public enum PriorityLevelType { Low, High, Critical }
        public enum PowerSupplyState { Up, Down, Unknown }
        public enum ChassisStatus { Unknown, Up, Down }
        public enum DictionaryType
        {
            SystemRunningDir, Chassis, Cmm, MicroCode, LanPower, LanPowerCfg, PortsList, PowerSupply, LldpRemoteList,
            MacAddressList, TemperatureList, CpuTrafficList, LldpInventoryList, SwitchDebugAppList
        }
        public enum ConfigType { Enable, Disable, Unavailable }
        public enum DeviceType
        {
            [Description("Camera")]
            Camera = 0,
            [Description("Access Point")]
            AP = 1,
            [Description("Telephone")]
            Phone = 2,
            [Description("Other")]
            Other = 3
        }
        public enum ThresholdType { Unknown, UnderThreshold, OverThreshold, Danger }
        public enum WizardResult { Starting, Ok, Warning, Fail, NothingToDo, Proceed, Skip };
        public enum PortSubType { Unknown = 0, InterfaceAlias = 1, PortComponent = 2, MacAddress = 3, NetworkAddress = 4, InterfaceName = 5, AgentCircuitId = 6, LocallyAssigned = 7 }
        public enum SyncStatusType
        {
            [Description("Unknown")]
            Unknown = 0,
            [Description("Synchronized (Certified)")]
            Synchronized = 1,
            [Description("Not Synchronized")]
            NotSynchronized = 2,
            [Description("Synchronized (Need Certified)")]
            SynchronizedNeedCertified = 3,
            [Description("Synchronized (Certified Unknown)")]
            SynchronizedUnknownCertified = 4
        }
        public enum SwitchDebugLogLevel { Off = 0, Alarm = 1, Error = 2, Alert = 3, Warn = 4, Event = 5, Info = 6, Debug1 = 7, Debug2 = 8, Debug3 = 9, Unknown = -1 }
        #endregion

        #region dictionaries
        public static readonly Dictionary<string, string> fpgaVersions = new Dictionary<string, string>()
        {
            {"OS6360-P10", "0.11"}, {"OS6360-P10A", "0.1"}, {"OS6360-P24", "0.15"}, {"OS6360-P24X", "0.12"},
            {"OS6360-PH24", "0.12"}, {"OS6360-P48", "0.15"},{"OS6360-P48X", "0.12"}, {"OS6360-PH48", "0.12"},
            {"OS6465-P6", "0.10" }, {"OS6465-P12", "0.10" }, {"OS6465-P28", "0.5" }, {"OS6465T-P12", "0.4" },
            {"OS6560-P24Z24", "0.6" }, {"OS6560-P24Z8", "0.6"}, {"OS6560-P24X4", "0.4"},
            {"OS6560-P48Z16", "0.6"}, {"OS6560-P48X4", "0.4" },
            {"OS6860", "0.9"}, {"OS6860E-P24Z8", "0.5"},
            {"OS6865-U28X", "0.14" }, {"OS6865-P16X", "0.25"}, {"OS6865-U12X", "0.25"}
        };

        public static readonly Dictionary<string, string> cpldVersions = new Dictionary<string, string>()
        {
            {"OS6860N-U28", "12" }, {"OS6860N-P48Z", "12"}, {"OS6860N-P48M", "11"},
            {"OS6860N-P24M", "12"}, {"OS6860N-P24Z", "2"}
        };

        public static readonly Dictionary<string, string> powerClassTable= new Dictionary<string, string>()
        {
            {"0", "15.4 Watts"}, {"1", "4 Watts"}, {"2", "7 Watts"}, {"3", "15.4 Watts"},
            {"4", "30 Watts"}, {"5", "45 Watts"},{"6", "60 Watts"}, {"7", "75 Watts"},
            {"8", "90-100 Watts" }
        };


        #endregion

        #region strings
        public const string DEFAULT_APP_STATUS = "Idle";
        public const string DEFAULT_PASSWORD = "switch";
        public const string DEFAULT_USERNAME = "admin";
        public const string ERROR = "ERROR: ";
        public const string FLASH_SYNCHRO = " flash-synchro";
        public const string MODEL_NAME = "Model Name";
        public const string SERIAL_NUMBER = "Serial Number";
        public const string WORKING_DIR = "Working";
        public const string CERTIFIED_DIR = "Certified";
        public const string MIN_AOS_VERSION = "8.9 R1";
        public const string VCBOOT_PATH = "/flash/working/vcboot.cfg";
        public const string SWLOG_PATH = "/flash/tech_support_complete.tar";
        // Used by "Utils" class
        public const string P_CHASSIS = "CHASSIS_ID";
        public const string P_SLOT = "SLOT_ID";
        public const string P_PORT = "PORT_ID";
        // Used by "SHOW_PORTS_LIST"
        public const string CHAS_SLOT_PORT = "Chas/Slot/Port";
        public const string PORT = "Port";
        public const string MAX_POWER = "Max Power";
        public const string MAXIMUM = "Maximum(mW)";
        public const string USED = "Actual Used(mW)";
        public const string USAGE_THRESHOLD = "Usage Threshold";
        public const string ADMIN_STATUS = "Admin Status";
        public const string OPERATIONAL_STATUS = "Operational Status";
        public const string LINK_STATUS = "Link Status";
        public const string STATUS = "Status";
        public const string BT_SUPPORT = "8023BT Support";
        public const string CLASS_DETECTION = "Class Detection";
        public const string HI_RES_DETECTION = "High-Res Detection";
        public const string FPOE = "FPOE";
        public const string PPOE = "PPOE";
        public const string PRIORITY = "Priority";
        public const string CLASS = "Class";
        public const string TYPE = "Type";
        public const string PRIO_DISCONNECT = "Priority Disconnect";
        // Used by "SHOW_LAN_POWER"
        public const string POWERED_ON = "Powered On";
        public const string POWERED_OFF = "Powered Off";
        public const string SEARCHING = "Searching";
        public const string FAULT = "Fault";
        public const string DENY = "Deny";
        public const string BAD_VOLTAGE_INJECTION = "Bad!VoltInj";
        public const string ON_OFF= "On/Off";
        // Used by "SHOW_CHASSIS"
        public const string ID = "ID";
        public const string MODULE_TYPE = "Module Type";
        public const string ROLE = "Role";
        public const string PART_NUMBER = "Part Number";
        public const string HARDWARE_REVISION = "Hardware Revision";
        public const string CHASSIS_MAC_ADDRESS = "MAC Address";
        public const string INIT_STATUS = "Init Status";
        // Used by "SHOW_SYSTEM"
        public const string NAME = "Name";
        public const string DESCRIPTION = "Description";
        public const string LOCATION = "Location";
        public const string CONTACT = "Contact";
        public const string UP_TIME = "Up Time";
        // Used by "SHOW_CMM":
        public const string FPGA = "FPGA";
        public const string CPLD = "CPLD";
        // Used by "SHOW_RUNNING_DIR"
        public const string RUNNING_CONFIGURATION = "Running configuration";
        public const string SYNCHRONIZATION_STATUS = "Running Configuration";
        // Used by "SHOW_SYSTEM_RUNNING_DIR"
        public const string CONFIG_CHANGE_STATUS = "configChangeStatus";
        public const string SYS_NAME = "sysName";
        public const string SYS_DESCR = "sysDescr";
        public const string SYS_LOCATION = "sysLocation";
        public const string SYS_CONTACT = "sysContact";
        public const string SYS_UP_TIME = "sysUpTime";
        public const string SYS_RUNNING_CONFIGURATION = "chasControlCurrentRunningVersion";
        public const string CHAS_CONTROL_CERTIFY = "chasControlCertifyStatus";
        public const string SYNCHRONIZED = "Synchronized";
        public const string NOT_SYNCHRONIZED = "Not Synchronized";
        // Used by "SHOW_MICROCODE"
        public const string RELEASE = "Release";
        // Used by SHOW_POWER_SUPPLY
        public const string CHAS_PS = "Chassis/PS";
        public const string POWER = "Power Provision";
        public const string PS_TYPE = "Module Type";
        // Used by "SHOW_MAC_LEARNING" and "SHOW_MAC_LEARNING_PORT"
        public const string PORT_MAC_LIST = "Mac Address";
        public const string INTERFACE = "Interface";
        // Used by "SHOW_LAN_POWER_CONFIG"
        public const string POWER_4PAIR = "4-Pair";
        public const string POWER_OVER_HDMI = "power-over -HDMI";
        public const string POWER_CAPACITOR_DETECTION = "Capacitor Detection";
        public const string POWER_823BT = "802.3bt";
        // Used by "SHOW_LLDP_REMOTE"
        public const string REMOTE_ID = "Remote ID";
        public const string LOCAL_PORT = "Local Port";
        public const string REMOTE_PORT = "Remote Port";
        public const string PORT_SUBTYPE = "Port Subtype";
        public const string CAPABILITIES_ENABLED = "Capabilities Enabled";
        public const string SYSTEM_NAME = "System Name";
        public const string SYSTEM_DESCRIPTION = "System Description";
        public const string MED_CAPABILITIES = "MED Capabilities";
        public const string MAU_TYPE = "Mau Type";
        public const string MED_POWER_TYPE = "MED Power Type";
        public const string MED_POWER_SOURCE = "MED Power Source";
        public const string MED_POWER_PRIORITY = "MED Power Priority";
        public const string MED_POWER_VALUE = "MED Power Value";
        public const string MED_IP_ADDRESS = "Management IP Address";
        public const string MAC_NAME = "MAC Name";
        // Used by "SHOW_LLDP_INVENTORY"
        public const string MED_MANUFACTURER = "Manufacturer Name";
        public const string MED_MODEL = "Model Name";
        public const string MED_HARDWARE_REVISION = "Hardware Revision";
        public const string MED_FIRMWARE_REVISION = "Firmware Revision";
        public const string MED_SOFTWARE_REVISION = "Software Revision";
        public const string MED_SERIAL_NUMBER = "Serial Number";
        // Used by "SHOW_CHASSIS"
        public const string CHAS_DEVICE = "Chassis/Device";
        public const string CURRENT = "Current";
        public const string RANGE = "Range";
        public const string DANGER = "Danger";
        public const string THRESHOLD = "Thresh";
        // Used by "SHOW_HEALTH"
        public const string CPU_THRESHOLD = "CPU Threshold";
        public const string CPU = "CPU";
        public const string SWITCH = "Switch";
        // Used by SwitchDebugModel class
        public const string DEBUG_SWITCH_LOG = "systemSwitchLogging";
        public const string DEBUG_APP_ID = "systemSwitchLoggingApplicationAppId";
        public const string DEBUG_SUB_APP_ID = "systemSwitchLoggingApplicationSubAppId";
        public const string DEBUG_APP_NAME = "systemSwitchLoggingApplicationAppName";
        public const string DEBUG_SUB_APP_NAME = "systemSwitchLoggingApplicationSubAppName";
        public const string DEBUG_SUB_APP_LEVEL = "systemSwitchLoggingApplicationSubAppLevel";
        public const string DEBUG_SUB_APP_LANNI = "LanNi";
        public const string DEBUG_SUB_APP_LANXTR = "LanXtr";
        public const string DEBUG_SUB_APP_LANUTIL = "LanNiUtl";
        public const string DEBUG_SUB_APP_LANCMM = "LanCmm";
        public const string DEBUG_SUB_APP_LANCMMPWR = "LanCmmPwr";
        public const string DEBUG_SUB_APP_LANCMMMIP = "LanCmmMip";
        public const string DEBUG_SUB_APP_LANCMMUTL = "LanCmmUtl";
        public const string DEBUG_CLI_APP_NAME = "Application Name";
        public const string DEBUG_CLI_SUB_APP_ID = "SubAppl ID";
        public const string DEBUG_CLI_SUB_APP_NAME = "Sub Application Name";
        public const string DEBUG_CLI_SUB_APP_LEVEL = "Level";
        public const string LPNI = "lpNi";
        public const string LPCMM = "lpCmm";
        public const string DEBUG_APP_INDEX = "systemSwitchLoggingApplicationIndex";
        // Used by "SHOW_INTERFACES"
        public const string TRAF_SLOT_PORT = "Chassis/Slot/Port";
        public const string TRAF_STATUS = "Operational Status";
        public const string TRAF_LINK_QUALITY = "Link-Quality";
        public const string TRAF_MAC_ADDRESS = "MAC address";
        public const string TRAF_BANDWIDTH = "BandWidth (Megabits)";
        public const string TRAF_LONG_FRAME_SIZE = "Long Frame Size(Bytes)";
        public const string TRAF_INTER_FRAME_GAP = "Inter Frame Gap(Bytes)";
        public const string TRAF_RX_BYTES = "Bytes Received";
        public const string TRAF_TX_BYTES = "Bytes Xmitted";
        public const string TRAF_UNICAST_FRAMES = "Unicast Frames";
        public const string TRAF_BROADCAST_FRAMES = "Broadcast Frames";
        public const string TRAF_MULTICAST_FRAMES = "M-cast Frames";
        public const string TRAF_UNDERSIZE_FRAMES = "UnderSize Frames";
        public const string TRAF_OVERSIZE_FRAMES = "OverSize Frames";
        public const string TRAF_LOST_FRAMES = "Lost Frames";
        public const string TRAF_ERROR_FRAMES = "Error Frames";
        public const string TRAF_CRC_ERROR_FRAMES = "CRC Error Frames";
        public const string TRAF_ALIGNEMENTS_ERROR = "Alignments Err";
        public const string TRAF_COLLIDED_FRAMES = "Collided Frames";
        public const string TRAF_COLLISIONS = "Collisions";
        public const string TRAF_LATE_COLLISIONS = "Late collisions";
        public const string TRAF_EXC_COLLISIONS = "Exc-Collisions";
        // Used by Config Wizard commands
        public const string SWITCH_CFG_DNS = "systemDNS";
        public const string SWITCH_CFG_DHCP = "alaDhcpRelay";
        public const string SWITCH_CFG_NTP = "alaNtpPeer";
        // Used by BuildOuiTable method of MainWindow.xaml.cs class
        public const string OUI_FILE = "oui.csv";

        public const string C = " \u2103";
        public const string F = " \u2109";
        #endregion

        #region regex patterns
        public const string MATCH_S_2 = "\\s{2,}";
        public const string MATCH_PORT = @"(?=Port:)";
        public const string MATCH_COLON = "([^:]+):(.+)";
        public const string MATCH_EQUALS = "([^:]+)=(.+)";
        public const string MATCH_TABLE_SEP = @"(-+\++)+";
        public const string MATCH_CHASSIS = @"([Local|Remote] Chassis ID )(\d+) \((.+)\)";
        public const string MATCH_AOS_VERSION = @"(\d+)\.(\d+)([\.\d +]+)(\.R)(\d+)";
        public const string MATCH_POE_RUNNING =@"Lanpower chassis \d slot \d service running ...";
        #endregion
    }
}

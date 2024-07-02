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
        public enum TemplateOptions { Dhcp, Edge, Core, Lps, Lldp, Security, MaxConfigs };
        public enum AosVersion { V6, V8 };
        public enum SwitchStatus { Unknown, Reachable, Unreachable, LoginFail }
        public enum PortStatus { Unknown, Up, Down }
        public enum PoeStatus { On, Off, Fault, Deny, Conflict, NoPoe }
        public enum EType { Fiber, Copper, Unknown }
        public enum PriorityLevelType { Low, High, Critical, Unknown }
        public enum PowerSupplyState { Up, Down, Unknown }
        public enum ChassisStatus { Unknown, Up, Down }
        public enum DictionaryType { System, Chassis, RunningDir, MicroCode, LanPower, PortsList }

        #endregion

        #region strings
        public const char SPACE = ' ';
        public const string A_LITERAL = "a";
        public const string ACCESS_SWITCH = "Access";
        public const string ADMIN_STATE = "Admin-State";
        public const string AUTHENTICATION_FAILED = "Authentication failure";
        public const string CENTRAL_SWITCH = "Central Switch";
        public const string CERTIFIED_DIR = "Certified";
        public const string CHASSIS_MAC = "MAC Address";
        public const string CLI_FULL_PROMPT = "Cli Default Full Prompt";
        public const string CLI_PROMPT = "Cli Default Prompt";
        public const string CLI_TIMEOUT = "Cli Inactivity Timer in minutes";
        public const string SECONDARY_DEVICE = "Cannot apply to the Secondary device, please disconnect and connect to the Primary switch.";
        public const string COMMA = ",";
        public const string COMMENT_CHAR = "!";
        public const string CONFIRM_EXIT_Y_N = "Confirm exit (Y/N)?";
        public const string DEFAULT_APP_STATUS = "Idle";
        public const string DEFAULT_DHCP_IP = "192.168.255.254";
        public const string DEFAULT_PASSWORD = "switch";
        public const string DEFAULT_PROMPT = "->";
        public const string DEFAULT_USERNAME = "admin";
        public const string DHCP = "DHCP";
        public const string DHCPD_CONF = "dhcpd.conf";
        public const string DHCPD_PCY = "dhcpd.pcy";
        public const string ENABLED = "ENABLED";
        public const string EOF_STRING = "EOF";
        public const string ERROR = "ERROR: ";
        public const string ESC_STRING = "\u001B";
        public const string FLASH_SWITCH_DIR = "/flash/switch/";
        public const string FLASH_WORKING_DIR = "/flash/working/";
        public const string FLASH_SYNCHRO = " flash-synchro";
        public const string HAS_BEEN_APPLIED_CONFIG_FILE_NAME = HAS_BEEN_APPLIED_CONFIG_FLAG_NAME + ".txt";
        public const string HAS_BEEN_APPLIED_CONFIG_FLAG = FLASH_WORKING_DIR + HAS_BEEN_APPLIED_CONFIG_FILE_NAME;
        public const string HAS_BEEN_APPLIED_CONFIG_FLAG_NAME = "config_wizard_applied";
        public const string LANPOWER_RUNNING = "Lanpower chassis 1 slot 1 service running ...";
        public const string LINE_FEED = "\n";
        public const string LOCKED = "LOCKED";
        public const string LOGIN_ATTEMPTS = "Login Attempts";
        public const string LOGIN_PROMPT = "login:";
        public const string LOGIN_PROMPT_6X = "login :";
        public const string LOGOUT = "logout";
        public const string LOGOUT_RESPONSE = "Ylogout";
        public const string LPS_DISABLED = "LPS not enabled";
        public const string MAC_6 = "MAC Address";
        public const string MAC_8 = "MAC";
        public const string MAC_ADDRESS = "Mac Address";
        public const string MODEL_NAME = "Model Name";
        public const string NO_PORT_SECURITY = "No Port Security";
        public const string OPERATION_MODE = "Operation Mode";
        public const string OR_CHAR = "|";
        public const string PASSWORD = " password ";
        public const string PASSWORD_PROMPT = "assword:";
        public const string PASSWORD_PROMPT_6X = "assword :";
        public const string RESTRICTED = "RESTRICTED";
        public const string RUNNING_CMM = "Running CMM";
        public const string SECONDARY = "SECONDARY";
        public const string SERIAL_NUMBER = "Serial Number";
        public const string SESSION_TIMEOUT = "30";
        public const string SLAVE_PRIMARY = "SLAVE-PRIMARY";
        public const string TEMPLATE_FOLDER = "templates";
        public const string TXT_EXTENSION = ".txt";
        public const string USER = "user ";
        public const string VALID_MACS = "Total number of Valid MAC addresses above";
        public const string VI = "vi";
        public const string VI_QUIT = ":q!";
        public const string VI_X = ":x";
        public const string VIEWING_ROOM_SWITCH = "Viewing Room";
        public const string WORKING = "WORKING";
        public const string WORKING_DIR = "Working";
        public const string Y = "Y";
        public const string Y_LITERAL = "Y";

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
        public const string POWERED_ON = "Powered On";
        public const string POWERED_OFF = "Powered Off";
        public const string SEARCHING = "Searching";
        public const string FAULT = "Fault";
        public const string DENY = "Deny";
        public const string BAD_VOLTAGE_INJECTION = "Bad!VoltInj";
        // Used by "SHOW_CHASSIS"
        public const string ID = "ID";
        public const string MODULE_TYPE = "Module Type";
        public const string ROLE = "Role";
        public const string OPERATIONAL_STATUS = "Operational Status";
        public const string PART_NUMBER = "Part Number";
        public const string HARDWARE_REVISION = "Hardware Revision";
        public const string CHASSIS_MAC_ADDRESS = "MAC Address";
        // Used by "SHOW_SYSTEM"
        public const string NAME = "Name";
        public const string DESCRIPTION = "Description";
        public const string LOCATION = "Location";
        public const string CONTACT = "Contact";
        public const string UP_TIME = "Up Time";
        // Used by "SHOW_RUNNING_DIR"
        public const string RUNNING_CONFIGURATION = "Running configuration";
        // Used by "SHOW_MICROCODE"
        public const string RELEASE = "Release";
        #endregion

        #region integers
        public const int CONN_TIMEOUT = 10000;
        public const int DEFAULT_BAUD_RATE = 9600;
        public const int ESC_BYTE = 0x1b;
        #endregion

        #region regex patterns
        public const string ESC_SEQUENCE = @"\u001b\[[?0-9;]*[a-zA-Z]";
        public const string MATCH_ANY_CHAR = ".+";
        public const string MATCH_MODEL = "^(.*-)([A-Z]?)([0-9]{1,2})(.*)$";
        public const string MATCH_MORE = @"\r\n\r\n--MORE--\(\d+%\)$";
        public const string MATCH_VERSION = @"[\d]+[.][\d]+[.][\d]+[.][\d]*[.]?[R][\d]+";
        public const string PATTERN_CMM_OS6X = "CMM in slot";
        public const string PATTERN_CMM_OS8X = "in slot CMM";
        public const string SPACE_REGEX_S_2 = "\\s{2,}";
        public const string MATCH_PORT = @"(?=Port:)";
        #endregion
    }
}

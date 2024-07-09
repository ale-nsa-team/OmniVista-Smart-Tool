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
        public enum SlotPoeStatus { Normal, NearThreshold, Critical }
        public enum EType { Fiber, Copper, Unknown }
        public enum PriorityLevelType { Low, High, Critical, Unknown }
        public enum PowerSupplyState { Up, Down, Unknown }
        public enum ChassisStatus { Unknown, Up, Down }
        public enum DictionaryType { System, Chassis, RunningDir, MicroCode, LanPower, LanPowerCfg, PortsList, PowerSupply, LldpRemoteList }
        public enum ConfigType { Enabled, Disabled, Unavailable }

        #endregion

        #region strings
        public const string DEFAULT_APP_STATUS = "Idle";
        public const string DEFAULT_PASSWORD = "switch";
        public const string DEFAULT_USERNAME = "admin";
        public const string ERROR = "ERROR: ";
        public const string FLASH_SYNCHRO = " flash-synchro";
        public const string MODEL_NAME = "Model Name";
        public const string SERIAL_NUMBER = "Serial Number";
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
        public const string SYNCHRONIZATION_STATUS = "Running Configuration";
        // Used by "SHOW_MICROCODE"
        public const string RELEASE = "Release";
        // Used by SHOW_POWER_SUPPLY
        public const string CHAS_PS = "Chassis/PS";
        public const string POWER = "Power Provision";
        public const string PS_TYPE = "Module Type";
        // Used by "SHOW_MAC_LEARNING_PORT"
        public const string PORT_MAC_LIST = "static address,  Mac Address";
        // Used by "SHOW_LAN_POWER_CONFIG"
        public const string POWER_4PAIR = "4-Pair";
        public const string POWER_OVER_HDMI = "power-over -HDMI";
        public const string POWER_CAPACITOR_DETECTION = "Capacitor Detection";
        public const string POWER_823BT = "802.3bt";
        // Used by "SHOW_LLDP_REMOTE"
        public const string LOCAL_PORT = "Local Port";
        public const string REMOTE_PORT = "Remote Port";
        public const string CAPABILITIES_ENABLED = "Capabilities Enabled";
        public const string MED_DEVICE_TYPE = "MED Device Type";
        public const string MED_CAPABILITIES = "MED Capabilities";
        public const string MAU_TYPE = "Mau Type";
        public const string MED_POWER_TYPE = "MED Power Type";
        public const string MED_POWER_SOURCE = "MED Power Source";
        public const string MED_POWER_PRIORITY = "MED Power Priority";
        public const string MED_POWER_VALUE = "MED Power Value";
        public const string MED_IP_ADDRESS = "Management IP Address";
        #endregion

        #region regex patterns
        public const string MATCH_S_2 = "\\s{2,}";
        public const string MATCH_PORT = @"(?=Port:)";
        public const string MATCH_COLON = "([^:]+):(.+)";
        public const string MATCH_EQUALS = "([^:]+)=(.+)";
        public const string MATCH_TABLE_SEP = @"(-+\++)+";
        public const string MATCH_CHASSIS = @"([Local|Remote] Chassis ID )(\d+) \((.+)\)";
        #endregion
    }
}

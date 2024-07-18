using PoEWizard.Exceptions;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;

namespace PoEWizard.Data
{
    public class RestUrl
    {

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

        public enum CommandType
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
            // 70 - 99: Special switch commands
            WRITE_MEMORY = 70,
            SHOW_CONFIGURATION = 71,
            REBOOT_SWITCH = 72,
            // 100 - 119: Virtual commands
            CHECK_POWER_PRIORITY = 100,
            RESET_POWER_PORT = 101,
            CHECK_823BT = 102
        }

        private readonly static Dictionary<CommandType, string> CLI_TABLE = new Dictionary<CommandType, string>
        {
            // 0 - 29: Basic commands to gather switch data
            [CommandType.SHOW_SYSTEM] = "show system",                                            //  0
            [CommandType.SHOW_MICROCODE] = "show microcode",                                      //  1
            [CommandType.SHOW_RUNNING_DIR] = "show running-directory",                            //  2
            [CommandType.SHOW_CHASSIS] = "show chassis",                                          //  3
            [CommandType.SHOW_PORTS_LIST] = "show interfaces alias",                              //  4
            [CommandType.SHOW_POWER_SUPPLIES] = $"show powersupply",                              //  5
            [CommandType.SHOW_POWER_SUPPLY] = $"show powersupply {DATA_0}",                       //  6
            [CommandType.SHOW_LAN_POWER] = $"show lanpower slot {DATA_0}",                        //  7
            [CommandType.SHOW_CHASSIS_LAN_POWER_STATUS] = $"show lanpower chassis {DATA_0} status", //  8
            [CommandType.SHOW_SLOT] = $"show slot {DATA_0}",                                      //  9
            [CommandType.SHOW_MAC_LEARNING] = $"show mac-learning domain vlan",                   // 10
            [CommandType.SHOW_TEMPERATURE] = $"show temperature",                                 // 11
            [CommandType.SHOW_HEALTH] = $"show health all cpu",                                   // 12
            [CommandType.SHOW_LAN_POWER_CONFIG] = $"show lanpower slot {DATA_0} port-config",     // 13
            [CommandType.SHOW_LLDP_REMOTE] = "show lldp remote-system",                           // 14
            [CommandType.SHOW_CMM] = "show cmm",                                                  // 15
            [CommandType.POWER_CLASS_DETECTION_ENABLE] = $"lanpower slot {DATA_0} class-detection enable", // 16
            [CommandType.SHOW_SLOT_LAN_POWER_STATUS] = $"show lanpower slot {DATA_0} status",     //  17
            [CommandType.LLDP_SYSTEM_DESCRIPTION_ENABLE] = "lldp nearest-bridge chassis tlv management port-description enable system-name enable system-description enable", // 18
            [CommandType.SHOW_HEALTH_CONFIG] = "show health configuration",                       // 19
            // 30 - 69: Commands related to actions on port
            [CommandType.POWER_DOWN_PORT] = $"lanpower port {DATA_0} admin-state disable",        // 30
            [CommandType.POWER_UP_PORT] = $"lanpower port {DATA_0} admin-state enable",           // 31
            [CommandType.POWER_PRIORITY_PORT] = $"lanpower port {DATA_0} priority {DATA_1}",      // 32
            [CommandType.POWER_4PAIR_PORT] = $"lanpower port {DATA_0} 4pair enable",              // 33
            [CommandType.POWER_2PAIR_PORT] = $"lanpower port {DATA_0} 4pair disable",             // 34
            [CommandType.POWER_DOWN_SLOT] = $"lanpower slot {DATA_0} service stop",               // 35
            [CommandType.POWER_UP_SLOT] = $"lanpower slot {DATA_0} service start",                // 36
            [CommandType.POWER_823BT_ENABLE] = $"lanpower slot {DATA_0} 8023bt enable",           // 37
            [CommandType.POWER_823BT_DISABLE] = $"lanpower slot {DATA_0} 8023bt disable",         // 38
            [CommandType.POWER_HDMI_ENABLE] = $"lanpower port {DATA_0} power-over-hdmi enable",   // 39
            [CommandType.POWER_HDMI_DISABLE] = $"lanpower port {DATA_0} power-over-hdmi disable", // 40
            [CommandType.LLDP_POWER_MDI_ENABLE] = $"lldp nearest-bridge port {DATA_0} tlv dot3 power-via-mdi enable",          // 41
            [CommandType.LLDP_POWER_MDI_DISABLE] = $"lldp nearest-bridge port {DATA_0} tlv dot3 power-via-mdi disable",        // 42
            [CommandType.LLDP_EXT_POWER_MDI_ENABLE] = $"lldp nearest-bridge port {DATA_0} tlv med ext-power-via-mdi enable",   // 43
            [CommandType.LLDP_EXT_POWER_MDI_DISABLE] = $"lldp nearest-bridge port {DATA_0} tlv med ext-power-via-mdi disable", // 44
            [CommandType.POE_FAST_ENABLE] = $"lanpower slot {DATA_0} fpoe enable",                // 45
            [CommandType.POE_PERPETUAL_ENABLE] = $"lanpower slot {DATA_0} ppoe enable",           // 46
            [CommandType.SHOW_PORT_MAC_ADDRESS] = $"show mac-learning port {DATA_0}",             // 47
            [CommandType.SHOW_PORT_STATUS] = $"show interfaces port {DATA_0} alias",              // 48
            [CommandType.SHOW_PORT_POWER] = $"show lanpower slot {DATA_0}|grep {DATA_1}",         // 49
            [CommandType.SHOW_PORT_LLDP_REMOTE] = $"show lldp port {DATA_0} remote-system",       // 50
            [CommandType.POE_FAST_DISABLE] = $"lanpower slot {DATA_0} fpoe disable",              // 51
            [CommandType.POE_PERPETUAL_DISABLE] = $"lanpower slot {DATA_0} ppoe disable",         // 52
            [CommandType.SET_MAX_POWER_PORT] = $"lanpower port {DATA_0} power {DATA_1}",          // 53
            // 70 - 99: Special switch commands
            [CommandType.WRITE_MEMORY] = "write memory flash-synchro",                            // 70
            [CommandType.SHOW_CONFIGURATION] = "show configuration snapshot",                     // 71
            [CommandType.REBOOT_SWITCH] = "reload from working no rollback-timeout"               // 72
        };

        public static string ParseUrl(RestUrlEntry entry)
        {
            string cli = GetCliFromTable(entry.RestUrl, entry.Data).Trim();
            if (string.IsNullOrEmpty(cli)) return null;
            return $"cli/aos?cmd={WebUtility.UrlEncode(cli)}";
        }

        private static string GetCliFromTable(CommandType restUrlId, string[] data)
        {
            if (CLI_TABLE.ContainsKey(restUrlId))
            {
                string url = CLI_TABLE[restUrlId];
                switch (restUrlId)
                {

                    // 0 - 29: Basic commands to gather switch data
                    case CommandType.SHOW_POWER_SUPPLY:           //  6
                    case CommandType.SHOW_LAN_POWER:              //  7
                    case CommandType.SHOW_CHASSIS_LAN_POWER_STATUS: //  8
                    case CommandType.SHOW_LAN_POWER_CONFIG:       // 13
                    case CommandType.POWER_CLASS_DETECTION_ENABLE: // 16
                    case CommandType.SHOW_SLOT_LAN_POWER_STATUS:  //17
                    // 30 - 69: Commands related to actions on port
                    case CommandType.POWER_DOWN_PORT:             // 30
                    case CommandType.POWER_UP_PORT:               // 31
                    case CommandType.POWER_4PAIR_PORT:            // 33
                    case CommandType.POWER_2PAIR_PORT:            // 34
                    case CommandType.POWER_DOWN_SLOT:             // 35
                    case CommandType.POWER_UP_SLOT:               // 36
                    case CommandType.POWER_823BT_ENABLE:          // 37
                    case CommandType.POWER_823BT_DISABLE:         // 38
                    case CommandType.POWER_HDMI_ENABLE:           // 39
                    case CommandType.POWER_HDMI_DISABLE:          // 40
                    case CommandType.LLDP_POWER_MDI_ENABLE:       // 41
                    case CommandType.LLDP_POWER_MDI_DISABLE:      // 42
                    case CommandType.LLDP_EXT_POWER_MDI_ENABLE:   // 43
                    case CommandType.LLDP_EXT_POWER_MDI_DISABLE:  // 44
                    case CommandType.POE_FAST_ENABLE:             // 45
                    case CommandType.POE_PERPETUAL_ENABLE:        // 46
                    case CommandType.SHOW_PORT_MAC_ADDRESS:       // 47
                    case CommandType.SHOW_PORT_STATUS:            // 48
                    case CommandType.SHOW_PORT_LLDP_REMOTE:       // 50
                    case CommandType.POE_FAST_DISABLE:            // 51
                    case CommandType.POE_PERPETUAL_DISABLE:       // 52
                        if (data == null || data.Length < 1) throw new SwitchCommandError($"Invalid url {Utils.PrintEnum(restUrlId)}!");
                        return url.Replace(DATA_0, (data == null || data.Length < 1) ? "" : data[0]);

                    // 30 - 69: Commands related to actions on port
                    case CommandType.POWER_PRIORITY_PORT:         // 32
                    case CommandType.SHOW_PORT_POWER:             // 49
                        if (data == null || data.Length < 2) throw new SwitchCommandError($"Invalid url {Utils.PrintEnum(restUrlId)}!");
                        return url.Replace(DATA_0, data[0]).Replace(DATA_1, data[1]);

                    // 100 - 119: Virtual commands
                    case CommandType.CHECK_POWER_PRIORITY:                //  100
                    case CommandType.RESET_POWER_PORT:                    //  101
                    case CommandType.NO_COMMAND:                          //  -1
                        return null;

                    default:
                        return url;
                }
            }
            else
            {
                throw new SwitchCommandError($"Invalid url {Utils.PrintEnum(restUrlId)}!");
            }
        }

    }
}

using PoEWizard.Exceptions;
using System.Collections.Generic;
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
        public const string OUTPUT = "output";
        public const string NODE = "node";
        public const string HTTP_RESPONSE = "diag";

        public const string DATA_0 = "%1_DATA_1%";
        public const string DATA_1 = "%2_DATA_2%";
        public const string DATA_2 = "%3_DATA_3%";
        public const string DATA_3 = "%4_DATA_4%";

        public enum RestUrlId
        {
            // 0 - 29: Basic commands to gather switch data
            SHOW_SYSTEM = 0,
            SHOW_MICROCODE = 1,
            SHOW_RUNNING_DIR = 2,
            SHOW_CHASSIS = 3,
            SHOW_PORTS_LIST = 4,
            SHOW_POWER_SUPPLIES = 5,
            SHOW_POWER_SUPPLY = 6,
            SHOW_LAN_POWER = 7,
            SHOW_LAN_POWER_STATUS = 8,
            SHOW_SLOT = 9,
            SHOW_MAC_LEARNING = 10,
            SHOW_TEMPERATURE = 11,
            SHOW_HEALTH = 12,
            SHOW_LAN_POWER_CONFIG = 13,
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
            // 70 - 99: Special switch commands
            WRITE_MEMORY = 70,
            SHOW_CONFIGURATION = 71,
            // 100 - 119: Virtual commands
            CHECK_POWER_PRIORITY = 100,
        }

        public static Dictionary<RestUrlId, string> REST_URL_TABLE = new Dictionary<RestUrlId, string>
        {
            // 0 - 29: Basic commands to gather switch data
            [RestUrlId.SHOW_SYSTEM] = "show system",                                            //  0
            [RestUrlId.SHOW_MICROCODE] = "show microcode",                                      //  1
            [RestUrlId.SHOW_RUNNING_DIR] = "show running-directory",                            //  2
            [RestUrlId.SHOW_CHASSIS] = "show chassis",                                          //  3
            [RestUrlId.SHOW_PORTS_LIST] = "show interfaces alias",                              //  4
            [RestUrlId.SHOW_POWER_SUPPLIES] = $"show powersupply",                              //  5
            [RestUrlId.SHOW_POWER_SUPPLY] = $"show powersupply {DATA_0}",                       //  6
            [RestUrlId.SHOW_LAN_POWER] = $"show lanpower slot {DATA_0}",                        //  7
            [RestUrlId.SHOW_LAN_POWER_STATUS] = $"show lanpower chassis {DATA_0} status",       //  8
            [RestUrlId.SHOW_SLOT] = $"show slot {DATA_0}",                                      //  9
            [RestUrlId.SHOW_MAC_LEARNING] = $"show mac-learning domain vlan",                   // 10
            [RestUrlId.SHOW_TEMPERATURE] = $"show temperature",                                 // 11
            [RestUrlId.SHOW_HEALTH] = $"show health all cpu",                                   // 12
            [RestUrlId.SHOW_LAN_POWER_CONFIG] = $"show lanpower slot {DATA_0} port-config",     // 13
            // 30 - 69: Commands related to actions on port
            [RestUrlId.POWER_DOWN_PORT] = $"lanpower port {DATA_0} admin-state disable",        // 30
            [RestUrlId.POWER_UP_PORT] = $"lanpower port {DATA_0} admin-state enable",           // 31
            [RestUrlId.POWER_PRIORITY_PORT] = $"lanpower port {DATA_0} priority {DATA_1}",      // 32
            [RestUrlId.POWER_4PAIR_PORT] = $"lanpower port {DATA_0} 4pair enable",              // 33
            [RestUrlId.POWER_2PAIR_PORT] = $"lanpower port {DATA_0} 4pair disable",             // 34
            [RestUrlId.POWER_DOWN_SLOT] = $"lanpower slot {DATA_0} service stop",               // 35
            [RestUrlId.POWER_UP_SLOT] = $"lanpower slot {DATA_0} service start",                // 36
            [RestUrlId.POWER_823BT_ENABLE] = $"lanpower slot {DATA_0} 8023bt enable",           // 37
            [RestUrlId.POWER_823BT_DISABLE] = $"lanpower slot {DATA_0} 8023bt disable",         // 38
            [RestUrlId.POWER_HDMI_ENABLE] = $"lanpower port {DATA_0} power-over-hdmi enable",   // 39
            [RestUrlId.POWER_HDMI_DISABLE] = $"lanpower port {DATA_0} power-over-hdmi disable", // 40
            [RestUrlId.LLDP_POWER_MDI_ENABLE] = $"lldp nearest-bridge port {DATA_0} tlv dot3 power-via-mdi enable",          // 41
            [RestUrlId.LLDP_POWER_MDI_DISABLE] = $"lldp nearest-bridge port {DATA_0} tlv dot3 power-via-mdi disable",        // 42
            [RestUrlId.LLDP_EXT_POWER_MDI_ENABLE] = $"lldp nearest-bridge port {DATA_0} tlv med ext-power-via-mdi enable",   // 43
            [RestUrlId.LLDP_EXT_POWER_MDI_DISABLE] = $"lldp nearest-bridge port {DATA_0} tlv med ext-power-via-mdi disable", // 44
            [RestUrlId.POE_FAST_ENABLE] = $"lanpower slot {DATA_0} fpoe enable",                // 45
            [RestUrlId.POE_PERPETUAL_ENABLE] = $"lanpower slot {DATA_0} ppoe enable",           // 46
            [RestUrlId.SHOW_PORT_MAC_ADDRESS] = $"show mac-learning port {DATA_0}",             // 47
            [RestUrlId.SHOW_PORT_STATUS] = $"show interfaces port {DATA_0} alias",              // 48
            [RestUrlId.SHOW_PORT_POWER] = $"show lanpower slot {DATA_0}|grep {DATA_1}",         // 49
            // 70 - 99: Special switch commands
            [RestUrlId.WRITE_MEMORY] = "write memory flash-synchro",                            // 70
            [RestUrlId.SHOW_CONFIGURATION] = "show configuration snapshot"                      // 71
        };

        public static string ParseUrl(RestUrlEntry entry)
        {
            string url = $"cli/aos?cmd={WebUtility.UrlEncode(GetUrlFromTable(entry.RestUrl, entry.Data).Trim())}";
            return url;
        }

        private static string GetUrlFromTable(RestUrlId restUrlId, string[] data)
        {
            if (REST_URL_TABLE.ContainsKey(restUrlId))
            {
                string url = REST_URL_TABLE[restUrlId];
                switch (restUrlId)
                {
                    // 0 - 19: Basic commands to gather switch data
                    case RestUrlId.SHOW_SYSTEM:                 //  0
                    case RestUrlId.SHOW_MICROCODE:              //  1
                    case RestUrlId.SHOW_RUNNING_DIR:            //  2
                    case RestUrlId.SHOW_CHASSIS:                //  3
                    case RestUrlId.SHOW_PORTS_LIST:             //  4
                    case RestUrlId.SHOW_POWER_SUPPLIES:         //  5
                    case RestUrlId.SHOW_SLOT:                   //  9
                    case RestUrlId.SHOW_MAC_LEARNING:           // 10
                    case RestUrlId.SHOW_TEMPERATURE:            // 11
                    case RestUrlId.SHOW_HEALTH:                 // 12
                    // 70 - 99: Special switch commands
                    case RestUrlId.WRITE_MEMORY:                // 70
                    case RestUrlId.SHOW_CONFIGURATION:          // 71
                        return url;

                    // 0 - 19: Basic commands to gather switch data
                    case RestUrlId.SHOW_POWER_SUPPLY:           //  6
                    case RestUrlId.SHOW_LAN_POWER:              //  7
                    case RestUrlId.SHOW_LAN_POWER_STATUS:       //  8
                    case RestUrlId.SHOW_LAN_POWER_CONFIG:       // 13
                    // 30 - 69: Commands related to actions on port
                    case RestUrlId.POWER_DOWN_PORT:             // 30
                    case RestUrlId.POWER_UP_PORT:               // 31
                    case RestUrlId.POWER_4PAIR_PORT:            // 33
                    case RestUrlId.POWER_2PAIR_PORT:            // 34
                    case RestUrlId.POWER_DOWN_SLOT:             // 35
                    case RestUrlId.POWER_UP_SLOT:               // 36
                    case RestUrlId.POWER_823BT_ENABLE:          // 37
                    case RestUrlId.POWER_823BT_DISABLE:         // 38
                    case RestUrlId.POWER_HDMI_ENABLE:           // 39
                    case RestUrlId.POWER_HDMI_DISABLE:          // 40
                    case RestUrlId.LLDP_POWER_MDI_ENABLE:       // 41
                    case RestUrlId.LLDP_POWER_MDI_DISABLE:      // 42
                    case RestUrlId.LLDP_EXT_POWER_MDI_ENABLE:   // 43
                    case RestUrlId.LLDP_EXT_POWER_MDI_DISABLE:  // 44
                    case RestUrlId.POE_FAST_ENABLE:             // 45
                    case RestUrlId.POE_PERPETUAL_ENABLE:        // 46
                    case RestUrlId.SHOW_PORT_MAC_ADDRESS:       // 47
                    case RestUrlId.SHOW_PORT_STATUS:            // 48
                        if (data == null || data.Length < 1) throw new SwitchCommandError($"Invalid url {Utils.PrintEnum(restUrlId)}!");
                        return url.Replace(DATA_0, (data == null || data.Length < 1) ? "" : data[0]);

                    // 30 - 69: Commands related to actions on port
                    case RestUrlId.POWER_PRIORITY_PORT:         // 32
                    case RestUrlId.SHOW_PORT_POWER:             // 49
                        if (data == null || data.Length < 2) throw new SwitchCommandError($"Invalid url {Utils.PrintEnum(restUrlId)}!");
                        return url.Replace(DATA_0, data[0]).Replace(DATA_1, data[1]);

                    default:
                        throw new SwitchCommandError($"Invalid url {Utils.PrintEnum(restUrlId)}!");
                }
            }
            else
            {
                throw new SwitchCommandError($"Invalid url {Utils.PrintEnum(restUrlId)}!");
            }
        }

    }
}

using PoEWizard.Exceptions;
using System.Collections.Generic;

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
            // 0 - 19: Basic commands to gather switch data
            SHOW_SYSTEM = 0,
            SHOW_MICROCODE = 1,
            SHOW_RUNNING_DIR = 2,
            SHOW_CHASSIS = 3,
            SHOW_PORTS_LIST = 4,
            SHOW_POWER_SUPPLY = 5,
            SHOW_LAN_POWER = 6,
            SHOW_LAN_POWER_STATUS = 7,
            SHOW_SLOT = 8,
            SHOW_MAC_LEARNING = 9,
            SHOW_TEMPERATURE = 10,
            SHOW_HEALTH = 11,
            // 20 - 39: Commands related to actions on power
            POWER_DOWN_PORT = 20,
            POWER_UP_PORT = 21,
            POWER_PRIORITY_PORT = 22,
            POWER_4PAIR_PORT = 23,
            POWER_2PAIR_PORT = 24,
            POWER_DOWN_SLOT = 25,
            POWER_UP_SLOT = 26,
            POWER_823BT_ENABLE = 27,
            POWER_823BT_DISABLE = 28,
            POWER_HDMI_ENABLE = 29,
            POWER_HDMI_DISABLE = 30,
            LLDP_POWER_MDI_ENABLE = 31,
            LLDP_POWER_MDI_DISABLE = 32,
            LLDP_EXT_POWER_MDI_ENABLE = 33,
            LLDP_EXT_POWER_MDI_DISABLE = 34,
            POE_FAST_ENABLE = 35,
            POE_PERPETUAL_ENABLE = 36,
            // 40 - 59: Special switch commands
            WRITE_MEMORY = 40
        }

        public static Dictionary<RestUrlId, string> REST_URL_TABLE = new Dictionary<RestUrlId, string>
        {
            // 0 - 19: Basic commands to gather switch data
            [RestUrlId.SHOW_SYSTEM] = "show system",                                            //  0
            [RestUrlId.SHOW_MICROCODE] = "show microcode",                                      //  1
            [RestUrlId.SHOW_RUNNING_DIR] = "show running-directory",                            //  2
            [RestUrlId.SHOW_CHASSIS] = "show chassis",                                          //  3
            [RestUrlId.SHOW_PORTS_LIST] = "show interfaces alias",                              //  4
            [RestUrlId.SHOW_POWER_SUPPLY] = $"show powersupply {DATA_0}",                       //  5
            [RestUrlId.SHOW_LAN_POWER] = $"show lanpower slot {DATA_0}",                        //  6
            [RestUrlId.SHOW_LAN_POWER_STATUS] = $"show lanpower slot {DATA_0} status",          //  7
            [RestUrlId.SHOW_SLOT] = $"show slot {DATA_0}",                                      //  8
            [RestUrlId.SHOW_MAC_LEARNING] = $"show mac-learning",                               //  9
            [RestUrlId.SHOW_TEMPERATURE] = $"show temperature",                                 // 10
            [RestUrlId.SHOW_HEALTH] = $"show health all cpu",                                   // 11
            // 20 - 39: Commands related to actions on power
            [RestUrlId.POWER_DOWN_PORT] = $"lanpower port {DATA_0} admin-state disable",        // 20
            [RestUrlId.POWER_UP_PORT] = $"lanpower port {DATA_0} admin-state enable",           // 21
            [RestUrlId.POWER_PRIORITY_PORT] = $"lanpower port {DATA_0} priority {DATA_1}",      // 22
            [RestUrlId.POWER_4PAIR_PORT] = $"lanpower port {DATA_0} 4pair enable",              // 23
            [RestUrlId.POWER_2PAIR_PORT] = $"lanpower port {DATA_0} 4pair disable",             // 24
            [RestUrlId.POWER_DOWN_SLOT] = $"lanpower slot {DATA_0} service stop",               // 25
            [RestUrlId.POWER_UP_SLOT] = $"lanpower slot {DATA_0} service start",                // 26
            [RestUrlId.POWER_823BT_ENABLE] = $"lanpower slot {DATA_0} 8023bt enable",           // 27
            [RestUrlId.POWER_823BT_DISABLE] = $"lanpower slot {DATA_0} 8023bt disable",         // 28
            [RestUrlId.POWER_HDMI_ENABLE] = $"lanpower port {DATA_0} power-over-hdmi enable",   // 29
            [RestUrlId.POWER_HDMI_DISABLE] = $"lanpower port {DATA_0} power-over-hdmi disable", // 30
            [RestUrlId.LLDP_POWER_MDI_ENABLE] = $"lldp nearest-bridge port {DATA_0} tlv dot3 power-via-mdi enable",          // 31
            [RestUrlId.LLDP_POWER_MDI_DISABLE] = $"lldp nearest-bridge port {DATA_0} tlv dot3 power-via-mdi disable",        // 32
            [RestUrlId.LLDP_EXT_POWER_MDI_ENABLE] = $"lldp nearest-bridge port {DATA_0} tlv med ext-power-via-mdi enable",   // 33
            [RestUrlId.LLDP_EXT_POWER_MDI_DISABLE] = $"lldp nearest-bridge port {DATA_0} tlv med ext-power-via-mdi disable", // 34
            [RestUrlId.POE_FAST_ENABLE] = $"lanpower slot {DATA_0} fpoe enable",                // 35
            [RestUrlId.POE_PERPETUAL_ENABLE] = $"lanpower slot {DATA_0} ppoe enable",           // 36
            // 40 - 59: Special switch commands
            [RestUrlId.WRITE_MEMORY] = "write memory flash-synchro"                             // 40
        };

        public static string ParseUrl(RestUrlEntry entry)
        {
            string url = $"cli/aos?cmd={GetUrlFromTable(entry.RestUrl, entry.Data).Trim().Replace(" ", "%20").Replace("/", "%2F")}";
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
                    case RestUrlId.SHOW_POWER_SUPPLY:           //  5
                    case RestUrlId.SHOW_SLOT:                   //  8
                    case RestUrlId.SHOW_MAC_LEARNING:           //  9
                    case RestUrlId.SHOW_TEMPERATURE:            // 10
                    case RestUrlId.SHOW_HEALTH:                 // 11
                    // 20 - 39: Commands related to actions on power
                    case RestUrlId.WRITE_MEMORY:                // 40
                        return url;

                    // 0 - 19: Basic commands to gather switch data
                    case RestUrlId.SHOW_LAN_POWER:              //  6
                    case RestUrlId.SHOW_LAN_POWER_STATUS:       //  7
                    // 20 - 39: Commands related to actions on power
                    case RestUrlId.POWER_DOWN_PORT:             // 20
                    case RestUrlId.POWER_UP_PORT:               // 21
                    case RestUrlId.POWER_4PAIR_PORT:            // 23
                    case RestUrlId.POWER_2PAIR_PORT:            // 24
                    case RestUrlId.POWER_DOWN_SLOT:             // 25
                    case RestUrlId.POWER_UP_SLOT:               // 26
                    case RestUrlId.POWER_823BT_ENABLE:          // 27
                    case RestUrlId.POWER_823BT_DISABLE:         // 28
                    case RestUrlId.POWER_HDMI_ENABLE:           // 29
                    case RestUrlId.POWER_HDMI_DISABLE:          // 30
                    case RestUrlId.LLDP_POWER_MDI_ENABLE:       // 31
                    case RestUrlId.LLDP_POWER_MDI_DISABLE:      // 32
                    case RestUrlId.LLDP_EXT_POWER_MDI_ENABLE:   // 33
                    case RestUrlId.LLDP_EXT_POWER_MDI_DISABLE:  // 34
                    case RestUrlId.POE_FAST_ENABLE:             // 35
                    case RestUrlId.POE_PERPETUAL_ENABLE:        // 36
                        if (data == null || data.Length < 1) throw new SwitchCommandError($"Invalid url {Utils.PrintEnum(restUrlId)}!");
                        return url.Replace(DATA_0, (data == null || data.Length < 1) ? "" : data[0]);

                    // 20 - 39: Commands related to actions on power
                    case RestUrlId.POWER_PRIORITY_PORT:     // 22
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

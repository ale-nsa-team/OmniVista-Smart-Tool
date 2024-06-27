using PoEWizard.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

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
            SHOW_CHASSIS = 1,
            SHOW_PORTS_LIST = 2,
            SHOW_POWER_SUPPLY = 3,
            SHOW_LAN_POWER = 4,
            SHOW_SLOT_POWER = 5,
            SHOW_MAC_LEARNING = 6,
            SHOW_TEMPERATURE = 7,
            SHOW_HEALTH = 8,
            SHOW_TRAFFIC = 9,
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
            SHOW_PORT_MAC_LEARNING = 35,
            // 40 - 59: Special switch commands
            WRITE_MEMORY = 40
        }

        public static Dictionary<RestUrlId, string> REST_URL_TABLE = new Dictionary<RestUrlId, string>
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
            [RestUrlId.POWER_PRIORITY_PORT] = "domain=mib&urn=pethPsePortTable",

            [RestUrlId.WRITE_MEMORY] = "cli/aos?cmd=write memory flash-synchro",

            [RestUrlId.SHOW_SLOT_POWER] = "domain=mib&urn=alaPethMainPseTable&mibObject0=pethMainPseGroupIndex&mibObject1=alaPethMainPseMaxPower&mibObject2=pethMainPseUsageThreshold&mibObject3=pethMainPseConsumptionPower&function=chassisSlot_vcSlotNum&object=pethMainPseGroupIndex&ignoreError=true",

            [RestUrlId.POWER_DOWN_PORT] = "domain=mib&urn=pethPsePortTable",
            [RestUrlId.POWER_UP_PORT] = "domain=mib&urn=pethPsePortTable",

            [RestUrlId.SHOW_PORT_MAC_LEARNING] = $"domain=cli&cmd=debug show json-mac-learning limit 4096 {DATA_0}"
        };

        public static string ParseUrl(RestUrlEntry entry)
        {
            string url = GetUrlFromTable(entry.RestUrl, entry.Data).Trim();
            string[] urlSplit = url.Split('=');
            url = $"{urlSplit[0]} {urlSplit[1].Replace(" ", "%20").Replace("/", "%2F")}";
            return url;
        }

        private static string GetUrlFromTable(RestUrlId restUrlId, string[] data)
        {
            if (REST_URL_TABLE.ContainsKey(restUrlId))
            {
                string url = REST_URL_TABLE[restUrlId];
                switch (restUrlId)
                {
                    case RestUrlId.SHOW_SYSTEM:
                    case RestUrlId.SHOW_CHASSIS:
                    case RestUrlId.SHOW_PORTS_LIST:
                    case RestUrlId.SHOW_TRAFFIC:
                    case RestUrlId.SHOW_TEMPERATURE:
                    case RestUrlId.SHOW_HEALTH:
                    case RestUrlId.SHOW_SLOT_POWER:
                    case RestUrlId.WRITE_MEMORY:
                        return url;

                    //using domain cli
                    case RestUrlId.SHOW_MAC_LEARNING:
                    case RestUrlId.SHOW_PORT_MAC_LEARNING:
                    case RestUrlId.SHOW_POWER_SUPPLY:
                    case RestUrlId.SHOW_LAN_POWER:
                    case RestUrlId.POWER_DOWN_PORT:
                    case RestUrlId.POWER_UP_PORT:
                    case RestUrlId.POWER_PRIORITY_PORT:
                        if (data == null || data.Length < 1) throw new SwitchCommandError($"Invalid url {Utils.PrintEnum(restUrlId)}!");
                        return url.Replace(DATA_0, (data == null || data.Length < 1) ? "" : data[0]);

                    default:
                        throw new SwitchCommandError($"Invalid url {Utils.PrintEnum(restUrlId)}!");
                }
            }
            else
            {
                throw new SwitchCommandError($"Invalid url {Utils.PrintEnum(restUrlId)}!");
            }
        }

        public static HttpMethod GetHttpMethod(RestUrlId restUrlId)
        {
            if (REST_URL_TABLE.ContainsKey(restUrlId))
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
                    case RestUrlId.SHOW_LAN_POWER:
                    case RestUrlId.SHOW_SLOT_POWER:
                    case RestUrlId.WRITE_MEMORY:
                        return HttpMethod.Get;

                    case RestUrlId.POWER_DOWN_PORT:
                    case RestUrlId.POWER_UP_PORT:
                    case RestUrlId.POWER_PRIORITY_PORT:
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

        public static Dictionary<RestUrlId, Dictionary<string, string>> CONTENT_TABLE = new Dictionary<RestUrlId, Dictionary<string, string>>
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
            [RestUrlId.WRITE_MEMORY] = new Dictionary<string, string>
            {
                { "mibObject0", "configWriteMemory:1"}
            }
        };

        public static Dictionary<string, string> GetContent(RestUrlId restUrlId, string[] data)
        {
            if (data == null || data.Length < 1)
            {
                throw new SwitchCommandError("Missing data to get the content for the Rest Api!");
            }
            if (CONTENT_TABLE.ContainsKey(restUrlId))
            {
                Dictionary<string, string> content = CONTENT_TABLE[restUrlId];
                var dict = new Dictionary<string, string>(content);
                switch (restUrlId)
                {
                    // Ports actions
                    case RestUrlId.POWER_DOWN_PORT:
                    case RestUrlId.POWER_UP_PORT:
                    case RestUrlId.POWER_PRIORITY_PORT:
                    // Switch actions
                    case RestUrlId.WRITE_MEMORY:
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

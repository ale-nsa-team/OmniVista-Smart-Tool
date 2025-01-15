using System.Collections.Generic;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    public class TdrModel
    {
        public string Port { get; set; }
        public string Pair1State { get; set; }
        public string Pair1Len { get; set; }
        public string Pair2State { get; set; }
        public string Pair2Len { get; set; }
        public string Pair3State { get; set; }
        public string Pair3Len { get; set; }
        public string Pair4State { get; set; }
        public string Pair4Len { get; set; }
        public string Result { get; set; }

        public TdrModel(Dictionary<string, string> data)
        {
            if (data.Count == 10)
            {
                Port = GetValue(data, CSP).Split(' ')[0];
                Pair1State = GetValue(data, PAIR1_STATE);
                Pair1Len = GetValue(data, PAIR1_LEN);
                Pair2State = GetValue(data, PAIR2_STATE);
                Pair2Len = GetValue(data, PAIR2_LEN);
                Pair3State = GetValue(data, PAIR3_STATE);
                Pair3Len = GetValue(data, PAIR3_LEN);
                Pair4State = GetValue(data, PAIR4_STATE);
                Pair4Len = GetValue(data, PAIR4_LEN);
                Result = GetValue(data, TEST_RESULT);
            }

        }

        private string GetValue(Dictionary<string, string> data, string key)
        {
            return data.TryGetValue(key, out string value) ? value : string.Empty;
        }
    }
}

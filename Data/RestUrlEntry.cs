using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using static PoEWizard.Data.RestUrl;

namespace PoEWizard.Data
{
    public class RestUrlEntry
    {

        public RestUrlId RestUrl { get; set; }
        public DateTime StartTime { get; set; }
        public int MaxWait { get; set; }
        public string Duration { get; set; }
        public HttpMethod Method { get; set; }
        public Dictionary<string, string> Response { get; set; }
        public Dictionary<string, string> Content { get; set; }
        public string[] Data { get; set; }

        public RestUrlEntry(RestUrlId url, int maxWait, string[] data = null)
        {
            this.RestUrl = url;
            this.Data = data;
            this.MaxWait = maxWait;
            this.StartTime = DateTime.Now;
        }

        public override string ToString()
        {
            try
            {
                StringBuilder txt = new StringBuilder();
                txt.Append(Utils.PrintEnum(this.RestUrl));
                txt.Append(", MaxWait: ").Append(this.MaxWait).Append(" sec, Duration: ").Append(this.Duration);
                return txt.ToString();
            }
            catch { }
            return "";
        }
    }

}

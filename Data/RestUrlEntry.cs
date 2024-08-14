using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using static PoEWizard.Data.RestUrl;

namespace PoEWizard.Data
{
    public class RestUrlEntry
    {

        public Command RestUrl { get; set; }
        public DateTime StartTime { get; set; }
        public string Duration { get; set; }
        public HttpMethod Method { get; set; }
        public Dictionary<string, string> Response { get; set; }
        public Dictionary<string, string> Content { get; set; }
        public string[] Data { get; set; }

        public RestUrlEntry(Command url, string[] data = null)
        {
            this.RestUrl = url;
            this.Data = data;
            this.StartTime = DateTime.Now;
        }

        public override string ToString()
        {
            try
            {
                StringBuilder txt = new StringBuilder("Command ").Append(Utils.PrintEnum(this.RestUrl)).Append(", Duration: ").Append(this.Duration);
                return txt.ToString();
            }
            catch { }
            return "";
        }
    }

}

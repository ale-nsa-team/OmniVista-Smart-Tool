using System;
using System.IO;

namespace PoEWizard.Data
{
    public static class Activity
    {
        private const string fileName = "activity.txt";
        private static readonly object lockObj = new object();
        private static string dataPath;
        public static string FilePath { get; private set; }
        public static string DataPath
        {
            get => dataPath;
            set
            {
                dataPath = value;
                FilePath = Path.Combine(dataPath, "Log", fileName);
            }
        }

        public static void Log(string text)
        {
            string strDate = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss tt");
            try
            {
                lock (lockObj)
                {
                    using (StreamWriter file = File.AppendText(FilePath))
                    {
                        file.WriteLine($"{strDate} - {text}");
                    }
                }
            }
            catch { }
        }
    }
}

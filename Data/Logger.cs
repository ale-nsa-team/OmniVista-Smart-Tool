using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Data
{
    /// <summary>
    /// Logger Class
    /// Implements a simple logger that writes entries
    /// to a log file. If the required log level is Error,
    /// it will also write the entry to Windows Event Log
    /// </summary>
    public static class Logger
    {
        #region variables
        private static int logSize;
        private static int logCount;
        private static EventLog eventLog;
        private static readonly object lockObj = new object();
        private static bool eventLogOk;

        public static string LogPath { get; private set; }
        public static LogLevel LogLevel { get; set; }
        #endregion

        #region constructor
        static Logger()
        {
            try
            {
                LogLevel = LogLevel.Info;
                string filename = "PoEWizard.log";
                LogPath = Path.Combine(MainWindow.dataPath, "Log", filename);
                if (!Directory.Exists(Path.GetDirectoryName(LogPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                }
                logSize = 1000000;
                logCount = 5;
            }
            catch { }
            try
            {
                eventLog = new EventLog();
                string source = nameof(PoEWizard);
                if (!EventLog.SourceExists(source))
                {
                    EventLog.CreateEventSource(source, "Application");
                }
                eventLog.Source = source;
                eventLog.Log = "Application";
                eventLogOk = true;
            }
            catch
            {
                eventLogOk = false;
            }
        }
        #endregion

        #region public methods
        public static void Error(string message)
        {
            Log(message, LogLevel.Error);
        }

        public static void Error(Exception ex)
        {
            string logmsg = $"{ex.Message}:\n{ex.StackTrace}";
            Log(logmsg, LogLevel.Error);
        }

        public static void Error(string message, Exception ex)
        {
            string logmsg = $"{message}: {ex.Message}";
            if (!string.IsNullOrEmpty(ex.StackTrace)) logmsg += $"\n{ex.StackTrace}";
            Log(logmsg, LogLevel.Error);
        }

        public static void Warn(string message)
        {
            Log(message, LogLevel.Warn);
        }

        public static void Info(string message)
        {
            Log(message, LogLevel.Info);
        }

        public static void Debug(string message)
        {
            Log(message, LogLevel.Debug);
        }

        public static void Trace(string message)
        {
            Log(message, LogLevel.Trace);
        }

        public static void Clear()
        {
            if (File.Exists(LogPath))
            {
                try
                {
                    lock (lockObj)
                    {
                        File.Create(LogPath);
                    }
                    Task.Delay(TimeSpan.FromSeconds(1)).ContinueWith(t => Log("Log file cleared by user", LogLevel.Info));
                }
                catch (Exception ex)
                {
                    Log($"Failed to clear log file: {ex.Message}", LogLevel.Error);
                }
            }
        }
        #endregion

        #region private methods

        private static void Log(string message, LogLevel level)
        {
            try
            {
                // Error messages are also written to event log
                if (level == LogLevel.Error && eventLogOk)
                {
                    eventLog.WriteEntry(message, EventLogEntryType.Error);
                }
                if (level <= LogLevel)
                {
                    string strDate = DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt");
                    string caller = GetMethodClass();
                    string logMsg = $"{strDate} [{level,-5}] ({caller}) - {message}";
                    lock (lockObj)
                    {
                        using (StreamWriter file = File.AppendText(LogPath))
                        {
                            file.WriteLine(logMsg);
                        }
                    }
                    Rotate();
                }
            }
            catch { }
        }

        private static string GetMethodClass()
        {
            int skipFrames = 1;
            var method = new StackFrame(skipFrames).GetMethod();
            while (method.DeclaringType.Name == "Logger")
            {
                skipFrames++;
                method = new StackFrame(skipFrames).GetMethod();
            }
            string fname = method.DeclaringType.FullName;
            if (fname.Contains("<"))
            {
                string[] parts = fname.Split(new char[] { '.', '<', '>' });
                if (parts.Length > 2) return $"{parts[1].Replace("+", "")}: {parts[2]}";
            }
            return $"{method.DeclaringType.Name}: {method.Name}";
        }

        private static void Rotate()
        {
            if (File.Exists(LogPath))
            {
                try
                {
                    FileInfo fi = new FileInfo(LogPath);
                    if (fi.Length > logSize)
                    {
                        lock (lockObj)
                        {
                            for (int i = logCount - 1; i > 0; i--)
                            {
                                string thisLogName = $"{Path.ChangeExtension(LogPath, null)}{i:00}.log";
                                string nextLogName = $"{Path.ChangeExtension(LogPath, null)}{i + 1:00}.log";
                                if (File.Exists(thisLogName)) File.Copy(thisLogName, nextLogName, true);
                            }
                            string firstLogName = $"{Path.ChangeExtension(LogPath, null)}01.log"; ;
                            File.Copy(LogPath, firstLogName, true);
                            File.Delete(LogPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Failed to rotate log file: {ex.Message}", LogLevel.Error);
                }
            }
        }
        #endregion
    }
}

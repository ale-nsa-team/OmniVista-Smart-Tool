using PoEWizard.Comm;
using PoEWizard.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Device
{
    public static class FileHanlder
    {

        private const int NO_OF_EXECUTED_PER_PART_6X = 4;
        private const int WAIT_TIME_PER_PART_6X = 2500;
        public static DeviceModel Device { get; set; }
        public static string TemplatePath { get; set; }

        public static bool WriteToDevice(List<string> cmds, string fileName, string cdToWriteDirCmd)
        {
            if (cmds.Count == 0)
            {
                Logger.Warn($"There are no lines to write file {fileName}");
                return false;
            }
            cdToWriteDirCmd = cdToWriteDirCmd ?? "";
            return Device.IsAos6x ? WriteFile6x(cmds, fileName, cdToWriteDirCmd) : WriteFile8x(cmds, fileName, cdToWriteDirCmd);
        }

        public static List<string> LoadTemplate(string filename, int replacement = 0)
        {
            List<string> lines = ReadFromDisk(Path.Combine(TemplatePath, filename));
            if (lines == null || replacement == 0) return lines;
            return lines.Select(l => l.Replace("{N}", replacement.ToString())).ToList();
        }

        public static List<string> ReadFromDisk(string filepath)
        {
            string filename = Path.GetFileName(filepath);
            List<string> lines = new List<string>();

            if (File.Exists(filepath))
            {
                try
                {
                    foreach (string line in File.ReadLines(filepath, Encoding.UTF8))

                    {
                        if (line.StartsWith(COMMENT_CHAR) || line == "") continue;
                        lines.Add(line + LINE_FEED);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Exception while reading config file {filename}: {ex.Message}");
                    return null;
                }
            }
            return lines;
        }

        private static bool WriteFile6x(List<string> cmds, string fileName, string cdToWriteDirCmd)
        {
            bool success = false;
            try
            {
                //VI to enter append/edit mode
                CmdExecutor sender = new CmdExecutor();
                sender = sender.Send(cdToWriteDirCmd, 10000).Response().EndsWith(DEFAULT_PROMPT)
                .Send(VI + SPACE + fileName).Wait(250).Send(A_LITERAL);
                int noOfExecutedCmds = 0;
                int noOfExecutedCmdsPerPart = 0;
                int noOfCmds = cmds.Count;
                foreach (string cmd in cmds)
                {
                    noOfExecutedCmds++;
                    noOfExecutedCmdsPerPart++;
                    if (noOfExecutedCmdsPerPart == NO_OF_EXECUTED_PER_PART_6X || noOfExecutedCmds == noOfCmds)
                    {
                        noOfExecutedCmdsPerPart = 0;
                        sender = sender.Send(cmd).Wait(WAIT_TIME_PER_PART_6X);
                    }
                    else
                    {
                        sender = sender.Send(cmd);
                    }
                }
                string escseq = @"\u001b\[[?0-9;]*[a-zA-Z]\0*$";
                //need to send Esc to exit edit mode
                byte[] esc = new byte[] { ESC_BYTE };
                //let's try several Escapes
                for (int i = 0; i < 10; i++)
                {
                    sender = sender.Send(esc);
                }
                sender.Send(VI_X)
                .Response().Regex($"{escseq}")
                .Consume(new ResultCallback(result =>
                {
                    success = true;
                }, error =>
                {
                    Logger.Error($"Failed to write 6x file {fileName} to device S/N {Device.SerialNumber} model {Device.Model}: {error}");
                }));
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception when preparing to write 6x file {fileName} to device S/N {Device.SerialNumber} model {Device.Model}: {ex.Message}");
            }
            return success;
        }

        private static bool WriteFile8x(List<string> cmds, string fileName, string cdToWriteDirCmd)
        {
            bool success = false;
            try
            {
                CmdExecutor sender = new CmdExecutor();
                sender = sender.Send(cdToWriteDirCmd).Response()
                    .EndsWith(null)
                    .Send(Commands.CatStartEoF + fileName);
                foreach (string cmd in cmds)
                {
                    sender = sender.Send(cmd);
                }
                sender.Send(EOF_STRING)
                .Response().Regex($"Ctrl/C")
                .Consume(new ResultCallback(result =>
                {
                    success = true;
                }, error =>
                {
                    Logger.Error($"Failed to write 8x file {fileName} to device S/N {Device.SerialNumber} model {Device.Model}: {error}");
                }));
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception when preparing to write 8x file {fileName} to device S/N {Device.SerialNumber} model {Device.Model}: {ex.Message}");

            }
            return success;
        }
    }
}

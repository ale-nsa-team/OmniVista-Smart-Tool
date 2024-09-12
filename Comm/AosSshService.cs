using PoEWizard.Data;
using PoEWizard.Device;
using PoEWizard.Exceptions;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.RestUrl;

namespace PoEWizard.Comm
{
    public class AosSshService : SshClient
    {

        private const string TERM_TYPE = "xterm";
        private const int NB_COLUMNS = 255;
        private const int NB_ROWS = 1000;
        private const int WIDTH = 800;
        private const int HEIGHT = 600;

        private const int SHELL_BUFFER_SIZE = 64 * 1024;
        private const string CMD_FIND_PROMPT = "show session config";

        private const string D_CLI_PROMPT = "CLI DEFAULT PROMPT";
        private const string D_CLI_TIMER = "CLI INACTIVITY TIMER";
        private const string D_FTP_TIMER = "FTP INACTIVITY TIMER";
        private const string D_HTTP_TIMER = "HTTP INACTIVITY TIMER";
        private const string D_LOGIN_TIMER = "LOGIN TIMER";
        private const string D_LOGIN_ATTEMPTS = "LOGIN ATTEMPTS";

        private const string PREFIX_KEY = "PREFIX";
        private const string HEADER_KEY = "HEADER";
        private const string TEXT_KEY = "TEXT";
        private const string EMPTY_KEY = "EMPTY";
        private const string SWITCH_ERROR = "ERROR: ";
        private const string CMD = "COMMAND";
        private const string PROMPT = "SESSION_PROMPT";

        public const string DEFAULT_PROMPT = "->";

        public string SessionPrompt { get; set; }

        internal SwitchModel _switch;
        internal SshClient _client = null;
        internal StreamWriter _writer = null;
        internal ShellStream _shell_stream = null;
        internal PasswordConnectionInfo _connection_info;
        internal TimeSpan _cnx_timeout;
        internal StringBuilder _received_buffer = new StringBuilder();
        internal string _prev_command_failed = null;

        private int _cli_inactivity_timer = 0;
        private int _ftp_inactivity_timer = 0;
        private int _http_inactivity_timer = 0;
        private int _login_timer = 0;
        private int _login_attempts = 0;

        public AosSshService(SwitchModel switchData) : base(switchData.IpAddress, switchData.Login, switchData.Password)
        {
            this._switch = switchData;
            this._prev_command_failed = null;
        }

        public string ConnectSshClient()
        {
            this.SessionPrompt = DEFAULT_PROMPT;
            DateTime startConnectTime = DateTime.Now;
            ConnectSSH();
            DateTime startPromptTime = DateTime.Now;
            SearchPrompt();
            LogSSHConnection(startConnectTime, startPromptTime);
            return this.SessionPrompt;
        }

        public bool IsSwitchConnected()
        {
            return (this._client != null && this._client.IsConnected);
        }
        public void DisconnectSshClient()
        {
            try
            {
                DateTime startDisconnectTime = DateTime.Now;
                CloseSshStreams();
                if (this._client != null)
                {
                    this._client.Disconnect();
                    this._client.Dispose();
                }
                StringBuilder txt = new StringBuilder("Disconnecting Switch \"");
                txt.Append(this._switch.Name).Append("\" (IP: ").Append(this._switch.IpAddress).Append("), Disconnect Time Duration: ");
                txt.Append(Utils.CalcStringDuration(startDisconnectTime));
                LogDebug("DisconnectSshClient", txt.ToString());
            }
            catch (Exception ex)
            {
                LogException("DisconnectSshClient", ex);
            }
            this._client = null;
        }

        private void ConnectSSH()
        {
            try
            {
                this._cnx_timeout = TimeSpan.FromSeconds(this._switch.CnxTimeout);
                this._connection_info = new PasswordConnectionInfo(this._switch.IpAddress, this._switch.Login, this._switch.Password)
                {
                    Timeout = this._cnx_timeout
                };
                this._client = new SshClient(this._connection_info);
                this._client.Connect();
                CreateSshStreams();
            }
            catch (SshConnectionException ex)
            {
                this._client = null;
                if (ex.Message.ToLower().Contains("closed before"))
                {
                    throw GenerateRejectConnectionException(null);
                }
                else
                {
                    throw GenerateConnectionFailException();
                }
            }
            catch (SshAuthenticationException ex)
            {
                this._client = null;
                throw new SwitchAuthenticationFailure(ex.Message);
            }
            catch (Exception ex)
            {
                if (!ex.Message.ToLower().Contains("connection failed to establish") && !ex.Message.ToLower().Contains("refused it"))
                {
                    LogException("ConnectSshClient", ex);
                    DisconnectSshClient();
                }
                throw GenerateConnectionFailException();
            }
        }

        public void Abort()
        {
            DisconnectSshClient();
        }

        private string SearchPrompt()
        {
            try
            {
                ClearReceiveBuffer();
                string response = SendCommandToFindPrompt();
                if (!string.IsNullOrEmpty(response))
                {
                    ParseSessionConfig(response);
                }
                if (this.SessionPrompt == null)
                {
                    this.SessionPrompt = DEFAULT_PROMPT;
                }
                FlushSshStream();
                return this.SessionPrompt;
            }
            catch (SwitchCommandError ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string SendCommandToFindPrompt()
        {
            FlushSshStream();
            ClearReceiveBuffer();
            SendCommandToSwitch(CMD_FIND_PROMPT, 10);
            Thread.Sleep(100);
            DateTime startTime = DateTime.Now;
            double dur = 0;
            while (dur < 120)
            {
                Thread.Sleep(100);
                string result = this._received_buffer.ToString();
                if ((!string.IsNullOrEmpty(result)) && result.Contains(CMD_FIND_PROMPT) && result.ToUpper().Contains(D_CLI_PROMPT) &&
                     result.ToUpper().Contains(D_LOGIN_TIMER) && result.ToUpper().Contains(D_LOGIN_ATTEMPTS))
                {
                    return result;
                }
                dur = Utils.GetTimeDuration(startTime);
            }
            return null;
        }

        private void ParseSessionConfig(string response)
        {
            try
            {
                List<Dictionary<string, string>> keyValList = ParseKeyValList(response, null, null, CMD_FIND_PROMPT.ToUpper(), '=');
                foreach (Dictionary<string, string> keyValues in keyValList)
                {
                    foreach (KeyValuePair<string, string> keyVal in keyValues)
                    {
                        try
                        {
                            if (keyVal.Key.Contains(D_CLI_PROMPT.Replace(" ", "_")))
                            {
                                this.SessionPrompt = keyVal.Value;
                            }
                            else if (keyVal.Key.Contains(D_CLI_TIMER.Replace(" ", "_")))
                            {
                                this._cli_inactivity_timer = Utils.StringToInt(keyVal.Value);
                            }
                            else if (keyVal.Key.Contains(D_FTP_TIMER.Replace(" ", "_")))
                            {
                                this._ftp_inactivity_timer = Utils.StringToInt(keyVal.Value);
                            }
                            else if (keyVal.Key.Contains(D_HTTP_TIMER.Replace(" ", "_")))
                            {
                                this._http_inactivity_timer = Utils.StringToInt(keyVal.Value);
                            }
                            else if (keyVal.Key.Contains(D_LOGIN_TIMER.Replace(" ", "_")))
                            {
                                this._login_timer = Utils.StringToInt(keyVal.Value);
                            }
                            else if (keyVal.Key.Contains(D_LOGIN_ATTEMPTS.Replace(" ", "_")))
                            {
                                this._login_attempts = Utils.StringToInt(keyVal.Value);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogException("ParseSessionConfig", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogException("ParseSessionConfig", ex);
            }
        }

        public Dictionary<string, string> SendLinuxCommand(LinuxCommand cmdLinux)
        {
            try
            {
                Dictionary<string, string> resp = SendCliCommand(cmdLinux.Command, 60);
                return ParseResponse(resp[RESPONSE], cmdLinux.Command);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw ex;
            }
        }

        public Dictionary<string, string> SendCommand(RestUrlEntry cmdEntry, string[] data, string expected = null)
        {
            string cmd = GetReqFromCmdTbl(cmdEntry.RestUrl, data);
            if (cmd == null) return null;
            cmdEntry.StartTime = DateTime.Now;
            Dictionary<string, string> response;
            response = SendCliCommand(cmd, 60, expected);
            cmdEntry.Duration = response[DURATION];
            cmdEntry.Response = ParseResponse(response[RESPONSE], cmd);
            return cmdEntry.Response;
        }

        private Dictionary<string, string> SendCliCommand(string cmd, int maxWait, string expected = null)
        {
            if (cmd == null) throw new SwitchCommandError("Command line is empty!");
            try
            {
                if (IsSwitchConnected())
                {
                    if (this._prev_command_failed != null)
                    {
                        this.ResetSSHConnection(cmd);
                        StringBuilder txt = new StringBuilder("Trying to send the command \"");
                        txt.Append(cmd).Append("\" to the Switch \"").Append(this._switch.Name).Append("\" after resetting SSH connection");
                        LogInfo("SendCommand", txt.ToString());
                    }
                    this._prev_command_failed = null;
                    Dictionary<string, string> response = new Dictionary<string, string>
                    {
                        [RESPONSE] = "",
                        [DURATION] = ""
                    };
                    DateTime startTime = DateTime.Now;
                    string result = null;
                    ClearReceiveBuffer();
                    SendCommandToSwitch(cmd, maxWait, expected);
                    Thread.Sleep(100);
                    result = WaitEndDataReceived(cmd, maxWait, expected);
                    if (result.Contains("Confirm") && result.Contains("(Y/N)"))
                    {
                        SendCommandToSwitch("Y", 10, "Y");
                        Thread.Sleep(200);
                        result = WaitEndDataReceived(cmd, 10, "copy images before reloading");
                    }
                    response[RESPONSE] = result;
                    response[DURATION] = Utils.CalcStringDuration(startTime);
                    return response;
                }
                else
                {
                    throw GenerateConnectionDroppedException(cmd);
                }
            }
            catch (SwitchCommandError ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                if (ex.Message.ToLower().Contains("not connected"))
                {
                    throw GenerateRejectConnectionException(cmd);
                }
                throw ex;
            }
        }
        private void ResetSSHConnection(string cmd)
        {
            try
            {
                StringBuilder txt = new StringBuilder("Trying to run the command \"");
                txt.Append(cmd).Append("\" when Switch \"").Append(this._switch.Name);
                txt.Append("\" failed to abort the previous command \"").Append(this._prev_command_failed).Append("\"!");
                LogError("ResetSSHConnection", "Reseting the SSH Connection!\r\nReason: " + txt);
                DateTime startTime = DateTime.Now;
                DisconnectSshClient();
                txt = new StringBuilder("Disconnected the SSH session on Switch \"");
                txt.Append(this._switch.Name).Append("\", Duration: ").Append(Utils.CalcStringDuration(startTime));
                LogInfo("ResetSSHConnection", txt.ToString());
                Thread.Sleep(1000);
                startTime = DateTime.Now;
                ConnectSSH();
                txt = new StringBuilder("Reconnected the SSH session on Switch \"");
                txt.Append(this._switch.Name).Append("\", Duration: ").Append(Utils.CalcStringDuration(startTime));
                LogInfo("ResetSSHConnection", txt.ToString());
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void CreateSshStreams()
        {
            if (IsSwitchConnected())
            {
                CreateShellStream();
                CreateStreamWriter();
            }
            else
            {
                throw GenerateConnectionDroppedException(null);
            }
        }
        private void CreateShellStream()
        {
            if (IsSwitchConnected())
            {
                this._shell_stream = this._client.CreateShellStream(TERM_TYPE, NB_COLUMNS, NB_ROWS, WIDTH, HEIGHT, SHELL_BUFFER_SIZE);
                this._shell_stream.DataReceived += DataReceived;
            }
            else
            {
                throw GenerateConnectionDroppedException(null);
            }
        }
        private void ClearReceiveBuffer()
        {
            this._received_buffer = new StringBuilder();
            if (IsSwitchConnected())
            {
                if (this._shell_stream == null)
                {
                    CreateShellStream();
                }
            }
            else
            {
                throw GenerateConnectionDroppedException(null);
            }
        }
        private void CreateStreamWriter()
        {
            if (IsSwitchConnected())
            {
                if (this._shell_stream == null)
                {
                    CreateShellStream();
                }
                this._writer = new StreamWriter(this._shell_stream) { AutoFlush = true };
            }
            else
            {
                throw GenerateConnectionDroppedException(null);
            }
        }
        private void CloseSshStreams()
        {
            CloseStreamWriter();
            CloseShellStream();
        }
        private void CloseStreamWriter()
        {
            try
            {
                if (this._writer != null)
                {
                    this._writer.Close();
                    this._writer.Dispose();
                }
            }
            catch (Exception ex)
            {
                LogException("SSH CloseStreamWriter error: " + ex.Message, ex);
            }
            this._writer = null;
        }
        private void CloseShellStream()
        {
            try
            {
                if (this._shell_stream != null)
                {
                    this._shell_stream.Close();
                    this._shell_stream.Dispose();
                }
            }
            catch (Exception ex)
            {
                LogException("SSH CloseShellStream error: " + ex.Message, ex);
            }
            this._shell_stream = null;
        }
        private void SendCommandToSwitch(string cmd, int maxWait, string expected = null)
        {
            try
            {
                if (this._writer == null) CreateStreamWriter();
                FlushSshStream();
                this._writer.Flush();
                if (string.IsNullOrEmpty(expected))
                {
                    this._writer.WriteLine(cmd);
                }
                else
                {
                    this._writer.Write(cmd + "\r");
                }
                DateTime startTime = DateTime.Now;
                Thread.Sleep(30);
                double dur = 0;
                while (dur < maxWait)
                {
                    if (this._shell_stream.Length >= cmd.Length)
                    {
                        LogSendCommand(cmd, startTime);
                        return;
                    }
                    Thread.Sleep(20);
                    dur = Utils.GetTimeDuration(startTime);
                }
                throw new SwitchCommandError("Took too long (> " + Utils.CalcStringDuration(startTime) + ") to send command \"" + cmd + "\"!");
            }
            catch (SwitchCommandError ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private void DataReceived(object sender, ShellDataEventArgs e)
        {
            try
            {
                if ((e != null) && (e.Data != null) && (e.Data.Length > 0))
                {
                    string chunkData = Encoding.UTF8.GetString(e.Data, 0, e.Data.Length);
                    lock (this._received_buffer)
                    {
                        if (this._received_buffer == null) this._received_buffer = new StringBuilder();
                        if (!string.IsNullOrEmpty(chunkData)) this._received_buffer.Append(chunkData);
                    }
                }
            }
            catch (Exception ex)
            {
                LogException("DataReceived", ex);
            }
        }
        private string WaitEndDataReceived(string cmd, int maxWait, string expected = null)
        {
            try
            {
                DateTime startTime = DateTime.Now;
                int waitCmdCnt = 0;
                DateTime waitCmd = DateTime.MinValue;
                double dur = 0;
                this._prev_command_failed = null;
                string rec_buffer = null;
                while (dur < maxWait)
                {
                    lock (this._received_buffer)
                    {
                        rec_buffer = this._received_buffer.ToString();
                    }
                    bool recAll = false;
                    if (string.IsNullOrEmpty(expected))
                    {
                        recAll = (rec_buffer.Trim().EndsWith(this.SessionPrompt) && rec_buffer.Contains(cmd));
                    }
                    else
                    {
                        recAll = (rec_buffer.Trim().Contains(expected) && rec_buffer.Contains(cmd));
                    }
                    if (recAll)
                    {
                        LogResponseCommand(cmd, startTime);
                        return rec_buffer;
                    }
                    Thread.Sleep(100);
                    dur = Utils.GetTimeDuration(startTime);
                    if (!this._received_buffer.ToString().Contains(cmd) && (dur >= (maxWait / 2)))
                    {
                        waitCmdCnt++;
                        LogCommandIncomplete("Still waiting the command", startTime, waitCmdCnt, cmd, this._received_buffer.ToString());
                        Thread.Sleep(500);
                    }
                }
                bool abortCommand = false;
                string error = "";
                if (!this._received_buffer.ToString().Trim().EndsWith(this.SessionPrompt) && this._received_buffer.ToString().Contains(cmd))
                {
                    abortCommand = true;
                    error = "Couldn't find the prompt at the end";
                }
                else if (this._received_buffer.ToString().Trim().EndsWith(this.SessionPrompt) && !this._received_buffer.ToString().Contains(cmd))
                {
                    error = "Received the prompt from the previous command line";
                }
                else
                {
                    abortCommand = true;
                    error = "Waited too long for the response from the command";
                }
                if (string.IsNullOrEmpty(error)) LogCommandIncomplete(error, startTime, waitCmdCnt, cmd, this._received_buffer.ToString());
                if (abortCommand)
                {
                    LogCommandIncomplete("Sending \"CTRL+C\" to the switch to abort the command",
                                          startTime, waitCmdCnt, cmd, this._received_buffer.ToString());
                    AbortCommand(cmd, maxWait);
                }
                throw new SwitchCommandError("Waited too long for the response from the command \"" + cmd + "\" (> " + Utils.CalcStringDuration(startTime) + ")!");
            }
            catch (SwitchCommandError ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        private bool AbortCommand(string cmd, int maxWait)
        {
            this._received_buffer = new StringBuilder();
            string chunkData = "";
            int waitPromptCnt = 1;
            int timeCnt = 0;
            double dur = 0;
            FlushSshStream();
            this._writer.Write('\u0003');
            Thread.Sleep(50);
            DateTime startTime = DateTime.Now;
            while ((dur < maxWait) && (waitPromptCnt < 3))
            {
                if (this._received_buffer.ToString().Trim().Contains(this.SessionPrompt))
                {
                    return true;
                }
                timeCnt++;
                if (timeCnt >= 10)
                {
                    this._writer.Write('\u0003');
                    LogCommandIncomplete("Sending \"CTRL+C\" to the switch to abort the previous command", startTime, waitPromptCnt, cmd, chunkData);
                    waitPromptCnt++;
                    timeCnt = 0;
                }
                Thread.Sleep(500);
                dur = Utils.GetTimeDuration(startTime);
            }
            this._prev_command_failed = cmd;
            LogCommandIncomplete("Waited too long for the prompt on Abort Command", startTime, waitPromptCnt, cmd, chunkData);
            return false;
        }
        private void FlushSshStream()
        {
            if ((this._shell_stream != null) && this._shell_stream.DataAvailable)
            {
                this._shell_stream.Flush();
            }
        }
        private Dictionary<string, string> ParseResponse(string response, string cmd)
        {
            if (!IsResponseError(response, cmd, this.SessionPrompt))
            {
                Dictionary<string, string> result = new Dictionary<string, string>
                {
                    [CMD] = cmd,
                    [OUTPUT] = response,
                    [PROMPT] = this.SessionPrompt
                };
                return result;
            }
            return null;
        }

        #region Commands
        public static bool IsResponseError(string response, string cmd, string prompt)
        {
            try
            {
                if (!string.IsNullOrEmpty(response) && response.Contains(SWITCH_ERROR))
                {
                    string resp = response.ToLower().Replace("^", "").Replace(prompt, "").Replace("\r\n", "").Trim();
                    StringBuilder error = new StringBuilder("Command: \"");
                    error.Append(cmd).Append("\"\r\nError: ").Append(response);
                    throw new SwitchCommandError(error.ToString());
                }
            }
            catch { }
            return false;
        }
        #endregion

        #region CliUtils
        public List<Dictionary<string, string>> ParseKeyValList(string data, string cmd, string sessionPrompt, string headerDelim, char delim)
        {
            if (string.IsNullOrEmpty(data)) return null;
            List<Dictionary<string, string>> keyValTbl = new List<Dictionary<string, string>>();
            Dictionary<string, string> keyVal = null;
            using (StringReader reader = new StringReader(data))
            {
                string line;
                bool foundHeader = false;
                string prefix = "";
                StringBuilder text = null;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line == string.Empty) continue;
                    if (!string.IsNullOrEmpty(cmd)) line = line.Replace(cmd, "");
                    if (!string.IsNullOrEmpty(sessionPrompt)) line = line.Replace(sessionPrompt, "");
                    if (!string.IsNullOrEmpty(headerDelim) && line.ToUpper().Contains(headerDelim))
                    {
                        line = line.Trim();
                        if (foundHeader && (keyVal != null))
                        {
                            if (text != null) keyVal[TEXT_KEY] = text.ToString();
                            keyValTbl.Add(keyVal);
                        }
                        foundHeader = true;
                        prefix = "";
                        keyVal = ParseKeyValHeader(line.Trim().ToUpper().Split(delim), '/');
                        text = null;
                    }
                    else
                    {
                        if (text == null)
                        {
                            if (line.Contains(delim))
                            {
                                line = line.Trim();
                                keyVal = StringToKeyVal(keyVal, line, prefix, delim);
                                if (keyVal.ContainsKey(PREFIX_KEY))
                                {
                                    prefix = keyVal[PREFIX_KEY];
                                    keyVal.Remove(PREFIX_KEY);
                                }
                            }
                            else
                                text = new StringBuilder(line).Append("\r\n");
                        }
                        else
                            text.Append(line).Append("\r\n");
                    }
                }
                if (foundHeader && (keyVal != null))
                {
                    if (text != null) keyVal[TEXT_KEY] = text.ToString();
                    keyValTbl.Add(keyVal);
                }
            }
            return keyValTbl;
        }
        private Dictionary<string, string> StringToKeyVal(Dictionary<string, string> keyVal, string line, string inPrefix, char delim)
        {
            string prefix = "";
            if (!string.IsNullOrEmpty(inPrefix)) prefix = inPrefix + "_";
            string[] entryList = line.Split(',');
            if ((entryList.Length < 1) || string.IsNullOrEmpty(entryList[0])) entryList[0] = line;
            if (keyVal == null) keyVal = new Dictionary<string, string>();
            if (entryList.Length > 0)
            {
                foreach (string entry in entryList)
                {
                    if (string.IsNullOrEmpty(entry)) continue;
                    string[] arrKeyVal = entry.Trim().Split(delim);
                    if ((arrKeyVal.Length > 0) && !string.IsNullOrEmpty(arrKeyVal[0]))
                    {
                        string sVal = "";
                        if ((arrKeyVal.Length > 1) && !string.IsNullOrEmpty(arrKeyVal[1]))
                        {
                            sVal = arrKeyVal[1].Trim();
                            if (arrKeyVal.Length > 2)
                            {
                                for (int idx = 2; idx < arrKeyVal.Length; idx++)
                                {
                                    if ((idx == 2) && (sVal.Length > 2)) sVal += ", " + arrKeyVal[idx].Trim();
                                    else sVal += ":" + arrKeyVal[idx].Trim();
                                }
                            }
                        }
                        if (string.IsNullOrEmpty(sVal)) keyVal[PREFIX_KEY] = arrKeyVal[0].Trim().ToUpper();
                        else keyVal[prefix + arrKeyVal[0].Trim().ToUpper().Replace("/", "_").Replace(" ", "_").Replace("-", "_")] = sVal;
                    }
                }
            }
            return keyVal;
        }
        private Dictionary<string, string> ParseKeyValHeader(string[] arrVal, char sep)
        {
            if ((arrVal == null) || (arrVal.Length < 1) || string.IsNullOrEmpty(arrVal[0])) return null;
            arrVal[0] = arrVal[0].Trim();
            if (arrVal[0].Contains(" "))
            {
                string[] splits = arrVal[0].Split(' ');
                arrVal = new string[2];
                arrVal[0] = null;
                arrVal[1] = null;
                foreach (string sVal in splits)
                {
                    if (string.IsNullOrEmpty(sVal)) continue;
                    if (arrVal[0] == null) arrVal[0] = sVal.Trim().ToUpper();
                    else if (arrVal[1] == null)
                    {
                        arrVal[1] = sVal.Trim();
                        break;
                    }
                }
                if (string.IsNullOrEmpty(arrVal[0]) || string.IsNullOrEmpty(arrVal[1])) return null;
            }
            string[] keysList = arrVal[0].Split(sep);
            string[] valuesList = null;
            if ((arrVal.Length > 0) && !string.IsNullOrEmpty(arrVal[1]))
            {
                valuesList = arrVal[1].Split(sep);
                if ((keysList.Length < 1) || (valuesList.Length < 1)) return null;
                if (keysList.Length < valuesList.Length)
                {
                    string[] keys = keysList;
                    keysList = new string[valuesList.Length];
                    int id = keys.Length - 1;
                    for (int idx = valuesList.Length - 1; idx >= 0; idx--)
                    {
                        if (id >= 0)
                        {
                            keysList[idx] = keys[id];
                            id--;
                        }
                        else
                            keysList[idx] = PREFIX_KEY + "_" + (idx + 1);
                    }
                }
            }
            if (keysList.Length != valuesList.Length) return null;
            Dictionary<string, string> keyVal = new Dictionary<string, string>();
            for (int idx = 0; idx < keysList.Length; idx++)
            {
                if ((!string.IsNullOrEmpty(keysList[idx])) && (idx < valuesList.Length)) keyVal[ParseKey(keysList[idx])] = valuesList[idx].Trim();
            }
            return keyVal;
        }
        private string ParseKey(string key)
        {
            return key.Replace("|", "").Trim().Replace("/", "_").Replace(" ", "_").ToUpper(); ;
        }
        #endregion

        private SwitchRejectConnection GenerateRejectConnectionException(string cmd)
        {
            return new SwitchRejectConnection(PrintExceptionInfo("Switch rejected connection", cmd));
        }
        private SwitchConnectionDropped GenerateConnectionDroppedException(string cmd)
        {
            return new SwitchConnectionDropped(PrintExceptionInfo("SSH connection dropped", cmd));
        }

        private string PrintExceptionInfo(string title, string cmd)
        {
            try
            {
                StringBuilder error = new StringBuilder(title);
                error.Append(" (Switch: \"").Append(this._switch.Name).Append("\", IP: ").Append(this._switch.IpAddress);
                if (cmd != null) error.Append(", command: \"").Append(cmd).Append("\"");
                error.Append(")");
                return error.ToString();
            }
            catch (Exception ex)
            {
                LogException("PrintExceptionInfo", ex);
            }
            return "";
        }

        private SwitchConnectionFailure GenerateConnectionFailException()
        {
            StringBuilder error = new StringBuilder("Switch connection fail!");
            try
            {
                error.Append("\r\nSwitch: \"").Append(this._switch.Name).Append("\", IP: ").Append(this._switch.IpAddress).Append(", Timeout: ");
                error.Append(this._switch.CnxTimeout).Append(" sec");
            }
            catch (Exception ex)
            {
                LogException("GenerateConnectionFailException", ex);
            }
            return new SwitchConnectionFailure(error.ToString());
        }

        protected override void OnDisconnecting()
        {
            LogInfo("AosSshClient", "Disconnecting the SSH client!");
        }
        protected override void OnDisconnected()
        {
            LogInfo("AosSshClient", "SSH client disconnected!");
        }
        private string PrintCommandResponseInfo(string title, string cmd, string response, DateTime startTime)
        {
            try
            {
                StringBuilder txt = new StringBuilder(title);
                txt.Append("\r\nCommand: \"").Append(cmd).Append("\" , Duration: ");
                txt.Append(Utils.CalcStringDuration(startTime)).Append("\r\nData Received from the switch:\r\n").Append(response);
                return txt.ToString();
            }
            catch { }
            return "";
        }
        private void LogCommandIncomplete(string header, DateTime startTime, int waitCmdCnt, string cmd, string result)
        {
            try
            {
                StringBuilder txt = new StringBuilder(header);
                if (waitCmdCnt > 0) txt.Append(" (#Retry: ").Append(waitCmdCnt).Append(")");
                txt.Append("!");
                string msg = PrintCommandResponseInfo(txt.ToString(), cmd, result.ToString(), startTime);
                if (header.Contains("too long") || header.Contains("previous command"))
                {
                    LogError("WaitResponseFromSwitch", msg);
                }
                else
                {
                    LogWarn("WaitResponseFromSwitch", msg);
                }
            }
            catch (Exception ex)
            {
                LogException("LogCommandIncomplete", ex);
            }
        }
        private void LogSendCommand(string cmd, DateTime startTime)
        {
            try
            {
                if (Logger.LogLevel == LogLevel.Trace)
                {
                    StringBuilder txt = new StringBuilder("Command: \"");
                    txt.Append(cmd).Append("\", Send Duration: ").Append(Utils.CalcStringDuration(startTime));
                    LogTrace("SendCommandToSwitch", txt.ToString());
                }
            }
            catch (Exception ex)
            {
                LogException("LogSendCommand", ex);
            }
        }
        private void LogResponseCommand(string cmd, DateTime startTime)
        {
            try
            {
                if (Logger.LogLevel == LogLevel.Trace && this._received_buffer != null)
                {
                    StringBuilder txt = new StringBuilder("Command: \"");
                    txt.Append(cmd).Append("\", Response Duration: ").Append(Utils.CalcStringDuration(startTime));
                    txt.Append("\r\nSwitch Response:\r\n").Append(this._received_buffer);
                    LogTrace("WaitResponseFromSwitch", txt.ToString());
                }
            }
            catch (Exception ex)
            {
                LogException("LogResponseCommand", ex);
            }
        }
        private void LogSSHConnection(DateTime startConnectTime, DateTime startPromptTime)
        {
            try
            {
                StringBuilder txt = new StringBuilder("Connecting to Switch \"");
                txt.Append(this._switch.Name).Append("\" (IP: ").Append(this._switch.IpAddress).Append("), Connect Time Duration: ");
                txt.Append(Utils.CalcStringDuration(startConnectTime)).Append(", Search Prompt Duration: ");
                txt.Append(Utils.CalcStringDuration(startPromptTime)).Append("\r\nSSH Session configuration:\r\n").Append("Session Prompt: \"");
                txt.Append(this.SessionPrompt).Append("\", Login Timer: ").Append(this._login_timer).Append(" sec, Max. Login Attempts: ");
                txt.Append(this._login_attempts).Append(", CLI Inactivity Timer: ").Append(this._cli_inactivity_timer);
                txt.Append(" min, FTP Inactivity Timer: ").Append(this._ftp_inactivity_timer).Append(" min, HTTP Inactivity Timer: ");
                txt.Append(this._http_inactivity_timer).Append(" min");
                LogDebug("ConnectSshClient", txt.ToString());
            }
            catch (Exception ex)
            {
                LogException("LogSSHConnection", ex);
            }
        }
        private void LogTrace(string title, string txt)
        {
            Logger.Trace($"{PrintTitle(title)}\n{txt}");
        }
        private void LogDebug(string title, string txt)
        {
            Logger.Debug($"{PrintTitle(title)}\n{txt}");
        }
        private void LogInfo(string title, string txt)
        {
            Logger.Info($"{PrintTitle(title)}\n{txt}");
        }
        private void LogWarn(string title, string warn)
        {
            Logger.Warn($"{PrintTitle(title)}\n{warn}");
        }
        private void LogError(string title, string warn)
        {
            Logger.Error($"{PrintTitle(title)}\n{warn}");
        }
        private void LogException(string title, Exception ex)
        {
            Logger.Error(PrintTitle(title), ex);
        }
        private string PrintTitle(string title)
        {
            StringBuilder txt = new StringBuilder("SSH Client (");
            txt.Append(title).Append(", Switch \"").Append(this._switch.Name).Append("\", IP: ").Append(this._switch.IpAddress).Append(")");
            return txt.ToString();
        }


    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using static PoEWizard.Data.Constants;
using PoEWizard.Data;
using PoEWizard.Comm;
using PoEWizard.Components;
using PoEWizard.Device;

namespace PoEWizard.Device
{
    public class DeviceModel
    {
        #region Private stuff
        private int currentSlot = 1;
        private bool showRetryDialog;
        private int sendPasswordWaitingTimes = 0;
        private bool isRunningFromWorking = false;
        private ChassisInfo chassisInfo = null;
        private IProgress<ProgressReport> progress;
        #endregion
        #region Properties
        public static string sessionPrompt = DEFAULT_PROMPT;
        public static string UserName { get; set; }
        public static string Password { get; set; }
        public static bool IsLoginWithDefault { get; set; } = true;
        public string SerialNumber { get; set; } = "";
        public string Model { get; set; } = "";
        public string MacAddress { get; set; } = "";
        public string ReleaseNumber { get; set; } = "";
        public string RunDir { get; set; } = "";
        public bool IsConnected { get; set; } = false;
        public bool NeedToReboot { get; set; } = false;
        public int ChassisCount { get; set; } = 1;
        public bool Is6350 => Model.StartsWith("OS6350");
        public bool HasTemplate { get; set; } = true;
        public bool NewDhcpVersion
        {
            get
            {
                string[] version = Regex.Split(ReleaseNumber, "\\.");
                int res = int.TryParse(version[0], out int v) ? v : 0;
                if (res < 8) return false;
                res = int.TryParse(version[1], out v) ? v : 0;
                return res > 5;
            }
        }
        public bool DisableDhcpService { get; set; } = false;
        public bool IsAos6x { get; set; } = false;
        public string RebootDeviceModel { get; set; } = "";
        public bool IsSecondary { get; set; }
        public int PortCount => PortList.Count;
        public string ModelPorts
        {
            get
            {
                string pattern = MATCH_MODEL;
                string result = null;
                Match m = Regex.Match(Model, pattern);
                if (m.Success)
                {
                    result = m.Groups?[3].Value;
                }
                return result;
            }
        }
        public bool IsConfigApplied { get; set; } = false;
        public string AppliedConfig { get; set; } = string.Empty;
        public bool IsSshEnabled { get; set; } = false;
        public bool IsQosEnabled { get; set; } = false;
        public List<string> PortList { get; set; } = new List<string>();
        public bool ResetCliTimeout { get; set; } = true;
        public string CliTimeout { get; set; }
        public string ErrorLog { get; set; } = string.Empty;
        #endregion
        #region Public methods
        public DeviceModel() { }

        public void ConnectDevice(IProgress<ProgressReport> progress)
        {
            this.progress = progress;
            if (!IsConnected)
            {
                CheckConnection();
            }
            else
            {
                DisconnectDevice();
            }
        }

        #endregion
        #region Private methods
        private void CheckConnection()
        {
            if (IsLoginWithDefault)
            {
                UserName = DEFAULT_USERNAME;
                Password = DEFAULT_PASSWORD;
            }
            ResetRebootInfo();
            string exceptionCommand = null;
            string runningCMM = null;
            progress.Report(new ProgressReport("Logging in to the switch..."));
            new CmdExecutor().Enter().Response().Regex(MATCH_ANY_CHAR).Custom(new DeviceActor((actor0, data0) =>
            {
                // This method must return a root branch and it is CmdExecutor
                string cli = data0.Trim();

                progress.Report(new ProgressReport("Checking session configuration..."));
                CmdExecutor showSessionConfig = new CmdExecutor();
                showSessionConfig.Send(Commands.ShowSessionConfig)
                .Response().Contains(LOGIN_ATTEMPTS).Custom(new DeviceActor((actor1, data1) =>
                {
                    string promptTemp;
                    Dictionary<string, string> sessionCfg = CliParseUtils.ParseETable(data1);
                    string s;
                    if (data1.Contains(CLI_FULL_PROMPT))
                        promptTemp = sessionCfg.TryGetValue(CLI_FULL_PROMPT, out s) ? s : null;
                    else
                        promptTemp = sessionCfg.TryGetValue(CLI_PROMPT, out s) ? s : null;

                    sessionPrompt = promptTemp ?? DEFAULT_PROMPT;
                    CliTimeout = sessionCfg.TryGetValue(CLI_TIMEOUT, out s) ? s : "4";
                    Logger.Debug($"Session Prompt: {sessionPrompt}");
                    Logger.Debug($"Session Timeout: {CliTimeout}");

                    CmdExecutor showMicrocode = new CmdExecutor();
                    showMicrocode.Send(Commands.ShowMicrocode).Response().EndsWith(sessionPrompt)
                    .Custom(new DeviceActor((actor2, data2) =>
                    {
                        Regex regex = new Regex(MATCH_VERSION);
                        Match match = regex.Match(data2);
                        if (match.Success)
                        {
                            string ver = match.Value;
                            ReleaseNumber = string.IsNullOrEmpty(ver) ? "N/A" : ver;
                        }
                        else
                        {
                            ReleaseNumber = "N/A";
                        }
                        string firstChar = Regex.Match(ReleaseNumber, @"^\d+\.").Value.Replace(".", "");
                        int release = int.TryParse(firstChar, out int v) ? v : 0;
                        IsAos6x = release >= 6 && release < 8;
                        Commands.Version = IsAos6x ? AosVersion.V6 : AosVersion.V8;
                        progress.Report(new ProgressReport("Checking stack topology..."));
                        CmdExecutor showTopology = new CmdExecutor();
                        showTopology.Send(Commands.ShowStackTopology).Response().EndsWith(sessionPrompt)
                        .Custom(new DeviceActor((actor3, data3) =>
                        {
                            CmdExecutor showChassis = new CmdExecutor();
                            showChassis.Send(Commands.ShowChassis).Response().EndsWith(sessionPrompt)
                            .Custom(new DeviceActor((actor4, data4) =>
                            {
                                bool isAloneSwitch = false;
                                List<List<string>> list;
                                try
                                {
                                    list = CliParseUtils.ParseRowInfoNoKeyTable(data3, Commands.ShowStackTopology, sessionPrompt, true);
                                    ChassisCount = list.Count;
                                }
                                catch (Exception ex)
                                {
                                    exceptionCommand = Commands.ShowStackTopology;
                                    Logger.Error($"Error parsing result of command {Commands.ShowStackTopology}", ex);
                                    return null;
                                }
                                foreach (List<string> strings in list)
                                {
                                    if (strings.Count < 3) continue;
                                    if (runningCMM != "" && strings[0].ToUpper().Contains(runningCMM.ToUpper()))
                                    {
                                        currentSlot = int.TryParse(strings[2], out int i) ? i : 1;
                                        break;
                                    }
                                    else if (runningCMM.ToUpper().Contains(strings[1].ToUpper()))
                                    {
                                        currentSlot = int.TryParse(strings[3], out int i) ? i : 1;
                                        break;
                                    }
                                    else
                                    {
                                        isAloneSwitch = true;
                                    }
                                    break;
                                }
                                if (!isAloneSwitch)
                                {
                                    bool isChassisID = data4.Contains("Chassis ID");
                                    int startIndex = data4.IndexOf((isChassisID ? "Chassis ID " : "Chassis ") + currentSlot);
                                    if (startIndex == -1) startIndex = 0;
                                    int lastIndex = data4.IndexOf((isChassisID ? "Chassis ID " : "Chassis ") + (currentSlot + 1));
                                    if (lastIndex == -1) data4 = data4.Substring(startIndex);
                                    else data4 = data4.Substring(startIndex, lastIndex - startIndex);
                                }
                                // save chassis info then active PoE then use saved chassis info to complete login
                                // use chassis info to detect model and execute appropriate PoE cmd
                                chassisInfo = GetChassisInfo(data4);
                                if (chassisInfo?.ModelName == null) return null;
                                Commands.Version = chassisInfo.IsOS6x ? AosVersion.V6 : AosVersion.V8;
                                CmdExecutor nextCmd = new CmdExecutor();
                                nextCmd.Send(Commands.StartPoE).Response().EndsWith(sessionPrompt)
                                .Send("no swlog output console").Response().EndsWith(sessionPrompt);

                                progress.Report(new ProgressReport("Checking POE status..."));
                                return nextCmd;
                            }));

                            progress.Report(new ProgressReport("Reading model name and _serialNo number..."));
                            return showChassis;
                        }));

                        CmdExecutor setSessionTimeout = new CmdExecutor();
                        setSessionTimeout.Send(Commands.SessionTimeout + SESSION_TIMEOUT).Response().EndsWith(sessionPrompt)
                        .Custom(new DeviceActor((actor5, data5) => showTopology));

                        return setSessionTimeout;
                    }));

                    CmdExecutor runningDir = new CmdExecutor();
                    runningDir.CtrlBreak().Response().EndsWith(sessionPrompt)
                    .Send(Commands.ShowRunningDir).Response().EndsWith(sessionPrompt)
                    .Custom(new DeviceActor((actor5, data5) =>
                    {
                        string runDir = GetRunningDir(data5);
                        runningCMM = GetRunningCMM(data5);
                        isRunningFromWorking = runDir != null && runDir.Equals(WORKING, StringComparison.CurrentCultureIgnoreCase);
                        IsSecondary = data5.Contains(SLAVE_PRIMARY) || data5.Contains(SECONDARY);
                        progress.Report(new ProgressReport("Reading software version..."));
                        return IsSecondary ? null : showMicrocode;
                    }));

                    progress.Report(new ProgressReport("Checking running directory..."));
                    return runningDir;
                }));

                CmdExecutor sendPassword = new CmdExecutor();
                sendPassword.Send(Password).Wait(1000).Response().Regex(MATCH_ANY_CHAR)
                .Custom(new DeviceActor((actor6, data6) =>
                {
                    if (!data6.Contains(AUTHENTICATION_FAILED))
                    {
                        showRetryDialog = false;
                        return showSessionConfig;
                    }
                    else
                    {
                        CmdExecutor tryToGetAllDataAfterSendPassword = new CmdExecutor();
                        tryToGetAllDataAfterSendPassword.Enter().Wait(100).Response().Regex(MATCH_ANY_CHAR)
                        .Custom(new DeviceActor((actor7, data7) =>
                        {
                            string resp = data7.Trim();
                            if (!resp.Contains(AUTHENTICATION_FAILED))
                            {
                                showRetryDialog = false;
                                return showSessionConfig;
                            }
                            else if (resp.EndsWith(PASSWORD_PROMPT) || resp.EndsWith(PASSWORD_PROMPT_6X))
                            {
                                showRetryDialog = true;
                                return null;
                            }
                            else if (sendPasswordWaitingTimes < 3)
                            {
                                sendPasswordWaitingTimes++;
                                return tryToGetAllDataAfterSendPassword;
                            }
                            else
                            {
                                showRetryDialog = true;
                                return null;
                            }
                        }));
                        sendPasswordWaitingTimes = 1;
                        return tryToGetAllDataAfterSendPassword;
                    }
                }));

                CmdExecutor login = new CmdExecutor();
                login.Send(UserName).Response()
                .Regex(".*(" + PASSWORD_PROMPT + OR_CHAR + PASSWORD_PROMPT_6X + ").*")
                .Custom(new DeviceActor((actor9, data9) => sendPassword));

                if (cli.EndsWith(LOGIN_PROMPT) || cli.EndsWith(LOGIN_PROMPT_6X))
                {
                    ClearLoggedInState();
                    return login;
                }
                else if (cli.EndsWith(PASSWORD_PROMPT) || cli.EndsWith(PASSWORD_PROMPT_6X))
                {
                    ClearLoggedInState();
                    return sendPassword;
                }
                else if (cli.EndsWith(CONFIRM_EXIT_Y_N))
                {
                    return GetConfirmExitCmd(login);
                }
                else if (cli.Contains(AUTHENTICATION_FAILED))
                {
                    ClearLoggedInState();
                    new CmdExecutor().Enter().Wait(5000).Consume();
                    return login;
                }
                else
                {
                    // already logged in?
                    // log out and log back in to make sure we capture the right prompt
                    ResetCliTimeout = false;
                    DisconnectDevice(true);
                    return login;
                }

            }))
            .Consume(new ResultCallback(result =>
            {
                if (string.IsNullOrEmpty(exceptionCommand))
                {
                    CompleteLogin(chassisInfo);
                }
                else
                {
                    Logger.Error($"Error executing {exceptionCommand}");
                    progress.Report(new ProgressReport(ReportType.Error, "Connect", $"Error reading data: {exceptionCommand}"));
                }
            }, error =>
            {
                Logger.Error($"Error connecting to switch: {error}");
                progress.Report(new ProgressReport(ReportType.Error, "Connect", $"Error when connecting to device:\n{error}\n(Please try again)"));
                ClearLoggedInState();
            }));
        }
        private void CheckAppliedConfig()
        {
            progress.Report(new ProgressReport("Checking applied configuration..."));
            string viewCfgFileCmd;
            string result = string.Empty;
            bool noFile = true;
            if (IsAos6x)
            {
                viewCfgFileCmd = $"more {HAS_BEEN_APPLIED_CONFIG_FLAG}";
                new CmdExecutor().Send(viewCfgFileCmd).Response().Regex($"{MATCH_MORE}|{sessionPrompt}")
                    .Wait(1000).Send("q").Consume(new ResultCallback(res =>
                    {
                        result = res;
                    }, err =>
                    {
                        Logger.Error($"Failed to check applied configuration on device S/N {SerialNumber}, model {Model}: {err}");
                    }));
            }
            else
            {
                viewCfgFileCmd = $"grep \"! Applied configuration\" {HAS_BEEN_APPLIED_CONFIG_FLAG}";
                new CmdExecutor().Send(viewCfgFileCmd).Wait(250).Response().EndsWith($"\n{sessionPrompt}")
                    .Consume(new ResultCallback(res =>
                    {
                        result = res;
                    }, err =>
                    {
                        Logger.Error($"Failed to check applied configuration on device S/N {SerialNumber}, model {Model}: {err}");
                    }));
            }
            noFile = result.ToLower().Contains("does not exist") || result.ToLower().Contains("no such file");
            if (!noFile)
            {
                IsConfigApplied = true;
                result = Regex.Match(result, "Applied configuration:.+").Value.Replace("\r", "");
                if (!string.IsNullOrEmpty(result))
                {
                    AppliedConfig = result.Contains(": restored") ? ("Configuration" + result.Split(':')[1]) : result;
                }
                else
                {
                    AppliedConfig = "Applied configuration: unkown";
                }
            }
            else
            {
                IsConfigApplied = false;
            }
            FileHanlder.Device = this;
            progress.Report(new ProgressReport("Checking security features..."));

        }
        private string ReadSwitchFile(string filename)
        {
            string res = string.Empty;
            string showFileCmd;
            if (IsAos6x)
            {
                showFileCmd = $"more {filename}";
                bool isDone = false;
                string promptpat = $"{sessionPrompt.Trim()}$";
                new CmdExecutor()
                    .Send(showFileCmd).Response().Regex($"{MATCH_MORE}|{promptpat}")
                    .Consume(new ResultCallback(r =>
                    {
                        res = r;
                        isDone = res.EndsWith(sessionPrompt);
                    }, e => isDone = true));

                res = Regex.Replace(res, MATCH_MORE, "");
                while (!isDone)
                {
                    new CmdExecutor().Send("\n").Response().Regex($"{MATCH_MORE}|{promptpat}")
                    .Consume(new ResultCallback(r =>
                    {
                        r = Regex.Replace(r, MATCH_MORE, "");
                        res += r.Substring(r.LastIndexOf("\r\n"));
                        isDone = res.EndsWith(sessionPrompt);
                    }, e => isDone = true));
                }
                res = Regex.Replace(res, ESC_SEQUENCE, "");
                res = Regex.Replace(res, @"[\0|\00]", "");
            }
            else
            {
                showFileCmd = $"cat {filename}";
                new CmdExecutor()
                    .Send(showFileCmd).Response().EndsWith(sessionPrompt)
                    .Consume(new ResultCallback(
                        result => res = result,
                        error => res = error)
                    );
            }
            string[] lines = Regex.Split(res, "\r\n|\r|\n");
            res = string.Join(Environment.NewLine, lines.Take(lines.Length - 1).Skip(2));

            return res.Replace(showFileCmd, "");

        }
        private void DisconnectDevice(bool noJournal = false)
        {
            CmdExecutor disconnectDev = new CmdExecutor();
            if (ResetCliTimeout)
            {
                if (CliTimeout == null) CliTimeout = "4";
                disconnectDev = disconnectDev.Send(Commands.SessionTimeout + CliTimeout)
                    .Response().EndsWith(sessionPrompt);
            }
            disconnectDev.Send(Commands.Exit).Response()
            .Regex(".*(" + LOGOUT + OR_CHAR + LOGIN_PROMPT + OR_CHAR + LOGIN_PROMPT_6X + OR_CHAR + CONFIRM_EXIT_Y_N + ").*")
            .Custom(new DeviceActor((actor, data) =>
            {
                string cli = data.Trim();
                if (cli.EndsWith(LOGIN_PROMPT) || cli.EndsWith(LOGIN_PROMPT_6X))
                {
                    return null;
                }
                else if (cli.Trim().EndsWith(CONFIRM_EXIT_Y_N))
                {
                    return GetConfirmExitCmd(null);
                }
                else
                {
                    return null;
                }
            }))
            .Consume(new ResultCallback(result =>
            {
                if (!noJournal) Activity.Log($"Disconnected from  switch S/N {SerialNumber}, model {Model}");
                Logger.Info($"Disconnected from  switch S/N {SerialNumber}, model {Model}");
                ClearLoggedInState();
                CloseSession();
            }, error =>
            {
                if (!noJournal) Activity.Log($"Disconnected from  switch S/N {SerialNumber}, model {Model}");
                Logger.Info($"Disconnected from  switch S/N {SerialNumber}, model {Model}");
                ClearLoggedInState();
                CloseSession();
            }));
        }

        private static CmdActor GetConfirmExitCmd(CmdExecutor nextCmd)
        {
            CmdExecutor confirmExit = new CmdExecutor();
            confirmExit.Send(Y_LITERAL).Response()
                .Regex($".*({LOGIN_PROMPT}{OR_CHAR}{LOGIN_PROMPT_6X}{OR_CHAR}{LOGOUT_RESPONSE}).*")
                .Custom(new DeviceActor((actor, data) => nextCmd));
            return confirmExit;
        }
        private void ClearLoggedInState()
        {
            SerialNumber = "";
            Model = "";
            MacAddress = "";
            ReleaseNumber = "";
            RunDir = "";
            IsConnected = false;
            chassisInfo = null;
        }

        private void CloseSession()
        {
            Password = "";
            currentSlot = 1;
            ChassisCount = 1;
            IsSecondary = false;
            PortList = new List<string>();
            IsLoginWithDefault = true;
            IsConfigApplied = false;
            ErrorLog = string.Empty;
        }

        private string GetRunningCMM(string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                Dictionary<string, string> chassisTable = CliParseUtils.ParseVTable(data);
                return chassisTable.TryGetValue(RUNNING_CMM, out string dir) ? dir : "";
            }
            return "";
        }
        private ChassisInfo GetChassisInfo(string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                Dictionary<string, string> chassisTable = CliParseUtils.ParseVTable(data);
                string sn = chassisTable.TryGetValue(SERIAL_NUMBER, out string s) ? s : null;
                string mac = chassisTable.TryGetValue(CHASSIS_MAC, out s) ? s : null;
                string model = chassisTable.TryGetValue(MODEL_NAME, out s) ? s : null;
#if DEBUG
                //test
                //model = "OS6560-P24Z8";
                //end test
#endif
                if (string.IsNullOrEmpty(sn) || string.IsNullOrEmpty(model) || string.IsNullOrEmpty(mac))
                {
                    return null;
                }
                return new ChassisInfo(sn, mac, model);
            }
            return null;
        }

        private string GetRunningDir(string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                Dictionary<string, string> chassisTable = CliParseUtils.ParseVTable(data);
                // "Running configuration" is running directory info and different from "Running Configuration".
                return chassisTable.TryGetValue(RUNNING_CONFIGURATION, out string s) ? (s != string.Empty ? s : null) : null;
            }
            return null;
        }

        private void CompleteLogin(ChassisInfo chassisInfo)
        {
            bool success = false;
            if (chassisInfo != null)
            {
                Model = chassisInfo.ModelName;
                SerialNumber = chassisInfo.SerialNumber;
                MacAddress = chassisInfo.MacAddres;
                IsConnected = true;
                if (isRunningFromWorking)
                {
                    RunDir = WORKING_DIR;
                    GetPortInfo();
                }
                else
                {
                    RunDir = CERTIFIED_DIR;
                    SetBootInfo(chassisInfo);
                }
                success = true;
            }
            else
            {
                ClearLoggedInState();
            }
            if (IsSecondary)
            {
                progress.Report(new ProgressReport(ReportType.Error, "Connect", SECONDARY_DEVICE));
            }
            if (!success)
            {
                ClearLoggedInState();
                if (showRetryDialog)
                {
                    LoginTrigger();
                }
            }
        }

        private void SetBootInfo(ChassisInfo chassisInfo)
        {
            if (chassisInfo != null)
            {
                RebootDeviceModel = chassisInfo.ModelName;
            }
            NeedToReboot = true;
        }

        private void ResetRebootInfo()
        {
            RebootDeviceModel = "";
            NeedToReboot = false;
        }

        private void GetPortInfo()
        {
            progress.Report(new ProgressReport("Reading port information..."));
            PortList = new List<string>();
            CmdExecutor executor = new CmdExecutor();
            CmdConsumer sender = executor.Enter().Response().EndsWith(sessionPrompt);
            sender = sender.Send(Commands.ShowInterfaceStatus).Response().EndsWith(sessionPrompt);
            sender.Consume(new ResultCallback(result =>
            {
                string resultErrors = CliParseUtils.GetErrors(result);
                if (resultErrors == string.Empty)
                {
                    try
                    {
                        List<List<string>> portStatusData = CliParseUtils.ParseRowInfoNoKeyTable(result, Commands.ShowInterfaceStatus, sessionPrompt, true);
                        List<string> dupList = new List<string>();
                        for (int i = 0; i < portStatusData.Count; i++)
                        {
                            //name port is position 1. Ex: [3/1, Enable, 1000, Full, NA, Auto, Auto, NA, -]
                            //prevent device 6x spilit this line, because CliDataTableUtils.parseRowNoInfoKeyTable only spilit 2 "spacing",
                            //but this line have 2 "spacing" (PreferredFiber  F)
                            //FF - ForcedFiber PF - PreferredFiber  F - Fiber
                            if (portStatusData[i].Count > 2) dupList.Add(portStatusData[i][0]);
                        }
                        //remove duplicated hybrid ports
                        PortList = dupList.Distinct().ToList();
                        Logger.Debug($"Number of ports detected: {PortCount}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Error parsing result of command {Commands.ShowInterfaceStatus}", ex);
                        progress.Report(new ProgressReport(ReportType.Error, "Connect", $"Error: Wizard cannot read data (Command: {Commands.ShowInterfaceStatus}"));
                    }
                }
                else
                {
                    Logger.Error($"Error reading interface status: {resultErrors}");
                    progress.Report(new ProgressReport(ReportType.Error, "Connect", resultErrors));
                }
                IsConnected = true;
            }, error =>
            {
                Logger.Error($"Error on result of {Commands.ShowInterfaceStatus} command: {error}");
                progress.Report(new ProgressReport(ReportType.Error, "Connect", error));
                IsConnected = true;
            }));
        }

        private void LoginTrigger()
        {
            if (IsLoginWithDefault)
            {
                IsLoginWithDefault = false;
            }
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                Login login = new Login(UserName);
                login.Owner = MainWindow.Instance;
                login.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (login.ShowDialog() == true)
                {
                    UserName = login.User;
                    Password = login.Password;
                }
            }));
            CheckConnection();
        }

        private void ResetOk(string successMsg, bool clearExistingConfig)
        {
            progress.Report(new ProgressReport(ReportType.Info, clearExistingConfig ? "Factory Reset" : "Reboot", successMsg));
            string logMsg = (clearExistingConfig ? "Cleared existing configuration and t" : "T") +
                $"riggered reboot from Working on device S/N {SerialNumber}, model {Model}";
            Logger.Info(logMsg);
            Activity.Log(logMsg);
            ClearLoggedInState();
            CloseSession();
            sessionPrompt = DEFAULT_PROMPT;
        }
        #endregion
    }
}

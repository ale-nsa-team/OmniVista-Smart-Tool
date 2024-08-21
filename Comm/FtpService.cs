using PoEWizard.Data;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace PoEWizard.Comm
{
    public class FtpService
    {

        private const int Port = 21; // Default FTP port
        private const string CMD_USER = "USER";
        private const string CMD_PASS = "PASS";
        private const string CMD_SYST = "SYST";
        private const string CMD_TYPE = "TYPE";
        private const string CMD_PORT = "PORT";
        private const string CMD_PASV = "PASV";
        private const string CMD_LIST = "LIST";
        private const string CMD_RETR = "RETR";
        private const string CMD_PWD = "PWD";
        private const string CMD_QUIT = "QUIT";
        private const string CMD_RSTATUS = "RSTATUS";
        private const string CMD_RHELP = "RHELP";

        private static TcpListener _passiveListener = null;
        private static IPEndPoint _dataEndpoint = null;
        private static bool _isPassive = false;

        public string FtpRoot { get; set; }

        public FtpService()
        {
            // Ensure the application is running as administrator
            if (!IsRunningAsAdministrator())
            {
                throw new Exception("FTP Service must be run as an administrator.");
            }
            FtpRoot = Path.Combine(MainWindow.dataPath, "FTP");
            if (!Directory.Exists(FtpRoot))
            {
                Directory.CreateDirectory(FtpRoot);
            }
            // Start the FTP server
            var server = new TcpListener(IPAddress.Any, Port);
            server.Start();

            // List all files in FTPRoot at the start
            Logger.Activity($"FTP server started on port {Port} ...");
            Logger.Debug($"Files available for download in {FtpRoot}:");
            if (Directory.Exists(FtpRoot))
            {
                var files = Directory.GetFiles(FtpRoot);
                foreach (var file in files)
                {
                    Logger.Debug($"\n\t- {Path.GetFileName(file)}");
                }
            }
            else
            {
                Logger.Error($"Directory {FtpRoot} does not exist!");
            }
            while (true)
            {
                var client = server.AcceptTcpClient();
                Logger.Info("Client connected.");

                var clientThread = new Thread(() => HandleClient(client));
                clientThread.Start();
            }
        }

        private void HandleClient(TcpClient client)
        {
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream) { AutoFlush = true })
            {
                // Send welcome message
                writer.WriteLine("220 Simple FTP Server Ready");

                // Read client commands
                while (true)
                {
                    try
                    {
                        var command = ReadCommand(stream);
                        if (string.IsNullOrEmpty(command)) break;

                        Logger.Debug($"Received command: {command}");

                        if (command.StartsWith(CMD_USER, StringComparison.OrdinalIgnoreCase))
                        {
                            writer.WriteLine("331 Username ok, need password.");
                        }
                        else if (command.StartsWith(CMD_PASS, StringComparison.OrdinalIgnoreCase))
                        {
                            writer.WriteLine("230 User logged in.");
                        }
                        else if (command.StartsWith(CMD_SYST, StringComparison.OrdinalIgnoreCase))
                        {
                            writer.WriteLine("215 UNIX Type: L8");
                        }
                        else if (command.StartsWith(CMD_TYPE, StringComparison.OrdinalIgnoreCase))
                        {
                            writer.WriteLine("200 Type set to I.");
                        }
                        else if (command.StartsWith(CMD_PORT, StringComparison.OrdinalIgnoreCase))
                        {
                            HandlePortCommand(command, writer);
                        }
                        else if (command.StartsWith(CMD_PASV, StringComparison.OrdinalIgnoreCase))
                        {
                            HandlePasvCommand(writer, client.Client.LocalEndPoint as IPEndPoint);
                        }
                        else if (command.StartsWith(CMD_LIST, StringComparison.OrdinalIgnoreCase))
                        {
                            HandleListCommand(writer);
                        }
                        else if (command.StartsWith(CMD_RETR, StringComparison.OrdinalIgnoreCase))
                        {
                            var fileName = command.Substring(5).Trim();
                            HandleRetrCommand(writer, fileName);
                        }
                        else if (command.StartsWith(CMD_PWD, StringComparison.OrdinalIgnoreCase))
                        {
                            writer.WriteLine($"257 \"{FtpRoot}\" is the current directory.");
                        }
                        else if (command.StartsWith(CMD_QUIT, StringComparison.OrdinalIgnoreCase) || command.StartsWith("BYE", StringComparison.OrdinalIgnoreCase))
                        {
                            writer.WriteLine("221 Goodbye.");
                            break;
                        }
                        else if (command.StartsWith(CMD_RSTATUS, StringComparison.OrdinalIgnoreCase) || command.StartsWith("STAT", StringComparison.OrdinalIgnoreCase))
                        {
                            HandleRStatusCommand(writer);
                        }
                        else if (command.StartsWith(CMD_RHELP, StringComparison.OrdinalIgnoreCase) || command.StartsWith("HELP", StringComparison.OrdinalIgnoreCase))
                        {
                            HandleRHelpCommand(writer);
                        }
                        else
                        {
                            writer.WriteLine("502 Command not implemented.");
                            Logger.Debug($"Unknown command: {command}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                }
                Logger.Info("Client disconnected.");
            }
        }

        private static void HandlePortCommand(string command, StreamWriter writer)
        {
            try
            {
                // Parse the PORT command argument
                string[] parts = command.Substring(5).Split(',');
                string ipAddress = string.Join(".", parts, 0, 4);
                int port = (int.Parse(parts[4]) << 8) + int.Parse(parts[5]);

                _dataEndpoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
                _isPassive = false;

                writer.WriteLine("200 Command okay.");
                Logger.Debug($"PORT command received: {_dataEndpoint}");
            }
            catch (Exception ex)
            {
                writer.WriteLine("501 Syntax error in parameters or arguments.");
                Logger.Error(ex);
            }
        }

        private static void HandlePasvCommand(StreamWriter writer, IPEndPoint clientEndPoint)
        {
            if (clientEndPoint == null)
            {
                writer.WriteLine("425 Cannot enter passive mode. Invalid client endpoint.");
                Logger.Warn("Error: clientEndPoint is null.");
                return;
            }
            try
            {
                _isPassive = true;
                _passiveListener = new TcpListener(IPAddress.Any, 0);
                _passiveListener.Start();

                IPEndPoint localEndPoint = (IPEndPoint)_passiveListener.LocalEndpoint;
                string localIp = clientEndPoint.Address.ToString().Replace('.', ',');
                int port = localEndPoint.Port;
                string portHigh = (port / 256).ToString();
                string portLow = (port % 256).ToString();

                writer.WriteLine($"227 Entering Passive Mode ({localIp},{portHigh},{portLow})");
                Logger.Debug($"PASV command received: {_passiveListener.LocalEndpoint}");
            }
            catch (Exception ex)
            {
                writer.WriteLine("502 Command not implemented.");
                Logger.Error(ex);
            }
        }

        private void HandleListCommand(StreamWriter writer)
        {
            writer.WriteLine("150 Here comes the directory listing.");

            try
            {
                using (var dataClient = _isPassive ? _passiveListener.AcceptTcpClient() : new TcpClient())
                {
                    if (!_isPassive)
                    {
                        dataClient.Connect(_dataEndpoint);
                    }

                    using (var dataStream = dataClient.GetStream())
                    using (var dataWriter = new StreamWriter(dataStream) { AutoFlush = true })
                    {
                        foreach (var file in Directory.GetFiles(FtpRoot))
                        {
                            var fileInfo = new FileInfo(file);
                            dataWriter.WriteLine(fileInfo.Name);
                        }
                    }
                }
                writer.WriteLine("226 Directory send OK.");
            }
            catch (Exception ex)
            {
                writer.WriteLine("550 Failed to open directory.");
                Logger.Error(ex);
            }
        }

        private void HandleRetrCommand(StreamWriter writer, string fileName)
        {
            var filePath = Path.Combine(FtpRoot, fileName);
            if (!File.Exists(filePath))
            {
                writer.WriteLine("550 File not found.");
                Logger.Error($"File not found: {fileName}");
                return;
            }
            writer.WriteLine("150 Opening data connection.");
            try
            {
                using (var dataClient = _isPassive ? _passiveListener.AcceptTcpClient() : new TcpClient())
                {
                    if (!_isPassive)
                    {
                        dataClient.Connect(_dataEndpoint);
                    }

                    using (var dataStream = dataClient.GetStream())
                    {
                        byte[] fileBytes = File.ReadAllBytes(filePath);
                        dataStream.Write(fileBytes, 0, fileBytes.Length);
                    }
                }
                writer.WriteLine("226 Transfer complete.");
                Logger.Debug($"File {fileName} downloaded.");
            }
            catch (Exception ex)
            {
                writer.WriteLine("450 Requested file action not taken. Failed to send file.");
                Logger.Error(ex);
            }
        }

        private void HandleRStatusCommand(StreamWriter writer)
        {
            writer.WriteLine("211-Server status:");
            writer.WriteLine($"Connected clients: 1");
            writer.WriteLine($"Current directory: {FtpRoot}");
            writer.WriteLine($"Files available in {FtpRoot}: {Directory.GetFiles(FtpRoot).Length}");
            writer.WriteLine("211 End of status.");
        }

        private static void HandleRHelpCommand(StreamWriter writer)
        {
            writer.WriteLine("214-The following commands are available:");
            writer.WriteLine("USER - Authenticate as a user.");
            writer.WriteLine("PASS - Provide password for authentication.");
            writer.WriteLine("SYST - Get system type.");
            writer.WriteLine("TYPE - Set the transfer type (e.g., I for binary).");
            writer.WriteLine("PORT - Set the port for active mode data transfers.");
            writer.WriteLine("PASV - Enter passive mode for data transfers.");
            writer.WriteLine("LIST - List files in the current directory.");
            writer.WriteLine("RETR - Retrieve a file from the server.");
            writer.WriteLine("PWD - Print the working directory.");
            writer.WriteLine("QUIT/BYE - Disconnect from the server.");
            writer.WriteLine("RSTATUS/STAT - Display the server status.");
            writer.WriteLine("RHELP/HELP - Display this help message.");
            writer.WriteLine("214 End of help.");
        }

        private static string ReadCommand(NetworkStream stream)
        {
            // Read the command byte by byte and convert to ASCII
            var commandBytes = new byte[1024];
            int bytesRead = stream.Read(commandBytes, 0, commandBytes.Length);
            if (bytesRead > 0)
            {
                var commandString = Encoding.ASCII.GetString(commandBytes, 0, bytesRead);
                Logger.Debug($"Command received in raw bytes: {BitConverter.ToString(commandBytes, 0, bytesRead)}");
                return commandString.Trim();
            }
            return string.Empty;
        }

#pragma warning disable CA1416 // Platform compatibility
        private static bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

                Logger.Debug($"Debug: Is Admin: {isAdmin}");
                return isAdmin;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }
#pragma warning restore CA1416 // Platform compatibility

    }
}

using PoEWizard.Data;
using Renci.SshNet;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;

namespace PoEWizard.Comm
{
    public class DhcpService
    {

        public DhcpService()
        {
            if (!IsAdministrator())
            {
                throw new Exception("DHCP Service must be run as an administrator.");
            }

            PrintEthernetInterfaceInfo(); // Call to print Ethernet interface info
            Logger.Activity("Starting DHCP server...");
            RunDHCPServer();
        }

        private bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void PrintEthernetInterfaceInfo()
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            var ethernetInterfaces = networkInterfaces.Where(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet);
            StringBuilder txt = new StringBuilder();
            foreach (var ni in ethernetInterfaces)
            {
                txt.Append($"\n\tName: ").Append(ni.Name);
                txt.Append($"\n\tDescription: ").Append(ni.Description);
                txt.Append($"\n\tStatus: ").Append(ni.OperationalStatus);
                txt.Append($"\n\tSpeed: ").Append(ni.Speed / 1_000_000).Append(" Mbps");
                txt.Append($"\n\tMAC Address: ").Append(ni.GetPhysicalAddress()).Append("\n");
            }
            Logger.Activity(txt.ToString());
        }

        private void RunDHCPServer()
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, 67));

            Logger.Info("DHCP Server started. Listening for requests...");

            byte[] buffer = new byte[1024];

            while (true)
            {
                EndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                int receivedBytes = serverSocket.ReceiveFrom(buffer, ref clientEndPoint);

                Logger.Info($"Received {receivedBytes} bytes from {clientEndPoint}");

                byte messageType = buffer[242];
                Logger.Info($"Parsed DHCP message type: {messageType:X2}");

                if (messageType == 0x01) // DHCP Discover
                {
                    HandleDHCPDiscover(serverSocket, buffer, receivedBytes, clientEndPoint);
                }
                else if (messageType == 0x03) // DHCP Request
                {
                    HandleDHCPRequest(serverSocket, buffer, receivedBytes, clientEndPoint);
                }
                else
                {
                    Logger.Info($"Non-DHCP packet type {messageType:X2}. Ignoring.");
                }
            }
        }

        private void PrintPacketInfo(byte[] packet, int length)
        {
            StringBuilder txt = new StringBuilder("Packet Data:\n");
            for (int i = 0; i < length; i += 16)
            {
                for (int j = 0; j < 16 && (i + j) < length; j++)
                {
                    txt.Append($"{packet[i + j]:X2} ");
                }
                if ((i + 16) > length)
                {
                    for (int k = 0; k < 16 - (length % 16); k++)
                    {
                        txt.Append("   ");
                    }
                }
                txt.Append("  ");
                for (int j = 0; j < 16 && (i + j) < length; j++)
                {
                    if (packet[i + j] >= 32 && packet[i + j] <= 126)
                    {
                        txt.Append((char)packet[i + j]);
                    }
                    else
                    {
                        txt.Append(".");
                    }
                }
                txt.Append("\n");
            }
            Logger.Info(txt.ToString());

            if (length >= 240 && packet.Length >= 240)
            {
                if (packet[236] == 0x63 && packet[237] == 0x82 && packet[238] == 0x53 && packet[239] == 0x63)
                {
                    txt = new StringBuilder("DHCP Packet Detected:\n");
                    int offset = 240;
                    while (offset < length && offset < packet.Length && packet[offset] != 0xFF)
                    {
                        if (offset + 1 >= packet.Length) break;

                        byte optionType = packet[offset++];
                        if (offset >= packet.Length) break;

                        byte optionLength = packet[offset++];
                        if (offset + optionLength > packet.Length) break;

                        txt.Append("Option ").Append(optionType).Append(" (Length: ").Append(optionLength).Append("): ");
                        for (int i = 0; i < optionLength && (offset + i) < packet.Length; i++)
                        {
                            txt.Append(packet[offset + i]).Append(":X2 ");
                        }
                        txt.Append("\n");
                        offset += optionLength;
                    }
                    Logger.Info(txt.ToString());
                }
            }
            else
            {
                Logger.Info("Packet is too short to be a valid DHCP packet.");
            }
        }

        private void HandleDHCPDiscover(Socket serverSocket, byte[] buffer, int length, EndPoint clientEndPoint)
        {
            Logger.Info("Handling DHCP Discover");

            if (length < 240)
            {
                Logger.Info($"Packet length {length} is too short to contain a valid DHCP payload.");
                return;
            }

            byte[] sourceMac = new byte[6];
            Array.Copy(buffer, 28, sourceMac, 0, 6);
            string sourceMacString = BitConverter.ToString(sourceMac);
            Logger.Info($"Client MAC: {sourceMacString}");

            string payload = Encoding.ASCII.GetString(buffer, 0, length);
            Logger.Info($"DHCP Discover Payload:\n{payload}");

            if (payload.Contains("OmniSwitch-"))
            {
                Logger.Info("OmniSwitch client detected. Preparing to send DHCP Offer...");

                byte[] offerIP = { 192, 168, 0, 2 };
                byte[] serverIdentifier = { 192, 168, 0, 1 };
                byte[] subnetMask = { 255, 255, 255, 0 };
                byte[] routerIP = { 192, 168, 0, 1 };
                byte[] tftpServerIP = Encoding.ASCII.GetBytes("192.168.0.1"); // Option 66 corrected to ASCII format
                byte[] bootfileName = Encoding.ASCII.GetBytes("ulc.alu");
                byte[] option138IP = { 192, 168, 39, 77 };
                //int leaseTime = 3600;

                byte[] offerPacket = new byte[240 + 3 + 6 + 6 + 6 + 6 + 3 + bootfileName.Length + 6 + 1];

                offerPacket[0] = 0x02; // Message type: Boot Reply (2)
                offerPacket[1] = 0x01; // Hardware type: Ethernet (1)
                offerPacket[2] = 0x06; // Hardware address length: 6
                offerPacket[3] = 0x00; // Hops: 0

                Array.Copy(buffer, 4, offerPacket, 4, 4); // Transaction ID
                offerPacket[8] = 0x00; // Seconds elapsed
                offerPacket[9] = 0x00; // Seconds elapsed
                offerPacket[10] = 0x80; // Bootp flags: 0x8000, Broadcast flag
                offerPacket[11] = 0x00; // Bootp flags

                Array.Copy(offerIP, 0, offerPacket, 16, 4); // 'Your' IP address
                Array.Copy(serverIdentifier, 0, offerPacket, 20, 4); // Next server IP address (siaddr field)
                Array.Copy(sourceMac, 0, offerPacket, 28, 6); // Client MAC address

                offerPacket[236] = 0x63; // Magic cookie
                offerPacket[237] = 0x82; // Magic cookie
                offerPacket[238] = 0x53; // Magic cookie
                offerPacket[239] = 0x63; // Magic cookie

                int offset = 240;

                offerPacket[offset++] = 0x35;
                offerPacket[offset++] = 0x01;
                offerPacket[offset++] = 0x02;
                Logger.Info($"Offset {offset - 3:X2}: Option 53 (Length: 1): 02 (DHCP Offer)");

                offerPacket[offset++] = 0x01;
                offerPacket[offset++] = 0x04;
                Array.Copy(subnetMask, 0, offerPacket, offset, 4);
                Logger.Info($"Offset {offset - 2:X2}: Option 1 (Length: 4): {BitConverter.ToString(subnetMask)}");
                offset += 4;

                offerPacket[offset++] = 0x03;
                offerPacket[offset++] = 0x04;
                Array.Copy(routerIP, 0, offerPacket, offset, 4);
                Logger.Info($"Offset {offset - 2:X2}: Option 3 (Length: 4): {BitConverter.ToString(routerIP)}");
                offset += 4;

                offerPacket[offset++] = 0x42;
                offerPacket[offset++] = (byte)tftpServerIP.Length; // Length of the ASCII IP string
                Array.Copy(tftpServerIP, 0, offerPacket, offset, tftpServerIP.Length);
                Logger.Info($"Offset {offset - 2:X2}: Option 66 (Length: {tftpServerIP.Length}): {BitConverter.ToString(tftpServerIP)} (ASCII: {Encoding.ASCII.GetString(tftpServerIP)})");
                offset += tftpServerIP.Length;

                offerPacket[offset++] = 0x43;
                offerPacket[offset++] = (byte)bootfileName.Length;
                Array.Copy(bootfileName, 0, offerPacket, offset, bootfileName.Length);
                Logger.Info($"Offset {offset - 2:X2}: Option 67 (Length: {bootfileName.Length}): {BitConverter.ToString(bootfileName)} (ASCII: {Encoding.ASCII.GetString(bootfileName)})");
                offset += bootfileName.Length;

                offerPacket[offset++] = 0x8A; // Option 138
                offerPacket[offset++] = 0x04;
                Array.Copy(option138IP, 0, offerPacket, offset, option138IP.Length);
                Logger.Info($"Offset {offset - 2:X2}: Option 138 (Length: 4): {BitConverter.ToString(option138IP)} (ASCII: {string.Join(".", option138IP)})");
                offset += option138IP.Length;

                offerPacket[offset++] = 0xFF;
                Logger.Info($"Offset {offset - 1:X2}: End Option (255)");

                int paddingLength = (offset % 2 == 0) ? 0 : 1;
                for (int i = 0; i < paddingLength; i++)
                {
                    offerPacket[offset++] = 0x00; // Add a padding byte if needed
                }
                StringBuilder txt = new StringBuilder("Padding Length: ").Append(paddingLength);
                txt.Append("\nDHCP Offer Packet Details:");
                txt.Append($"\nOffer Packet Length: ").Append(offset);
                txt.Append($"\nOffer IP Address: ").Append(string.Join(".", offerIP));
                txt.Append($"\nServer Identifier: ").Append(string.Join(".", serverIdentifier));
                Logger.Info(txt.ToString());
                try
                {
                    IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, 68);
                    serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);

                    serverSocket.SendTo(offerPacket, offset, SocketFlags.None, broadcastEndPoint);
                    Logger.Info("DHCP Offer sent.");
                    PrintPacketInfo(offerPacket, offset);
                }
                catch (SocketException ex)
                {
                    Logger.Error(ex);
                }
            }
            else
            {
                Logger.Info("Payload does not contain 'Omniswitch-', ignoring packet.");
            }
        }

        private void HandleDHCPRequest(Socket serverSocket, byte[] buffer, int length, EndPoint clientEndPoint)
        {
            Logger.Info("Handling DHCP Request");

            if (length < 240)
            {
                Logger.Error($"Packet length {length} is too short to contain a valid DHCP payload.");
                return;
            }

            byte[] sourceMac = new byte[6];
            Array.Copy(buffer, 28, sourceMac, 0, 6);
            string sourceMacString = BitConverter.ToString(sourceMac);
            Logger.Info($"DHCP Request from MAC: {sourceMacString}");

            byte[] offerIP = { 192, 168, 0, 2 };
            byte[] serverIdentifier = { 192, 168, 0, 1 };
            byte[] subnetMask = { 255, 255, 255, 0 };
            byte[] routerIP = { 192, 168, 0, 1 };
            byte[] tftpServerIP = Encoding.ASCII.GetBytes("192.168.0.1"); // Option 66 corrected to ASCII format
            byte[] bootfileName = Encoding.ASCII.GetBytes("ulc.alu");
            //int leaseTime = 3600;

            byte[] ackPacket = new byte[240 + 3 + 6 + 6 + 6 + 6 + 3 + bootfileName.Length + 6 + 1];

            ackPacket[0] = 0x02; // Message type: Boot Reply (2)
            ackPacket[1] = 0x01; // Hardware type: Ethernet (1)
            ackPacket[2] = 0x06; // Hardware address length: 6
            ackPacket[3] = 0x00; // Hops: 0

            Array.Copy(buffer, 4, ackPacket, 4, 4); // Transaction ID
            ackPacket[8] = 0x00; // Seconds elapsed
            ackPacket[9] = 0x00; // Seconds elapsed
            ackPacket[10] = 0x80; // Bootp flags: 0x8000, Broadcast flag
            ackPacket[11] = 0x00; // Bootp flags

            Array.Copy(offerIP, 0, ackPacket, 16, 4); // 'Your' IP address
            Array.Copy(serverIdentifier, 0, ackPacket, 20, 4); // Next server IP address (siaddr field)
            Array.Copy(sourceMac, 0, ackPacket, 28, 6); // Client MAC address

            ackPacket[236] = 0x63; // Magic cookie
            ackPacket[237] = 0x82; // Magic cookie
            ackPacket[238] = 0x53; // Magic cookie
            ackPacket[239] = 0x63; // Magic cookie

            int offset = 240;

            ackPacket[offset++] = 0x35;
            ackPacket[offset++] = 0x01;
            ackPacket[offset++] = 0x05;
            Logger.Info($"Offset {offset - 3:X2}: Option 53 (Length: 1): 05 (DHCP ACK)");

            ackPacket[offset++] = 0x01;
            ackPacket[offset++] = 0x04;
            Array.Copy(subnetMask, 0, ackPacket, offset, 4);
            Logger.Info($"Offset {offset - 2:X2}: Option 1 (Length: 4): {BitConverter.ToString(subnetMask)}");
            offset += 4;

            ackPacket[offset++] = 0x03;
            ackPacket[offset++] = 0x04;
            Array.Copy(routerIP, 0, ackPacket, offset, 4);
            Logger.Info($"Offset {offset - 2:X2}: Option 3 (Length: 4): {BitConverter.ToString(routerIP)}");
            offset += 4;

            ackPacket[offset++] = 0x42;
            ackPacket[offset++] = (byte)tftpServerIP.Length; // Length of the ASCII IP string
            Array.Copy(tftpServerIP, 0, ackPacket, offset, tftpServerIP.Length);
            Logger.Info($"Offset {offset - 2:X2}: Option 66 (Length: {tftpServerIP.Length}): {BitConverter.ToString(tftpServerIP)} (ASCII: {Encoding.ASCII.GetString(tftpServerIP)})");
            offset += tftpServerIP.Length;

            ackPacket[offset++] = 0x43;
            ackPacket[offset++] = (byte)bootfileName.Length;
            Array.Copy(bootfileName, 0, ackPacket, offset, bootfileName.Length);
            Logger.Info($"Offset {offset - 2:X2}: Option 67 (Length: {bootfileName.Length}): {BitConverter.ToString(bootfileName)} (ASCII: {Encoding.ASCII.GetString(bootfileName)})");
            offset += bootfileName.Length;

            ackPacket[offset++] = 0xFF;
            Logger.Info($"Offset {offset - 1:X2}: End Option (255)");

            int paddingLength = (offset % 2 == 0) ? 0 : 1;
            for (int i = 0; i < paddingLength; i++)
            {
                ackPacket[offset++] = 0x00; // Add a padding byte if needed
            }
            Logger.Info($"Padding Length: {paddingLength}");

            try
            {
                IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, 68);
                serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);

                serverSocket.SendTo(ackPacket, offset, SocketFlags.None, broadcastEndPoint);
                Logger.Info("DHCP ACK sent.");
                PrintPacketInfo(ackPacket, offset);

                if (PingAssignedIP("192.168.0.2"))
                {
                    // ExecuteSSHCommands("192.168.0.2", "admin", "switch");
                }
            }
            catch (SocketException ex)
            {
                Logger.Info($"SocketException occurred: {ex.Message}");
            }
        }

        private bool PingAssignedIP(string ipAddress)
        {
            using (Ping pingSender = new Ping())
            {
                try
                {
                    PingReply reply = pingSender.Send(ipAddress, 1000); // 1 second timeout
                    if (reply.Status == IPStatus.Success)
                    {
                        Logger.Info($"Ping successful. IP: {ipAddress}. Switch is ready.");
                        return true;
                    }
                    else
                    {
                        Logger.Info($"Ping failed. IP: {ipAddress}. Status: {reply.Status}");
                        return false;
                    }
                }
                catch (PingException ex)
                {
                    Logger.Error(ex);
                    return false;
                }
            }
        }

        private void ExecuteSSHCommands(string ipAddress, string username, string password)
        {
            using (var client = new SshClient(ipAddress, username, password))
            {
                try
                {
                    client.Connect();
                    Logger.Info("SSH connected.");

                    string[] commands = {
                    "auto-fabric admin-state disable remove-global-config",
                    "aaa authentication default local",
                    "aaa authentication http local",
                    "write memory"
                };

                    foreach (var command in commands)
                    {
                        Logger.Info($"Executing command: {command}");
                        var result = client.RunCommand(command);
                        Logger.Info($"Command result: {result.Result}");
                    }

                    client.Disconnect();
                    Logger.Info("SSH disconnected.");

                    StartHttpsSession(ipAddress);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }

        private void StartHttpsSession(string ipAddress)
        {
            try
            {
                string url = $"https://{ipAddress}";
                string chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe"; // Path to Chrome executable

                if (System.IO.File.Exists(chromePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = chromePath,
                        Arguments = url,
                        UseShellExecute = true
                    });
                    Logger.Info($"Opened {url} in Chrome.");
                }
                else
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                    Logger.Info($"Opened {url} in the default browser.");
                }

                Environment.Exit(0); // Exit the program
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

    }
}

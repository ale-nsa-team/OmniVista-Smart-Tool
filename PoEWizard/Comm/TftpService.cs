using PoEWizard.Data;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;

namespace PoEWizard.Comm
{
    public class TftpService
    {

        private const int TftpPort = 69;
        private const int BufferSize = 516;
        private const int DataPacketSize = 512;
        private string TftpDirectory = Directory.GetCurrentDirectory();

        public TftpService()
        {
            // Check if the program is running as administrator
            if (!IsRunningAsAdmin())
            {
                throw new Exception("TFTP Service must be run as an administrator.");
            }
            Logger.Info($"TFTP Server started.\nTFTP Directory: {TftpDirectory}\nListening on port 69 ...");
            try
            {
                using (UdpClient udpClient = new UdpClient(TftpPort))
                {
                    while (true)
                    {
                        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                        byte[] receivedBytes = udpClient.Receive(ref remoteEP);

                        if (receivedBytes != null && receivedBytes.Length > 0)
                        {
                            // Process the request
                            HandleRequest(receivedBytes, udpClient, remoteEP);
                        }
                        else
                        {
                            Logger.Info("Received an empty or null request.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void HandleRequest(byte[] request, UdpClient udpClient, IPEndPoint remoteEP)
        {
            try
            {
                // Check for RRQ (Read Request)
                if (request[0] == 0 && request[1] == 1)
                {
                    string filename = ExtractFileName(request);
                    Logger.Debug($"RRQ received for file: {filename}");

                    string filePath = Path.Combine(TftpDirectory, filename);

                    if (File.Exists(filePath))
                    {
                        Logger.Debug($"Local copy of the file found: {filePath}");
                        SendFile(filePath, udpClient, remoteEP);
                    }
                    else
                    {
                        Logger.Debug($"Local copy of the file not found: {filePath}");
                        // TFTP Error - File not found
                        SendError(udpClient, remoteEP, 1, "File not found.");
                    }
                }
                else
                {
                    Logger.Debug("Received a non-RRQ request.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SendError(udpClient, remoteEP, 0, "Unexpected server error.");
            }
        }

        private string ExtractFileName(byte[] request)
        {
            try
            {
                // Extract filename from the request
                int endIndex = Array.IndexOf(request, (byte)0, 2);
                if (endIndex == -1)
                {
                    throw new Exception("Invalid request format. Filename not terminated with null.");
                }

                return Encoding.ASCII.GetString(request, 2, endIndex - 2);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        private void SendFile(string filePath, UdpClient udpClient, IPEndPoint remoteEP)
        {
            try
            {
                Logger.Debug($"Starting file transfer for: {filePath}");
                byte[] fileBytes = File.ReadAllBytes(filePath);
                int blockNumber = 1;
                int offset = 0;

                while (offset < fileBytes.Length)
                {
                    int dataSize = Math.Min(DataPacketSize, fileBytes.Length - offset);
                    byte[] dataPacket = new byte[4 + dataSize];

                    // Opcode (Data) - 2 bytes
                    dataPacket[0] = 0;
                    dataPacket[1] = 3;

                    // Block number - 2 bytes
                    dataPacket[2] = (byte)(blockNumber >> 8);
                    dataPacket[3] = (byte)(blockNumber & 0xFF);

                    // Data
                    Array.Copy(fileBytes, offset, dataPacket, 4, dataSize);

                    udpClient.Send(dataPacket, dataPacket.Length, remoteEP);
                    Logger.Debug($"Sent block {blockNumber}, size {dataSize} bytes.");

                    // Wait for ACK
                    IPEndPoint ackEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] ack = udpClient.Receive(ref ackEP);

                    if (ack != null && ack.Length >= 4 && ack[0] == 0 && ack[1] == 4 &&
                        ack[2] == (byte)(blockNumber >> 8) && ack[3] == (byte)(blockNumber & 0xFF))
                    {
                        // Move to the next block
                        offset += dataSize;
                        blockNumber++;
                    }
                    else
                    {
                        Logger.Debug("ACK not received correctly. Aborting transfer.");
                        break;
                    }
                }

                Logger.Debug($"File transfer completed for: {filePath}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void SendError(UdpClient udpClient, IPEndPoint remoteEP, ushort errorCode, string errorMessage)
        {
            try
            {
                byte[] errorBytes = Encoding.ASCII.GetBytes(errorMessage);
                byte[] errorPacket = new byte[4 + errorBytes.Length + 1];

                // Opcode (Error) - 2 bytes
                errorPacket[0] = 0;
                errorPacket[1] = 5;

                // Error code - 2 bytes
                errorPacket[2] = (byte)(errorCode >> 8);
                errorPacket[3] = (byte)(errorCode & 0xFF);

                // Error message
                Array.Copy(errorBytes, 0, errorPacket, 4, errorBytes.Length);

                // Null-terminator
                errorPacket[errorPacket.Length - 1] = 0;

                udpClient.Send(errorPacket, errorPacket.Length, remoteEP);
                Logger.Debug($"Sent error packet: {errorMessage}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private bool IsRunningAsAdmin()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }


    }
}

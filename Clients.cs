using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices; // Get OS that the code is being run on

namespace GameServer2
{
    class Clients
    {
        static CustomTcpListener tcpListener = null;
        static readonly Int32 port = 26950;
        static Dictionary<int, CustomTcpClient> clientsDict;
        private static int threadCount = 0;


        private static string CustomNewLine()
        {
            int os = GetOperatingSystem();
            if (os == 0)
            {
                return "\n";
            } else
            {
                return "\r\n";
            }
        }


        /*
         * 
         * Convert system's OS to int
         * 0 = Linux / MacOS
         * 1 = Windows
         * 
         */

        private static int GetOperatingSystem()
        {
            int os = 0;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                os = 1;
            }

            return os;
        }

        public static void AcceptNewClientsInAsync()
        {
            // Create the dictionaries for clients and threads handling the clients
            clientsDict = new Dictionary<int, CustomTcpClient>();

            if (tcpListener == null || !tcpListener.Active) {
                Console.WriteLine("Accepting incoming connections");
                tcpListener = new CustomTcpListener(IPAddress.Any, port);
                tcpListener.Start();
                Console.WriteLine("Listening");

                while (true)
                {
                    if (threadCount < Constants.MaxPlayers)
                    {
                        for (int i = threadCount; i < Constants.MaxPlayers; i++)
                        {
                            Console.WriteLine($"Created thread #{threadCount + 1} for accepting new client connection");

                            Thread t = new Thread(() => AcceptClient(tcpListener));
                            t.Start();

                            threadCount++;
                        }
                    }
                }
            }
        }


        private static void AcceptClient(TcpListener listener)
        {
            // Accept the incoming connection from current client
            CustomTcpClient client = new CustomTcpClient(
                listener.AcceptSocket());

            // Print the connected client's IP address and port number
            string clientIPAddress =
                ((IPEndPoint)client.Client.RemoteEndPoint).ToString();
            Console.WriteLine($"Client connected from IP: {clientIPAddress}");

            StoreClientInfo(client);
            WelcomeClient(client);
        }


        private static void StoreClientInfo(CustomTcpClient client)
        {
            //client.Id = clientsDict.Count + 1;
            client.Id = GetIndexForNextSlot();
            Console.WriteLine($"Assigned ID of {client.Id} to the client");
            clientsDict[client.Id - 1] = client;
            clientsDict.Add(client.Id, client);
        }


        private static int GetIndexForNextSlot()
        {
            int index = 0;
            for (int i = 0; i < clientsDict.Count; i++)
            {
                if (!clientsDict.ContainsKey(i))
                {
                    index = i;
                }
            }

            return index + 1;
        }


        private static void WelcomeClient(CustomTcpClient client)
        {
            // Get the incoming data through a network stream
            NetworkStream ns = client.GetStream();
            
            Byte[] sendBytes = Encoding.UTF8.GetBytes($"{client.Id}");
            ns.Write(sendBytes, 0, sendBytes.Length);
            Console.WriteLine($"Sent client's ID to client #{client.Id}");

            ReceiveMessageFromClient(client, ns);
        }


        private static void ReceiveMessageFromClient(CustomTcpClient client, NetworkStream ns)
        {
            // For client timeouts
            DateTime targetDate = DateTime.Now.AddMilliseconds(Constants.MS_TIMEOUT);

            while (targetDate >= DateTime.Now && CheckIfClientExists(client))
            {
                try
                {
                    // Reads NetworkStream into a byte buffer.
                    byte[] bytes = new byte[client.ReceiveBufferSize];

                    // This method blocks until at least one byte is read.
                    ns.Read(bytes, 0, (int)client.ReceiveBufferSize);
                    string returndata = Encoding.UTF8.GetString(bytes);

                    switch (GetMessageType(returndata))
                    {
                        case 1:
                            // Set received username as client's username
                            client.Username = GetUsernameFromMessage(returndata);
                            Console.WriteLine($"Client #{client.Id}'s username is {client.Username}");
                            break;
                        case 2:
                            Console.WriteLine($"Received ping from client #{client.Id}");
                            break;
                    }

                    // Reset timeout after every message from the client
                    targetDate = DateTime.Now.AddMilliseconds(Constants.MS_TIMEOUT);
                }
                // Client 'forcibly' disconnected (due to system/application crash or network/internet cut off)
                catch (IOException e)
                {
                    // Print error message to console
                    //Console.WriteLine(e);
                    Console.WriteLine(
                        $"Client #{client.Id} disconnected:{CustomNewLine()}{e}");
                    RemoveClient(client);
                }
            }

            if(CheckIfClientExists(client))
            Console.WriteLine(
                    $"Client #{client.Id} timed out");
            RemoveClient(client);
        }


        private static bool CheckIfClientExists(CustomTcpClient client)
        {
            if (clientsDict.ContainsKey(client.Id))
            {
                return true;
            } else
            {
                return false;
            }
        }


        /*
         * 
         * DataTypes:
         * 0 = Error when parsing, missing datatype
         * 1 = Client's username
         * 2 = Client's ping
         * 3 = Custom message
         * 
         */

        private static int GetMessageType(string message)
        {
            if (Int32.TryParse(message.Substring(0, 1), out int messageType))
            {
                return messageType;
            }
            else
            {
                Console.WriteLine("Message's type could not be parsed.");
                return 0;
            }
        }


        private static String GetUsernameFromMessage(string message)
        {
            return message.Substring(1, message.Length - 1);
            // This shortening doesn't seem to work on Linux
            //return message[1..];
        }


        private static void RemoveClient(CustomTcpClient client)
        {
            // Remove client connection
            if (clientsDict.ContainsKey(client.Id))
            {
                clientsDict.Remove(client.Id);

                // Check that the key was properly deleted
                if (clientsDict.ContainsKey(client.Id))
                {
                    Console.WriteLine(
                        $"Failed to delete the client #{client.Id}'s session");
                }

                // Client deleted successfully
                threadCount--;
            }
        }
    }
}

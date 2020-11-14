using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Threading;

namespace GameServer2
{
    class Clients
    {
        static CustomTcpListener tcpListener = null;
        static readonly Int32 port = 26950;
        static Dictionary<int, CustomTcpClient> clientsList;

        public static void AcceptClientsOnAnotherThread()
        {
            // Create the dictionary for clients
            clientsList = new Dictionary<int, CustomTcpClient>();

            Thread acceptClientsThread = new Thread(
                new ThreadStart(
                    LoopIncomingConnections));

            acceptClientsThread.Start();
        }

        public static void LoopIncomingConnections()
        {
            while (true)
            {
                while (clientsList.Count < Constants.MaxPlayers)
                {
                    // Check that TcpListener is accepting new clients
                    if (tcpListener == null || !tcpListener.Active) {
                        Console.WriteLine("Accepting incoming connections");
                        tcpListener = new CustomTcpListener(IPAddress.Any, port);
                        tcpListener.Start();
                        tcpListener.BeginAcceptTcpClient(
                            AcceptClient,
                            null);
                    }
                }

                while (clientsList.Count == Constants.MaxPlayers)
                {
                    // Do not accept new clients
                    if (tcpListener.Active)
                    {
                        Console.WriteLine("Stopped accepting incoming connections");
                        tcpListener.Stop();
                    }
                }
            }
        }

        private static void AcceptClient(IAsyncResult result)
        {
            // Accept the incoming connection from current client
            CustomTcpClient client = new CustomTcpClient(
                tcpListener.AcceptSocket());

            // Print the connected client's IP address and port number
            string clientIPAddress =
                ((IPEndPoint)client.Client.RemoteEndPoint).ToString();
            Console.WriteLine($"Client connected from IP: {clientIPAddress}");

            StoreClientInfo(client);
            WelcomeClient(client);
        }


        private static void StoreClientInfo(CustomTcpClient client)
        {
            client.Id = clientsList.Count + 1;
            clientsList.Add(client.Id, client);
        }

        private static void WelcomeClient(CustomTcpClient client)
        {
            Console.WriteLine($"Sending welcome message to client " +
                $"#{clientsList.Count + 1}");
            // Get the incoming data through a network stream
            NetworkStream ns = client.GetStream();

            if (ns.CanWrite)
            {
                Byte[] sendBytes = Encoding.UTF8.GetBytes($"{clientsList.Count + 1}");
                ns.Write(sendBytes, 0, sendBytes.Length);
                Console.WriteLine($"Client #{client.Id} " +
                    $"welcomed to the server");
            }

            Thread t = new Thread(() => ReceiveMessageFromClient(client));
            t.Start();
        }

        // Runs on another thread like calling function
        private static void ReceiveMessageFromClient(CustomTcpClient client)
        {
            Console.WriteLine($"Accepting messages from client");
            // Get the incoming data through a network stream
            NetworkStream ns = client.GetStream();

            if (ns.CanRead)
            {
                DateTime targetDate = DateTime.Now.AddSeconds(Constants.SEC_TIMEOUT);
                while (targetDate > DateTime.Now)
                {
                    if (ns.DataAvailable)
                    {
                        // Reads NetworkStream into a byte buffer.
                        byte[] bytes = new byte[client.ReceiveBufferSize];

                        // Read can return anything from 0 to numBytesToRead.
                        // This method blocks until at least one byte is read.
                        ns.Read(bytes, 0, (int)client.ReceiveBufferSize);

                        // Returns the data received from the host to the console.
                        string returndata = Encoding.UTF8.GetString(bytes);

                        Console.WriteLine(
                            $"Message from client #{client.Id}: "
                            + returndata);

                        targetDate = DateTime.Now.AddSeconds(Constants.SEC_TIMEOUT);
                    }
                }

                Console.WriteLine("Client timed out");
            }
        }
    }
}

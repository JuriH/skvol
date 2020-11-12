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
        static CustomTcpListener TCPListener = null;
        static readonly Int32 port = 26950;
        static Dictionary<int, TcpClient> clientsList;

        static Thread acceptClientsThread;


        public static void AcceptClientsOnAnotherThread()
        {
            clientsList = new Dictionary<int, TcpClient>();
            acceptClientsThread = new Thread(
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
                    if (TCPListener == null || !TCPListener.Active) {
                        Console.WriteLine("Accepting incoming connections");
                        TCPListener = new CustomTcpListener(IPAddress.Any, port);
                        TCPListener.Start();
                        TCPListener.BeginAcceptTcpClient(
                            AcceptClient,
                            null);
                    }
                }

                while (clientsList.Count == Constants.MaxPlayers)
                {
                    // Do not accept new clients
                    if (TCPListener.Active)
                    {
                        Console.WriteLine("Stopped accepting incoming connections");
                        TCPListener.Stop();
                    }
                }
            }
        }

        private static void AcceptClient(IAsyncResult result)
        {
            // Accept the incoming connection from current client
            TcpClient client =
                TCPListener.EndAcceptTcpClient(result);

            // Print the connected client's IP address and port number
            string clientIPAddress =
                ((IPEndPoint)client.Client.RemoteEndPoint).ToString();
            Console.WriteLine($"Client connected from IP: {clientIPAddress}");
        }
    }
}

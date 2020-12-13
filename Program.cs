using System;
using System.Threading;

namespace GameServer
{
    internal static class Program
    {
        private static void Main()
        {
            Console.Title = "Game Server";
            Console.WriteLine("Server started");
            StartThreads();
            //Clients.AcceptNewClientsInAsync();
        }
        
        private static void StartThreads()
        {
            var threadTcp = new Thread(TcpServer.SetupTcp);
            var threadWebSocket = new Thread(Websocket.SetupWebSocket);
            threadTcp.Start();
            threadWebSocket.Start();
        }
    }
}

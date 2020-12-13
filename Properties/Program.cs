﻿using System;
using System.Threading;

namespace GameServer
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.Title = "Game Server";
            Console.WriteLine("Server started");
            StartThreads();
            //Clients.AcceptNewClientsInAsync();
        }
        
        static void StartThreads()
        {
            Thread threadClients = new Thread(Clients.AcceptNewClientsInAsync);
            Thread threadBrowsers = new Thread(Browser.SetupWebSocket);
            threadClients.Start();
            threadBrowsers.Start();
        }
    }
}

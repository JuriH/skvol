using System;

namespace GameServer2
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Game Server";
            Console.WriteLine("Server started");
            Clients.AcceptClientsOnAnotherThread();
        }
    }
}

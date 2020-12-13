using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace GameServer
{
    internal static class Constants
    {
        // TCP connections
        public const int MaxPlayers = 2;
        
        // WebSocket connections
        public const int MaxWebSocketClients = 2;
        
        // Database
        public const string PhpInsertUsername =
            "http://localhost:80/php/insertUsername.php";
        public const string PhpDeleteUsername =
            "http://localhost:80/php/deleteUsername.php";

        // WebSocket
        public const string WebSocketIp = "127.0.0.1";
        public const int WebSocketPort = 8080;

        public static string CustomNewLine()
        {
            var os = GetOperatingSystem();
            return os == 0 ? "\n" : "\r\n";
        }


        /*
         * Convert system's OS to int
         * 0 = Linux / MacOS
         * 1 = Windows
         */
        private static int GetOperatingSystem()
        {
            var os = 0;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                os = 1;
            }
            return os;
        }
    }
}

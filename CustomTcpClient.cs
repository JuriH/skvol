using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GameServer
{
    internal class CustomTcpClient : TcpClient
    {
        //public int Id { get; set; }
        public string Username { get; set; }

        public CustomTcpClient(Socket acceptedSocket)
        {
            // https://stackoverflow.com/a/49363782
            // And then pass socket from AcceptSocket to constructor.
            // But I don't like it, because default constructor of
            // TcpClient will be called, which creates new socket
            // (which is disposed at the first line).
            Client.Dispose();
            Client = acceptedSocket;
            Active = true;
        }
        
        
        // https://stackoverflow.com/a/19706302
        public static TcpState GetState(TcpClient tcpClient)
        {
            var foo = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpConnections()
                .SingleOrDefault(x =>
                    x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint));
            return foo?.State ?? TcpState.Unknown;
        }
    }
}

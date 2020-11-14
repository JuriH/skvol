using System.Net.Sockets;

namespace GameServer2
{
    class CustomTcpClient : TcpClient
    {
        public int Id { get; set; }

        public CustomTcpClient(Socket acceptedSocket)
        {
            // https://stackoverflow.com/a/49363782
            // And then pass socket from AcceptSocket to constructor.
            // But I don't like it, because default constructor of
            // TcpClient will be called, which creates new socket
            // (which is disposed at the first line).
            this.Client.Dispose();
            this.Client = acceptedSocket;
            this.Active = true;
        }
    }
}

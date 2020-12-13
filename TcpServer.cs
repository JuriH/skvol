using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Threading;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;

namespace GameServer
{
    internal static class TcpServer
    {
        private static CustomTcpListener _tcpListener;
        private const int Port = 26950;
        // If we want to have a way to identify and reference to a specific client
        private static List<CustomTcpClient> _clientsList;
        private static bool _createNewThread = true;
        private static NetworkStream _ns;


        public static void SetupTcp()
        {
            Console.WriteLine("Starting TCP server");
            // Create the dictionaries for clients and threads handling the clients
            _clientsList = new List<CustomTcpClient>();

            if (_tcpListener != null && _tcpListener.Active) return;

            try
            {
                _tcpListener = new CustomTcpListener(IPAddress.Any, Port);
                _tcpListener.Start();
                Console.WriteLine($"TCP server has started on port {Port}");
            }
            catch (SocketException ex)
            {
                Console.WriteLine(
                    "Error creating TCP listener:" +
                    $"{Constants.CustomNewLine()}{ex}");
            }
            
            HandleTcpClientConnections();
        }

        private static void HandleTcpClientConnections()
        {
            while (true)
            {
                if (!_createNewThread) continue;
                var t = new Thread(() => AcceptClient(_tcpListener));
                t.Start();

                _createNewThread = false;
            }
            // ReSharper disable once FunctionNeverReturns
        }


        private static void AcceptClient(TcpListener listener)
        {
            Console.WriteLine("Created a thread to accept a TCP connection");
            
            if (_clientsList.Count == Constants.MaxWebSocketClients)
                Console.WriteLine("Maximum amount of TcpClients clients reached");

            // Accept the incoming connection from current client
            var client = new CustomTcpClient(
                listener.AcceptSocket());

            if (_clientsList.Count != Constants.MaxPlayers)
                _createNewThread = true;

            // Print the connected client's IP address and port number
            var clientIpAddress =
                ((IPEndPoint)client.Client.RemoteEndPoint).ToString();
            Console.WriteLine($"TcpClient connected from IP: {clientIpAddress}");

            // Store client to List
            _clientsList.Add(client);
            
            WelcomeClient(client);
        }


        private static void WelcomeClient(CustomTcpClient client)
        {
            // Get the incoming data through a network stream
            _ns = client.GetStream();
            
            var sendBytes = Encoding.UTF8.GetBytes("OK");
            _ns.Write(sendBytes, 0, sendBytes.Length);
            //Console.WriteLine($"Sent TcpClient's ID to TcpClient #{client.Id}");

            ReceiveMessageFromClient(client, _ns);
        }


        private static void ReceiveMessageFromClient(CustomTcpClient client, NetworkStream ns)
        {
            while (CheckIfConnected(client))
            {
                try
                {
                    // Reads NetworkStream into a byte buffer.
                    var bytes = new byte[client.ReceiveBufferSize];

                    // This method blocks until at least one byte is read.
                    ns.Read(bytes, 0, client.ReceiveBufferSize);
                    var returnData = Encoding.UTF8.GetString(bytes);

                    var message = GetActualMessage(returnData);

                    if (!message.StartsWith("username")) continue;
                    const string removeString = "username";
                    
                    // https://stackoverflow.com/questions/2201595/c-sharp-simplest-way-to-remove-first-occurrence-of-a-substring-from-another-st
                    // https://docs.microsoft.com/en-us/dotnet/api/system.stringcomparison?view=net-5.0
                    var index = message.IndexOf("username", StringComparison.Ordinal);
                    client.Username = message.Remove(index, removeString.Length);
                    
                    InsertIntoDatabase(client);
                }
                // Client 'forcibly' disconnected (due to system/application crash or network/internet cut off)
                catch (IOException e)
                {
                    // Print error message to console
                    //Console.WriteLine(e);
                    Console.WriteLine(
                        "TcpClient with username " +
                        $"{client.Username} disconnected:" +
                        $"{Constants.CustomNewLine()}{e}");
                    
                    DeleteClient(client);
                }
            }

            if(CheckIfClientExists(client))
                Console.WriteLine(
                    $"TcpClient with username {client.Username} timed out");
            
            DeleteFromDatabase(client);
            DeleteClient(client);
        }
        
        
        private static void DeleteClient(CustomTcpClient client)
        {
            for (var i = 0; i < _clientsList.Count; i++)
            {
                var clientObj = _clientsList[i];
                if (clientObj.Username != client.Username) continue;
                clientObj.Close();
                _clientsList.RemoveAt(i);
            }
            
            client.Close();
            _createNewThread = true;
        }
        
        
        private static bool CheckIfClientExists(CustomTcpClient client)
        {
            // Return true if a match is found -> client exists
            return _clientsList.Any(
                clientObj => clientObj.Username == client.Username);
        }
        
        
        private static int GetMessageLength(string message)
        {
            try
            {
                int.TryParse(
                    message.Substring(
                        0,
                        message.IndexOf(' ')
                    ), out var messageLength);

                //Console.WriteLine($"Message's length is {messageLength}");
                return messageLength;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine(
                    $"Error in TCP:{Constants.CustomNewLine()}{ex}");
            }
            
            return 0;
        }
        
        
        private static string GetActualMessage(string message)
        {
            var msgLength = GetMessageLength(message);
            /*var removeLength = msgLength.ToString().Length;
            var msg = message.Substring(
                removeLength,
                msgLength);*/
            
            return message.Substring(
                message.IndexOf(' ') + 1, 
                msgLength);
        }


        private static void InsertIntoDatabase(CustomTcpClient client)
        {
            Console.WriteLine(
                $"Storing TcpClient's username '{client.Username}' to the database");

            // https://stackoverflow.com/a/32677290

            var byteArray = Encoding.UTF8.GetBytes(
                "username=" + client.Username);

            try
            {
                var webRequest =
                    (HttpWebRequest) WebRequest.Create(Constants.PhpInsertUsername);
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.ContentLength = byteArray.Length;

                using (var webpageStream = webRequest.GetRequestStream())
                {
                    webpageStream.Write(byteArray, 0, byteArray.Length);
                }

                Console.WriteLine("Sent TcpClient's username to the PHP-script");

                var webResponse = webRequest.GetResponse();
                Console.WriteLine("webResponse");
                var receiveStream = webResponse.GetResponseStream();
                Console.WriteLine("receiveStream");
                var readStream = new StreamReader(receiveStream);
                Console.WriteLine("readStream");
                // var readStream =
                //     new StreamReader(receiveStream ?? throw new Exception(), Encoding.UTF8);
                var response = readStream.ReadToEnd();
                Console.WriteLine("Read the stream");

                // Condition ? TRUE : FALSE
                Console.WriteLine(
                    response.Equals("OK")
                        ? "Username stored successfully"
                        : response);

                webResponse.Close();
                readStream.Close();
            }
            catch (WebException we)
            {
                Console.WriteLine($"{we}");
            }
        }


        private static void DeleteFromDatabase(CustomTcpClient client)
        {
            Console.WriteLine(
                $"Deleting TcpClient's username '{client.Username}' from the database");
            
            var byteArray = Encoding.UTF8.GetBytes(
                "username=" + client.Username);
            var webRequest =
                (HttpWebRequest)WebRequest.Create(Constants.PhpDeleteUsername);
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = byteArray.Length;

            using (var webpageStream = webRequest.GetRequestStream())
            {
                webpageStream.Write(byteArray, 0, byteArray.Length);
            }

            var webResponse = webRequest.GetResponse();
            var receiveStream = webResponse.GetResponseStream();
            var readStream = new StreamReader(receiveStream ?? throw new Exception(), Encoding.UTF8);
            var response = readStream.ReadToEnd();
            
            // Condition ? TRUE : FALSE
            Console.WriteLine(
                response.Equals("OK") ? "Username deleted successfully"
                    : response);
            
            webResponse.Close();
            readStream.Close();
        }


        private static bool CheckIfConnected(CustomTcpClient client)
        {
            // Get an object that provides information about the local
            // computer's network connectivity and traffic statistics.
            var ipGlobalProperties
                = IPGlobalProperties.GetIPGlobalProperties();
            
            // Check if the remote endpoint exists in the active connections
            var tcpConnections =
                ipGlobalProperties.GetActiveTcpConnections().Where(x =>
                    x.LocalEndPoint.Equals(client.Client.LocalEndPoint) &&
                    x.RemoteEndPoint.Equals(client.Client.RemoteEndPoint)).ToArray();
            
            // If results were found for the given endpoints
            if (tcpConnections.Length <= 0) return true;
            
            // Get the state of the first result
            var stateOfConnection = tcpConnections.First().State;
            return stateOfConnection == TcpState.Established;
        }
        
        
        public static void ForwardToUnityClient(string username, string message)
        {
            foreach (var clientObj in _clientsList.Where(clientObj => clientObj.Username.Equals(username)))
            {
                if (_ns == null)
                    _ns = clientObj.GetStream();
                
                var sendBytes = Encoding.UTF8.GetBytes(message);
                _ns.Write(sendBytes, 0, sendBytes.Length);
                Console.WriteLine("Action sent to TcpClient");
            }
        }
    }
}

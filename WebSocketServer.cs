using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace GameServer
{
    internal static class Websocket
    {
        private static TcpListener _listener;
        private static List<TcpClient> _webSocketClients;
        private static bool _createNewThread = true;

        public static void SetupWebSocket()
        {
            _webSocketClients = new List<TcpClient>();

            Console.WriteLine("Starting WebSocket server");

            try
            {
                // If _listener is null, create a new one
                if (_listener == null)
                    _listener = new TcpListener(
                    IPAddress.Parse(Constants.WebSocketIp), 
                    Constants.WebSocketPort);
                _listener.Start();

                Console.WriteLine(
                    "WebSocket server has started on " +
                    $"{_listener.LocalEndpoint}");
            }
            catch (SocketException ex)
            {
                Console.WriteLine(
                    "Error creating WebSocket listener:" +
                    $"{Constants.CustomNewLine()}{ex}");
                return;
            }

            HandleWebSocketConnections();
        }


        private static void HandleWebSocketConnections()
        {
            while (true)
            {
                if (!_createNewThread) continue;
                var t = new Thread(() => AcceptClient(_listener));
                t.Start();

                _createNewThread = false;
            }
            // ReSharper disable once FunctionNeverReturns
        }


        private static void AcceptClient(TcpListener listener)
        {
            Console.WriteLine(
                "Created a thread to accept a WebSocket connection");

            // Accept a client connection
            var client = new CustomTcpClient(
                listener.AcceptSocket());
            
            _webSocketClients.Add(client);
            if (_webSocketClients.Count < Constants.MaxWebSocketClients) 
                _createNewThread = true;

            // Print the connected client's IP address and port number
            var clientIpAddress =
                ((IPEndPoint)client.Client.RemoteEndPoint).ToString();
            Console.WriteLine($"WebSocket client connected from IP: {clientIpAddress}");
            
            ReceiveMessagesFromClient(client);
        }


        private static void ReceiveMessagesFromClient(TcpClient client)
        {
            var stream = client.GetStream();
            
            // Infinite cycle to handle changes in stream
            while (!ClientDisconnected(client))
            {
                // Wait for readable data
                while (!stream.DataAvailable) { }

                // Match against "get"
                while (client.Available < 3) { }

                var bytes = new byte[client.Available];
                stream.Read(bytes, 0, bytes.Length);

                //translate bytes of request to string
                var data = Encoding.UTF8.GetString(bytes);

                // https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_servers
                // Example of handshake sent from the client
                // to the server:
                //
                // GET / chat HTTP / 1.1
                // Host: example.com:8000
                // Upgrade: websocket
                // Connection: Upgrade
                // Sec - WebSocket - Key: dGhlIHNhbXBsZSBub25jZQ
                // Sec - WebSocket - Version: 13

                if (Regex.IsMatch(
                    data, 
                    "^GET", 
                    RegexOptions.IgnoreCase))
                {
                    Console.WriteLine("Handshake from WebSocket client");
                    var secWebSocketKey = Regex.Match(
                        data, 
                        "Sec-WebSocket-Key: (.*)")
                        .Groups[1].Value.Trim();

                    // Add a special GUID specified by RFC 6455 to the key
                    // received from the client
                    var secWebSocketKeyAccept = secWebSocketKey +
                        "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

                    var secWebSocketKeyAcceptSha1 =
                        System.Security.Cryptography.SHA1.Create()
                        .ComputeHash(Encoding.UTF8.GetBytes(
                            secWebSocketKeyAccept));

                    var secWebSocketKeyAcceptSha1Base64 =
                        Convert.ToBase64String(secWebSocketKeyAcceptSha1);

                    // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                    var response = Encoding.UTF8.GetBytes(
                        "HTTP/1.1 101 Switching Protocols\r\n" +
                        "Connection: Upgrade\r\n" +
                        "Upgrade: websocket\r\n" +
                        "Sec-WebSocket-Accept: " +
                        secWebSocketKeyAcceptSha1Base64 +
                        "\r\n\r\n");

                    stream.Write(
                        response,
                        0,
                        response.Length);
                    Console.WriteLine("Response sent to client");
                }
                else
                {
                    bool fin = (bytes[0] & 0b10000000) != 0,
                        // All messages from the client to the server
                        // have this this bit set
                        mask = (bytes[1] & 0b10000000) != 0;

                    // Expecting 1 - text message
                    int opCode = bytes[0] & 0b00001111,
                        // & 0111 1111
                        msgLen = bytes[1] - 128,
                        offset = 2;

                    // ReSharper disable once ConvertIfStatementToSwitchStatement
                    if (msgLen == 126)
                    {
                        // Was ToUInt16(bytes, offset) but the
                        // result was incorrect
                        msgLen = BitConverter.ToUInt16(
                            new[] {
                                bytes[3],
                                bytes[2]
                            }, 0);
                        offset = 4;
                    }
                    else if (msgLen == 127)
                    {
                        Console.WriteLine(
                            "TODO: msglen == 127, needs qword to store msglen");
                        // I don't really know the byte order, please edit this
                        // MsgLen = BitConverter.ToUInt64(new byte[] { bytes[5], bytes[4], bytes[3], bytes[2], bytes[9], bytes[8], bytes[7], bytes[6] }, 0);
                        // offset = 10;
                    }

                    
                    // To my observation this seems to mean that
                    // the client has disconnected
                    if (msgLen == 0)
                    {
                        Console.WriteLine("Message's length == 0");
                        RemoveClient(client);
                    }
                    else if (mask)
                    {
                        var decoded = new byte[msgLen];
                        var masks =
                            new[] {
                                bytes[offset],
                                bytes[offset + 1],
                                bytes[offset + 2],
                                bytes[offset + 3]
                            };
                        offset += 4;

                        // ++i increments the variable but uses its
                        // old/previous value
                        for (var i = 0; i < msgLen; ++i)
                        {
                            decoded[i] =
                                (byte)(bytes[offset + i] ^ masks[i % 4]);
                        }

                        var text = Encoding.UTF8.GetString(decoded);
                        Console.WriteLine($"Message from WebSocket client: {text}");
                        
                        if (!text.Equals("Connected"))
                        {
                            // TODO: Forward to Unity-client based on the client name in the message
                            ForwardToTcpServer(text);
                        }
                    }
                    else
                        Console.WriteLine("Mask-bit not set");
                }
            }
            Console.WriteLine("WebSocket client disconnected");
            RemoveClient(client);
            
            // ReSharper disable once FunctionNeverReturns
        }

        
        private static bool ClientDisconnected(TcpClient client)
        {

            // https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.poll?view=net-5.0
            // Returns true if Listen has been called and connection is pending,
            // data is available for reading or
            // if the connection has been closed, reset or terminated
            if (!client.Client.Poll(
                0,
                SelectMode.SelectRead))
                return false;
            
            var buff = new byte[1];
            // Peek at the incoming message, return true if there is nothing
            return client.Client.Receive(buff, SocketFlags.Peek) == 0;
        }
        
        
        private static void ForwardToTcpServer(string text)
        {
            var username = text.Substring(0, text.IndexOf(" ", StringComparison.Ordinal));
            var action = text.Substring(text.IndexOf(" ", StringComparison.Ordinal));
            
            TcpServer.ForwardToUnityClient(username, action);
        }


        private static void RemoveClient(TcpClient client)
        {
            for (var i = 0; i < _webSocketClients.Count; i++)
            {
                if (!_webSocketClients[i].Client.RemoteEndPoint
                    .Equals(client.Client.RemoteEndPoint)) continue;
                
                _webSocketClients.RemoveAt(i);
                Console.WriteLine("WebSocket client removed");
            }

            _createNewThread = true;
        }
    }
}

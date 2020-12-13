using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

public class Client : MonoBehaviour
{
    // While const is initialized at compile time,
    // readonly keyword allow the variable to be
    // initialized either at compile time or runtime.
    public static Client Instance;
    private const string Host = "18.192.144.101";
    private const int Port = 26950;

    private readonly IPEndPoint _ipEndPoint = new IPEndPoint(
        IPAddress.Parse(Host), Port);
    
    private TcpClient _client;
    private static NetworkStream _ns;
    private static byte[] _receiveBuffer;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }


    private void OnApplicationQuit()
    {
        try
        {
            _ns.Close();
            _ns = null;
            _client.Close();
            _client = null;
        }
        catch (NullReferenceException ex)
        {
            Debug.Log($"{ex}");
        }
    }


    public void Connect()
    {
        var thread = new Thread(
                ConnectToServer);
        thread.Start();
    }


    private void ConnectToServer()
    {
        Debug.Log("Connecting to the server");
        try
        {
            _client = new TcpClient();
            _client.Connect(_ipEndPoint);
            var receiveThread = new Thread(ReceiveFromServer);
            receiveThread.Start();
        }
        catch (Exception ex)
        {
            Debug.Log($"Failed to connect to the server:\r\n{ex}");

            try
            {
                if (_client == null) Task.Delay(1000).ContinueWith(t => ConnectToServer());
            }
            catch (Exception ex2)
            {
                // Stop looping, running Unity has been stopped
                Debug.Log($"{ex2}");
            }
        }
    }


    private static void SendUsername()
    {
        var username = UIManager.Instance.usernameField.text.Trim();
        var message = $"username{username}";

        // First character is the length of the message
        var sendBytes = Encoding.UTF8.GetBytes($"{message.Length} {message}");
        
        _ns.Write(sendBytes, 0, sendBytes.Length);
    }


    private void ReceiveFromServer()
    {
        Debug.Log("Connected to the server");
        try
        {
            _ns = _client.GetStream();
            SendUsername();

            while (CheckIfConnected())
            // while (true)
            {
                // Reads NetworkStream into a byte buffer.
                var bytes = new byte[_client.ReceiveBufferSize];

                // Read can return anything from 0 to numBytesToRead.
                // This method blocks until at least one byte is read.
                _ns.Read(bytes, 0, _client.ReceiveBufferSize);

                // Returns the data received from the host to the console.
                var returned = Encoding.UTF8.GetString(bytes);

                if (returned.Length > 0)
                {
                    Debug.Log($"Message from server: {returned}");
                }
            }

            Debug.Log("Disconnected");

            ConnectToServer();
        }
        catch (SocketException ex)
        {
            Debug.Log($"Unity-client already disconnected: {ex}");
        }
    }


    private bool CheckIfConnected()
    {

        // Get an object that provides information about the local
    // computer's network connectivity and traffic statistics.
    var ipGlobalProperties
        = IPGlobalProperties.GetIPGlobalProperties();
            
    // Check if the remote endpoint exists in the active connections
    var tcpConnections =
        ipGlobalProperties.GetActiveTcpConnections().Where(x =>
            x.LocalEndPoint.Equals(_client.Client.LocalEndPoint) &&
            x.RemoteEndPoint.Equals(_client.Client.RemoteEndPoint)).ToArray();
            
    // If results were found for the given endpoints
    if (tcpConnections.Length > 0)
    {
        // Get the state of the first result
        var stateOfConnection = tcpConnections.First().State;
        if (stateOfConnection != TcpState.Established)
        {
            // Connection not established, not connected anymore
            return false;
        }
    }

    return true;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;
using System.IO;
using System.Text;  // For Encoding

public class Client : MonoBehaviour
{
    public static Client instance;
    static public String host = "18.192.144.101";
    static public Int32 port = 26950;
    public IPEndPoint ipEndPoint = new IPEndPoint(
        IPAddress.Parse(host), port);
    public TcpListener listener = null;
    public TcpClient client = null;
    public static NetworkStream ns = null;
    private static byte[] receiveBuffer;


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }



    void OnApplicationQuit()
    {
        ns.Close();
        ns = null;
        client.Close();
        client = null;
    }


    public void Connect()
    {
        Thread thread = new Thread(
                new ThreadStart(
                    ConnectToServer));
        thread.Start();
    }


    public void ConnectToServer()
    {
        client = new TcpClient();
        client.Connect(ipEndPoint);

        ns = client.GetStream();

        Thread receiveThread = new Thread(() => ReceiveFromServer());
        SendUsername();
    }


    public void SendUsername()
    {
        String username = UIManager.instance.usernameField.text;

        // First character in message is the datatype so server can identify
        // what kind of message its receiving from client
        Byte[] sendBytes = Encoding.UTF8.GetBytes($"1{username}");

        ns.Write(sendBytes, 0, sendBytes.Length);

        // Start pinging the server after sending client's username
        PingServer();
    }


    public void ReceiveFromServer()
    {
        NetworkStream ns = client.GetStream();

        while (true)
        {
            // Reads NetworkStream into a byte buffer.
            byte[] bytes = new byte[client.ReceiveBufferSize];

            // Read can return anything from 0 to numBytesToRead.
            // This method blocks until at least one byte is read.
            ns.Read(bytes, 0, (int)client.ReceiveBufferSize);

            // Returns the data received from the host to the console.
            string returndata = Encoding.UTF8.GetString(bytes);

            if (returndata.Length > 0)
            {
                Debug.Log("Server assigned ID of " + returndata + " to you");
            }
        }
    }


    public void PingServer()
    {
        // Pinging server every 5 seconds just
        // to compensate possible network delays
        DateTime targetTime = DateTime.Now.AddMilliseconds(5000);

        try
        {
            while (client.Connected)
            {
                while (targetTime > DateTime.Now && client != null)
                {
                    // Do nothing
                }
                targetTime = DateTime.Now.AddSeconds(9);
                Ping();
            }
        }
        catch (NullReferenceException ex)
        {
            Debug.Log(ex);
        }
    }


    public void Ping()
    {
        // '2' is the dataType on server for pings
        Byte[] sendBytes = Encoding.UTF8.GetBytes("2");
        try
        {
            ns.Write(sendBytes, 0, sendBytes.Length);
        }
        catch (Exception ex)
        {
            if (ex is ObjectDisposedException || ex is OverflowException)
            {
                Debug.Log(ex);
            } else
            {
                // Debug any error
                Debug.Log(ex);
            }
        }
        Debug.Log("Ping sent to server");
    }
}
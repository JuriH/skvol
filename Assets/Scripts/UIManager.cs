using System;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static  UIManager Instance;

    public GameObject startMenu;
    public InputField usernameField;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.Log("Instance already exists, destroying object");
            Destroy(this);
        }
    }

    public void ConnectToServer()
    {
        startMenu.SetActive(false);
        usernameField.interactable = false;

        if (usernameField.text.Trim() == "")
        {
            Debug.Log("Enter a valid username");
            // TODO: Visual feedback about empty username field
        }
        else
        {
            if (IsUsernameAvailable())
            {
                Debug.Log(
                    "Username available, connecting to the server");
                Client.Instance.Connect();
            }
            else
            {
                Debug.Log("Username is already taken");
                // TODO: Visual feedback of already taken username
            }
        }
    }


    private bool IsUsernameAvailable()
    {
    Debug.Log(
            "Checking if username is available");

    var byteArray = Encoding.UTF8.GetBytes(
            "username=" + usernameField.text);
        var webRequest =
            (HttpWebRequest)WebRequest.Create(
                "http://18.192.144.101/php/isUsernameAvailable.php/");
        webRequest.Method = "POST";
        webRequest.ContentType = "application/x-www-form-urlencoded";
        webRequest.ContentLength = byteArray.Length;

        using (var webpageStream = webRequest.GetRequestStream())
        {
            webpageStream.Write(byteArray, 0, byteArray.Length);
        }

        Debug.Log("Sent Unity-client's username to the PHP-script for checking availability");

        var webResponse = webRequest.GetResponse();
        var receiveStream = webResponse.GetResponseStream();
        var readStream = new StreamReader(receiveStream ?? throw new Exception(), Encoding.UTF8);
        var response = readStream.ReadToEnd();
        
        Debug.Log(response);
        
        webResponse.Close();
        readStream.Close();

        // Condition met? TRUE : FALSE
        return response.Equals("OK");
    }
}
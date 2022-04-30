using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    //Panel References
    [SerializeField] private GameObject menuParent;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject clientMenu;
    [SerializeField] private GameObject serverMenu;
    //Buttons References
    [SerializeField] private Button createServer;
    [SerializeField] private Button createClient;
    [SerializeField] private Button startServer;
    [SerializeField] private Button startClient;
    [SerializeField] private Button retryConnection;
    //Inputs References
    [SerializeField] private InputField serverPortInput;
    [SerializeField] private InputField serverIPInput;
    //Text References
    [SerializeField] private Text portText;
    [SerializeField] private Text IPText;
    //Network Managers References
    [SerializeField] private GameObject serverObject;
    [SerializeField] private GameObject clientObject;



    private void Start()
    {
        createClient.onClick.AddListener(() => OnCreateClient());
        createServer.onClick.AddListener(() => OnCreateServer());
        startClient.onClick.AddListener(() => OnStartClient());
        startServer.onClick.AddListener(() => OnStartServer());
        retryConnection.onClick.AddListener(() => OnRetryConnection());
    }

    #region Button Actions
    private void OnCreateServer()
    {
        mainMenu.SetActive(false);
        serverObject.SetActive(true);
        portText.text += " "+GameServer.instance.GetReliablePort();
        serverMenu.SetActive(true);
        StartCoroutine(GetIPAddress());
    }
    private void OnCreateClient()
    {
        mainMenu.SetActive(false);
        clientObject.SetActive(true);
        clientMenu.SetActive(true);
       
    }
    private void OnStartClient()
    {
        if (serverIPInput.text.Trim().Equals("") || serverPortInput.text.Trim().Equals("")) { return; }
        menuParent.SetActive(false);
        GameClient.instance.StartClient(int.Parse(serverPortInput.text),serverIPInput.text);
        GameClient.instance.AddConnectionFailureHandler(() => retryConnection.gameObject.SetActive(true));
    }
    private void OnStartServer()
    {
        menuParent.SetActive(false);
        GameServer.instance.StartServer();
    }
    private void OnRetryConnection()
    {
        retryConnection.gameObject.SetActive(false);
        GameClient.instance.TryConnect();
    }
    #endregion
    #region Utils
    IEnumerator GetIPAddress()
    {
        UnityWebRequest myExtIPWWW = UnityWebRequest.Get("http://checkip.dyndns.org");
        yield return myExtIPWWW.SendWebRequest();
        if (myExtIPWWW.isNetworkError || myExtIPWWW.isHttpError)
        {
            Debug.Log(myExtIPWWW.error);
            IPText.text += " error de conexión";
        }
        string result = myExtIPWWW.downloadHandler.text;

        // This results in a string similar to this: <html><head><title>Current IP Check</title></head><body>Current IP Address: 123.123.123.123</body></html>
        // where 123.123.123.123 is your external IP Address.
        //  Debug.Log("" + result);

        string[] a = result.Split(':'); // Split into two substrings -> one before : and one after. 
        string a2 = a[1].Substring(1);  // Get the substring after the :
        string[] a3 = a2.Split('<');    // Now split to the first HTML tag after the IP address.
        string a4 = a3[0];              // Get the substring before the tag.

        IPText.text += " " + a4;
    }
   
    #endregion
}

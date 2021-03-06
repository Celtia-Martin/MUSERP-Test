using Muse_RP.Hosts;
using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

//Class that manages the use of the communication protocol in the game if the user is a client
public class GameClient : MonoBehaviour
{
    //Singleton
    public static GameClient instance;

    //References
    private Character mine;

    private Queue<Action> jobs;

    private Dictionary<int, Character> players;

    private IClientProtocol clientProtocol;
    //Properties
    private int myID = -2;
    [SerializeField]
    protected int timeOut;
    [SerializeField]
    protected int connectionTries;
    [SerializeField]
    protected int timeOutConnection;
    [SerializeField]
    protected int serverReliablePort;
    [SerializeField]
    protected string serverIP;

    #region Unity Events
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        players = new Dictionary<int, Character>();
        jobs = new Queue<Action>();
    }
    private void Update()
    {
        if (jobs.Count > 0)
        {
            jobs.Dequeue().Invoke();
        }
        if (clientProtocol is TCPClient)
        {
            ((TCPClient)clientProtocol).onUpdate?.Invoke();
        }
    }
    #endregion
    public void StartClient(int serverReliablePort, string IP)
    {

        this.serverIP = IP;
        this.serverReliablePort = serverReliablePort;
        //MUSE-RP: 
        ConnectionInfo serverInfo = new ConnectionInfo(IP, serverReliablePort, 0);
        HostOptions clientOptions = new HostOptions()
        {
            maxConnections = 1,
            timeOut = timeOut,
            timePing = 1000,
            windowSize = 1000,
            timerTime = 200,
            waitTime = 1

        };
        clientProtocol = new MuseRPClient(serverInfo, clientOptions, timeOutConnection, connectionTries);

        //Telepathy:
        //clientProtocol = new TCPClient(UnityEngine.Random.Range(49152, 65535), new IPEndPoint(IPAddress.Parse(IP), serverReliablePort));
        //Ruffles:
        //clientProtocol = new RufflesClient(new IPEndPoint(IPAddress.Parse(IP), serverReliablePort));
        //GServer:
        //clientProtocol = new GServerClient(serverReliablePort, IP);
        //UDPClient:
        // clientProtocol = new UDPClient(new IPEndPoint(IPAddress.Parse(IP), serverReliablePort), UnityEngine.Random.Range(49152, 65535));

        clientProtocol.OnStart(OnConnected);
        clientProtocol.AddHandler(4, OnNewCharacterMessage);
        clientProtocol.AddHandler(5, OnPositionMessage);
        clientProtocol.AddHandler(66, MyCharacterMessage);
        clientProtocol.AddHandler(6, OnEndClientReceived);
        clientProtocol.AddHandler(7, OnShotReceived);
        clientProtocol.AddHandler(8, OnPointsReceive);
        clientProtocol.AddHandler(9, OnDisappearReceive);
        clientProtocol.AddHandler(10, OnSpawnEnemyReceive);
        clientProtocol.AddHandler(11, OnStartGame);
        clientProtocol.AddHandler(12, OnEndGame);

        clientProtocol.AddOnConnectionFailedHandler(() => jobs.Enqueue(() => Console.instance.WriteLine("Impossible connect to server")));
    }
    public void AddConnectionFailureHandler(Action handler)
    {
        clientProtocol.AddOnConnectionFailedHandler(() => jobs.Enqueue(handler));
    }
    public void TryConnect()
    {
        clientProtocol.TryConnect();
    }

    #region Senders
    public void SendPositionToServer(Vector2 position)
    {
        clientProtocol.SendToServer(5, GameSerializer.positionInfoToBytes(position, myID), false);

    }
    public void SendShotServer(Vector2 position, int ID)
    {
        clientProtocol.SendToServer(7, GameSerializer.positionInfoToBytes(position, ID));
    }

    #endregion
    #region Event Handlers

    protected void OnConnected()
    {
        clientProtocol.AddOnDisconnectedHandler(() => jobs.Enqueue(OnServerDisconnectedJob));
        Debug.Log("Conectado");
    }

    private void OnApplicationQuit()
    {
        clientProtocol?.OnAppQuit();
    }
    private void OnDestroy()
    {
        clientProtocol?.OnAppQuit();
    }
    private void OnPointsReceive(MessageObject message, Connection source)
    {
        jobs.Enqueue(() => OnPointsJob(message, source));
    }
    private void OnDisappearReceive(MessageObject message, Connection source)
    {
        jobs.Enqueue(() => OnDisappearJob(message, source));
    }
    private void OnSpawnEnemyReceive(MessageObject message, Connection source)
    {
        jobs.Enqueue(() => OnSpawnJob(message, source));
    }
    public void OnPositionMessage(MessageObject message, Connection source)
    {
        jobs.Enqueue(() => OnPositionMessageJob(message, source));

    }
    public void OnEndClientReceived(MessageObject message, Connection source)
    {
        jobs.Enqueue(() => EndCharacterJob(message, source));

    }
    public void MyCharacterMessage(MessageObject message, Connection source)
    {
        jobs.Enqueue(() => MyCharacterJob(message, source));
    }
    public void OnNewCharacterMessage(MessageObject message, Connection source)
    {
        jobs.Enqueue(() => NewCharacterJob(message, source));
    }
    public void OnShotReceived(MessageObject message, Connection source)
    {
        jobs.Enqueue(() => OnShotMessageJob(message, source));
    }

    public void OnStartGame(MessageObject message, Connection source)
    {
        jobs.Enqueue(() => OnStartGameJob(message, source));
    }
    public void OnEndGame(MessageObject message, Connection source)
    {
        jobs.Enqueue(() => OnEndGameJob());
    }

    #endregion
    #region Jobs
    private void OnServerDisconnectedJob()
    {
        Console.instance.WriteLine("Disconnected");
        clientProtocol.RemoveHandler(4);
        clientProtocol.RemoveHandler(66);
        clientProtocol.RemoveHandler(5);
        clientProtocol.RemoveHandler(6);
        clientProtocol.RemoveHandler(7);
        clientProtocol.RemoveHandler(8);
        clientProtocol.RemoveHandler(9);
        clientProtocol.RemoveHandler(10);

        clientProtocol.RemoveOnDisconnectedHandler(() => jobs.Enqueue(OnServerDisconnectedJob));
    }
    private void NewCharacterJob(MessageObject message, Connection source)
    {
        Color color = GameSerializer.getCharacterFromBytes(message.getData(), out int id);
        if (id == myID) return;
        Console.instance.WriteLine("New client " + id);
        Character newChara = PoolManager.singleton.getFromPool("Character").GetComponent<Character>();
        newChara.CharacterCreated(id, false, false);
        newChara.SetColor(color);
        players.Add(id, newChara);
    }
    private void MyCharacterJob(MessageObject message, Connection source)
    {

        Color color = GameSerializer.getCharacterFromBytes(message.getData(), out int id);

        Console.instance.WriteLine("My ID: " + id);
        mine = PoolManager.singleton.getFromPool("Character").GetComponent<Character>();
        mine.CharacterCreated(id, true, false);
        mine.SetColor(color);
        myID = id;
        if (!players.ContainsKey(id))
        {
            players.Add(id, mine);
        }
        Debug.Log("recibi mi character con color " + color.ToString());
    }
    private void EndCharacterJob(MessageObject message, Connection source)
    {
        int id = BitConverter.ToInt32(message.getData(), 0);
        Console.instance.WriteLine("Client " + id + " is gone");
        if (players.TryGetValue(id, out Character chara))
        {
            PoolManager.singleton.addToPool("Character", chara.gameObject);
            players.Remove(id);
        }
    }
    private void OnPositionMessageJob(MessageObject message, Connection source)
    {
        Debug.Log("recibi la posicion");
        Vector2 position = GameSerializer.getPositionFromBytes(message.getData(), out int id);
        if (id == myID) return;
        if (players.TryGetValue(id, out Character chara))
        {
            chara.SetPosition(position);
        }
    }
    private void OnShotMessageJob(MessageObject message, Connection source)
    {
        Vector2 position = GameSerializer.getPositionFromBytes(message.getData(), out int ID);
        if (ID == myID) return;
        if (players.TryGetValue(ID, out Character chara))
        {
            chara.Shot(position);
            Debug.Log("shot de " + ID);

        }
    }
    private void OnPointsJob(MessageObject message, Connection source)
    {
        int points = GameSerializer.getPointsFromBytes(message.getData(), out int ID);
        if (players.TryGetValue(ID, out Character value))
        {
            value.AddPoints(points);
        }
    }
    private void OnDisappearJob(MessageObject message, Connection source)
    {
        EnemySpawner.instance.RemoveEnemyClient(GameSerializer.getDisappearEnemyFromBytes(message.getData()));
    }
    private void OnSpawnJob(MessageObject message, Connection source)
    {
        int index = GameSerializer.getSpawnEnemyFromBytes(message.getData(), out ushort type);
        EnemySpawner.instance.NewEnemyClient(index, type);
    }
    private void OnStartGameJob(MessageObject message, Connection source)
    {
        Character.myCharacter.StartGame();
        UIManager.StartGame();

    }
    private void OnEndGameJob()
    {
        string results = "";
        int i = 0;
        foreach (Character chara in players.Values)
        {
            string color = "<color=#" + ColorUtility.ToHtmlStringRGB(chara.color) + ">";
            results += color + "Player " + chara.getID() + " :" + chara.getPoints() + "</color>\n";
            i++;
        }
        UIManager.OnEndGame(results);
    }
    #endregion
}

using Muse_RP.Hosts;
using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private int myID=-2;
    [SerializeField]
    protected int timeOut;
    [SerializeField]
    protected int connectionTries;
    [SerializeField]
    protected int timeOutConnection;
    [SerializeField]
    protected int serverReliablePort; // Solo se necesita el fiable
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
    }
    #endregion
    public void StartClient(int serverReliablePort, string IP)
    {
        this.serverIP = IP;
        this.serverReliablePort = serverReliablePort;
        //MUSERP
        HostOptions options = new HostOptions(1, timeOut, 1000, 1, 0, 0, 1, 200, 100, null);

        ConnectionInfo serverInfo = new ConnectionInfo(IP, serverReliablePort, 0, -1);
        clientProtocol = new MuseRPClient(serverInfo, options, timeOutConnection, connectionTries);
        /////
        clientProtocol.OnStart(OnConnected);

        clientProtocol.AddHandler(4, OnNewCharacterMessage);
        clientProtocol.AddHandler(5, OnPositionMessage);
        clientProtocol.AddHandler(3, MyCharacterMessage);
        clientProtocol.AddHandler(6, OnEndClientReceived);
        clientProtocol.AddHandler(7, OnShotReceived);
        clientProtocol.AddHandler(8, OnPointsReceive);
        clientProtocol.AddHandler(9, OnDisappearReceive);
        clientProtocol.AddHandler(10, OnSpawnEnemyReceive);
        clientProtocol.AddOnConnectionFailedHandler(() => jobs.Enqueue(() => Console.instance.WriteLine("Imposible conectar con el servidor")));
    }
    public void AddConnectionFailureHandler(Action handler)
    {
        clientProtocol.AddOnConnectionFailedHandler(()=>jobs.Enqueue(handler));
    }
    public void TryConnect()
    {
        clientProtocol.TryConnect();
    }
    #region Senders
    public void SendPositionToServer(Vector2 position)
    {
        clientProtocol.SendToServer(5, GameSeralizer.positionInfoToBytes(position, myID), false);

    }
    public void SendShotServer(Vector2 position, int ID)
    {
        clientProtocol.SendToServer(7, GameSeralizer.positionInfoToBytes(position, ID));
    }

    #endregion
    #region Event Handlers

    protected void OnConnected()
    {
        //Mal, deberia de hacer remove

        clientProtocol.AddOnDisconnectedHandler(() => jobs.Enqueue(OnServerDisconnectedJob));
        Debug.Log("Conectado");
    }

    private void OnApplicationQuit()
    {
        clientProtocol.OnAppQuit();
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

    #endregion
    #region Jobs
    private void OnServerDisconnectedJob()
    {
        Console.instance.WriteLine("Desconectado del servidor");
        //Remove all handlers????
        clientProtocol.RemoveHandler(4);
        clientProtocol.RemoveHandler(3);
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
        Color color = GameSeralizer.getCharacterFromBytes(message.getData(), out int id);
        Console.instance.WriteLine("Nuevo cliente " + id);
        if (id == myID) return;
        Character newChara = PoolManager.singleton.getFromPool("Character").GetComponent<Character>();
        newChara.CharacterCreated(id, false, false);
        newChara.SetColor(color);
        players.Add(id, newChara);
    }
    private void MyCharacterJob(MessageObject message, Connection source)
    {
        Debug.Log("recibi mu character");
        Color color = GameSeralizer.getCharacterFromBytes(message.getData(), out int id);
        Console.instance.WriteLine("Mi cliente es el  " + id);
        mine = PoolManager.singleton.getFromPool("Character").GetComponent<Character>();
        mine.CharacterCreated(id, true, false);
        mine.SetColor(color);
        myID = id;
        players.Add(id, mine);
    }
    private void EndCharacterJob(MessageObject message, Connection source)
    {
        int id = BitConverter.ToInt32(message.getData(), 0);
        Console.instance.WriteLine("Cliente " + id + " se ha ido");
        if (players.TryGetValue(id, out Character chara))
        {
            PoolManager.singleton.addToPool("Character", chara.gameObject);
            players.Remove(id);
        }
    }
    private void OnPositionMessageJob(MessageObject message, Connection source)
    {
        Debug.Log("recibi la posicion");
        Vector2 position = GameSeralizer.getPositionFromBytes(message.getData(), out int id);
        if (id == myID) return;
        if (players.TryGetValue(id, out Character chara))
        {
            chara.SetPosition(position);
        }
    }
    private void OnShotMessageJob(MessageObject message, Connection source)
    {
        Vector2 position = GameSeralizer.getPositionFromBytes(message.getData(), out int ID);
        if (ID == myID) return;
        if (players.TryGetValue(ID, out Character chara))
        {
            chara.Shot(position);
            Debug.Log("shot de " + ID);

        }
        else
        {
            Debug.Log("kapasao ");
        }
    }

    private void OnPointsJob(MessageObject message, Connection source)
    {
        int points = GameSeralizer.getPointsFromBytes(message.getData(), out int ID);
        if(players.TryGetValue(ID,out Character value))
        {
            value.SetPoints(points);
        }
    }
    private void OnDisappearJob(MessageObject message, Connection source)
    {
        EnemySpawner.instance.RemoveEnemyClient(GameSeralizer.getDisappearEnemyFromBytes(message.getData()));
    }
    private void OnSpawnJob(MessageObject message, Connection source)
    {
        int index = GameSeralizer.getSpawnEnemyFromBytes(message.getData(), out ushort type);
        EnemySpawner.instance.NewEnemyClient(index, type);
    }
    #endregion
}

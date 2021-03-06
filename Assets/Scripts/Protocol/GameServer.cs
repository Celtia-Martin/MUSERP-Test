using Muse_RP.Hosts;
using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;

//Class that manages the use of the communication protocol in the game if the user is a server
public class GameServer : MonoBehaviour
{

    //Singleton
    public static GameServer instance;

    //Properties in editor
    [SerializeField]
    private int reliablePort;
    [SerializeField]
    private int noReliablePort;
    [SerializeField]
    private int maxConnections;
    [SerializeField]
    private int timeOut;
    [SerializeField]
    private int timePing;
    [SerializeField]
    private uint reliablePercentage;

    //References
    private Dictionary<int, Character> players;

    private Dictionary<Connection, int> idPlayers;

    private List<Character> characters;

    private Character myPlayer;

    private Queue<Action> jobs;

    //State
    private int myID = -1;

    private int contClientID = 0;

    protected IServerProtocol serverProtocol;

    private bool isServer = false;

    #region Unity Events
    public static void OnGameEnd()
    {
        if (instance == null || !instance.isServer)
        {
            return;
        }
        string results = "";
        int i = 0;
        foreach (Character chara in instance.players.Values)
        {
            string color = "<color=#" + ColorUtility.ToHtmlStringRGB(chara.color) + ">";
            results += color + "Player " + chara.getID() + " :" + chara.getPoints() + "</color>\n";
            i++;
        }
        UIManager.OnEndGame(results);
        instance.serverProtocol.SendToAll(12, null, true);
    }
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
        idPlayers = new Dictionary<Connection, int>();
        characters = new List<Character>();
        jobs = new Queue<Action>();
    }
    private void Update()
    {
        if (jobs.Count > 0)
        {
            jobs.Dequeue().Invoke();
        }
        if (serverProtocol is TCPServer)
        {
            ((TCPServer)serverProtocol).onUpdate?.Invoke();
        }
    }
    #endregion
    public void AddJob(Action newJob)
    {
        jobs.Enqueue(newJob);
    }
    public void StartServer()
    {
        isServer = true;
        //MUSE-RP:
        serverProtocol = new MuseRPServer(reliablePort, noReliablePort, maxConnections, timeOut, timePing, reliablePercentage);
        //Telepathy:
        //serverProtocol = new TCPServer(reliablePort, maxConnections);
        //Ruffles:
        // serverProtocol = new RufflesServer(reliablePort);
        //GServer
        //serverProtocol = new GServerServer(reliablePort,this);
        //UDP:
        //serverProtocol = new UDPServer(reliablePort, maxConnections);
        serverProtocol.OnStart();
        ServerIniciado();
    }

    public void GameIsStarted()
    {
        Character.myCharacter.StartGame();
        EnemySpawner.instance.InitEnemySpawner(true);
        serverProtocol.SendToAll(11, null, true);
        UIManager.StartGame();
    }

    public int GetReliablePort() { return reliablePort; }
    #region Event Handlers
    private void OnPositionReceive(MessageObject message, Connection source)
    {
        jobs.Enqueue(() => OnPositionChangeJob(message, source));
    }
    private void OnShotReceived(MessageObject message, Connection source)
    {
        jobs.Enqueue(() => ShotReceivedJob(message, source));
    }
    private void OnClientConnected(ConnectionInfo clientInfo)
    {
        jobs.Enqueue(() => NewClientJob(clientInfo));

    }
    private void InitMyCharacter()
    {
        myPlayer = PoolManager.singleton.getFromPool("Character").GetComponent<Character>();
        myPlayer.CharacterCreated(myID, true, true);
        players.Add(myID, myPlayer);
        characters.Add(myPlayer);
        serverProtocol.AddHandler(5, OnPositionReceive);
        serverProtocol.AddHandler(7, OnShotReceived);

    }
    private void OnApplicationQuit()
    {
        serverProtocol?.OnAppQuit();
    }
    private void OnDestroy()
    {
        serverProtocol?.OnAppQuit();
    }
    private void ServerIniciado()
    {
        InitMyCharacter();
        serverProtocol.InitServer(OnClientConnected, OnClientDisconnected);


    }
    private void OnClientDisconnected(ConnectionInfo info)
    {
        jobs.Enqueue(() => OnClientDisconnectedJob(info));
    }

    #endregion
    #region Senders
    public void SendPositionServer(Vector2 position, int ID)
    {
        serverProtocol.SendToAll(5, GameSerializer.positionInfoToBytes(position, ID), false);
    }
    public void SendShot(Vector2 position, int ID)
    {
        serverProtocol.SendToAll(7, GameSerializer.positionInfoToBytes(position, ID), true);
    }
    public void SendPoints(int points, int ID)
    {
        serverProtocol.SendToAll(8, GameSerializer.pointsToBytes(ID, points), true);
    }
    public void SendDisappearEnemy(int ID)
    {
        serverProtocol.SendToAll(9, GameSerializer.enemyDisappearToBytes(ID), true);
    }
    public void SendSpawnEnemy(int index, ushort type)
    {
        serverProtocol.SendToAll(10, GameSerializer.spawnEnemyToBytes(index, type), true);
    }
    public void SendSpawnEnemy(int index, ushort type, Connection conn)
    {
        serverProtocol.SendTo(10, GameSerializer.spawnEnemyToBytes(index, type), conn, true);
    }

    #endregion
    #region Jobs
    private void OnClientDisconnectedJob(ConnectionInfo info)
    {
        Connection reliableConn = new Connection(info.IP, info.reliablePort, true);
        if (!idPlayers.TryGetValue(reliableConn, out int playerID))
        {
            return;
        }
        Console.instance.WriteLine("Cliente desconnected" + playerID);

        Character deleted = players[playerID];
        if (deleted == null) return;
        PoolManager.singleton.addToPool("Character", deleted.gameObject);
        serverProtocol.SendToAll(6, BitConverter.GetBytes(playerID), true);
        players.Remove(playerID);
        characters.Remove(deleted);
        idPlayers.Remove(new Connection(info.IP, info.reliablePort, true));
    }

    private void ShotReceivedJob(MessageObject message, Connection source)
    {
        Vector2 position = GameSerializer.getPositionFromBytes(message.getData(), out int ID);

        if (players.TryGetValue(ID, out Character chara))
        {
            chara.Shot(position);
            SendShot(position, ID);
        }
    }
    private void NewClientJob(ConnectionInfo clientInfo)
    {
        int currentID = contClientID;
        contClientID++;
        Console.instance.WriteLine("Client " + currentID + " connected");
        Character newCharacter = PoolManager.singleton.getFromPool("Character").GetComponent<Character>();
        newCharacter.CharacterCreated(currentID, false, true);
        Connection reliableConnection = new Connection(clientInfo.IP, clientInfo.reliablePort, true);

        idPlayers.Add(reliableConnection, currentID);
        byte[] data = GameSerializer.newCharacterToBytes(newCharacter.color, currentID);
        byte[] charaInfo;

        serverProtocol.SendTo(66, data, reliableConnection, true);

        foreach (Character chara in characters)
        {
            charaInfo = GameSerializer.newCharacterToBytes(chara.color, chara.getID());

            serverProtocol.SendTo(4, charaInfo, reliableConnection, true);
            serverProtocol.SendTo(8, GameSerializer.pointsToBytes(chara.getID(), chara.getPoints()), reliableConnection, true);

        }
        EnemySpawner.instance.SendAllCurrentEnemies(reliableConnection);
        players.Add(currentID, newCharacter);
        characters.Add(newCharacter);
        serverProtocol.SendToAll(4, data, true);
    }
    private void OnPositionChangeJob(MessageObject message, Connection source)
    {
        Vector2 position = GameSerializer.getPositionFromBytes(message.getData(), out int id);
        if (players.TryGetValue(id, out Character chara))
        {
            chara.SetPosition(position);
            SendPositionServer(position, id);
        }
    }

    #endregion

}

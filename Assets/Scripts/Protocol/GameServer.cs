using Muse_RP.Hosts;
using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;

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

    private int myID = -1;

    private int contClientID = 0;

    protected IServerProtocol serverProtocol;



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
    }
    #endregion
    public void StartServer()
    {
        //serverProtocol = new MuseRPServer(reliablePort, noReliablePort, maxConnections, timeOut, timePing, reliablePercentage);
       // serverProtocol = new TCPServer(reliablePort,maxConnections);
        //serverProtocol = new RufflesServer(reliablePort);
        serverProtocol = new GServerServer(reliablePort);
        //serverProtocol = new UDPServer(reliablePort, maxConnections);
        serverProtocol.OnStart();
        ServerIniciado();
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
    private void OnClientConnected(ConnectionInfo clientInfo)//No es concurrente, no necesita 
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
    private void ServerIniciado()
    {
        InitMyCharacter();
        serverProtocol.InitServer(OnClientConnected, OnClientDisconnected);
        EnemySpawner.instance.InitEnemySpawner(true);

    }
    private void OnClientDisconnected(ConnectionInfo info)
    {
        jobs.Enqueue(() => OnClientDisconnectedJob(info));
    }

    #endregion
    #region Senders
    public void SendPositionServer(Vector2 position, int ID)
    {
        serverProtocol.SendToAll(5, GameSeralizer.positionInfoToBytes(position, ID), false);
    }
    public void SendShot(Vector2 position, int ID)
    {
        serverProtocol.SendToAll(7, GameSeralizer.positionInfoToBytes(position, ID), true);
    }
    public void SendPoints(int points, int ID)
    {
        serverProtocol.SendToAll(8, GameSeralizer.pointsToBytes(ID, points), true);
    }
    public void SendDisappearEnemy(int ID){
        serverProtocol.SendToAll(9, GameSeralizer.enemyDisappearToBytes(ID), true);
    }
    public void SendSpawnEnemy(int index,ushort type)
    {
        serverProtocol.SendToAll(10, GameSeralizer.spawnEnemyToBytes(index,type), true);
    }
    public void SendSpawnEnemy(int index, ushort type,Connection conn)
    {
        serverProtocol.SendTo(10, GameSeralizer.spawnEnemyToBytes(index, type),conn, true);
    }

    #endregion
    #region Jobs
    private void OnClientDisconnectedJob(ConnectionInfo info)
    {
        Connection reliableConn = new Connection(info.IP, info.reliablePort, true);
        if(!idPlayers.TryGetValue(reliableConn,out int playerID))
        {
            return;
        }
        Console.instance.WriteLine("OnClientDisconnected");
        Console.instance.WriteLine("Cliente desconectado" + playerID);

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
        Vector2 position = GameSeralizer.getPositionFromBytes(message.getData(), out int ID);

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
        Console.instance.WriteLine("Cliente " + currentID + " conectado");
        Character newCharacter = PoolManager.singleton.getFromPool("Character").GetComponent<Character>();
        newCharacter.CharacterCreated(currentID, false, true);
        Connection reliableConnection = new Connection(clientInfo.IP, clientInfo.reliablePort, true);

        idPlayers.Add(reliableConnection, currentID);
        byte[] data = GameSeralizer.newCharacterToBytes(newCharacter.color, currentID);
        byte[] charaInfo;

        serverProtocol.SendTo(3, data, reliableConnection, true);

        foreach (Character chara in characters)
        {
            Console.instance.WriteLine("Mandando character " + chara.getID());
            charaInfo = GameSeralizer.newCharacterToBytes(chara.color, chara.getID());

            serverProtocol.SendTo(4, charaInfo, reliableConnection, true);
            serverProtocol.SendTo(8, GameSeralizer.pointsToBytes(chara.getID(), chara.getPoints()), reliableConnection, true);

        }
        EnemySpawner.instance.SendAllCurrentEnemies(reliableConnection);
        players.Add(currentID, newCharacter);
        characters.Add(newCharacter);
        serverProtocol.SendToAll(4, data, true);
        Debug.Log("Todo bien " + characters.Count);
    }
    private void OnPositionChangeJob(MessageObject message, Connection source)
    {
        Debug.Log("ha recibido posicion");
        Vector2 position = GameSeralizer.getPositionFromBytes(message.getData(), out int id);
        if (players.TryGetValue(id, out Character chara))
        {
            chara.SetPosition(position);
            SendPositionServer(position, id);
        }
    }
    #endregion
}

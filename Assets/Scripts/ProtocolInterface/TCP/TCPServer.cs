using Muse_RP.Hosts;
using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class TCPServer : IServerProtocol
{
    private Dictionary<ushort, Action<byte[]>> handlerDictionary;
    private List<int> clients;
    private OnServerDisconneced onDisconnected;
    private OnClientConnectedDelegate onConnected;
    private OnClientDisconnected disconnectedDelegate;
    private Telepathy.Server tcpServer;
    private int port;
    private int maxConnections;
    private int clientID;
    public Action onUpdate;

    private bool connected;

    public TCPServer(int port,int maxConnections)// o puerto aleatorio como en MUSE-RP https://stackoverflow.com/questions/36526332/simple-socket-server-in-unity
    {
        tcpServer = new Telepathy.Server(2000);
        this.port = port;
        this.maxConnections = maxConnections;
        tcpServer.NoDelay = true;
        onUpdate += OnUpdate;
        //tcpServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
        //tcpServer.Bind(localEndPoint);
        //tcpServer.NoDelay = true;
        handlerDictionary = new Dictionary<ushort, Action<byte[]>>();
        clients = new List<int>();
        //clients = new Dictionary<string, Socket>();
        //this.maxConnections = maxConnections;
        //listenerThread = new Thread[maxConnections];
      
        
    }
    public void AddHandler(ushort type, MessageDelegate handler)
    {
        handlerDictionary.Add(type, (b) =>
        {
            byte[] data = new byte[b.Length - 2];
            Array.Copy(b, 2, data, 0, b.Length - 2);
            ushort type = BitConverter.ToUInt16(b, 0);
            handler?.Invoke(new MessageObject(type, 0, 0, false, false, false, false, data), null);
        });
    }

    public void AddHandler(ushort type, Action<byte[]> handler)
    {
        handlerDictionary.Add(type, handler);
    }

    public void InitServer(OnClientConnectedDelegate connectedDelegate, OnClientDisconnected disconnectedDelegate)
    {
        this.onConnected += connectedDelegate;
        this.disconnectedDelegate += disconnectedDelegate;
    }

    public void OnAppQuit()
    {
        tcpServer.Stop();
    }

    public void OnStart()
    {
        connected = true;
    
        tcpServer.Start(port);
        tcpServer.OnConnected += ClientConnected;
        tcpServer.OnConnected += (i) => Debug.Log("Cliente " + i + " conectado");
        tcpServer.OnDisconnected += ClientDisConnected;
        tcpServer.OnData += OnData;
    }

    public void RemoveHandler(ushort type)
    {
        handlerDictionary.Remove(type);
    }

    public void SendEndToAll()
    {
       // throw new NotImplementedException();
    }

    public void SendTo(ushort type, int ID, byte[] message, bool reliable = true)
    {
        throw new NotImplementedException();
    }

    public void SendTo(ushort type, byte[] message, Connection conn, bool reliable = true)
    {
        List<byte> bytesToSend = new List<byte>();

        bytesToSend.AddRange(BitConverter.GetBytes(type));
        if (message != null)
        {
            bytesToSend.AddRange(message);
        }
        ArraySegment<byte> data = new ArraySegment<byte>(bytesToSend.ToArray());

        tcpServer.Send(conn.ID, data);
     
    }

    public void SendToAll(ushort type, byte[] message, bool reliable = true)
    {
        List<byte> bytesToSend = new List<byte>();

        bytesToSend.AddRange(BitConverter.GetBytes(type));
        if (message != null)
        {
            bytesToSend.AddRange(message);
        }
        ArraySegment<byte> data = new ArraySegment<byte>(bytesToSend.ToArray());
        for (int i =0; i<clients.Count; i++)
        {
            tcpServer.Send(clients[i], data);
        }
    }

   private void OnData(int connectionID, ArraySegment<byte> data)
    {
        ushort type = BitConverter.ToUInt16( data.Take(2).ToArray(),0);
        if(handlerDictionary.TryGetValue(type,out Action<byte[]> value))
        {
            value?.Invoke(data.ToArray());
        }
    }

    private void ClientConnected(int connectionID)
    {
        clients.Add(connectionID);
        string address= tcpServer.GetClientAddress(connectionID);
        ConnectionInfo clientInfo = new ConnectionInfo(address, connectionID, connectionID);
        onConnected?.Invoke(clientInfo);  
    }   
    private void ClientDisConnected(int connectionID)
    {
        clients.Remove(connectionID);
        string address= tcpServer.GetClientAddress(connectionID);
        ConnectionInfo clientInfo = new ConnectionInfo(address, connectionID, connectionID);
        disconnectedDelegate?.Invoke(clientInfo);  
    }
    private void OnUpdate()
    {
            tcpServer.Tick(100);
    }
}

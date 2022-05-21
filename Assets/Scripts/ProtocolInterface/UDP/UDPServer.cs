using Muse_RP.Hosts;
using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class UDPServer : IServerProtocol
{
    private Socket serverSocket;
    private Dictionary<ushort, Action<byte[]>> handlerDictionary;
    private bool connected;
    private OnClientConnectedDelegate onClientConnected;
    private Dictionary<Connection,EndPoint> clients;
   
    private int maxConnections;
    private int port;
    private Thread listenerThread;

    public UDPServer(int port, int maxConnections)
    {
        this.port = port;
        this.maxConnections = maxConnections;
        serverSocket= new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
        handlerDictionary = new Dictionary<ushort, Action<byte[]>>();

        clients = new Dictionary<Connection, EndPoint>();
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
        onClientConnected += connectedDelegate;
       
    }

    public void OnAppQuit()
    {
        throw new NotImplementedException();
    }

    public void OnStart()
    {
        listenerThread = new Thread(() => ListeningThread());
        listenerThread.Start();
    }

    public void RemoveHandler(ushort type)
    {
        throw new NotImplementedException();
    }

    public void SendEndToAll()
    {
        throw new NotImplementedException();
    }

    public void SendTo(ushort type, int ID, byte[] message, bool reliable = true)
    {
        throw new NotImplementedException();
    }

    public void SendTo(ushort type, byte[] message, Connection conn, bool reliable = true)
    {
        List<byte> listBytes = new List<byte>();
        listBytes.AddRange(BitConverter.GetBytes(type));
        listBytes.AddRange(message);
        if (clients.ContainsKey(conn))
        {
            serverSocket.SendTo(listBytes.ToArray(), clients[conn]);
        }
    }

    public void SendToAll(ushort type, byte[] message, bool reliable = true)
    {
        List<byte> listBytes = new List<byte>();
        listBytes.AddRange(BitConverter.GetBytes(type));
        listBytes.AddRange(message);
        foreach( EndPoint endpoint in clients.Values)
        {
            serverSocket.SendTo(listBytes.ToArray(), endpoint);
        }
   
    }
    private void OnNewConnection(EndPoint endpoint)
    {
        if (clients.Values.Count < maxConnections)
        {
            IPEndPoint ipEndPoint = endpoint as IPEndPoint;
            clients.Add(new Connection(endpoint,false),endpoint);
            onClientConnected?.Invoke(new ConnectionInfo(ipEndPoint.Address.ToString(), ipEndPoint.Port, ipEndPoint.Port, clients.Count));
            ushort type = 0;
            serverSocket.SendTo(BitConverter.GetBytes(type), endpoint);
        }
    }

    public void ListeningThread()
    {
        byte[] buffer = new byte[2000];
        EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);

        while (connected)
        {
            int size = serverSocket.ReceiveFrom(buffer, ref endPoint);
            if (clients.ContainsKey(new Connection(endPoint,false)))
            {
                ushort type = BitConverter.ToUInt16(buffer, 0);
                if (handlerDictionary.TryGetValue(type, out Action<byte[]> value))
                {
                    value?.Invoke(buffer);
                }
            }
            else
            {
                if(size == 2)
                {
                    ushort type = BitConverter.ToUInt16(buffer, 0);
                    if(type == 0)
                    {
                        OnNewConnection(endPoint);
                    }
                }
            }
        }
    }
}

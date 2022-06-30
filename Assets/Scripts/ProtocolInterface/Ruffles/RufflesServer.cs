using Muse_RP.Hosts;
using Muse_RP.Message;
using Ruffles.Channeling;
using Ruffles.Configuration;
using Ruffles.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using UnityEngine;

public class RufflesServer : IServerProtocol
{
    internal static readonly SocketConfig ServerConfig = new SocketConfig()
    {
        ChannelTypes = new ChannelType[]
           {
               ChannelType.Reliable,
               ChannelType.ReliableSequenced,
               ChannelType.Unreliable,
               ChannelType.UnreliableOrdered,
               ChannelType.ReliableSequencedFragmented
           },
        UseSimulator = false,
        EnableTimeouts = true,
        ConnectionTimeout = 30000,
        EnablePacketMerging = false,
     
        

    };
    private RuffleSocket serverSocket;
    private Dictionary<ushort, Action<byte[]>> handlerDictionary;
    private OnClientConnectedDelegate onClientConnected;
    private OnClientDisconnected onDisconnectedDelegate;
    private Thread listeningThread;
    private Dictionary<Connection, Ruffles.Connections.Connection> clients;
    private long messageCounter;
    public RufflesServer(int port)
    {
        ServerConfig.DualListenPort = port;
        serverSocket = new RuffleSocket(ServerConfig);
        handlerDictionary = new Dictionary<ushort, Action<byte[]>>();
        clients = new Dictionary<Connection, Ruffles.Connections.Connection>();
       

    }
    public void AddHandler(ushort type, MessageDelegate handler)
    {
        handlerDictionary.Add(type, (b) =>
        {
            byte[] data = new byte[b.Length - 2];
            Array.Copy(b, 2, data, 0, b.Length - 2);
            ushort type = BitConverter.ToUInt16(b, 0);
            Debug.Log("Recibido type " + type);
            if (data.Length < MessageObject.maxBytesData)
            {
                handler?.Invoke(new MessageObject(type, 0, 0, false, false, false, false, data), null);
            }
            else
            {
                List<byte> aux = new List<byte>();
                aux.AddRange(Array.FindAll(data, (a) => a != 0));
                handler?.Invoke(new MessageObject(type, 0, 0, false, false, false, false, aux.ToArray()), null);
            }

        });
    }

    public void AddHandler(ushort type, Action<byte[]> handler)
    {
        handlerDictionary.Add(type, handler);
    }

    public void InitServer(OnClientConnectedDelegate connectedDelegate, OnClientDisconnected disconnectedDelegate)
    {
        onClientConnected += connectedDelegate;
        onDisconnectedDelegate += disconnectedDelegate;
    }

    public void OnAppQuit()
    {
        serverSocket.Stop();
    }

    public void OnStart()
    {
        serverSocket.Start();
        listeningThread = new Thread(() => ListeningThread());
        listeningThread.Start();
    }

    public void RemoveHandler(ushort type)
    {
        handlerDictionary.Remove(type);
    }

    public void SendEndToAll()
    {

    }

    public void SendTo(ushort type, int ID, byte[] message, bool reliable = true)
    {
        //Not Implemented
    }

    public void SendTo(ushort type, byte[] message, Connection conn, bool reliable = true)
    {

        List<byte> data = new List<byte>();
        data.AddRange(BitConverter.GetBytes(type));
        if (message != null)
        {
            data.AddRange(message);
        }
    
        byte channel = reliable ? (byte)ChannelType.ReliableSequenced : (byte)ChannelType.UnreliableOrdered;
        if (clients.TryGetValue(conn, out Ruffles.Connections.Connection value))
        {
            value.Send(new ArraySegment<byte>(data.ToArray()), channel, true, (ulong)messageCounter);
            Debug.Log("Mandado mensaje " + type + " " + data.Count);
        }
        Interlocked.Increment(ref messageCounter);
    }

    public void SendToAll(ushort type, byte[] message, bool reliable = true)
    {
        List<byte> data = new List<byte>();
        data.AddRange(BitConverter.GetBytes(type));
        if (message != null)
        {
            data.AddRange(message);
        }
  
        byte channel = reliable ? (byte)ChannelType.ReliableSequenced : (byte)ChannelType.UnreliableOrdered;

        foreach (Ruffles.Connections.Connection conn in clients.Values)
        {
            conn.Send(new ArraySegment<byte>(data.ToArray()), channel, true, (ulong)messageCounter);
            Interlocked.Increment(ref messageCounter);
        }
    }

    private void ListeningThread()
    {
        while (serverSocket.IsRunning)
        {
            NetworkEvent serverEvent = serverSocket.Poll();
            switch (serverEvent.Type)
            {
                case NetworkEventType.Nothing:
                    break;
                case NetworkEventType.Connect:
                    clients.Add(new Connection(serverEvent.EndPoint, true), serverEvent.Connection);
                    onClientConnected?.Invoke(new ConnectionInfo(serverEvent.EndPoint.Address.ToString(), serverEvent.EndPoint.Port, serverEvent.EndPoint.Port));
                    break;
                case NetworkEventType.Disconnect:
                    clients.Remove(new Connection(serverEvent.EndPoint, true));
                    break;
                case NetworkEventType.Timeout:
                    onDisconnectedDelegate?.Invoke(new ConnectionInfo(serverEvent.EndPoint.Address.ToString(), serverEvent.EndPoint.Port, serverEvent.EndPoint.Port));
                    clients.Remove(new Connection(serverEvent.EndPoint, true));
                    break;
                case NetworkEventType.Data:
                    ushort type = BitConverter.ToUInt16(serverEvent.Data.Array, 0);
                    if (handlerDictionary.TryGetValue(type, out Action<byte[]> value))
                    {
                        value?.Invoke(serverEvent.Data.Array);
                    }
                    break;
                case NetworkEventType.UnconnectedData:
                    break;
                case NetworkEventType.BroadcastData:
                    break;
                case NetworkEventType.AckNotification:
                    break;
            }
            serverEvent.Recycle();
        }
    }
}

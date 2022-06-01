using Muse_RP.Hosts;
using Muse_RP.Message;
using Ruffles.Channeling;
using Ruffles.Configuration;
using Ruffles.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using UnityEngine;


public class RufflesClient : IClientProtocol
{

    internal static readonly SocketConfig ClientConfig = new SocketConfig()
    {
        DualListenPort = 0, // Port 0 means we get a port by the operating system
        UseSimulator = false,
        ChannelTypes = new ChannelType[]
           {
                ChannelType.Reliable,
                ChannelType.ReliableSequenced,
                ChannelType.Unreliable,
                ChannelType.UnreliableOrdered,
                ChannelType.ReliableSequencedFragmented
           },
        EnableTimeouts = true,
        ConnectionTimeout = 3000
    };

    private RuffleSocket clientSocket;
    private Dictionary<ushort, Action<byte[]>> handlerDictionary;
    private Thread listeningThread;
    private Ruffles.Connections.Connection serverConnection;
    private long messageCounter;
    private OnConnectionFailure onConnectionFailure;
    private OnServerDisconneced onDisconnected;
    private IPEndPoint serverEndpoint;
    private Thread tryConnect;
    private OnConnectedDelegate onConnected;
    
    public RufflesClient(IPEndPoint serverEndpoint)
    {
        clientSocket = new RuffleSocket(ClientConfig);
        handlerDictionary = new Dictionary<ushort, Action<byte[]>>();
        this.serverEndpoint = serverEndpoint;

    }
    public void AddHandler(ushort type, MessageDelegate handler)
    {
        handlerDictionary.Add(type, (b) =>
        {
            byte[] data = new byte[b.Length - 2];
            Array.Copy(b, 2, data, 0, b.Length - 2);
            ushort type = BitConverter.ToUInt16(b, 0);
            if (data.Length < MessageObject.maxBytesData)
            {
                handler?.Invoke(new MessageObject(type, 0, 0, false, false, false, false, data), null);
            }
        });
    }

    public void AddHandler(ushort type, Action<byte[]> handler)
    {
        handlerDictionary.Add(type, handler);
    }

    public void AddOnConnectionFailedHandler(OnConnectionFailure onConnectionFailure)
    {
        this.onConnectionFailure += onConnectionFailure;
    }

    public void AddOnDisconnectedHandler(OnServerDisconneced onDisconnected)
    {
        this.onDisconnected += onDisconnected;
    }

    public void OnAppQuit()
    {
        clientSocket.Stop();
    }

    public void OnStart(OnConnectedDelegate onConnected)
    {
        clientSocket.Start();
        TryConnect();
    }

    public void RemoveHandler(ushort type)
    {
        handlerDictionary.Remove(type);
    }

    public void RemoveOnConnectionFailedHandler(OnConnectionFailure onConnectionFailure)
    {
        this.onConnectionFailure -= onConnectionFailure;
    }

    public void RemoveOnDisconnectedHandler(OnServerDisconneced onDisconnected)
    {
        this.onDisconnected -= onDisconnected;
    }

    public void SendToServer(ushort type, byte[] message, bool reliable = true)
    {
        List<byte> data = new List<byte>();
        data.AddRange(BitConverter.GetBytes(type));
        data.AddRange(message);
        byte channel = reliable ? (byte)ChannelType.ReliableSequenced : (byte)ChannelType.UnreliableOrdered;
        serverConnection.Send(new ArraySegment<byte>(data.ToArray()), channel, true, (ulong)messageCounter);
        Interlocked.Increment(ref messageCounter);
    }

    public void TryConnect()
    {
        if (tryConnect != null && tryConnect.IsAlive)
        {
            tryConnect.Abort();
        }
        tryConnect = new Thread(() => ConnectThread());
        tryConnect.Start();
        
    }
    private void ConnectThread()
    {
        try
        {
            clientSocket.Connect(serverEndpoint);
            listeningThread = new Thread(() => ListeningThread());
            listeningThread.Start();
        }catch(Exception e)
        {
            onConnectionFailure?.Invoke();
        }


    }
    private void ListeningThread()
    {
        while (clientSocket.IsRunning)
        {
            NetworkEvent clientEvent = clientSocket.Poll();
            switch (clientEvent.Type)
            {
                case NetworkEventType.Nothing:
                    break;
                case NetworkEventType.Connect:
                    serverConnection = clientEvent.Connection;
                    onConnected?.Invoke();
                    break;
                case NetworkEventType.Disconnect:
                    onDisconnected?.Invoke();
                    break;
                case NetworkEventType.Timeout:
                    onDisconnected?.Invoke();
                    break;
                case NetworkEventType.Data:
                    ushort type = BitConverter.ToUInt16(clientEvent.Data.Array, 0);
                    if (handlerDictionary.TryGetValue(type, out Action<byte[]> value))
                    {
                        value?.Invoke(clientEvent.Data.Array);
                    }
                    break;
                case NetworkEventType.UnconnectedData:
                    break;
                case NetworkEventType.BroadcastData:
                    break;
                case NetworkEventType.AckNotification:
                    break;
            }
            clientEvent.Recycle();
        }
    }
}

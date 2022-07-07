using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
//Implementation of the raw UDP protocol client
public class UDPClient : IClientProtocol
{
    private Socket clientSocket;
    private Dictionary<ushort, Action<byte[]>> handlerDictionary;
    private bool connected;
    private OnConnectionFailure onConnectionFailure;
    private OnServerDisconneced onDisconnected;
    private OnConnectedDelegate onConnected;
    private EndPoint serverEndPoint;
    private int port;
    private Thread listenerThread;
    //We will use 0 type messages to connection purposes
    public UDPClient(EndPoint serverEndPoint, int port)
    {
        this.port = port;
        this.serverEndPoint = serverEndPoint;
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        clientSocket.Bind(new IPEndPoint(IPAddress.Any, port));
        handlerDictionary = new Dictionary<ushort, Action<byte[]>>();

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
        clientSocket.Close();
    }

    public void OnStart(OnConnectedDelegate onConnected)
    {
        this.onConnected += onConnected;
        connected = true;
        handlerDictionary.Add(0, (b) => this.onConnected?.Invoke());
        listenerThread = new Thread(() => ListenerThread());
        listenerThread.Start();
        List<byte> message = new List<byte>();
        TryConnect();
    }

    public void RemoveHandler(ushort type)
    {
        handlerDictionary.Remove(type);
    }

    public void RemoveOnDisconnectedHandler(OnServerDisconneced onDisconnected)
    {
        throw new NotImplementedException();
    }

    public void SendToServer(ushort type, byte[] message, bool reliable = true)
    {
        List<byte> listBytes = new List<byte>();
        listBytes.AddRange(BitConverter.GetBytes(type));
        if (message != null)
        {
            listBytes.AddRange(message);
        }
        clientSocket.SendTo(listBytes.ToArray(), serverEndPoint);
    }

    public void TryConnect()
    {
        ushort type = 0;
        clientSocket.SendTo(BitConverter.GetBytes(type), serverEndPoint);

    }

    public void ListenerThread()
    {
        byte[] buffer = new byte[2000];
        EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
        while (connected)
        {
            int size = clientSocket.ReceiveFrom(buffer, ref endPoint);
            if (endPoint.Equals(serverEndPoint))
            {
                ushort type = BitConverter.ToUInt16(buffer, 0);
                if (handlerDictionary.TryGetValue(type, out Action<byte[]> value))
                {
                    value?.Invoke(buffer.Take(size).ToArray());
                }
            }

        }
        Debug.Log("Disconnected");
    }
}

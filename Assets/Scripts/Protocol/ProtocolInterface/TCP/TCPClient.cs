using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
//Implementation of the Telepathy protocol client
public class TCPClient : IClientProtocol
{
    private Telepathy.Client tcpClient;
    private Dictionary<ushort, Action<byte[]>> handlerDictionary;
    private bool connected;
    private OnConnectionFailure onConnectionFailure;
    private OnServerDisconneced onDisconnected;
    private OnConnectedDelegate onConnected;
    private EndPoint serverEndPoint;
    public Action onUpdate;

    public TCPClient(int port, EndPoint serverEndPoint)
    {
        tcpClient = new Telepathy.Client(2000);
        this.serverEndPoint = serverEndPoint;

        tcpClient.OnData += OnData;
        tcpClient.NoDelay = true;
        onUpdate += OnUpdate;

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
        tcpClient.Disconnect();
    }

    public void OnStart(OnConnectedDelegate onConnected)
    {
        this.onConnected += onConnected;
        connected = true;

        TryConnect();
        tcpClient.OnConnected += () => this.onConnected?.Invoke();
        tcpClient.OnDisconnected += () => onDisconnected?.Invoke();
    }

    public void RemoveHandler(ushort type)
    {
        handlerDictionary.Remove(type);
    }

    public void RemoveOnDisconnectedHandler(OnServerDisconneced onDisconnected)
    {
        this.onDisconnected -= onDisconnected;
    }

    public void SendToServer(ushort type, byte[] message, bool reliable = true)
    {
        List<byte> bytesToSend = new List<byte>();

        bytesToSend.AddRange(BitConverter.GetBytes(type));
        if (message != null)
        {
            bytesToSend.AddRange(message);
        }
        ArraySegment<byte> data = new ArraySegment<byte>(bytesToSend.ToArray());
        tcpClient.Send(data);

    }

    public void TryConnect()
    {
        IPEndPoint endpoint = serverEndPoint as IPEndPoint;
        tcpClient.Connect(endpoint.Address.ToString(), endpoint.Port);
        Debug.Log("Connected?" + tcpClient.Connected);

    }

    private void OnData(ArraySegment<byte> data)
    {

        ushort type = BitConverter.ToUInt16(data.Take(2).ToArray(), 0);

        if (handlerDictionary.TryGetValue(type, out Action<byte[]> value))
        {
            value?.Invoke(data.ToArray());
        }
    }
    private void OnUpdate()
    {
        tcpClient.Tick(100);
    }
}

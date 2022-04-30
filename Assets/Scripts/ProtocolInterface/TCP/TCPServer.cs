using Muse_RP.Hosts;
using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using UnityEngine;

public class TCPServer : IServerProtocol
{
    private Dictionary<string, EndPoint> clients;
    private Thread listenerThread;
    private Dictionary<ushort, Action<byte[]>> handlerDictionary;
    private bool connected;
    private OnConnectionFailure onConnectionFailure;
    private OnServerDisconneced onDisconnected;
    private OnConnectedDelegate onConnected;
    private EndPoint serverEndPoint;
    public void AddHandler(ushort type, MessageDelegate handler)
    {
        throw new NotImplementedException();
    }

    public void AddHandler(ushort type, Action<byte[]> handler)
    {
        throw new NotImplementedException();
    }

    public void InitServer(OnClientConnectedDelegate connectedDelegate, OnClientDisconnected disconnectedDelegate)
    {
        throw new NotImplementedException();
    }

    public void OnAppQuit()
    {
        throw new NotImplementedException();
    }

    public void OnStart()
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }

    public void SendToAll(ushort type, byte[] message, bool reliable = true)
    {
        throw new NotImplementedException();
    }
}

using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UDPClient : IClientProtocol
{
    public void AddHandler(ushort type, MessageDelegate handler)
    {
        throw new NotImplementedException();
    }

    public void AddHandler(ushort type, Action<byte[]> handler)
    {
        throw new NotImplementedException();
    }

    public void AddOnConnectionFailedHandler(OnConnectionFailure onConnectionFailure)
    {
        throw new NotImplementedException();
    }

    public void AddOnDisconnectedHandler(OnServerDisconneced onDisconnected)
    {
        throw new NotImplementedException();
    }

    public void OnAppQuit()
    {
        throw new NotImplementedException();
    }

    public void OnStart(OnConnectedDelegate onConnected)
    {
        throw new NotImplementedException();
    }

    public void RemoveHandler(ushort type)
    {
        throw new NotImplementedException();
    }

    public void RemoveOnConnectionFailedHandler(OnConnectionFailure onConnectionFailure)
    {
        throw new NotImplementedException();
    }

    public void RemoveOnDisconnectedHandler(OnServerDisconneced onDisconnected)
    {
        throw new NotImplementedException();
    }

    public void SendToServer(ushort type, byte[] message, bool reliable = true)
    {
        throw new NotImplementedException();
    }

    public void TryConnect()
    {
        throw new NotImplementedException();
    }
}

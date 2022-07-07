using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Interface for protocol's client
public interface IClientProtocol
{
    public void OnStart(OnConnectedDelegate onConnected);
    public void TryConnect();
    public void OnAppQuit();
    public void SendToServer(ushort type, byte[] message, bool reliable = true);
    public void AddHandler(ushort type, MessageDelegate handler);
    public void RemoveHandler(ushort type);
    public void AddOnDisconnectedHandler(OnServerDisconneced onDisconnected);
    public void RemoveOnDisconnectedHandler(OnServerDisconneced onDisconnected);
    public void AddOnConnectionFailedHandler(OnConnectionFailure onConnectionFailure);

}

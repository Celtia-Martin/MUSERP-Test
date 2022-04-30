using Muse_RP.Hosts;
using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IServerProtocol
{
    public void OnStart();
    public void OnAppQuit();
    public void InitServer(OnClientConnectedDelegate connectedDelegate,OnClientDisconnected disconnectedDelegate);
 

    public void SendTo(ushort type,int ID, byte[] message, bool reliable = true);
    public void SendTo(ushort type,byte[] message,Connection conn , bool reliable = true);
    public void SendToAll(ushort type,byte[] message, bool reliable = true);
    public void SendEndToAll();

    public void AddHandler(ushort type, MessageDelegate handler);
    public void AddHandler(ushort type, Action handler);
    public void RemoveHandler(ushort type);

    
}

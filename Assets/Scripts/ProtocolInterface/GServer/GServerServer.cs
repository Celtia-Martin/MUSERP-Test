
using GServer.Messages;
using Muse_RP.Hosts;
using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using System.Text;
using System.Threading;
using GServer;
using System.Linq;

public class GServerServer : IServerProtocol
{
    private GServer.Host serverHost;
    private GameServer gameServer;

    public GServerServer(int port, GameServer gameServer)
    {
        serverHost = new GServer.Host(port);
        this.gameServer = gameServer;
    }
    public void OnStart()
    {
        //serverHost.StartListen();
    }

    public void OnAppQuit()
    {
       
        serverHost.Dispose();
    }

    public void InitServer(OnClientConnectedDelegate connectedDelegate, OnClientDisconnected disconnectedDelegate)
    {
        serverHost.StartListen();
        Timer timer = new Timer(o => serverHost.Tick());
        timer.Change(10, 10);
        serverHost.ConnectionCreated += (c) =>connectedDelegate(new ConnectionInfo(c.EndPoint.Address.ToString(), c.EndPoint.Port, c.EndPoint.Port));
    }

    public void SendTo(ushort type, int ID, byte[] message, bool reliable = true)
    {
        //noot implemented
    }

    public void SendTo(ushort type, byte[] data, Connection conn, bool reliable = true)
    {
        GServer.Connection.Connection gServerConnection = serverHost.GetConnections().Where((c) => c.EndPoint.Port.Equals(conn.port) && c.EndPoint.Address.ToString().Equals(conn.IP)).First();
        Message message = new Message((short)type, Mode.Reliable, data);
        serverHost.Send(message, gServerConnection);
        
    }

    public void SendToAll(ushort type, byte[] data, bool reliable = true)
    {

        Message message  = new Message((short)type, Mode.Reliable, data);
        foreach (GServer.Connection.Connection conn in serverHost.GetConnections())
        {
            serverHost.Send(message, conn);
        }
    }

    public void SendEndToAll()
    {
        foreach(GServer.Connection.Connection conn in serverHost.GetConnections())
        {
            serverHost.ForceDisconnect(conn);
        }
        
    }

    public void AddHandler(ushort type, MessageDelegate handler)
    {

        serverHost.AddHandler((short)type, (m, s) =>
        {
            Debug.Log("Mensaje nuevo: " + type + "Con, además: " + m.Body.Length + " de tamaño");
            MessageObject messageObject = new MessageObject(type, 0, 0, false, false, false, false, m.Body);
            Muse_RP.Hosts.Connection conn = new Muse_RP.Hosts.Connection(s.EndPoint, true);
            handler?.Invoke(messageObject, conn);

        });
    }

    public void AddHandler(ushort type, Action<byte[]> handler)
    {

        serverHost.AddHandler((short)type, (m, s) =>
        {
            handler?.Invoke(m.Body);

        });
    }

    public void RemoveHandler(ushort type)
    {
        //not implemented
    }


}

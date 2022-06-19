
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

public class GServerServer : IServerProtocol
{
    private GServer.Host serverHost;

    public GServerServer(int port)
    {
        serverHost = new GServer.Host(port);
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
        
        serverHost.ConnectionCreated += (c) => connectedDelegate(new ConnectionInfo(c.EndPoint.Address.ToString(), c.EndPoint.Port, c.EndPoint.Port));
        serverHost.StartListen();
        Timer timer = new Timer(o => serverHost.Tick());
        timer.Change(10, 10);
    }

    public void SendTo(ushort type, int ID, byte[] message, bool reliable = true)
    {
        //noot implemented
    }

    public void SendTo(ushort type, byte[] data, Connection conn, bool reliable = true)
    {
        GServer.Connection.Connection gServerConnection = new GServer.Connection.Connection(new System.Net.IPEndPoint(IPAddress.Parse(conn.IP), conn.port));
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

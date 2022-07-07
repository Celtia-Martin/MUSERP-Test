using Muse_RP.Hosts;
using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Implementation of the MUSE-RP protocol server
public class MuseRPServer : IServerProtocol
{
    //Properties
    private int reliablePort;

    private int noReliablePort;

    private int maxConnections;

    private int timeOut;

    private int timePing;

    private uint reliablePercentage;

    //References
    protected MessageHandler customHandler = new MessageHandler();

    protected Server server;

    private HostOptions options;
    #region Constructor
    public MuseRPServer(int reliablePort, int noReliablePort, int maxConnections, int timeOut, int timePing, uint reliablePercentage)
    {
        this.reliablePort = reliablePort;
        this.noReliablePort = noReliablePort;
        this.maxConnections = maxConnections;
        this.timeOut = timeOut;
        this.timePing = timePing;
        this.reliablePercentage = reliablePercentage;
    }
    #endregion
    #region Senders
    public void SendTo(ushort type, byte[] message, Connection conn, bool reliable = true)
    {
        server.SendTo(type, true, conn, message);
    }
    public void SendToAll(ushort type, byte[] message, bool reliable = true)
    {
        server.SendToAll(type, reliable, message);
    }

    #endregion
    #region Events
    public void OnAppQuit()
    {
        server.SendEndToAll();
        server.Stop();
    }
    public void OnStart()
    {
        Console.instance.WriteLine("Starting server...");
        options = new HostOptions()
        {
            maxConnections = maxConnections,
            timeOut = timeOut,
            timePing = timePing,
            reliablePort = reliablePort,
            noReliablePort = noReliablePort,
            windowSize = 1000,
            timerTime = 200,
            reliablePercentage = reliablePercentage,
            messageHandler = customHandler,
            waitTime = 1
        };
        server = new Server(options, true);
        server.Start();
        Console.instance.WriteLine("Server started");
        server.AddPingHandler((m, s) => Debug.Log("Ping!"));
        server.onEndReceived += () => Debug.Log("End recibido");
        server.onSendEnd += () => Debug.Log("He enviado un end");

    }
    public void InitServer(OnClientConnectedDelegate connectedDelegate, OnClientDisconnected disconnectedDelegate)
    {
        server.AddOnClientDisconnectedDelegate(disconnectedDelegate);
        server.AddOnClientConnectedDelegate(connectedDelegate);
    }

    #endregion
    #region Handlers
    public void RemoveHandler(ushort type)
    {
        customHandler.RemoveHandler(type);
    }

    public void AddHandler(ushort type, MessageDelegate handler)
    {
        customHandler.AddHandler(type, handler);
    }

    #endregion 
}

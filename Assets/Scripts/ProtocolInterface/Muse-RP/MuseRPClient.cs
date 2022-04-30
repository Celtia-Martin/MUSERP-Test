using Muse_RP.Hosts;
using Muse_RP.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuseRPClient : IClientProtocol
{
    //references

    protected Client client;
    private MessageHandler customHandler = new MessageHandler();

    #region Constructor
    public MuseRPClient(ConnectionInfo serverInfo, HostOptions options, int timeOutConnection, int connectionTries)
    {

        options.messageHandler = customHandler;
        client = new Client(options, serverInfo, timeOutConnection, connectionTries, true);
    }
    #endregion
    #region Handlers
    public void AddHandler(ushort type, MessageDelegate handler)
    {
        customHandler.AddHandler(type, handler);
    }

    public void AddHandler(ushort type, Action handler)
    {
        customHandler.AddHandler(type, (s, m) => handler.Invoke());
    }

    public void AddOnConnectionFailedHandler(OnConnectionFailure onConnectionFailure)
    {
        client.AddOnConnectionFailureHandler(onConnectionFailure);
    }

    public void AddOnDisconnectedHandler(OnServerDisconneced onDisconnected)
    {
        client.AddOnDisconnectedHandler(onDisconnected);
    }
    public void RemoveHandler(ushort type)
    {
        customHandler.RemoveHandler(type);
    }
    public void RemoveOnConnectionFailedHandler(OnConnectionFailure onConnectionFailure)
    {
        client.RemoveOnConnectionFailureHandler(onConnectionFailure);
    }

    public void RemoveOnDisconnectedHandler(OnServerDisconneced onDisconnected)
    {
        client.RemoveOnDisconnectedHandler(onDisconnected);
    }

    #endregion
    #region Senders
    public void SendToServer(ushort type, byte[] message, bool reliable = true)
    {
        client.SendToServer(type, reliable, message);
    }
    #endregion
    #region Events

    public void OnAppQuit()
    {
        if (client.isConnected())
        {
            client.SendEnd();
            client.ReceiveEnd(null, null);

        }

    }
    public void OnStart(OnConnectedDelegate onConnected)
    {
        client.AddOnConnectedHandler(onConnected);
        client.Start();
        client.TryConnect();
        Console.instance.WriteLine("Intentando conectar...");
    }

    public void TryConnect()
    {
        client.TryConnect();
    }

    #endregion

}
